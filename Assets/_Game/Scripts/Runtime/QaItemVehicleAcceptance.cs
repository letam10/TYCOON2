using System.Collections;
using System.Linq;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        IEnumerator ItemVehicleAcceptance()
        {
            if (System.Array.Exists(System.Environment.GetCommandLineArgs(),
                argument => argument == "--qa-dining-only"))
            {
                yield return PhysicalSavedAcceptance(false);
                yield return DiningAcceptance();
                Check(game.RuntimeErrors.Count == 0, "Dining: no runtime errors");
                yield break;
            }
            if (System.Array.Exists(System.Environment.GetCommandLineArgs(),
                argument => argument == "--qa-vehicle-rides-only"))
            {
                yield return PhysicalSavedAcceptance(false);
                yield return LiveVehicleAcceptance();
                Check(game.RuntimeErrors.Count == 0, "V8 rides: no runtime errors");
                yield break;
            }
            yield return CarrotSettingsAcceptance();
            yield return AllItemModelGallery();
            yield return CurrencyCargoGallery();
            yield return VehicleModelGallery();
            yield return WorldModelAngles();
            yield return DiningAcceptance();
            yield return LiveVehicleAcceptance();
            Check(game.RuntimeErrors.Count == 0, "V8: no runtime errors after all goods and vehicle checks");
        }

        IEnumerator CurrencyCargoGallery()
        {
            var baseline = game.Transactions.Snapshot();
            game.Player.CanControl = false;
            game.Player.transform.rotation = Quaternion.Euler(0, 200, 0);
            game.CameraRig.Distance = 6.5f;
            game.CameraRig.FocusOffset = new(0, .65f, .35f);
            foreach (long cash in new long[] { 1, 999999, 9000000000000000000 })
            {
                PhysicalCarryFixture(baseline, "carrot", 0, cash);
                yield return null;
                yield return null;
                var root = game.Player.transform.Find("FrontHandCarry/CashInHands");
                var models = root.GetComponentsInChildren<MeshFilter>();
                Check(game.Economy.CashInHand == cash && models.Length is > 0 and <= 18,
                    "V8 cash: full long balance with bounded models " + cash);
                Check(game.Player.Carry.Total == 0, "V8 cash: hand contains only cash");
                yield return Capture("cash-v8-" + cash + ".png");
                if (cash == 999999)
                    yield return CaptureModelAngles("cash-angles", ModelBounds(root).center, 6.5f);
            }
            Time.timeScale = 1;
            game.LoadGame();
            yield return TownWait(() => game.CanSimulate, 8, "restore private cash checkpoint");
            game.Player.CanControl = true;
            yield return PhysicalCargoAcceptance();
        }

        IEnumerator AllItemModelGallery()
        {
            var baseline = game.Transactions.Snapshot();
            game.Player.CanControl = false;
            game.Player.transform.rotation = Quaternion.Euler(0, 200, 0);
            game.CameraRig.Distance = 6.5f;
            game.CameraRig.FocusOffset = new(0, .65f, .35f);
            game.CameraRig.Snap();
            foreach (var item in Definitions.Items)
            {
                foreach (int count in new[] { 1, 24, 48 })
                {
                    PhysicalCarryFixture(baseline, item.id, count);
                    Time.timeScale = 1;
                    yield return new WaitForSecondsRealtime(.22f);
                    Time.timeScale = 0;
                    yield return null;
                    var stack = game.Player.GetComponentsInChildren<InventoryStack>()
                        .First(x => x.Inventory == game.Player.Carry);
                    var models = stack.GetComponentsInChildren<MeshFilter>()
                        .Where(x => x.sharedMesh && x.sharedMesh.name == "HandMesh_" + item.id).ToArray();
                    var layout = CarryItemLayout.For(item.id);
                    Check(models.Length == count && stack.VisibleCount == count, "V8 items: visible " + item.id + count);
                    Check(layout.Columns == 2 && layout.Rows == 3, "V8 items: 2x3 layout " + item.id);
                    Check(models.All(x => x.GetComponent<MeshRenderer>().sharedMaterials.Length == 1),
                        "V8 items: shared single material " + item.id);
                    Quaternion heldRotation = CarryContainerProfile.Supports(item.id)
                        ? Quaternion.identity : layout.Rotation;
                    Check(models.All(x => Quaternion.Angle(x.transform.localRotation, heldRotation) < .01f),
                        "V8 items: consistent orientation " + item.id);
                    Check(stack.transform.localPosition.z - stack.RearExtent >= .42f,
                        "V8 items: stack clears player body " + item.id);
                    yield return Capture("item-v8-" + item.id + "-" + count + ".png");
                    if (count == 1) yield return StandaloneItemAngles(item.id);
                    if (count == 48)
                    {
                        CheckModelGeometry(stack.transform, item.id);
                        yield return CaptureModelAngles("item-angles-" + item.id + "-" + count,
                            ModelBounds(stack.transform).center, 6.5f);
                    }
                }
            }
            Time.timeScale = 1;
            game.LoadGame();
            yield return TownWait(() => game.CanSimulate, 8, "V8: restore private checkpoint after model gallery");
        }
    }
}
