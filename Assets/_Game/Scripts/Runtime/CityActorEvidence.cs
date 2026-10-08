using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;

namespace Tycoon
{
    public sealed class CityActorEvidence
    {
        sealed class Track
        {
            public Vector3 position;
            public double distance;
            public float legMotion;
            public Transform[] legs;
            public Quaternion[] pose;
            public bool walkingState;
        }

        [Serializable]
        sealed class Evidence
        {
            public int observedNpcs, walkedNpcs, animatedNpcs;
            public double totalNpcDistance;
            public bool passed;
            public string mode = "live actors during ordinary gameplay";
        }

        readonly Dictionary<NavMeshAgent, Track> tracks = new();

        public void Sample(GameSession game)
        {
            foreach (var agent in UnityEngine.Object.FindObjectsByType<NavMeshAgent>(FindObjectsSortMode.None))
            {
                if (!agent.enabled || !agent.gameObject.activeInHierarchy) continue;
                var view = agent.GetComponentInChildren<ActorView>();
                if (!view || !view.Animator) continue;
                if (!tracks.TryGetValue(agent, out var track))
                {
                    var legs = new List<Transform>();
                    foreach (var bone in view.Animator.GetComponentsInChildren<Transform>())
                        if (bone.name.Contains("Leg") || bone.name.Contains("Foot")) legs.Add(bone);
                    track = new Track
                    {
                        position = agent.transform.position,
                        legs = legs.ToArray(),
                        pose = new Quaternion[legs.Count]
                    };
                    for (int i = 0; i < track.legs.Length; i++) track.pose[i] = track.legs[i].localRotation;
                    tracks.Add(agent, track);
                    continue;
                }
                Vector3 delta = agent.transform.position - track.position;
                delta.y = 0;
                track.position = agent.transform.position;
                if (delta.magnitude > 12) continue;
                track.distance += delta.magnitude;
                bool walking = Navigation.ActualSpeed(agent) > .4f
                    && view.State is "Walk" or "Run" or "CarryWalk";
                track.walkingState |= walking;
                float motion = 0;
                for (int i = 0; i < track.legs.Length; i++)
                {
                    if (!track.legs[i]) continue;
                    var rotation = track.legs[i].localRotation;
                    if (walking) motion += Quaternion.Angle(track.pose[i], rotation);
                    track.pose[i] = rotation;
                }
                track.legMotion = Mathf.Max(track.legMotion, motion);
            }
        }

        public bool Save(string directory)
        {
            var report = new Evidence { observedNpcs = tracks.Count };
            foreach (var track in tracks.Values)
            {
                report.totalNpcDistance += track.distance;
                if (track.distance > 3) report.walkedNpcs++;
                if (track.distance > 3 && track.walkingState && track.legMotion > 12) report.animatedNpcs++;
            }
            report.passed = report.walkedNpcs >= 8 && report.animatedNpcs >= 8;
            File.WriteAllText(Path.Combine(directory, "npc-motion-evidence.json"),
                JsonUtility.ToJson(report, true));
            return report.passed;
        }
    }
}
