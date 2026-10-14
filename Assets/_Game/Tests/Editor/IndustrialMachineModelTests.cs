using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class IndustrialMachineModelTests
    {
        readonly List<GameObject> owners = new();
        static IEnumerable<string> Recipes => Definitions.Recipes.Select(recipe => recipe.id);

        [TearDown]
        public void TearDown()
        {
            foreach (var owner in owners)
                Object.DestroyImmediate(owner);
            owners.Clear();
        }

        [TestCaseSource(nameof(Recipes))]
        public void EveryRecipeHasFiniteGeometryWithinTheGroundedMachineFootprint(string recipe)
        {
            var station = Create(recipe);
            var model = IndustrialMachineModels.Build(station, station.transform);
            Assert.That(model.transform.localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(model.GetComponentsInChildren<Collider>(true), Is.Empty);
            Bounds bounds = GeometryBounds(model.transform);
            AssertFootprint(bounds, recipe);
            Assert.That(bounds.min.y, Is.EqualTo(0).Within(.0001f), "Feet must reach the floor");
            Assert.That(bounds.max.y, Is.GreaterThan(1.3f));
            Assert.That(model.GetComponentsInChildren<Transform>().Count(x => x.name == "IsolationFoot"),
                Is.EqualTo(4));
            var staticBody = model.transform.Find("StaticEngineering");
            Assert.That(staticBody.GetComponent<DiningMeshLifetime>(), Is.Not.Null);
            var renderers = staticBody.GetComponentsInChildren<MeshRenderer>();
            Assert.That(renderers.Length, Is.InRange(2, 12), "Static pieces must be grouped by material");
            Assert.That(renderers.All(renderer => renderer.name == "DiningBatch"), Is.True);
            Assert.That(model.GetComponentsInChildren<Light>(), Is.Empty);
            Assert.That(model.GetComponentsInChildren<ParticleSystem>(), Is.Empty);
        }

        [TestCaseSource(nameof(Recipes))]
        public void FunctionalRotorsRemainSeparateAndStayInBoundsThroughAFullTurn(string recipe)
        {
            var station = Create(recipe);
            var model = IndustrialMachineModels.Build(station, station.transform);
            bool powered = recipe is "mill" or "feedmill" or "cheesemaker" or "saucemaker"
                or "soyextractor" or "milkbottler" or "spinner" or "loom" or "breadmixer" or "cakemixer";
            if (!powered)
            {
                Assert.That(station.Rotor, Is.Null);
                return;
            }
            Assert.That(station.Rotor, Is.Not.Null);
            Assert.That(station.Rotor.parent, Is.EqualTo(model.transform));
            Assert.That(station.Rotor.GetComponentsInChildren<MeshRenderer>(), Is.Not.Empty);
            Quaternion initial = station.Rotor.localRotation;
            for (int i = 0; i < 16; i++)
            {
                station.Rotor.localRotation = initial * Quaternion.Euler(0, i * 22.5f, 0);
                AssertFootprint(GeometryBounds(model.transform), recipe + " rotor step " + i);
            }
        }

        [TestCaseSource(nameof(Recipes))]
        public void DressingKeepsGameplayColliderInventoryAndInteractionAndWiresExistingMotion(string recipe)
        {
            var station = Create(recipe);
            var collider = station.gameObject.AddComponent<BoxCollider>();
            collider.center = new Vector3(.04f, .78f, .06f);
            collider.size = new Vector3(1.8f, 1.56f, 1.4f);
            Vector3 center = collider.center;
            Vector3 size = collider.size;
            var input = station.Input;
            var output = station.Inventory;
            Assert.That(station.Input.TryAdd(station.Recipe.inputs[0].id, 1), Is.True);
            Assert.That(station.Inventory.TryAdd(station.Recipe.output, 1), Is.True);
            var definition = station.Recipe;
            Vector3 interaction = station.InteractionPoint;
            WorldDressing.Machine(station);
            Assert.That(station.Id, Is.EqualTo("industrial-test-" + recipe));
            Assert.That(station.Recipe, Is.SameAs(definition));
            Assert.That(station.Input, Is.SameAs(input));
            Assert.That(station.Inventory, Is.SameAs(output));
            Assert.That(station.Input.Total, Is.EqualTo(1));
            Assert.That(station.Inventory.Total, Is.EqualTo(1));
            Assert.That(station.InteractionPoint, Is.EqualTo(interaction));
            Assert.That(collider.center, Is.EqualTo(center));
            Assert.That(collider.size, Is.EqualTo(size));
            Assert.That(collider.enabled, Is.True);
            Assert.That(station.GetComponentsInChildren<Collider>(true), Has.Length.EqualTo(1));
            Assert.That(station.GetComponent<CozyMachineMotion>(), Is.Not.Null);
            var status = station.GetComponent<MachineStatusLight>();
            Assert.That(status.Station, Is.SameAs(station));
            Assert.That(status.Lamp.name, Is.EqualTo("MachineStatusLamp"));
            Assert.That(status.Fill.name, Is.EqualTo("MachineProgress"));
        }

        [TestCaseSource(nameof(Recipes))]
        public void RepeatedInstancesReuseSurfaceMaterials(string recipe)
        {
            var a = Create(recipe);
            var b = Create(recipe);
            var first = IndustrialMachineModels.Build(a, a.transform);
            var second = IndustrialMachineModels.Build(b, b.transform);
            var firstMaterials = first.GetComponentsInChildren<Renderer>()
                .Select(renderer => renderer.sharedMaterial).Distinct().ToArray();
            var secondMaterials = second.GetComponentsInChildren<Renderer>()
                .Select(renderer => renderer.sharedMaterial).Distinct().ToArray();
            Assert.That(firstMaterials, Is.EquivalentTo(secondMaterials));
            Assert.That(firstMaterials.All(material => material), Is.True);
        }

        [Test]
        public void VesselCacheReusesClosedThickRingGeometry()
        {
            var station = Create("cheesemaker");
            var a = IndustrialMachineVessels.Ring(station.transform, "A", Vector3.zero, .5f, .45f, .7f);
            var b = IndustrialMachineVessels.Ring(station.transform, "B", Vector3.one, .5f, .45f, .7f);
            var mesh = a.GetComponent<MeshFilter>().sharedMesh;
            Assert.That(b.GetComponent<MeshFilter>().sharedMesh, Is.SameAs(mesh));
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;
            var edges = new Dictionary<(Vector3, Vector3), int>();
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 p = vertices[triangles[i]];
                Vector3 q = vertices[triangles[i + 1]];
                Vector3 r = vertices[triangles[i + 2]];
                Assert.That(Vector3.Cross(q - p, r - p).sqrMagnitude, Is.GreaterThan(1e-12f));
                Edge(p, q);
                Edge(q, r);
                Edge(r, p);
            }
            foreach (var edge in edges)
                Assert.That(edges.TryGetValue((edge.Key.Item2, edge.Key.Item1), out int count)
                    && count == edge.Value, Is.True, "Vessel has an open or inconsistently wound edge");

            void Edge(Vector3 p, Vector3 q)
            {
                // Khép seam lượng giác về cùng lưới để so cạnh hình học, không so index UV/normal.
                p = Quantize(p);
                q = Quantize(q);
                var key = (p, q);
                edges[key] = edges.TryGetValue(key, out int count) ? count + 1 : 1;
            }
        }

        MachineStation Create(string recipe)
        {
            var owner = new GameObject("IndustrialTestOwner");
            owners.Add(owner);
            var station = owner.AddComponent<MachineStation>();
            station.Id = "industrial-test-" + recipe;
            station.Recipe = Definitions.Recipe(recipe);
            station.Inventory = new Inventory(36);
            station.InteractionPoint = new Vector3(0, 0, -1.8f);
            return station;
        }

        static Bounds GeometryBounds(Transform root)
        {
            var filters = root.GetComponentsInChildren<MeshFilter>(true);
            Assert.That(filters, Is.Not.Empty);
            bool first = true;
            Bounds result = default;
            foreach (var filter in filters)
            {
                var matrix = root.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                foreach (var vertex in filter.sharedMesh.vertices)
                {
                    Vector3 point = matrix.MultiplyPoint3x4(vertex);
                    if (!Finite(point.x) || !Finite(point.y) || !Finite(point.z))
                        Assert.Fail("Nonfinite geometry in " + filter.name);
                    if (first)
                    {
                        result = new Bounds(point, Vector3.zero);
                        first = false;
                    }
                    else
                        result.Encapsulate(point);
                }
            }
            Assert.That(first, Is.False);
            return result;
        }

        static void AssertFootprint(Bounds bounds, string recipe)
        {
            Assert.That(bounds.min.x, Is.GreaterThanOrEqualTo(-1.1001f), recipe);
            Assert.That(bounds.max.x, Is.LessThanOrEqualTo(1.1001f), recipe);
            Assert.That(bounds.min.z, Is.GreaterThanOrEqualTo(-.9201f), recipe);
            Assert.That(bounds.max.z, Is.LessThanOrEqualTo(.9201f), recipe);
            Assert.That(bounds.min.y, Is.GreaterThanOrEqualTo(-.0001f), recipe);
            Assert.That(bounds.max.y, Is.LessThanOrEqualTo(2.3501f), recipe);
        }

        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        static Vector3 Quantize(Vector3 value) => new(Mathf.Round(value.x * 100000),
            Mathf.Round(value.y * 100000), Mathf.Round(value.z * 100000));
    }
}
