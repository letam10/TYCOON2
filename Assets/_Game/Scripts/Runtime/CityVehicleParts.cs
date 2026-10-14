using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Tycoon
{
    public static class CityVehicleParts
    {
        public static void Collider(Transform root, Vector3 center, Vector3 size)
        {
            var collider = root.gameObject.AddComponent<BoxCollider>();
            collider.center = center;
            collider.size = size;
            var obstacle = root.gameObject.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.center = center;
            obstacle.size = size;
            // Xe dừng phải khoét NavMesh để người đi bộ tìm đường vòng thay vì kẹt ở capsule.
            obstacle.carving = true;
            obstacle.carveOnlyStationary = true;
            obstacle.carvingTimeToStationary = .25f;
        }

        public static Transform Wheel(Transform root, CityVehicleRig rig, Vector3 at, float radius, float width)
        {
            var pivot = TownModelParts.Root("WheelPivot", at, root);
            var mesh = new CityVehicleGeometry();
            mesh.Cylinder(CityVehicleSurface.Rubber, Vector3.zero, radius, width, true, 28);
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * (width * .5f + .009f);
                mesh.Cylinder(CityVehicleSurface.Chrome, new(x, 0, 0), radius * .62f, .025f, true);
                mesh.Cylinder(CityVehicleSurface.Dark, new(x + side * .017f, 0, 0), radius * .46f, .027f, true);
                for (int spoke = 0; spoke < 8; spoke++)
                {
                    float angle = spoke * Mathf.PI / 4;
                    mesh.Beam(CityVehicleSurface.Chrome, new(x + side * .035f, 0, 0),
                        new(x + side * .035f, Mathf.Cos(angle) * radius * .55f,
                            Mathf.Sin(angle) * radius * .55f), .037f);
                }
                mesh.Cylinder(CityVehicleSurface.Chrome, new(x + side * .035f, 0, 0),
                    radius * .17f, .04f, true, 16);
            }
            mesh.BuildSingle("TireRimSpokes", pivot, rig);
            return pivot;
        }

        public static Transform[] Wheels(Transform root, CityVehicleRig rig, float width, float radius,
            params float[] axles)
        {
            var wheels = new List<Transform>();
            foreach (float z in axles)
                for (int side = -1; side <= 1; side += 2)
                    wheels.Add(Wheel(root, rig, new(side * (width * .5f - .12f), radius, z), radius, .22f));
            rig.WheelRadius = radius;
            return wheels.ToArray();
        }

        public static Transform Door(Transform root, CityVehicleRig rig, int side, float x, float z,
            float floor, float height, float width, Material paint, bool sliding)
        {
            var door = TownModelParts.Root(side > 0 ? "RightPassengerDoor" : "LeftPassengerDoor",
                new(side * x, floor, sliding ? z : z + width * .5f), root);
            var geometry = new CityVehicleGeometry();
            float middle = sliding ? 0 : -width * .5f;
            geometry.Box(paint, new(0, height * .21f, middle), new(.055f, height * .42f, width));
            float inset = sliding ? 0 : -side * .13f;
            geometry.Quad(CityVehicleSurface.Glass,
                new(0, height * .44f, middle - width * .45f),
                new(0, height * .44f, middle + width * .45f),
                new(inset, height * .98f, middle + width * .45f),
                new(inset, height * .98f, middle - width * .45f));
            foreach (float edge in new[] { -.5f, .5f })
                geometry.Beam(CityVehicleSurface.Dark, new(0, height * .41f, middle + width * edge),
                    new(inset, height, middle + width * edge), .048f);
            geometry.Box(paint, new(inset, height, middle), new(.065f, .05f, width));
            geometry.Box(CityVehicleSurface.Chrome, new(side * .045f, height * .4f, middle - width * .3f),
                new(.04f, .045f, .16f));
            geometry.BuildSingle("DoorPanelGlassHandle", door, rig);
            return door;
        }

        public static Transform CarDoor(Transform root, CityVehicleRig rig, int side, float half,
            float length, float belt, float roof, Material paint)
        {
            float rear = CityVehicleGeometryCars.DoorRear;
            float front = CityVehicleGeometryCars.DoorFront;
            var door = TownModelParts.Root(side > 0 ? "RightPassengerDoor" : "LeftPassengerDoor",
                new(side * half, .3f, front), root);
            var g = new CityVehicleGeometry();
            Vector3 Local(Vector3 point) => point - door.localPosition;
            Vector3 Panel(float z, float t) => Local(CityVehicleGeometryCars.PanelPoint(
                side * half, belt, length, z, Mathf.Lerp(.3f, belt, t)));
            // Dùng cùng tiết diện với thân: cửa đóng tiếp tuyến, không bị lõm hoặc thiếu vai cửa.
            for (int segment = 0; segment < 12; segment++)
            {
                float za = Mathf.Lerp(rear, front, segment / 12f);
                float zb = Mathf.Lerp(rear, front, (segment + 1) / 12f);
                for (int band = 0; band < 4; band++)
                {
                    var a = Panel(za, band / 4f);
                    var b = Panel(zb, band / 4f);
                    var c = Panel(zb, (band + 1) / 4f);
                    var d = Panel(za, (band + 1) / 4f);
                    g.Quad(paint, a, b, c, d);
                    g.Quad(paint, d, c, b, a);
                    Vector3 inner = new(-side * .035f, 0, 0);
                    g.Quad(paint, d + inner, c + inner, b + inner, a + inner);
                    g.Quad(paint, a + inner, b + inner, c + inner, d + inner);
                    if (band == 0) g.Quad(paint, a, a + inner, b + inner, b);
                    if (segment == 0) g.Quad(paint, a, d, d + inner, a + inner);
                    if (segment == 11) g.Quad(paint, b, b + inner, c + inner, c);
                }
            }
            Vector3 Sill(float z) => Local(new(side * half * .9f, belt + .035f, z));
            Vector3 Header(float z) => Local(new(side * half * .77f, roof - .045f, z));
            g.Quad(paint, Panel(rear, 1), Panel(front, 1), Sill(front), Sill(rear));
            g.Quad(paint, Sill(rear), Sill(front), Panel(front, 1), Panel(rear, 1));
            g.Quad(CityVehicleSurface.Glass, Sill(rear), Sill(front), Header(front), Header(rear));
            foreach (float z in new[] { rear, front })
                g.Beam(CityVehicleSurface.Dark, Sill(z), Header(z), .026f);
            g.Beam(paint, Header(rear), Header(front), .035f);
            g.Beam(CityVehicleSurface.Chrome, Sill(rear), Sill(front), .012f);
            g.Box(CityVehicleSurface.Chrome, Panel(rear + .19f, .83f) + new Vector3(side * .018f, 0, 0),
                new(.035f, .035f, .15f));
            g.BuildSingle("CarDoorContinuousPanelAndGlass", door, rig);
            return door;
        }

        public static void Seat(CityVehicleGeometry geometry, Vector3 at, bool plush = false, float width = .55f)
        {
            var material = plush ? CityVehicleSurface.WarmSeat : CityVehicleSurface.Seat;
            geometry.Box(CityVehicleSurface.Dark, at + new Vector3(0, .12f, 0), new(.1f, .24f, .1f));
            geometry.Box(material, at + new Vector3(0, .3f, 0), new(width, .15f, .52f), .07f);
            geometry.Box(material, at + new Vector3(0, .59f, -.23f),
                new(width, plush ? .65f : .5f, .12f), .045f);
            if (!plush) return;
            geometry.Box(material, at + new Vector3(0, .97f, -.21f), new(width * .7f, .2f, .17f), .06f);
            for (int side = -1; side <= 1; side += 2)
                geometry.Box(CityVehicleSurface.Dark, at + new Vector3(side * width * .5f, .5f, 0),
                    new(.045f, .05f, .42f));
        }

        public static void Front(CityVehicleGeometry geometry, float width, float front, float y,
            bool luxury = false)
        {
            geometry.Box(CityVehicleSurface.Dark, new(0, y, front),
                new(width * .47f, luxury ? .42f : .23f, .045f));
            for (int i = -4; i <= 4; i++)
            {
                Vector3 at = luxury ? new(i * width * .049f, y, front + .028f) :
                    new(0, y + i * .021f, front + .028f);
                Vector3 size = luxury ? new(.025f, .36f, .025f) : new(width * .42f, .009f, .025f);
                geometry.Box(CityVehicleSurface.Chrome, at, size);
            }
            geometry.Box(CityVehicleSurface.Light, new(0, y - .22f, front + .025f), new(.4f, .11f, .018f));
            for (int side = -1; side <= 1; side += 2)
            {
                geometry.Box(CityVehicleSurface.Dark, new(side * width * .34f, y + .1f, front),
                    new(width * .23f, .19f, .08f), .04f);
                geometry.Box(CityVehicleSurface.Light, new(side * width * .34f, y + .12f, front + .045f),
                    new(width * .2f, .09f, .018f));
            }
        }

        public static void Mirror(CityVehicleGeometry geometry, float x, float y, float z)
        {
            int side = x > 0 ? 1 : -1;
            geometry.Beam(CityVehicleSurface.Dark, new(x - side * .14f, y - .07f, z - .08f),
                new(x, y, z), .04f);
            geometry.Box(CityVehicleSurface.Dark, new(x, y, z), new(.17f, .16f, .22f), .035f);
            geometry.Box(CityVehicleSurface.Chrome, new(x, y, z - .115f), new(.14f, .12f, .008f));
        }

        public static void Windshield(CityVehicleGeometry geometry, float halfBottom, float halfTop,
            float bottomY, float topY, float bottomZ, float topZ)
        {
            var a = new Vector3(-halfBottom, bottomY, bottomZ);
            var b = new Vector3(halfBottom, bottomY, bottomZ);
            var c = new Vector3(halfTop, topY, topZ);
            var d = new Vector3(-halfTop, topY, topZ);
            geometry.Quad(CityVehicleSurface.Glass, a, b, c, d);
            geometry.Beam(CityVehicleSurface.Dark, a, d, .065f);
            geometry.Beam(CityVehicleSurface.Dark, b, c, .065f);
            geometry.Beam(CityVehicleSurface.Dark, a, b, .065f);
            geometry.Beam(CityVehicleSurface.Dark, d, c, .065f);
            geometry.Beam(CityVehicleSurface.Dark, a + new Vector3(.2f, .015f, .018f),
                Vector3.Lerp(a, c, .46f) + Vector3.forward * .023f, .026f);
        }
    }
}
