using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Tycoon
{
    public static class CityMobilityEvidence
    {
        [Serializable]
        sealed class VehicleFact
        {
            public string name;
            public int boarded, alighted, aboard;
            public Vector3 position;
        }

        [Serializable]
        sealed class WorkerFact
        {
            public string id, role, reason;
            public int carried;
            public bool retiring;
            public Vector3 position;
        }

        [Serializable]
        sealed class MobilityFacts
        {
            public bool passed;
            public int activeWorkers, retiringWorkers, pendingRetirements, professions;
            public float capability;
            public VehicleFact[] vehicles;
            public WorkerFact[] workers;
            public bool walkingPassengersHaveCollision;
        }

        public static bool Save(GameSession game)
        {
            var transit = UnityEngine.Object.FindAnyObjectByType<CityPublicTransport>();
            var vehicles = transit ? transit.Vehicles.Select(x => new VehicleFact
            {
                name = x.name,
                boarded = x.Boardings,
                alighted = x.Alightings,
                aboard = x.PassengerCount,
                position = x.transform.position
            }).ToArray() : Array.Empty<VehicleFact>();
            var passengers = UnityEngine.Object.FindObjectsByType<CityPublicTransportPassenger>();
            bool collision = passengers.Where(x => x.Phase is CityPassengerPhase.Walking or
                CityPassengerPhase.Approaching).All(x => x.GetComponent<CharacterController>()?.enabled == true);
            var facts = new MobilityFacts
            {
                activeWorkers = game.ActiveWorkerCount,
                retiringWorkers = game.RetiringWorkerCount,
                pendingRetirements = game.PendingWorkerRetirements,
                professions = game.Transactions.View.crews.Count,
                capability = WorkforceRules.Capability,
                walkingPassengersHaveCollision = collision,
                vehicles = vehicles,
                workers = game.Workers.Select(x => new WorkerFact
                {
                    id = x.WorkerId,
                    role = x.Role,
                    reason = x.Reason,
                    carried = x.Carry.Total,
                    retiring = x.Retiring,
                    position = x.transform.position
                }).ToArray()
            };
            facts.passed = facts.activeWorkers == WorkforceRules.MaximumActive && facts.professions == 26 &&
                facts.pendingRetirements == 0 && facts.retiringWorkers == 0 && collision &&
                vehicles.Length == 3 && vehicles.All(x => x.boarded > 0 && x.alighted > 0);
            File.WriteAllText(Path.Combine(game.QaDirectory, "city-mobility-evidence.json"),
                JsonUtility.ToJson(facts, true));
            return facts.passed;
        }
    }
}
