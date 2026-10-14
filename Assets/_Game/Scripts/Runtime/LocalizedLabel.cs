using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Tycoon
{
    [DefaultExecutionOrder(10000)]
    public sealed class LocalizedLabel : MonoBehaviour
    {
        static readonly List<LocalizedLabel> Active = new();
        Text uiText;
        TextMesh worldText;
        string source = "", rendered = "";
        int revision = -1;

        void Awake()
        {
            uiText = GetComponent<Text>();
            worldText = GetComponent<TextMesh>();
        }

        void OnEnable()
        {
            Active.Add(this);
            Refresh();
        }

        void OnDisable() => Active.Remove(this);

        public void SetSource(string value)
        {
            if (source == value && revision == GameSettingsStore.Revision)
            {
                Write(rendered);
                return;
            }
            source = value ?? "";
            revision = GameSettingsStore.Revision;
            rendered = GameLocalization.Translate(source);
            Write(rendered);
        }

        void LateUpdate() => Refresh();

        void Refresh()
        {
            if (!uiText && !worldText) return;
            string observed = uiText ? uiText.text : worldText.text;
            bool changedSource = observed != rendered && observed != source;
            if (changedSource) source = observed;
            if (revision != GameSettingsStore.Revision || changedSource || observed != rendered)
            {
                // Nhãn trạm được ghi lại mỗi frame: tái dùng bản dịch nếu nguồn không đổi.
                if (revision != GameSettingsStore.Revision || changedSource)
                    rendered = GameLocalization.Translate(source);
                revision = GameSettingsStore.Revision;
                Write(rendered);
            }
        }

        void Write(string value)
        {
            if (uiText && uiText.text != value) uiText.text = value;
            if (worldText && worldText.text != value) worldText.text = value;
        }

        public static void RefreshAll()
        {
            for (int i = 0; i < Active.Count; i++)
                if (Active[i]) Active[i].Refresh();
        }
    }
}
