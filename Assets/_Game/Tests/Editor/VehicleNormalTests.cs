using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tycoon.Tests
{
    public sealed class VehicleNormalTests
    {
        static CityVehicleKind[] Kinds => (CityVehicleKind[])Enum.GetValues(typeof(CityVehicleKind));

        [TestCaseSource(nameof(Kinds))]
        public void FinalVehicleMeshesHaveOnlyRealFacesAndUnitNormals(CityVehicleKind kind)
        {
            var root = new GameObject("VehicleNormalTests");
            try
            {
                if (kind == CityVehicleKind.Train) CityRailVehicleModel.Build(root.transform, "#63899E");
                else if (kind is CityVehicleKind.Bus or CityVehicleKind.Coach or CityVehicleKind.Taxi)
                    CityPublicTransportModel.Build(kind == CityVehicleKind.Bus ? 0
                        : kind == CityVehicleKind.Coach ? 1 : 2, root.transform);
                else CityVehicleModel.Build(kind, "#63899E", root.transform);
                var meshes = root.GetComponentsInChildren<MeshFilter>().Select(x => x.sharedMesh).Distinct();
                foreach (var mesh in meshes)
                {
                    Assert.That(mesh.normals, Has.Length.EqualTo(mesh.vertexCount), mesh.name);
                    Assert.That(mesh.normals.All(x => float.IsFinite(x.sqrMagnitude)
                        && Mathf.Abs(x.magnitude - 1) < .0002f), Is.True, mesh.name);
                    var vertices = mesh.vertices;
                    var triangles = mesh.triangles;
                    for (int i = 0; i < triangles.Length; i += 3)
                    {
                        var face = Vector3.Cross(vertices[triangles[i + 1]] - vertices[triangles[i]],
                            vertices[triangles[i + 2]] - vertices[triangles[i]]);
                        Assert.That(face.sqrMagnitude, Is.GreaterThan(1e-16f), mesh.name);
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
