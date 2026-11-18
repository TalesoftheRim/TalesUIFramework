using Verse;

namespace TalesUIFramework
{
    public class TalesUISettings : ModSettings
    {
        private static TalesUISettings _instance;

        public TalesUISettings()
        {
            _instance = this;
        }

        public bool showUI = true;
        public bool useCompactPanel = true;

        public static bool ShowUI
        {
            get => _instance == null || _instance.showUI;
            set
            {
                if (_instance != null && _instance.showUI != value)
                {
                    _instance.showUI = value;
                }
            }
        }

        public static bool UseCompactPanel
        {
            get => _instance == null || _instance.useCompactPanel;
            set
            {
                if (_instance != null && _instance.useCompactPanel != value)
                {
                    _instance.useCompactPanel = value;
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref showUI, "showUI", defaultValue: true);
            Scribe_Values.Look(ref useCompactPanel, "useCompactPanel", defaultValue: true);
        }
    }
}
