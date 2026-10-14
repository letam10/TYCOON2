using System.Collections;
using System.Linq;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        IEnumerator VehicleModelGallery()
        {
            Time.timeScale = 0;
            float distance = game.CameraRig.Distance;
            Vector3 focus = game.CameraRig.FocusOffset;
            float pitch = game.CameraRig.Pitch;
            float yaw = game.CameraRig.Yaw;
            game.CameraRig.Pitch = 30;
            game.CameraRig.Yaw = 215;
            Vector3 center = new(0, 0, 300);
            var board = new GameObject("VehicleModelInspection").transform;
            Art.Box("PreviewGround", center - Vector3.up * .09f, new(65, .18f, 48), "#9CA993", board);
            foreach (CityVehicleKind kind in System.Enum.GetValues(typeof(CityVehicleKind)))
            {
                Transform model;
                if (kind == CityVehicleKind.Train) model = CityRailVehicleModel.Build(board, "#4D7F6A");
                else if (kind is CityVehicleKind.Bus or CityVehicleKind.Coach or CityVehicleKind.Taxi)
                {
                    model = new GameObject("PublicVehicle_" + kind).transform;
                    model.SetParent(board, false);
                    CityPublicTransportModel.Build(kind == CityVehicleKind.Bus ? 0
                        : kind == CityVehicleKind.Coach ? 1 : 2, model);
                }
                else model = CityVehicleModel.Build(kind, kind == CityVehicleKind.Sports ? "#BD5148" : "#63899E", board);
                model.position = center;
                var rig = model.GetComponent<CityVehicleRig>();
                Check(rig && rig.Wheels.Length >= 4, "V8 vehicle: real wheel pivots " + kind);
                var renderers = model.GetComponentsInChildren<MeshRenderer>();
                Check(renderers.Length <= 32, "V8 vehicle: bounded renderers " + kind);
                Check(model.GetComponentsInChildren<Collider>().Length > 0, "V8 vehicle: collision hull " + kind);
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                game.CameraRig.FocusOffset = bounds.center - game.Player.transform.position;
                game.CameraRig.Distance = Mathf.Max(9, bounds.size.magnitude * 1.8f);
                game.CameraRig.Snap();
                rig.SetDoorOpen(0);
                rig.SetDoorOpen(0, -1);
                yield return null;
                yield return null;
                yield return Capture("vehicle-v8-" + kind + "-closed.png");
                CheckModelGeometry(model, kind.ToString());
                yield return CaptureModelAngles("vehicle-angles-" + kind, bounds.center, game.CameraRig.Distance);
                rig.SetDoorOpen(1);
                game.CameraRig.Pitch = 20;
                game.CameraRig.Yaw = 270;
                game.CameraRig.Snap();
                yield return Capture("vehicle-v8-" + kind + "-right-open.png");
                rig.SetDoorOpen(1, -1);
                game.CameraRig.Yaw = 90;
                game.CameraRig.Snap();
                yield return null;
                yield return Capture("vehicle-v8-" + kind + "-open.png");
                var before = rig.Wheels.Select(x => x.localRotation).ToArray();
                rig.SetTravel(4, .25f);
                Check(rig.Wheels.Where((x, i) => Quaternion.Angle(x.localRotation, before[i]) > 1).Any(),
                    "V8 vehicle: wheels respond to real travel " + kind);
                Object.Destroy(model.gameObject);
                yield return null;
            }
            Object.Destroy(board.gameObject);
            game.CameraRig.Distance = distance;
            game.CameraRig.FocusOffset = focus;
            game.CameraRig.Pitch = pitch;
            game.CameraRig.Yaw = yaw;
            game.CameraRig.Snap();
            Time.timeScale = 1;
        }
    }
}
