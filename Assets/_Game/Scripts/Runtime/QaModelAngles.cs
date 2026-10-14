using System.Collections;
using System.Linq;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        IEnumerator CaptureModelAngles(string prefix, Vector3 center, float distance, bool elevation = true,
            float yawOffset = 0)
        {
            var camera = game.CameraRig;
            float savedPitch = camera.Pitch;
            float savedYaw = camera.Yaw;
            float savedDistance = camera.Distance;
            Vector3 savedFocus = camera.FocusOffset;
            bool savedOverview = camera.Overview;
            var lens = camera.GetComponent<Camera>();
            float savedFar = lens.farClipPlane;
            bool savedFog = RenderSettings.fog;
            camera.Overview = false;
            camera.Distance = distance;
            camera.FocusOffset = center - camera.Target.position;
            camera.Snap();
            lens.farClipPlane = Mathf.Max(lens.farClipPlane, distance * 2.5f);
            if (distance > 60) RenderSettings.fog = false;
            string[] names = elevation
                ? new[] { "front", "right", "rear", "left", "three-quarter", "top",
                    "rear-quarter", "front-left", "rear-right" }
                : new[] { "front", "right", "rear", "left", "top" };
            float[] yaws = elevation ? new float[] { 180, 270, 0, 90, 225, 180, 45, 135, 315 }
                : new float[] { 180, 270, 0, 90, 180 };
            float[] pitches = elevation ? new float[] { 20, 20, 20, 20, 35, 78, 35, 35, 35 }
                : new float[] { 30, 30, 30, 30, 78 };
            int views = names.Length;
            for (int i = 0; i < views; i++)
            {
                camera.Pitch = pitches[i];
                camera.Yaw = yaws[i] + yawOffset;
                camera.Snap();
                yield return Capture(prefix + "-" + names[i] + ".png");
            }
            camera.Pitch = savedPitch;
            camera.Yaw = savedYaw;
            camera.Distance = savedDistance;
            camera.FocusOffset = savedFocus;
            camera.Overview = savedOverview;
            camera.Snap();
            lens.farClipPlane = savedFar;
            RenderSettings.fog = savedFog;
        }

        void CheckModelGeometry(Transform root, string label)
        {
            var meshes = root.GetComponentsInChildren<MeshFilter>(true)
                .Where(x => x.sharedMesh && x.sharedMesh.isReadable).Select(x => x.sharedMesh).Distinct().ToArray();
            Check(meshes.Length > 0, "multi-angle: readable geometry " + label);
            foreach (var mesh in meshes)
            {
                Check(mesh.vertices.All(Finite), "multi-angle: finite vertices " + label + "/" + mesh.name);
                Check(mesh.normals.All(x => Finite(x) && x.sqrMagnitude > .5f),
                    "multi-angle: valid normals " + label + "/" + mesh.name);
            }
        }

        static bool Finite(Vector3 value) => float.IsFinite(value.x)
            && float.IsFinite(value.y) && float.IsFinite(value.z);

        IEnumerator StandaloneItemAngles(string item)
        {
            var board = new GameObject("ItemModelInspection").transform;
            board.position = new Vector3(0, 0, 300);
            Art.Box("PreviewGround", Vector3.down * .09f, new(12, .18f, 12), "#9CA993", board);
            var model = HandItemModels.Build(item, board).transform;
            var layout = CarryItemLayout.For(item);
            model.localRotation = CarryContainerProfile.Supports(item)
                ? Quaternion.identity : layout.Rotation;
            model.localScale = Vector3.one * layout.Scale;
            model.localPosition = Vector3.up * -ModelBounds(model).min.y;
            var bounds = ModelBounds(model);
            float distance = Mathf.Clamp(bounds.size.magnitude * 2.5f, 1.6f, 4);
            CheckModelGeometry(model, item + " isolated");
            yield return CaptureModelAngles("item-single-" + item, bounds.center, distance);
            Object.Destroy(board.gameObject);
            yield return null;
        }

        static Bounds ModelBounds(Transform root)
        {
            var renderers = root.GetComponentsInChildren<MeshRenderer>()
                .Where(x => x.enabled && !x.GetComponent<TextMesh>()).ToArray();
            var bounds = new Bounds(root.position, Vector3.zero);
            if (renderers.Length == 0) return bounds;
            bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        IEnumerator WorldModelAngles()
        {
            // Đợi mô hình lắp ráp xong trước khi đóng băng để kiểm hình tĩnh.
            yield return new WaitForSeconds(2);
            Time.timeScale = 0;
            game.Player.CanControl = false;
            foreach (var station in game.Stations.Where(x => x.IsUnlocked
                && x is MachineStation or StorageStation or ShelfStation or CheckoutStation or SafeStation
                    or TableStation))
            {
                var bounds = ModelBounds(station.transform);
                float distance = Mathf.Clamp(bounds.size.magnitude * 1.8f, 6, 14);
                yield return CaptureModelAngles("world-model-" + station.Id, bounds.center, distance, false);
                yield return StandaloneWorldAngles(station.transform, station.Id);
            }
            yield return CaptureModelAngles("rail-station", new Vector3(10, 1.3f, -64), 89);
            foreach (var landmark in game.GetComponentsInChildren<CityLandmark>())
            {
                var bounds = ModelBounds(landmark.transform);
                float distance = bounds.size.magnitude * 1.8f;
                yield return CaptureModelAngles("city-placed-" + landmark.Id, bounds.center, distance, false);
                yield return StandaloneWorldAngles(landmark.transform, landmark.Id, "city-single-");
            }
            Time.timeScale = 1;
            game.Player.CanControl = true;
        }

        IEnumerator StandaloneWorldAngles(Transform source, string id, string prefix = "world-single-",
            float yawOffset = 0)
        {
            var board = new GameObject("WorldModelInspection").transform;
            board.position = new Vector3(0, 0, 1000);
            var model = new GameObject(id).transform;
            model.SetParent(board, false);
            // Chỉ sao chép mesh đang hiển thị, không nhân bản logic trạm hoặc dữ liệu kinh doanh.
            foreach (var original in source.GetComponentsInChildren<MeshFilter>())
            {
                var renderer = original.GetComponent<MeshRenderer>();
                if (!renderer || !renderer.enabled || !original.sharedMesh || original.GetComponent<TextMesh>())
                    continue;
                var copy = new GameObject(original.name).transform;
                copy.SetParent(model, false);
                copy.position = board.position + original.transform.position - source.position;
                copy.rotation = original.transform.rotation;
                copy.localScale = original.transform.lossyScale;
                copy.gameObject.AddComponent<MeshFilter>().sharedMesh = original.sharedMesh;
                var copiedRenderer = copy.gameObject.AddComponent<MeshRenderer>();
                copiedRenderer.sharedMaterials = renderer.sharedMaterials;
                copiedRenderer.shadowCastingMode = renderer.shadowCastingMode;
                copiedRenderer.receiveShadows = renderer.receiveShadows;
            }
            var bounds = ModelBounds(model);
            model.position += Vector3.up * -bounds.min.y;
            bounds = ModelBounds(model);
            float side = Mathf.Max(12, bounds.size.magnitude * 3);
            Art.Box("PreviewGround", Vector3.down * .09f, new(side, .18f, side), "#9CA993", board);
            CheckModelGeometry(model, id + " isolated");
            float distance = Mathf.Clamp(bounds.size.magnitude * 1.8f, 4, 280);
            yield return CaptureModelAngles(prefix + id, bounds.center, distance, true, yawOffset);
            Object.Destroy(board.gameObject);
            yield return null;
        }
    }
}
