using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TalesUIFramework
{
    public static class TalesFallbackGizmos
    {
        public static IEnumerable<Gizmo> BuildVanillaGizmos(List<TalesControlRow> rows)
        {
            if (rows == null || rows.Count == 0)
            {
                yield break;
            }

            for (int i = 0; i < rows.Count; i++)
            {
                TalesControlRow row = rows[i];
                bool enabled = row.IsEnabled();

                Command_Toggle toggleGizmo = new()
                {
                    defaultLabel = !string.IsNullOrEmpty(row.FallbackToggleLabel) ? row.FallbackToggleLabel : row.Title,
                    defaultDesc = row.ToggleTooltip,
                    icon = row.ToggleIcon,
                    isActive = row.IsEnabled,
                    toggleAction = () => row.SetEnabled(!row.IsEnabled()),
                    Order = row.Order
                };
                yield return toggleGizmo;

                if (enabled && row.Options.Count > 0)
                {
                    TalesControlOption currentOption = row.GetCurrentOption();
                    string actionLabel = row.FallbackSelectionLabel?.Invoke()
                        ?? currentOption?.Label
                        ?? row.Title;

                    Command_Action actionGizmo = new()
                    {
                        defaultLabel = actionLabel,
                        defaultDesc = row.SelectionTooltip,
                        icon = currentOption?.Icon ?? row.ToggleIcon,
                        Order = row.Order + 0.01f,
                        action = () => OpenSelectionMenu(row)
                    };
                    yield return actionGizmo;
                }
            }
        }

        private static void OpenSelectionMenu(TalesControlRow row)
        {
            List<FloatMenuOption> menuOptions = new(row.Options.Count);
            for (int i = 0; i < row.Options.Count; i++)
            {
                TalesControlOption option = row.Options[i];
                string optionId = option.Id;
                FloatMenuOption item = option.Icon != null
                    ? new FloatMenuOption(option.Label, () => row.SelectOption(optionId), option.Icon, Color.white)
                    : new FloatMenuOption(option.Label, () => row.SelectOption(optionId));
                menuOptions.Add(item);
            }

            Find.WindowStack.Add(new FloatMenu(menuOptions));
        }
    }
}
