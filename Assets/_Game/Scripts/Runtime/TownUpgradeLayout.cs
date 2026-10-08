using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tycoon
{
    public static class TownUpgradeLayout
    {
        static Bounds Box(Vector3 p, float width, float depth) => new(p, new(width, 2, depth));

        public static string District(UpgradeDefinition upgrade)
        {
            if (upgrade.role is "Animal" or "AnimalWorker") return "livestock";
            if (upgrade.kind == "worker") return GameSession.CrewFor(upgrade).area;
            if (upgrade.family.StartsWith("storage_")) return upgrade.family.Substring(8);
            return upgrade.family switch
            {
                "farm" or "player" => "farm",
                "animal" => "livestock",
                "counter" => "farm_shop",
                "mill" => "processing",
                "market" => "supermarket",
                "oven" => "bakery",
                "kitchen" => "restaurant",
                _ => upgrade.id switch
                {
                    "barn" or "milk_line" or "egg_line" or "sheep_line" => "livestock",
                    "mill" or "conveyor_processing" => "processing",
                    "supermarket" => "supermarket",
                    "bakery" or "conveyor_bakery" => "bakery",
                    "restaurant" => "restaurant",
                    _ => "farm_shop"
                }
            };
        }

        public static void Apply(GameSession game)
        {
            var occupied = Footprints(game);
            var placed = new List<Bounds>();
            foreach (var pad in game.Stations.OfType<PurchasePad>().OrderBy(p => p.Upgrade.id))
            {
                string district = District(pad.Upgrade);
                Vector3 target = Center(district);
                bool found = false;
                Vector3 best = target;
                // Các dải ô nằm ngoài mặt tiền, không chen vào thiết bị, hàng chờ và đường xe.
                foreach (var point in Candidates(district))
                {
                    if (!Free(point)) continue;
                    best = point;
                    found = true;
                    break;
                }
                if (!found)
                {
                    foreach (var point in Grid().OrderBy(p => (p - target).sqrMagnitude))
                    {
                        if (!Free(point)) continue;
                        best = point;
                        found = true;
                        break;
                    }
                }
                if (!found) throw new System.InvalidOperationException("Không có chỗ cho " + pad.Id);
                placed.Add(Box(best, 2.18f, 2.18f));
                pad.transform.position = best;
                pad.InteractionPoint = best;
                pad.InteractionRadius = 1;
                foreach (var label in pad.GetComponentsInChildren<TextMesh>())
                {
                    if (label == pad.StatusLabel) continue;
                    label.text = pad.Upgrade.cost.ToString("N0");
                    label.transform.localPosition = new(0, .12f, -.6f);
                }
                pad.InitializeIcon();
                var icon = pad.transform.Find("PurchaseIcon2D");
                icon.localPosition = new(0, .86f, .12f);
                icon.localScale = Vector3.one * 1.24f;

                bool Free(Vector3 p)
                {
                    var footprint = Box(p, 2.18f, 2.18f);
                    return !occupied.Any(b => b.Intersects(footprint))
                        && !placed.Any(b => b.Intersects(footprint));
                }
            }
        }

        static Vector3 Center(string district) => CityDistricts.Offset(district) + (district switch
        {
            "farm" => new(-33, 0, 0),
            "livestock" => new(-43, 0, 13),
            "processing" => new(37, 0, -5.5f),
            "supermarket" => new(43, 0, 48),
            "bakery" => new(8, 0, 23),
            "restaurant" => new(-24, 0, 24),
            _ => new(13, 0, -5.5f)
        });

        static IEnumerable<Vector3> Candidates(string district)
        {
            var c = Center(district);
            bool vertical = district is "farm" or "livestock";
            for (int row = 0; row < 5; row++)
            {
                for (int col = 0; col < 6; col++)
                {
                    var p = vertical ? new Vector3(c.x - row * 2.4f, 0, c.z + col * 2.4f)
                        : new Vector3(c.x + (col - 2) * 2.4f, 0, c.z - row * 2.4f);
                    if (CityDistricts.WorldBounds.Contains(p)) yield return p;
                }
            }
        }

        static IEnumerable<Vector3> Grid()
        {
            var bounds = CityDistricts.WorldBounds;
            for (float z = bounds.min.z + 4; z <= bounds.max.z - 4; z += 2.4f)
                for (float x = bounds.min.x + 4; x <= bounds.max.x - 4; x += 2.4f) yield return new(x, 0, z);
        }

        static List<Bounds> Footprints(GameSession game)
        {
            var list = new List<Bounds>
            {
                Box(new Vector3(7, 0, 6) + CityDistricts.Offset("farm_shop"), 22, 20),
                Box(new Vector3(36, 0, 7) + CityDistricts.Offset("processing"), 18, 18),
                Box(new Vector3(40, 0, 33) + CityDistricts.Offset("supermarket"), 22, 22),
                Box(new Vector3(11, 0, 38) + CityDistricts.Offset("bakery"), 20, 20),
                Box(new Vector3(-20, 0, 39) + CityDistricts.Offset("restaurant"), 22, 20),
                Box(new Vector3(-33, 0, 18) + CityDistricts.Offset("farm"), 10, 8),
                Box(new Vector3(-39, 0, 28) + CityDistricts.Offset("farm"), 8, 7),
                Box(new Vector3(-43, 0, 39) + CityDistricts.Offset("farm"), 8, 8)
            };
            foreach (var station in game.Stations.Where(x => x is not PurchasePad and not ConveyorStation))
            {
                list.Add(station is ProductionStation p && !p.Animal ? Box(station.transform.position, 6.6f, 6.6f)
                    : Box(station.transform.position, station is StorageStation ? 5.7f : 5.1f,
                        station is ShelfStation ? 7.2f : 4.9f));
                list.Add(Box(station.WorkPoint, 1.5f, 1.5f));
                list.Add(Box(station.WaitingPoint, 1.5f, 1.5f));
                if (station is not CheckoutStation counter) continue;
                list.Add(Box(counter.CollectionPoint, 2.5f, 2.5f));
                for (int i = 0; i < 30; i++) list.Add(Box(counter.QueuePoint(i), 1.2f, 1.2f));
            }
            foreach (var storage in game.Stations.OfType<StorageStation>())
                list.Add(Box(TruckRoutes.Dock(storage.Id), 4.4f, 6));
            list.AddRange(TownLayout.RoadBounds());
            return list;
        }
    }
}
