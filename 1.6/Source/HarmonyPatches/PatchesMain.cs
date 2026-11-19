using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TalesUIFramework
{
    [StaticConstructorOnStartup]
    public static class PatchesMain
    {
        static PatchesMain()
        {
            Harmony harmony = new("TalesoftheRim.TalesUIFramework");

            harmony.Patch(
                original: AccessTools.Method(typeof(Pawn), nameof(Pawn.GetGizmos)),
                postfix: new HarmonyMethod(typeof(PatchesMain), nameof(Pawn_GetGizmos_Postfix)));

            harmony.Patch(
                original: AccessTools.Method(typeof(PlaySettings), nameof(PlaySettings.DoPlaySettingsGlobalControls)),
                postfix: new HarmonyMethod(typeof(PatchesMain), nameof(PlaySettings_DoPlaySettingsGlobalControls_Postfix)));
        }

        public static IEnumerable<Gizmo> Pawn_GetGizmos_Postfix(IEnumerable<Gizmo> __result, Pawn __instance)
        {
            if (__instance != null
                && !__instance.Destroyed
                && TalesUISettings.ShowUI
                && (__instance.Faction is { IsPlayer: true } || DebugSettings.godMode))
            {
                List<TalesControlRow> rows = TalesUIRegistry.CollectControlsForPawn(__instance);
                if (rows.Count > 0)
                {
                    if (TalesUISettings.UseCompactPanel)
                    {
                        yield return new Gizmo_TalesControlPanel(__instance, rows);
                    }
                    else
                    {
                        foreach (Gizmo fallbackGizmo in TalesFallbackGizmos.BuildVanillaGizmos(rows))
                        {
                            yield return fallbackGizmo;
                        }
                    }
                }
            }

            if (__result != null)
            {
                foreach (Gizmo gizmo in __result)
                {
                    yield return gizmo;
                }
            }
        }

        public static void PlaySettings_DoPlaySettingsGlobalControls_Postfix(WidgetRow row, bool worldView)
        {
            if (worldView || row == null)
            {
                return;
            }

            bool showUI = TalesUISettings.ShowUI;
            bool prev = showUI;
            row.ToggleableIcon(
                ref showUI,
                TalesGraphics.TogglePanelIcon,
                "TalesUIFramework.PlaySettingsToggle".Translate(),
                SoundDefOf.Mouseover_ButtonToggle);

            if (showUI != prev)
            {
                TalesUISettings.ShowUI = showUI;
            }
        }
    }
}
