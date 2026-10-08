using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Tycoon
{
    // Chơi bằng gamepad ảo và UI thật; không tạo hàng, không sửa save hoặc dùng fixture.
    public sealed partial class CityPlaySession : MonoBehaviour
    {
        GameSession game;
        Gamepad pad;
        readonly List<InputDevice> disabledDevices = new();
        readonly List<string> failures = new();
        readonly List<string> completed = new();
        readonly List<string> captures = new();
        double started;
        bool recording;

        IEnumerator Start()
        {
            game = GameSession.Instance;
            foreach (var device in InputSystem.devices.ToArray())
            {
                if (!device.enabled || device is not (Keyboard or Mouse or Gamepad)) continue;
                disabledDevices.Add(device);
                InputSystem.DisableDevice(device);
            }
            pad = InputSystem.AddDevice<Gamepad>("CityPlay_Gamepad");
            while (!game.NavigationReady || !game.CanSimulate || !game.Player.CanControl) yield return null;
            started = Time.realtimeSinceStartupAsDouble;
            recording = true;
            var routines = new Stack<IEnumerator>();
            routines.Push(PlayAndCapture());
            while (routines.Count > 0)
            {
                object current = null;
                bool moved = false;
                try
                {
                    moved = routines.Peek().MoveNext();
                    if (moved) current = routines.Peek().Current;
                }
                catch (Exception error)
                {
                    failures.Add(error.Message);
                    Debug.LogException(error);
                    routines.Clear();
                }
                if (routines.Count == 0) break;
                if (!moved) routines.Pop();
                else if (current is IEnumerator nested) routines.Push(nested);
                else yield return current;
            }
            StopInput();
            recording = false;
            game.SaveGame();
            FinishReport();
            Application.Quit(failures.Count == 0 && game.RuntimeErrors.Count == 0 ? 0 : 2);
        }

        IEnumerator PlayAndCapture()
        {
            yield return Play();
            recording = false;
            SaveCityProfile();
            yield return CaptureCityPresentation();
            if (GameSession.Argument("--qa-city-motion", "") == "true")
                yield return CaptureCarryMotion();
        }

        IEnumerator Play()
        {
            Check(Time.timeScale == 1, "Tốc độ chơi bình thường 1x");
            string sourcePath = GameSession.Argument("--qa-city-from", "");
            if (!string.IsNullOrEmpty(sourcePath))
            {
                yield return ResumeCity(sourcePath);
                CheckCityNavigation();
                if (game.Economy.CashInHand > 0)
                {
                    var restoredSafe = game.Stations.OfType<SafeStation>().Single();
                    yield return Walk(restoredSafe.Deposit.Center);
                    yield return Wait(() => game.Economy.CashInHand == 0, 5,
                        "Gửi tiền đang cầm trước khi chơi hàng sau phục hồi");
                }
                BeginCityProfile();
                yield return Tour();
                Check(game.ActiveWorkerCount == WorkforceRules.MaximumActive,
                    "Giữ đủ đội ngũ khi tiếp tục chơi");
                Check(game.Player.SuccessfulInteractions >= 10, "Nhặt và cất hàng thật sau mở lại");
                yield break;
            }
            CheckCityNavigation();
            var plot = game.Producers.First(x => x.Id == "field_carrot");
            var counter = game.Checkouts.First(x => x.ShopId == "farm");
            var safe = game.Stations.OfType<SafeStation>().Single();
            yield return Walk(plot.InteractionPoint);
            yield return Wait(() => game.Player.Carry.Count("carrot") >= 6, 40, "Thu hoạch cà rốt thật");
            yield return Walk(plot.InteractionPoint + Vector3.back * 2);
            yield return Capture("01-carried-produce");
            yield return Walk(counter.transform.position + new Vector3(-1.7f, 0, -.3f));
            yield return Wait(() => counter.Cash > 0, 75, "Bán hàng cho khách từ cổng thành phố");
            if (game.Player.Carry.Total > 0) yield return StoreGoods(game.Storage);
            yield return Walk(counter.CollectionPoint);
            yield return Wait(() => game.Economy.CashInHand > 0, 8, "Thu tiền lên tay");
            long earned = game.Economy.CashInHand;
            yield return Capture("02-sale-cash");
            yield return Walk(safe.Deposit.Center);
            yield return Wait(() => game.Economy.CashInHand == 0, 5, "Gửi tiền vào két");
            Check(game.Economy.CashInSafe == earned, "Két giữ đúng tiền vừa bán");
            long beforeMod = game.Economy.CashInSafe;
            yield return Click("Mod game");
            yield return Click("Mod tiền");
            yield return Wait(() => game.Economy.CashInSafe == beforeMod + 999999, 2,
                "Nút Mod tiền cộng đúng số tiền vào két");
            yield return Walk(safe.Withdraw.Center);
            yield return Wait(() => game.Economy.CashInHand >= 999999, 5, "Rút tiền mod bằng vùng két");
            var upgrade = game.Stations.OfType<PurchasePad>()
                .Where(x => x.Available && x.Upgrade.family == "player").FirstOrDefault();
            if (upgrade)
            {
                yield return Walk(upgrade.InteractionPoint);
                yield return Hold(3);
                Check(game.Economy.CashInHand < earned + 999999, "Góp tiền thật trên ô nâng cấp");
                yield return Capture("03-upgrade-feedback");
            }
            yield return Walk(safe.Deposit.Center);
            yield return Wait(() => game.Economy.CashInHand == 0, 5, "Cất tiền trước khi lấy hàng");
            yield return Click("Mod game");
            yield return Click("Mod xây dựng tối đa");
            yield return Hold(2);
            Check(game.Workers.Count == game.Transactions.View.crews.Sum(x => x.count), "Đội ngũ đúng tiến trình");
            BeginCityProfile();
            yield return Tour();
            Check(Time.realtimeSinceStartupAsDouble - started >= 240, "Chơi liên tục ít nhất bốn phút");
            Check(game.Player.DistanceWalked >= 300, "Di chuyển thật qua các khu");
            Check(game.Player.SuccessfulInteractions >= 10, "Thao tác qua hệ thống player");
            Check(game.Economy.Revenue > 0, "Có doanh thu từ bán hàng thật");
            Check(game.Transactions.View.layoutRevision == CityDistricts.Revision, "Save dùng layout mới");
        }

        void Check(bool condition, string label)
        {
            (condition ? completed : failures).Add(label);
            Debug.Log("CITY_PLAY " + (condition ? "PASS " : "FAIL ") + label);
        }

        void OnDestroy()
        {
            if (pad != null && pad.added) InputSystem.RemoveDevice(pad);
            foreach (var device in disabledDevices)
                if (device.added) InputSystem.EnableDevice(device);
        }
    }
}
