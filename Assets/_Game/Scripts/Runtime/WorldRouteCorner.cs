using UnityEngine;

namespace Tycoon
{
    public readonly struct WorldRouteCorner
    {
        public int AnchorIndex { get; }
        public Vector3 Anchor { get; }
        public Vector3 Entry { get; }
        public Vector3 Exit { get; }
        public Vector3 Center { get; }
        public float Radius { get; }
        public float SweepRadians { get; }

        public WorldRouteCorner(int anchorIndex, Vector3 anchor, Vector3 entry,
            Vector3 exit, Vector3 center, float radius, float sweepRadians)
        {
            AnchorIndex = anchorIndex;
            Anchor = anchor;
            Entry = entry;
            Exit = exit;
            Center = center;
            Radius = radius;
            SweepRadians = sweepRadians;
        }

        public Vector3 Evaluate(float fraction)
        {
            if (fraction <= 0)
                return Entry;
            if (fraction >= 1)
                return Exit;
            float angle = SweepRadians * fraction;
            float cosine = Mathf.Cos(angle);
            float sine = Mathf.Sin(angle);
            Vector3 radial = Entry - Center;
            return new Vector3(Center.x + radial.x * cosine - radial.z * sine,
                Anchor.y, Center.z + radial.x * sine + radial.z * cosine);
        }
    }
}
