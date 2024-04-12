using UnityEngine;
using Verse;

namespace FrameRateControl
{
    [StaticConstructorOnStartup]
    public class FrameRateControlMod : Mod
    {
        public FrameRateControlMod(ModContentPack content) : base(content)
        {
            GetSettings<Settings>();

            QualitySettings.vSyncCount = 0;

            Settings.Apply();
        }

        public override string SettingsCategory() => "Frame Rate Control";
        public override void DoSettingsWindowContents(Rect inRect) => Settings.DoSettingsWindowContents(inRect);
    }

    class Settings : ModSettings
    {
        public static float targetFrameRate = 60;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref targetFrameRate, "targetFrameRate", 60);
        }

        public static void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard list = new Listing_Standard(GameFont.Small) {
                ColumnWidth = inRect.width
            };

            list.Begin(inRect);
            list.Label("Target Frame Rate:" + (Application.targetFrameRate == 0 ? "No Limit" : Application.targetFrameRate.ToString()), -1, "Default 60");
            targetFrameRate = list.Slider(targetFrameRate, 0f, 300f);
            list.End();

            Apply();
        }

        public static void Apply()
        {
            if (targetFrameRate <= 0 || targetFrameRate >= 300) {
                targetFrameRate = Application.targetFrameRate = 0;
            } else {
                Application.targetFrameRate = (int) ((targetFrameRate + 2.5) / 5) * 5;
            }
        }
    }
}
