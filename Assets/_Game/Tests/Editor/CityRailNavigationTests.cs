using System.Collections;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace Tycoon.Tests
{
    public sealed class CityRailNavigationTests : CityPedestrianPlayModeFixture
    {
        GameObject world;

        protected override void CreateWorld()
        {
            world = new GameObject("RailwayNavigationFixture");
            var ground = new GameObject("Ground", typeof(BoxCollider));
            ground.transform.SetParent(world.transform, false);
            ground.transform.localPosition = CityDistricts.WorldCenter - Vector3.up * .13f;
            ground.GetComponent<BoxCollider>().size =
                new(CityDistricts.GroundWidth, .25f, CityDistricts.GroundDepth);
            // Fixture NavMesh không khởi tạo GameSession/HUD cho bảng tên.
            CityRailStation.Build(world.transform, false);
            Physics.SyncTransforms();
            var surface = world.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.overrideVoxelSize = true;
            surface.voxelSize = .16f;
            surface.BuildNavMesh();
        }

        protected override void ClearWorld()
        {
            if (world) Object.DestroyImmediate(world);
        }

        [UnityTest]
        public IEnumerator PlatformToPlatformPathUsesTheAccessibleFootbridge()
        {
            var path = new NavMeshPath();
            Assert.That(NavMesh.SamplePosition(CityRailNetwork.PlatformPoint(0, 20), out var north,
                1, NavMesh.AllAreas), Is.True);
            Assert.That(NavMesh.SamplePosition(CityRailNetwork.PlatformPoint(1, 20), out var south,
                1, NavMesh.AllAreas), Is.True);
            Assert.That(NavMesh.CalculatePath(north.position, south.position, NavMesh.AllAreas, path), Is.True);
            var diagnostics = new System.Collections.Generic.List<string>();
            diagnostics.Add("route=" + path.status + " " +
                string.Join(";", System.Array.ConvertAll(path.corners, p => p.ToString())));
            var probes = new[]
            {
                north.position, new Vector3(13.5f, 0, -55.8f), new(20, 1.55f, -55.8f),
                new(31, 4.4f, -55.8f), new(34, 4.8f, -55.8f), new(34, 4.8f, -72.2f),
                new(31, 4.4f, -72.2f), new(20, 1.55f, -72.2f), new(13.5f, 0, -72.2f), south.position
            };
            for (int i = 1; i < probes.Length; i++)
            {
                bool a = NavMesh.SamplePosition(probes[i - 1], out var first, .5f, NavMesh.AllAreas);
                bool b = NavMesh.SamplePosition(probes[i], out var last, .5f, NavMesh.AllAreas);
                var part = new NavMeshPath();
                if (a && b) NavMesh.CalculatePath(first.position, last.position, NavMesh.AllAreas, part);
                diagnostics.Add("segment " + i + " sampled " + a + "/" + b + " " + part.status + " " +
                    string.Join(";", System.Array.ConvertAll(part.corners, p => p.ToString())));
            }
            foreach (float px in new[] { 15.5f, 20, 25, 30, 34 })
            {
                float py = (px - 15.5f) / 18.5f * 4.8f;
                foreach (float pz in new[] { -55.8f, -72.2f })
                {
                    var point = new Vector3(px, py, pz);
                    bool found = NavMesh.SamplePosition(point, out var hit, .45f, NavMesh.AllAreas);
                    diagnostics.Add(point + " -> " + found + " " + hit.position);
                }
            }
            foreach (float pz in new[] { -57.5f, -60, -64, -68, -70.5f })
            {
                var point = new Vector3(34, 4.8f, pz);
                bool found = NavMesh.SamplePosition(point, out var hit, .45f, NavMesh.AllAreas);
                diagnostics.Add("bridge " + point + " -> " + found + " " + hit.position);
            }
            Assert.That(System.Array.Exists(path.corners, p => p.y > 4), Is.True,
                "Both station sides connect through the elevated pedestrian bridge: " +
                string.Join(" | ", diagnostics));
            Assert.That(path.status, Is.EqualTo(NavMeshPathStatus.PathComplete), string.Join(" | ", diagnostics));
            foreach (float z in CityRailNetwork.Tracks)
                Assert.That(NavMesh.SamplePosition(new(10, 0, z), out _, .2f, NavMesh.AllAreas), Is.False,
                    "The track bed is excluded from ordinary pedestrian navigation");
            yield break;
        }
    }
}
