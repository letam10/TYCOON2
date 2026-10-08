using UnityEngine;

namespace Tycoon
{
    // Khử răng cưa theo độ phủ; ánh sáng từ trên trái được vẽ trực tiếp trong icon 2D.
    internal sealed class PurchaseIconCanvas
    {
        readonly Color32[] pixels;
        readonly float x;
        readonly float y;
        readonly float scale;
        const int Size = DrawnPurchaseIcons.Size;

        public PurchaseIconCanvas(Color32[] pixels, float x, float y, float scale)
        {
            this.pixels = pixels;
            this.x = x;
            this.y = y;
            this.scale = scale;
        }

        public PurchaseIconCanvas At(float px, float py, float size) =>
            new(pixels, x + px * scale, y + py * scale, scale * size);

        void Put(int px, int py, Color32 color, float coverage, float shade)
        {
            if (px < 0 || px >= Size || py < 0 || py >= Size) return;
            float alpha = color.a / 255f * Mathf.Clamp01(coverage);
            if (alpha <= 0) return;
            int index = py * Size + px;
            Color previous = pixels[index];
            Color next = color;
            float finalAlpha = alpha + previous.a * (1 - alpha);
            float old = previous.a * (1 - alpha);
            pixels[index] = new Color((next.r * shade * alpha + previous.r * old) / finalAlpha,
                (next.g * shade * alpha + previous.g * old) / finalAlpha,
                (next.b * shade * alpha + previous.b * old) / finalAlpha, finalAlpha);
        }

        public void Rect(float px, float py, float width, float height, Color32 color)
        {
            float left = x + px * scale;
            float bottom = y + py * scale;
            float right = left + width * scale;
            float top = bottom + height * scale;
            for (int iy = Mathf.FloorToInt(bottom); iy < Mathf.CeilToInt(top); iy++)
            {
                for (int ix = Mathf.FloorToInt(left); ix < Mathf.CeilToInt(right); ix++)
                {
                    float cx = Mathf.Min(ix + 1, right) - Mathf.Max(ix, left);
                    float cy = Mathf.Min(iy + 1, top) - Mathf.Max(iy, bottom);
                    float shade = .82f + .25f * (iy - bottom) / Mathf.Max(1, top - bottom);
                    Put(ix, iy, color, cx * cy, shade);
                }
            }
        }

        public void Circle(float px, float py, float radius, Color32 color) =>
            Ellipse(px, py, radius, radius, color);

        public void Ellipse(float px, float py, float rx, float ry, Color32 color)
        {
            float cx = x + px * scale;
            float cy = y + py * scale;
            rx *= scale;
            ry *= scale;
            for (int iy = Mathf.FloorToInt(cy - ry); iy <= Mathf.CeilToInt(cy + ry); iy++)
            {
                for (int ix = Mathf.FloorToInt(cx - rx); ix <= Mathf.CeilToInt(cx + rx); ix++)
                {
                    float dx = (ix + .5f - cx) / rx;
                    float dy = (iy + .5f - cy) / ry;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float coverage = Mathf.Clamp01((1 - distance) * Mathf.Min(rx, ry) + .5f);
                    float shade = .9f + dy * .13f - dx * .07f + (1 - distance) * .07f;
                    Put(ix, iy, color, coverage, shade);
                }
            }
        }

        public void Line(float ax, float ay, float bx, float by, float width, Color32 color)
        {
            Vector2 a = new(x + ax * scale, y + ay * scale);
            Vector2 b = new(x + bx * scale, y + by * scale);
            Vector2 delta = b - a;
            float radius = width * scale * .5f;
            for (int iy = Mathf.FloorToInt(Mathf.Min(a.y, b.y) - radius);
                iy <= Mathf.CeilToInt(Mathf.Max(a.y, b.y) + radius); iy++)
            {
                for (int ix = Mathf.FloorToInt(Mathf.Min(a.x, b.x) - radius);
                    ix <= Mathf.CeilToInt(Mathf.Max(a.x, b.x) + radius); ix++)
                {
                    Vector2 point = new(ix + .5f, iy + .5f);
                    float t = delta.sqrMagnitude == 0 ? 0
                        : Mathf.Clamp01(Vector2.Dot(point - a, delta) / delta.sqrMagnitude);
                    float distance = (point - a - delta * t).magnitude;
                    Put(ix, iy, color, radius + .5f - distance, 1);
                }
            }
        }

        public void Poly(Color32 color, params Vector2[] points)
        {
            Vector2 low = new(Size, Size);
            Vector2 high = Vector2.zero;
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = new(x + points[i].x * scale, y + points[i].y * scale);
                low = Vector2.Min(low, points[i]);
                high = Vector2.Max(high, points[i]);
            }
            for (int iy = Mathf.FloorToInt(low.y); iy <= Mathf.CeilToInt(high.y); iy++)
            {
                for (int ix = Mathf.FloorToInt(low.x); ix <= Mathf.CeilToInt(high.x); ix++)
                {
                    float coverage = 0;
                    for (int sample = 0; sample < 4; sample++)
                    {
                        Vector2 p = new(ix + .25f + sample % 2 * .5f, iy + .25f + sample / 2 * .5f);
                        bool inside = false;
                        for (int i = 0, j = points.Length - 1; i < points.Length; j = i++)
                        {
                            if ((points[i].y > p.y) != (points[j].y > p.y)
                                && p.x < (points[j].x - points[i].x) * (p.y - points[i].y)
                                    / (points[j].y - points[i].y) + points[i].x) inside = !inside;
                        }
                        if (inside) coverage += .25f;
                    }
                    float shade = .82f + .24f * (iy - low.y) / Mathf.Max(1, high.y - low.y);
                    Put(ix, iy, color, coverage, shade);
                }
            }
        }
    }
}
