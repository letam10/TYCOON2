using UnityEngine;

namespace Tycoon
{
    public sealed class UpgradeVisualMotion : MonoBehaviour
    {
        Transform[] pieces;
        Vector3[] positions;
        Vector3[] scales;
        Quaternion[] rotations;
        bool[] rotating;
        GameSession game;
        float elapsed;
        float phase;
        int speed;
        bool hasMovingParts;

        public void Initialize(GameSession session, bool animate, int speedLevel)
        {
            game = session;
            speed = speedLevel;
            int count = transform.childCount;
            pieces = new Transform[count];
            positions = new Vector3[count];
            scales = new Vector3[count];
            rotations = new Quaternion[count];
            rotating = new bool[count];
            for (int i = 0; i < count; i++)
            {
                var piece = transform.GetChild(i);
                pieces[i] = piece;
                positions[i] = piece.localPosition;
                scales[i] = piece.localScale;
                rotations[i] = piece.localRotation;
                rotating[i] = piece.name is "MotorRotor" or "SprinklerHead";
                hasMovingParts |= rotating[i];
            }
            elapsed = animate && Application.isPlaying ? 0 : 1;
            ApplyPose();
            enabled = elapsed < 1 || hasMovingParts;
        }

        void Update()
        {
            if (!game || !game.CanSimulate || pieces == null) return;
            elapsed = Mathf.Min(1, elapsed + Time.deltaTime);
            phase = Mathf.Repeat(phase + Time.deltaTime * (45 + speed * 25), 360);
            ApplyPose();
            if (elapsed >= 1 && !hasMovingParts) enabled = false;
        }

        void ApplyPose()
        {
            for (int i = 0; i < pieces.Length; i++)
            {
                if (!pieces[i]) continue;
                float delay = .22f * i / Mathf.Max(1, pieces.Length - 1);
                float t = Mathf.Clamp01((elapsed - delay) / .52f);
                float settle = Mathf.SmoothStep(0, 1, t);
                float lift = (1 - settle) * .22f;
                float scale = Mathf.Lerp(.72f, 1, settle) + Mathf.Sin(t * Mathf.PI) * .035f;
                pieces[i].localPosition = positions[i] + Vector3.up * lift;
                pieces[i].localScale = scales[i] * scale;
                if (rotating[i])
                    pieces[i].localRotation = rotations[i] * Quaternion.AngleAxis(phase, Vector3.up);
            }
        }
    }
}
