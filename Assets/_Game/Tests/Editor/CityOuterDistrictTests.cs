using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Unity.AI.Navigation;

namespace Tycoon.Tests
{
    public sealed class CityOuterDistrictTests
    {
        [Test]
        public void ExpandedWorldDoublesBothAxesWithoutChangingExistingStationsOrRevision()
        {
            Assert.That(CityDistricts.GroundWidth, Is.EqualTo(565.685424f * 2).Within(.001f));
            Assert.That(CityDistricts.GroundDepth, Is.EqualTo(384.666088f * 2).Within(.001f));
            Assert.That(CityDistricts.Revision, Is.EqualTo(2));
            Assert.That(CityDistricts.PlayerSpawn, Is.EqualTo(new Vector3(-46, .05f, -25)));
            Assert.That(CityDistricts.Center("processing"), Is.EqualTo(new Vector3(67, 0, -13)));
        }

        [Test]
        public void LandmarksFitReservedLotsAndHaveGroundedSolidBuildingColliders()
        {
            var root = new GameObject("OuterDistrictTests");
            var previous = GameSession.Instance;
            var game = root.AddComponent<GameSession>();
            GameSession.Instance = game;
            game.IsQa = true;
            try
            {
                var space = new CityDressingSpace(game, root.transform);
                CityOuterDistricts.Build(root.transform, space);
                var landmarks = root.GetComponentsInChildren<CityLandmark>();
                Assert.That(landmarks.Length, Is.EqualTo(13));
                Assert.That(landmarks.Select(x => x.Id).Distinct().Count(), Is.EqualTo(13));
                foreach (var landmark in landmarks)
                {
                    var renderers = landmark.GetComponentsInChildren<MeshRenderer>();
                    Assert.That(renderers.Length, Is.GreaterThan(0), landmark.Id);
                    foreach (var renderer in renderers)
                    {
                        var bounds = renderer.bounds;
                        Vector3 offset = bounds.center - landmark.transform.position;
                        Assert.That(Mathf.Abs(offset.x) + bounds.extents.x,
                            Is.LessThanOrEqualTo(landmark.Footprint.x * .5f + .1f), landmark.Id);
                        Assert.That(Mathf.Abs(offset.z) + bounds.extents.z,
                            Is.LessThanOrEqualTo(landmark.Footprint.z * .5f + .1f), landmark.Id);
                        Assert.That(bounds.min.y, Is.GreaterThanOrEqualTo(-.001f), landmark.Id);
                        Assert.That(renderer.sharedMaterial.enableInstancing, Is.True);
                    }
                }
                var shells = root.GetComponentsInChildren<BoxCollider>()
                    .Where(x => x.name == "BuildingShellCollision").ToArray();
                Assert.That(shells.Length, Is.EqualTo(4));
                Assert.That(shells.All(x => Mathf.Abs(x.bounds.min.y) < .001f), Is.True);
                var water = root.GetComponentsInChildren<NavMeshModifierVolume>();
                Assert.That(water.Length, Is.EqualTo(12));
                Assert.That(water.All(x => x.area == 1), Is.True);
                var cafe = landmarks.Single(x => x.Id == "CoffeeHouse");
                Assert.That(cafe.Approach.z, Is.EqualTo(-134).Within(.001f));
                Assert.That(cafe.Approach.z, Is.GreaterThan(cafe.transform.position.z));
            }
            finally
            {
                Object.DestroyImmediate(root);
                GameSession.Instance = previous;
            }
        }

        [Test]
        public void SharedSceneryPrimitivesHaveNoZeroAreaFacesOrInvalidNormals()
        {
            var meshes = new[] { CityStaticGeometryMeshes.Box(), CityStaticGeometryMeshes.Radial(1, 0, 12),
                CityStaticGeometryMeshes.Crown(), CityHarborHull.Build() };
            try
            {
                foreach (var mesh in meshes)
                {
                    Assert.That(mesh.normals.All(x => float.IsFinite(x.x) && x.sqrMagnitude > .5f), Is.True);
                    var vertices = mesh.vertices;
                    var indices = mesh.triangles;
                    for (int i = 0; i < indices.Length; i += 3)
                    {
                        Vector3 a = vertices[indices[i]];
                        Vector3 b = vertices[indices[i + 1]];
                        Vector3 c = vertices[indices[i + 2]];
                        Assert.That(Vector3.Cross(b - a, c - a).sqrMagnitude,
                            Is.GreaterThan(1e-12f), mesh.name + " triangle " + i / 3);
                    }
                }
            }
            finally
            {
                foreach (var mesh in meshes) Object.DestroyImmediate(mesh);
            }
        }
    }
}
