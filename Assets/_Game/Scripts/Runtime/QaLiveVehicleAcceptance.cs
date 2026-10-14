using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        IEnumerator LiveVehicleAcceptance()
        {
            var world = game.transform.Find("World");
            var rail = world.GetComponentInChildren<CityRailService>();
            var transit = world.GetComponentInChildren<CityPublicTransport>();
            yield return TownWait(() => rail.Trains.Count == 2 && transit.Vehicles.Count == 3,
                12, "V8: two live trains and all public vehicle types start");
            var road = world.GetComponentsInChildren<CityRoadVehiclePassengers>();
            Check(road.Length == 6, "V8: sedan small truck cargo tanker luxury sports all run");
            var north = CityRailNetwork.PlatformPoint(0, 20);
            var south = CityRailNetwork.PlatformPoint(1, 20);
            var path = new NavMeshPath();
            Check(NavMesh.CalculatePath(north, south, NavMesh.AllAreas, path)
                && path.status == NavMeshPathStatus.PathComplete, "V8: station sides have a complete walking path");
            Check(path.corners.Any(x => x.y > 4), "V8: station path crosses above both tracks");
            game.Player.CanControl = true;
            game.CameraRig.Distance = 13;
            yield return PlacedVehicleAngles(rail, transit, road);
            game.CameraRig.FocusOffset = Vector3.zero;
            yield return Travel(north);
            yield return Travel(south);
            Check(game.Player.transform.position.y < .4f, "V8: player returns to ground after crossing station");
            yield return Capture("rail-station-two-platforms.png");
            yield return Travel(north);
            game.Player.CanControl = false;
            float until = Time.realtimeSinceStartup + 240;
            float nextSnapshot = 0;
            bool captured = false;
            while (Time.realtimeSinceStartup < until)
            {
                if (Time.realtimeSinceStartup >= nextSnapshot)
                {
                    nextSnapshot = Time.realtimeSinceStartup + 5;
                    foreach (var vehicle in road)
                    {
                        var rider = vehicle.GetComponentInChildren<CityPublicTransportPassenger>();
                        string row = Time.realtimeSinceStartup.ToString("F2") + " "
                            + vehicle.GetComponent<CityVehicleRig>().Kind + " position=" + vehicle.transform.position
                            + " forward=" + vehicle.transform.forward + " stop=" + vehicle.AtStop
                            + " doors=" + vehicle.DoorsOpen + " boarding=" + vehicle.Boardings
                            + " alighting=" + vehicle.Alightings + " rider=" + (rider ? rider.Phase.ToString() : "none")
                            + " riderPosition=" + (rider ? rider.transform.position.ToString("F3") : "none")
                            + " exit=" + vehicle.DoorOutside.ToString("F3");
                        if (rider && rider.Phase == CityPassengerPhase.Disembarking)
                        {
                            var hits = Physics.OverlapCapsule(vehicle.DoorOutside + Vector3.up * .36f,
                                vehicle.DoorOutside + Vector3.up * 1.48f, .32f, ~0, QueryTriggerInteraction.Ignore);
                            row += " exitColliders=" + string.Join(",", hits.Select(x => x.name));
                            if (NavMesh.SamplePosition(vehicle.DoorOutside, out var hit, .45f, NavMesh.AllAreas))
                                row += " navExit=" + hit.position.ToString("F3");
                        }
                        File.AppendAllText(Path.Combine(game.QaDirectory, "vehicle-rides-state.txt"), row + "\n");
                    }
                    foreach (var vehicle in transit.Vehicles)
                    {
                        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                        var route = (Vector3[])typeof(CityPublicTransportVehicle).GetField("route", flags)
                            .GetValue(vehicle);
                        int next = (int)typeof(CityPublicTransportVehicle).GetField("next", flags).GetValue(vehicle);
                        Vector3 target = route[next];
                        Vector3 direction = (target - vehicle.transform.position).normalized;
                        Vector3 nose = vehicle.transform.position + direction *
                            (vehicle.name == "CityTaxi" ? 2 : 4.4f);
                        var hits = Physics.SphereCastAll(nose + Vector3.up * 1.25f, 1.05f,
                            direction, .8f, ~0, QueryTriggerInteraction.Ignore)
                            .Where(x => !x.collider.transform.IsChildOf(vehicle.transform));
                        string row = Time.realtimeSinceStartup.ToString("F2") + " " + vehicle.name
                            + " position=" + vehicle.transform.position.ToString("F3")
                            + " forward=" + vehicle.transform.forward.ToString("F3")
                            + " stop=" + vehicle.AtStop + " speed=" + vehicle.Speed
                            + " board=" + vehicle.Boardings + " alight=" + vehicle.Alightings
                            + " target=" + target.ToString("F3") + " direction=" + direction.ToString("F3")
                            + " blocked=" + typeof(CityPublicTransportVehicle).GetMethod("Blocked", flags)
                                .Invoke(vehicle, new object[] { direction, 0f })
                            + " noseHits=" + string.Join(",", hits.Select(x => x.collider.name));
                        File.AppendAllText(Path.Combine(game.QaDirectory, "vehicle-rides-state.txt"), row + "\n");
                    }
                }
                if (!captured && rail.Trains.Any(x => x.DoorsOpen && x.PassengerCount > 0))
                {
                    var train = rail.Trains.First(x => x.DoorsOpen && x.PassengerCount > 0);
                    game.CameraRig.FocusOffset = train.DoorOutside - game.Player.transform.position;
                    game.CameraRig.Distance = 10;
                    game.CameraRig.Snap();
                    yield return Capture("rail-live-boarding.png");
                    yield return CapturePassengerMotion("rail-passenger-motion", train.DoorOutside);
                    captured = true;
                }
                if (rail.Trains.All(x => x.Boardings > 0 && x.Alightings > 0)
                    && transit.Vehicles.All(x => x.Boardings > 0 && x.Alightings > 0)
                    && road.All(x => x.Boardings > 0 && x.Alightings > 0)) break;
                yield return null;
            }
            var facts = new VehicleLiveFacts
            {
                trains = rail.Trains.Select(x => new VehicleRideFact
                {
                    name = x.name, boardings = x.Boardings, alightings = x.Alightings, distance = x.Travelled
                }).ToArray(),
                publicVehicles = transit.Vehicles.Select(x => new VehicleRideFact
                {
                    name = x.name, boardings = x.Boardings, alightings = x.Alightings
                }).ToArray(),
                roadVehicles = road.Select(x => new VehicleRideFact
                {
                    name = x.GetComponent<CityVehicleRig>().Kind.ToString(),
                    boardings = x.Boardings, alightings = x.Alightings
                }).ToArray(),
                worldErrors = game.RuntimeErrors.Count
            };
            File.WriteAllText(Path.Combine(game.QaDirectory, "vehicle-live-facts.json"), JsonUtility.ToJson(facts, true));
            foreach (var train in rail.Trains)
                Check(train.Boardings > 0 && train.Alightings > 0 && train.Travelled > 30,
                    "V8: train boards travels and alights " + train.Track);
            foreach (var vehicle in transit.Vehicles)
                Check(vehicle.Boardings > 0 && vehicle.Alightings > 0,
                    "V8: public vehicle completes a ride " + vehicle.name);
            foreach (var vehicle in road)
                Check(vehicle.Boardings > 0 && vehicle.Alightings > 0,
                    "V8: road vehicle completes a ride " + vehicle.GetComponent<CityVehicleRig>().Kind);
            game.CameraRig.FocusOffset = Vector3.zero;
            game.CameraRig.Distance = 15;
            game.Player.CanControl = true;
        }

        [Serializable]
        sealed class VehicleLiveFacts
        {
            public VehicleRideFact[] trains, publicVehicles, roadVehicles;
            public int worldErrors;
        }

        [Serializable]
        sealed class VehicleRideFact
        {
            public string name;
            public int boardings, alightings;
            public float distance;
        }
    }
}
