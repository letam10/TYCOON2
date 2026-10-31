using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public static class CoastalTownLayout
    {
        public static readonly Vector3 IndustrialCenter = new(-200, 0, 315);
        public static readonly Vector3 IndustrialFootprint = new(180, 4, 140);
        public static readonly Vector3 IndustrialEntry = new(-280, 0, 265);
        public static readonly Vector3 IndustrialVisit = new(-145, 0, 345);
        public static readonly Vector3 IndustrialExit = new(-160, 0, 335);
        public static readonly Vector3 MarinaCenter = new(230, 0, -215);
        public static readonly Vector3 MarinaFootprint = new(180, 4, 110);
        public static readonly Vector3 MarinaEntry = new(180, 0, -230);
        public const float MarinaQuayX = 218;
        // Giao hai đoạn (-274,248)→(-235,226) và (-260,235)→(-280,265): t=160/730.
        public static readonly Vector3 ForestIndustryCrossing = Vector3.Lerp(
            new Vector3(-274, 0, 248), new Vector3(-235, 0, 226), 160f / 730f);

        public static readonly IReadOnlyList<Vector3> IndustrialRoad = Array.AsReadOnly(new[]
        {
            new Vector3(-195, 0, 155), new Vector3(-218, 0, 200), new Vector3(-260, 0, 235),
            IndustrialEntry, new Vector3(-250, 0, 285), new Vector3(-215, 0, 310),
            new Vector3(-178, 0, 329), IndustrialVisit
        });

        public static readonly IReadOnlyList<Vector3> IndustrialWalk = Array.AsReadOnly(new[]
        {
            new Vector3(-190.1f, 0, 150.1f), new Vector3(-210, 0, 200),
            new Vector3(-252, 0, 240), new Vector3(-280, 0, 245), IndustrialEntry,
            new Vector3(-280, 0, 285), new Vector3(-250, 0, 300),
            new Vector3(-211, 0, 325), new Vector3(-175, 0, 345), IndustrialVisit
        });

        public static readonly IReadOnlyList<Vector3> MarinaDock = Array.AsReadOnly(new[]
        {
            MarinaEntry, new Vector3(164, 0, -240), new Vector3(164, 0, -247),
            new Vector3(174, 0, -247), new Vector3(250, 0, -247), new Vector3(280, 0, -247)
        });

        public static readonly IReadOnlyList<Vector3> MarinaWalk = Array.AsReadOnly(new[]
        {
            new Vector3(185.1f, 0, -107.1f), new Vector3(184, 0, -140),
            new Vector3(198, 0, -175), new Vector3(201, 0, -205), MarinaEntry,
            MarinaDock[1], MarinaDock[2], MarinaDock[3], MarinaDock[4], MarinaDock[5]
        });
    }
}
