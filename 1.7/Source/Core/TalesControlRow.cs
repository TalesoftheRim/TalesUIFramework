using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TalesUIFramework
{
    public sealed class TalesControlRow
    {
        public string Id { get; }
        public float Order { get; }
        public string Title { get; }

        // Left column (Toggle)
        public Texture2D ToggleIcon { get; }
        public string ToggleTooltip { get; }
        public Func<bool> IsEnabled { get; }
        public Action<bool> SetEnabled { get; }
        public string FallbackToggleLabel { get; }

        // Right column (Direction / Stance selection)
        public IReadOnlyList<TalesControlOption> Options { get; }
        public Func<string> GetSelectedOptionId { get; }
        public Action<string> SelectOption { get; }
        public string SelectionTooltip { get; }
        public string SelectionDisabledTooltip { get; }
        public Func<string> FallbackSelectionLabel { get; }

        public TalesControlRow(
            string id,
            float order,
            string title,
            Texture2D toggleIcon,
            string toggleTooltip,
            Func<bool> isEnabled,
            Action<bool> setEnabled,
            IReadOnlyList<TalesControlOption> options,
            Func<string> getSelectedOptionId,
            Action<string> selectOption,
            string selectionTooltip,
            string selectionDisabledTooltip = null,
            string fallbackToggleLabel = null,
            Func<string> fallbackSelectionLabel = null)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("Row id must not be null or empty.", nameof(id));
            }

            Id = id;
            Order = order;
            Title = title ?? id;
            ToggleIcon = toggleIcon ?? BaseContent.BadTex;
            ToggleTooltip = toggleTooltip ?? Title;
            IsEnabled = isEnabled ?? (() => false);
            SetEnabled = setEnabled ?? (_ => { });
            Options = options ?? Array.Empty<TalesControlOption>();
            GetSelectedOptionId = getSelectedOptionId ?? (() => null);
            SelectOption = selectOption ?? (_ => { });
            SelectionTooltip = selectionTooltip ?? Title;
            SelectionDisabledTooltip = selectionDisabledTooltip;
            FallbackToggleLabel = fallbackToggleLabel;
            FallbackSelectionLabel = fallbackSelectionLabel;
        }

        public TalesControlOption GetCurrentOption()
        {
            if (Options.Count == 0)
            {
                return null;
            }

            string selectedId = GetSelectedOptionId();
            if (!string.IsNullOrEmpty(selectedId))
            {
                for (int i = 0; i < Options.Count; i++)
                {
                    if (Options[i].Id == selectedId)
                    {
                        return Options[i];
                    }
                }
            }

            return Options[0];
        }

        public TalesControlOption GetNextOption()
        {
            if (Options.Count == 0)
            {
                return null;
            }

            string selectedId = GetSelectedOptionId();
            int currentIndex = -1;
            if (!string.IsNullOrEmpty(selectedId))
            {
                for (int i = 0; i < Options.Count; i++)
                {
                    if (Options[i].Id == selectedId)
                    {
                        currentIndex = i;
                        break;
                    }
                }
            }

            int nextIndex = (currentIndex + 1) % Options.Count;
            return Options[nextIndex];
        }

        public string BuildToggleTooltip()
        {
            string header = "TalesUIFramework.UI.ToggleHeader".Translate(Title).ToString();
            string stateText = (IsEnabled()
                ? "TalesUIFramework.UI.StateOn".Translate()
                : "TalesUIFramework.UI.StateOff".Translate()).ToString();
            string currentLine = "TalesUIFramework.UI.CurrentValue".Translate(stateText).ToString();
            return $"{header}: {ToggleTooltip}\n{currentLine}";
        }

        public string BuildSelectionTooltip(bool enabled, TalesControlOption currentOption)
        {
            string currentLabel = currentOption?.Label ?? "-";
            string currentLine = "TalesUIFramework.UI.CurrentValue".Translate(currentLabel).ToString();

            if (!enabled)
            {
                string disabledHint = !string.IsNullOrEmpty(SelectionDisabledTooltip)
                    ? SelectionDisabledTooltip
                    : "TalesUIFramework.UI.DisabledHint".Translate().ToString();
                return $"{Title}\n{SelectionTooltip}\n{currentLine}\n{disabledHint}";
            }

            string clickHint = "TalesUIFramework.UI.ClickHint".Translate().ToString();
            return $"{Title}\n{SelectionTooltip}\n{currentLine}\n{clickHint}";
        }
    }
}
