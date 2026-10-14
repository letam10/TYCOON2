namespace Tycoon
{
    internal static class StationLabelPresentation
    {
        public static void Refresh(Station station, bool visible)
        {
            var label = station.StatusLabel;
            // Chỉ tạo chữ đang nhìn thấy; cập nhật trước khi bật lại để tránh hiện nội dung cũ.
            if (visible)
            {
                using var probe = QaFrameProbe.Measure("Station.Label.VisiblePrompt");
                string prompt = station.Prompt;
                if (label.text != prompt) label.text = prompt;
            }
            var labelObject = label.gameObject;
            if (labelObject.activeSelf != visible) labelObject.SetActive(visible);
        }
    }
}
