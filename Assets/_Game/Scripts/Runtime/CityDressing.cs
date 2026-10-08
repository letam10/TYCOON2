using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Tycoon
{
    public static class CityDressing
    {
        public static void Build(GameSession game, Transform world)
        {
            var root = new GameObject("CityScenery").transform;
            root.SetParent(world, false);
            Physics.SyncTransforms();
            var space = new CityDressingSpace(game, world);
            var geometry = new CityStaticGeometry(root);
            CityArchitecture.Farm(geometry, space);
            CityArchitecture.Businesses(geometry, space);
            CityArchitecture.Neighborhoods(geometry, space);
            CityProps.Districts(geometry, space);
            CityLandscape.Build(geometry, space);
            geometry.Finish();
            if (game.IsOrdinaryPlayCheck) space.WriteReport(game.QaDirectory);
        }
    }

    public sealed class CityDressingSpace
    {
        readonly List<Bounds> reserved = new();
        readonly List<string> names = new();
        readonly List<Placement> placements = new();

        [Serializable]
        sealed class Placement
        {
            public string name, blocker;
            public Vector3 center;
            public float width, depth;
            public bool placed;
            public Bounds blockingBounds;
        }

        [Serializable]
        sealed class PlacementReport
        {
            public Placement[] placements;
        }

        public CityDressingSpace(GameSession game, Transform world)
        {
            foreach (var road in CityStreetNetwork.Bounds())
            {
                var bounds = road;
                bounds.Expand(new Vector3(2, 0, 2));
                Reserve(bounds, "Road");
            }
            // Chừa cả vị trí đứng và đường nối hàng chờ, kể cả trạm chưa mở khóa.
            foreach (var station in game.Stations)
            {
                if (!station) continue;
                Keep(station.transform.position, station is CargoDock ? 6 : 2.8f, station.Id);
                Keep(station.InteractionPoint, station.InteractionRadius + 1.5f, station.Id + ":interaction");
                Keep(station.WaitingPoint, 2, station.Id + ":waiting");
                var corridor = new Bounds(station.InteractionPoint, Vector3.zero);
                corridor.Encapsulate(station.WaitingPoint);
                corridor.Expand(new Vector3(3.2f, 0, 3.2f));
                Reserve(corridor, station.Id + ":corridor");
            }
            foreach (var collider in world.GetComponentsInChildren<Collider>(true))
            {
                if (collider.isTrigger || collider.bounds.size.y < .35f) continue;
                var bounds = collider.bounds;
                bounds.Expand(new Vector3(1, 0, 1));
                Reserve(bounds, collider.name);
            }
        }

        public bool Take(Vector3 center, float width, float depth, string name = "Scenery")
        {
            var bounds = new Bounds(center, new(width, 10, depth));
            var attempt = new Placement { name = name, center = center, width = width, depth = depth };
            placements.Add(attempt);
            var world = CityDistricts.WorldBounds;
            if (bounds.min.x < world.min.x + 1 || bounds.max.x > world.max.x - 1
                || bounds.min.z < world.min.z + 1 || bounds.max.z > world.max.z - 1)
            {
                attempt.blocker = "World edge";
                return false;
            }
            for (int index = 0; index < reserved.Count; index++)
            {
                var other = reserved[index];
                if (bounds.min.x < other.max.x && bounds.max.x > other.min.x
                    && bounds.min.z < other.max.z && bounds.max.z > other.min.z)
                {
                    attempt.blocker = names[index];
                    attempt.blockingBounds = other;
                    return false;
                }
            }
            Reserve(bounds, name);
            attempt.placed = true;
            return true;
        }

        public void WriteReport(string directory)
        {
            File.WriteAllText(Path.Combine(directory, "city-scenery-placement.json"),
                JsonUtility.ToJson(new PlacementReport { placements = placements.ToArray() }, true));
        }

        void Keep(Vector3 center, float radius, string name)
        {
            Reserve(new Bounds(center, new(radius * 2, 10, radius * 2)), name);
        }

        void Reserve(Bounds bounds, string name)
        {
            reserved.Add(bounds);
            names.Add(name);
        }
    }
}
