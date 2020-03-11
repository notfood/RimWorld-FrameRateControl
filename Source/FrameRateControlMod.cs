using System;
using System.Linq;

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
            Application.targetFrameRate = (int) Settings.targetFrameRate;

            Settings.throttlingAvailable = TryToEnableThrottling();
        }

        bool TryToEnableThrottling()
        {
            var mod = LoadedModManager.RunningMods.FirstOrDefault(m => m.PackageId == "brrainz.harmony");
            if (mod == null) {
                return false;
            }

            void wrapperForSafety()
            {
                var harmony = new HarmonyLib.Harmony(Content.PackageId);
                {
                    var target = HarmonyLib.AccessTools.Method(typeof(RealTime), nameof(RealTime.Update));
                    var postfix = HarmonyLib.AccessTools.Method(typeof(FrameRateControlMod), nameof(ThrottleEngine));

                    harmony.Patch(target, postfix: new HarmonyLib.HarmonyMethod(postfix));
                }
                {
                    var target = HarmonyLib.AccessTools.Method(typeof(TickManager), nameof(TickManager.DoSingleTick));
                    var postfix = HarmonyLib.AccessTools.Method(typeof(FrameRateControlMod), nameof(SetWorstAllowedFPS));

                    harmony.Patch(target, postfix: new HarmonyLib.HarmonyMethod(postfix));
                }
            };

            try {
                wrapperForSafety();
            } catch (Exception e) {
                Log.Warning("FrameRateControl :: Despite HarmonyMod being loaded we can't patch, something went very wrong...\n" + e);
                return false;
            }

            return true;
        }

        static void ThrottleEngine()
        {
            if (!Settings.throttle) {
                return;
            }

            int snooze = (int) (Settings.targetSleepTime - Time.deltaTime);
            if (snooze > 0) {
                System.Threading.Thread.Sleep(snooze);
            }
        }

        static void SetWorstAllowedFPS(ref float ___WorstAllowedFPS)
        {
            if (Settings.throttle) {
                ___WorstAllowedFPS = Settings.targetFrameRate;
            } else {
                ___WorstAllowedFPS = 22f;
            }

        }

        public override string SettingsCategory() => "Frame Rate Control";
        public override void DoSettingsWindowContents(Rect inRect) => Settings.DoSettingsWindowContents(inRect);
    }

    class Settings : ModSettings
    {
        public static float targetFrameRate = 60;
        public static float targetSleepTime;

        public static bool throttle = true;
        public static bool throttlingAvailable;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref targetFrameRate, "targetFrameRate", 60);
            Scribe_Values.Look(ref throttle, "throttle", true);
        }

        public static void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard list = new Listing_Standard(GameFont.Small) {
                ColumnWidth = inRect.width
            };

            list.Begin(inRect);
            list.Label("Target Frame Rate:" + (Application.targetFrameRate == 0 ? "No Limit" : Application.targetFrameRate.ToString()), -1, "Default 60");
            targetFrameRate = list.Slider(targetFrameRate, 0f, 300f);
            if (throttlingAvailable) {
                list.CheckboxLabeled("Throttle Engine", ref throttle, "");
            }
            list.End();

            Application.targetFrameRate = (int) ((targetFrameRate + 2.5) / 5) * 5;
            targetSleepTime = 1000f / Application.targetFrameRate;
        }
    }
}
