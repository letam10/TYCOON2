using UnityEngine;

namespace Tycoon
{
    [DefaultExecutionOrder(100)]
    public sealed class BillboardLabel : MonoBehaviour
    {
        TextMesh label;
        MeshRenderer mesh;
        Station station;
        bool quantity;
        string stationTitle, stationPrompt, stationText;
        string measuredText;
        Font measuredFont;
        int measuredSize, measuredLines;
        FontStyle measuredStyle;
        float measuredSpacing;
        Vector2 glyphMetrics;

        void Awake()
        {
            label = GetComponent<TextMesh>();
            mesh = GetComponent<MeshRenderer>();
            station = GetComponentInParent<Station>();
            quantity = GetComponentInParent<InventoryStack>() || GetComponentInParent<CarryPresentation>();
            RefreshNow();
        }

        void OnEnable() => RefreshNow();
        void LateUpdate() => RefreshNow();

        public void RefreshNow()
        {
            using var timing = QaFrameProbe.Measure("BillboardLabel.RefreshNow");
            if (!label || !mesh) return;
            var camera = Camera.main;
            if (!camera)
            {
                mesh.enabled = false;
                return;
            }
            transform.rotation = camera.transform.rotation;
            mesh.enabled = true;
            if (station && station.StatusLabel == label)
            {
                var player = GameSession.Instance ? GameSession.Instance.Player : null;
                mesh.enabled = player && (player.transform.position - station.InteractionPoint).sqrMagnitude < 12.25f;
                string title = station is StationZone zone && zone.Target ? zone.Target.Label : station.Label;
                string detail = station.Prompt;
                if (detail.StartsWith(title)) detail = detail.Substring(title.Length).Trim(' ', '•', '\n');
                int separator = detail.IndexOf(" • ", System.StringComparison.Ordinal);
                if (separator >= 0) detail = detail.Substring(0, separator);
                if (detail == title) detail = "";
                if (stationTitle != title || stationPrompt != detail)
                {
                    stationTitle = title;
                    stationPrompt = detail;
                    stationText = ShortLine(title, 27) + (detail.Length > 0 ? "\n" + ShortLine(detail, 30) : "");
                }
                if (label.text != stationText) label.text = stationText;
            }
            if (!mesh.enabled || string.IsNullOrEmpty(label.text)) return;
            float distance = Vector3.Dot(transform.position - camera.transform.position, camera.transform.forward);
            if (distance <= camera.nearClipPlane)
            {
                mesh.enabled = false;
                return;
            }
            MeasureText();
            float pixels = Mathf.Min(210, camera.pixelWidth * .15f);
            float worldPerPixel = camera.orthographic
                ? camera.orthographicSize * 2 / Mathf.Max(1, camera.pixelHeight)
                : 2 * distance * Mathf.Tan(camera.fieldOfView * .5f * Mathf.Deg2Rad)
                    / Mathf.Max(1, camera.pixelHeight);
            float scale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
            float widthLimit = pixels * worldPerPixel / Mathf.Max(.001f, glyphMetrics.x * .1f * scale);
            float heightLimit = WorldTypography.LinePixels(camera, quantity) * Mathf.Min(measuredLines, 2)
                * worldPerPixel / Mathf.Max(.001f, glyphMetrics.y * .1f * scale);
            label.characterSize = Mathf.Min(widthLimit, heightLimit);
        }

        void MeasureText()
        {
            if (measuredText == label.text && measuredFont == label.font && measuredSize == label.fontSize &&
                measuredStyle == label.fontStyle && measuredSpacing == label.lineSpacing) return;
            measuredText = label.text;
            measuredFont = label.font;
            measuredSize = label.fontSize;
            measuredStyle = label.fontStyle;
            measuredSpacing = label.lineSpacing;
            // Đo ngay khi chữ đổi; kích thước theo camera vẫn được giới hạn mỗi frame.
            float longest = 0;
            float line = 0;
            float glyphHeight = 0;
            int lines = 1;
            if (label.font)
            {
                label.font.RequestCharactersInTexture(label.text, label.fontSize, label.fontStyle);
                foreach (char letter in label.text)
                {
                    if (letter == '\n')
                    {
                        longest = Mathf.Max(longest, line);
                        line = 0;
                        lines++;
                    }
                    else if (label.font.GetCharacterInfo(letter, out var glyph, label.fontSize, label.fontStyle))
                    {
                        line += glyph.advance;
                        glyphHeight = Mathf.Max(glyphHeight, glyph.maxY - glyph.minY);
                    }
                    else line += label.fontSize * .6f;
                }
            }
            longest = Mathf.Max(longest, line);
            measuredLines = lines;
            glyphMetrics = new Vector2(longest, (glyphHeight > 0 ? glyphHeight : label.fontSize) +
                (lines - 1) * label.fontSize * label.lineSpacing);
        }

        static string ShortLine(string text, int limit) =>
            text.Length <= limit ? text : text.Substring(0, limit - 1) + "…";
    }
}
