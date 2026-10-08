using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class CityPlaySession
    {
        [Serializable]
        sealed class CityCpuReport
        {
            public string mode = "CPU timings during ordinary gameplay, no fixture";
            public CpuWorkSample[] frameWork;
            public CpuWorkSample[] transactionWork;
        }

        [Serializable]
        sealed class CityRestoreReport
        {
            public int sourceLayout, restoredLayout, sourceItems, restoredItems, preservedReceipts;
            public long sourceCashInHand, restoredCashInHand, sourceCashInSafe, restoredCashInSafe;
        }

        CityNativeCpuProbe nativeProbe;

        IEnumerator ResumeCity(string path)
        {
            var source = SaveStore.Read(path);
            if (source?.transactionState == null) throw new InvalidDataException("Checkpoint không hợp lệ");
            Time.timeScale = 0;
            game.Player.CanControl = false;
            game.Transactions.Detach();
            SaveStore.Write(game.SavePath, source);
            game.LoadGame();
            float end = Time.realtimeSinceStartup + 12;
            while (!game.CanSimulate && Time.realtimeSinceStartup < end) yield return null;
            if (!game.CanSimulate) throw new InvalidOperationException("Không phục hồi được thành phố");
            var state = game.Transactions.Snapshot();
            var before = source.transactionState;
            Check(state.cashInHand == source.cashInHand && state.cashInSafe == source.cashInSafe,
                "Giữ nguyên tiền tay và két khi phục hồi");
            string Goods(TransactionState value) => string.Join(";", value.stacks.GroupBy(x => x.item)
                .OrderBy(x => x.Key).Select(x => x.Key + ":" + x.Sum(y => y.quantity)));
            Check(Goods(state) == Goods(before), "Giữ nguyên tổng từng loại hàng khi phục hồi");
            Check(before.receipts.All(x => state.receipts.Any(y => y.id == x.id)),
                "Giữ mã giao dịch để chống lặp sau phục hồi");
            File.WriteAllText(Path.Combine(game.QaDirectory, "city-restore.json"),
                JsonUtility.ToJson(new CityRestoreReport
                {
                    sourceLayout = before.layoutRevision,
                    restoredLayout = state.layoutRevision,
                    sourceItems = before.stacks.Sum(x => x.quantity),
                    restoredItems = state.stacks.Sum(x => x.quantity),
                    preservedReceipts = before.receipts.Count,
                    sourceCashInHand = source.cashInHand,
                    restoredCashInHand = state.cashInHand,
                    sourceCashInSafe = source.cashInSafe,
                    restoredCashInSafe = state.cashInSafe
                }, true));
            Time.timeScale = 1;
            game.Player.CanControl = true;
            Check(game.Transactions.View.layoutRevision == CityDistricts.Revision, "Mở lại bản đồ đã lưu");
        }

        void BeginCityProfile()
        {
            if (GameSession.Argument("--qa-city-profile", "") != "true") return;
            QaFrameProbe.Reset();
            QaCpuProbe.Reset();
            QaFrameProbe.Start();
            QaCpuProbe.Start();
            nativeProbe = game.gameObject.AddComponent<CityNativeCpuProbe>();
            nativeProbe.Begin();
        }

        void SaveCityProfile()
        {
            QaFrameProbe.Stop();
            QaCpuProbe.Stop();
            if (nativeProbe) nativeProbe.Finish(game.QaDirectory);
            if (GameSession.Argument("--qa-city-profile", "") != "true") return;
            var report = new CityCpuReport
            {
                frameWork = QaFrameProbe.Snapshot(),
                transactionWork = QaCpuProbe.Snapshot()
            };
            File.WriteAllText(Path.Combine(game.QaDirectory, "ordinary-play-cpu.json"),
                JsonUtility.ToJson(report, true));
        }
    }
}
