using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Tycoon
{
    // Chỉ ghi khi lượt chơi QA bị chặn, không quét collider mỗi frame.
    public static class CityCollisionEvidence
    {
        [Serializable]
        sealed class Obstacle
        {
            public string name;
            public Vector3 center, size;
        }

        [Serializable]
        sealed class Report
        {
            public Vector3 position, destination, corner;
            public Vector3[] path;
            public float stepHeight;
            public Obstacle[] obstacles;
        }

        public static void Save(GameSession game, Vector3 destination, Vector3 corner, Vector3[] path)
        {
            var position = game.Player.transform.position;
            var report = new Report
            {
                position = position,
                destination = destination,
                corner = corner,
                path = path,
                stepHeight = game.Player.Controller.stepOffset,
                obstacles = Physics.OverlapSphere(position + Vector3.up * .8f, 2)
                    .Where(x => !x.isTrigger && x != game.Player.Controller)
                    .Select(x => new Obstacle
                    {
                        name = x.name,
                        center = x.bounds.center,
                        size = x.bounds.size
                    }).ToArray()
            };
            File.WriteAllText(Path.Combine(game.QaDirectory, "blocked-walk.json"),
                JsonUtility.ToJson(report, true));
        }
    }
}
