using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public enum GameLanguage { Vietnamese, English }
    public enum GameGraphicsPreset { Low, Balanced, High }

    [Serializable]
    public sealed class GameSettingsData
    {
        public GameLanguage language = GameLanguage.Vietnamese;
        public GameGraphicsPreset quality = GameGraphicsPreset.Balanced;
        public bool showFps;
        public bool syncDisplay = true;
        public int fpsLimit = 165;
        public float renderScale = 1;
        public bool fullscreen;
        public int resolutionWidth;
        public int resolutionHeight;
        public List<KeyboardBinding> keyBindings = new();

        public void Validate()
        {
            keyBindings ??= new();
            keyBindings.RemoveAll(x => x == null || string.IsNullOrEmpty(x.id)
                || string.IsNullOrEmpty(x.path) || !x.path.StartsWith("<Keyboard>/", StringComparison.Ordinal));
            if (!Enum.IsDefined(typeof(GameLanguage), language)) language = GameLanguage.Vietnamese;
            if (!Enum.IsDefined(typeof(GameGraphicsPreset), quality)) quality = GameGraphicsPreset.Balanced;
            fpsLimit = Mathf.Clamp(fpsLimit, 30, 360);
            renderScale = float.IsNaN(renderScale) || float.IsInfinity(renderScale)
                ? 1 : Mathf.Clamp(renderScale, .5f, 1.5f);
            if (resolutionWidth < 640 || resolutionHeight < 360)
            {
                resolutionWidth = 0;
                resolutionHeight = 0;
            }
            resolutionWidth = Mathf.Min(resolutionWidth, 7680);
            resolutionHeight = Mathf.Min(resolutionHeight, 4320);
        }
    }
}
