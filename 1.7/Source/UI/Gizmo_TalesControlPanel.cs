using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TalesUIFramework
{
    public class Gizmo_TalesControlPanel : Command
    {
        public const float MinGizmoSize = 75f;
        public const float MaxPanelHeight = InspectPaneUtility.PaneHeight - 25f; // 140f: stays below Left Info Panel height (165f)
        public const float ContentPadding = 6f;
        public const float GroupPadding = 2f;
        public const float ButtonSize = 60f;
        public const float ColumnGap = 8f;
        public const float RowGap = 4f;
        public const float StatusBadgeSize = 18f;

        private static readonly Color GroupBaseBg = new(0.06f, 0.06f, 0.08f, 0.65f);
        private static readonly Color GroupBaseBorder = new(0.25f, 0.26f, 0.29f, 0.80f);
        private static readonly Color GroupActiveBorder = new(0.32f, 0.54f, 0.35f, 0.90f);
        private static readonly Color GroupMixedBorder = new(0.58f, 0.50f, 0.24f, 0.90f);

        private static readonly Color SlotBaseBg = new(0.11f, 0.11f, 0.13f, 0.92f);
        private static readonly Color SlotMouseOverBg = new(0.24f, 0.23f, 0.17f, 0.96f);
        private static readonly Color SlotDisabledBg = new(0.08f, 0.08f, 0.09f, 0.80f);

        private static readonly Color SlotBaseBorder = new(0.36f, 0.36f, 0.40f, 0.95f);
        private static readonly Color SlotActiveBorder = new(0.38f, 0.65f, 0.40f, 0.95f);
        private static readonly Color SlotMixedBorder = new(0.75f, 0.64f, 0.28f, 0.95f);
        private static readonly Color SlotMouseOverBorder = new(0.82f, 0.78f, 0.48f, 1.0f);
        private static readonly Color SlotDisabledBorder = new(0.22f, 0.22f, 0.24f, 0.70f);

        private static readonly Color DisabledIconColor = new(0.42f, 0.42f, 0.42f, 0.45f);
        private static readonly Color OffToggleIconColor = new(0.70f, 0.70f, 0.70f, 0.90f);
        private static readonly Color DisabledBadgeTextColor = new(0.55f, 0.55f, 0.55f, 0.55f);
        private static readonly Color ShadowColor = new(0f, 0f, 0f, 0.85f);

        private enum InteractionRow
        {
            None,
            Toggle,
            Selection
        }

        private readonly Pawn _pawn;
        private readonly List<TalesControlRow> _ownRows;
        private readonly Dictionary<string, TalesControlRow> _ownRowsById = new();
        private readonly List<TalesControlRow> _displayColumns = new();
        private readonly Dictionary<string, TalesControlRow> _displayColumnsById = new();
        private readonly List<Gizmo_TalesControlPanel> _mergedPanels = new();

        private string _pendingColumnId;
        private InteractionRow _pendingRow = InteractionRow.None;
        private int _pendingMouseButton = -1;
        private Event _pendingEvent;

        public string RowSignature { get; }

        public Gizmo_TalesControlPanel(Pawn pawn, List<TalesControlRow> rows)
        {
            _pawn = pawn;
            _ownRows = rows ?? new List<TalesControlRow>();
            _mergedPanels.Add(this);

            StringBuilder sig = new();
            for (int i = 0; i < _ownRows.Count; i++)
            {
                TalesControlRow row = _ownRows[i];
                if (i > 0)
                {
                    sig.Append('|');
                }
                sig.Append(row.Id);

                if (!_ownRowsById.ContainsKey(row.Id))
                {
                    _ownRowsById[row.Id] = row;
                }
                if (!_displayColumnsById.ContainsKey(row.Id))
                {
                    _displayColumns.Add(row);
                    _displayColumnsById[row.Id] = row;
                }
            }
            RowSignature = sig.ToString();

            defaultLabel = "TalesUIFramework.UI.PanelLabel".Translate();
            tutorTag = "TalesUIFramework_ControlPanel";
            shrinkable = false;
            groupable = true;
            activateSound = null;
            Order = -10000f;
        }

        public override float GetWidth(float maxWidth)
        {
            return Mathf.Min(CalculatePreferredWidth(), maxWidth);
        }

        private float CalculatePreferredWidth()
        {
            if (_displayColumns.Count == 0)
            {
                return MinGizmoSize;
            }

            float columnsWidth = _displayColumns.Count * ButtonSize + (_displayColumns.Count - 1) * ColumnGap;
            return Mathf.Max(MinGizmoSize, (ContentPadding + GroupPadding) * 2f + columnsWidth);
        }

        public override bool GroupsWith(Gizmo other)
        {
            return other is Gizmo_TalesControlPanel;
        }

        public override void MergeWith(Gizmo other)
        {
            if (other is not Gizmo_TalesControlPanel otherPanel || ReferenceEquals(otherPanel, this))
            {
                return;
            }

            for (int i = 0; i < _mergedPanels.Count; i++)
            {
                if (ReferenceEquals(_mergedPanels[i], otherPanel)
                    || (_pawn != null && otherPanel._pawn != null && _pawn == otherPanel._pawn))
                {
                    return;
                }
            }

            _mergedPanels.Add(otherPanel);

            bool addedColumn = false;
            for (int i = 0; i < otherPanel._ownRows.Count; i++)
            {
                TalesControlRow candidate = otherPanel._ownRows[i];
                if (!_displayColumnsById.ContainsKey(candidate.Id))
                {
                    _displayColumns.Add(candidate);
                    _displayColumnsById[candidate.Id] = candidate;
                    addedColumn = true;
                }
            }

            if (addedColumn && _displayColumns.Count > 1)
            {
                _displayColumns.Sort((a, b) =>
                {
                    int cmp = a.Order.CompareTo(b.Order);
                    return cmp != 0 ? cmp : string.CompareOrdinal(a.Id, b.Id);
                });
            }
        }

        public override bool InheritInteractionsFrom(Gizmo other)
        {
            return false;
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            _pendingColumnId = null;
            _pendingRow = InteractionRow.None;
            _pendingMouseButton = -1;
            _pendingEvent = null;

            if (_displayColumns.Count == 0)
            {
                return new GizmoResult(GizmoState.Clear);
            }

            float width = GetWidth(maxWidth);
            int columnCount = _displayColumns.Count;
            float chromeWidth = (ContentPadding + GroupPadding) * 2f;
            float horizontalGaps = Mathf.Max(0, columnCount - 1) * ColumnGap;
            float maxSlotByWidth = Mathf.Max(24f, (width - chromeWidth - horizontalGaps) / columnCount);
            float maxSlotByHeight = (MaxPanelHeight - (ContentPadding + GroupPadding) * 2f - RowGap) * 0.5f;
            float slotSize = Mathf.Min(ButtonSize, Mathf.Min(maxSlotByWidth, maxSlotByHeight));

            float contentWidth = columnCount * slotSize + horizontalGaps;
            float groupInnerHeight = slotSize * 2f + RowGap;
            float groupTotalHeight = groupInnerHeight + GroupPadding * 2f;
            float totalHeight = Mathf.Clamp(ContentPadding * 2f + groupTotalHeight, MinGizmoSize, MaxPanelHeight);

            // Keep bottom edge aligned with the standard 75f gizmo row; expand upward bounded below Left Info Panel height.
            float bottomY = topLeft.y + MinGizmoSize;
            Rect gizmoRect = new(topLeft.x, bottomY - totalHeight, width, totalHeight);

            Widgets.DrawWindowBackground(gizmoRect);

            float startX = gizmoRect.x + (gizmoRect.width - contentWidth) * 0.5f;
            float topToggleY = gizmoRect.y + (gizmoRect.height - groupInnerHeight) * 0.5f;
            float bottomOptionY = topToggleY + slotSize + RowGap;

            int totalPanelCount = _mergedPanels.Count;

            for (int i = 0; i < columnCount; i++)
            {
                TalesControlRow displayRow = _displayColumns[i];
                float columnX = startX + i * (slotSize + ColumnGap);

                CollectColumnAggregate(
                    displayRow.Id,
                    out int matchingCount,
                    out int enabledCount,
                    out TalesControlRow firstEnabledRow,
                    out TalesControlRow firstMatchingRow,
                    out bool hasMixedOptions);

                bool anyEnabled = enabledCount > 0;
                bool allEnabled = matchingCount > 0 && enabledCount == matchingCount;
                bool isMixedToggle = anyEnabled && !allEnabled;

                TalesControlRow representativeRow = firstEnabledRow ?? firstMatchingRow ?? displayRow;
                TalesControlOption currentOption = representativeRow.GetCurrentOption();

                Rect groupRect = new(
                    columnX - GroupPadding,
                    topToggleY - GroupPadding,
                    slotSize + GroupPadding * 2f,
                    groupTotalHeight);
                Color groupBorder = allEnabled
                    ? GroupActiveBorder
                    : (isMixedToggle ? GroupMixedBorder : GroupBaseBorder);
                Widgets.DrawBoxSolidWithOutline(groupRect, GroupBaseBg, groupBorder, 1);

                Rect toggleRect = new(columnX, topToggleY, slotSize, slotSize);
                Rect selectionRect = new(columnX, bottomOptionY, slotSize, slotSize);

                DrawToggleCell(
                    toggleRect,
                    representativeRow,
                    anyEnabled,
                    allEnabled,
                    isMixedToggle,
                    matchingCount,
                    totalPanelCount,
                    enabledCount);

                DrawSelectionCell(
                    selectionRect,
                    representativeRow,
                    anyEnabled,
                    hasMixedOptions,
                    currentOption,
                    matchingCount,
                    totalPanelCount);
            }

            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            return _pendingRow != InteractionRow.None
                ? new GizmoResult(GizmoState.Interacted, _pendingEvent ?? Event.current)
                : new GizmoResult(GizmoState.Clear);
        }

        private void CollectColumnAggregate(
            string rowId,
            out int matchingCount,
            out int enabledCount,
            out TalesControlRow firstEnabledRow,
            out TalesControlRow firstMatchingRow,
            out bool hasMixedOptions)
        {
            matchingCount = 0;
            enabledCount = 0;
            firstEnabledRow = null;
            firstMatchingRow = null;
            hasMixedOptions = false;

            string firstSelectedOptionId = null;

            for (int i = 0; i < _mergedPanels.Count; i++)
            {
                TalesControlRow ownRow = _mergedPanels[i].FindOwnRow(rowId);
                if (ownRow == null)
                {
                    continue;
                }

                matchingCount++;
                firstMatchingRow ??= ownRow;

                if (!ownRow.IsEnabled())
                {
                    continue;
                }

                enabledCount++;
                firstEnabledRow ??= ownRow;

                string optionId = ownRow.GetSelectedOptionId() ?? string.Empty;
                if (firstSelectedOptionId == null)
                {
                    firstSelectedOptionId = optionId;
                }
                else if (firstSelectedOptionId != optionId)
                {
                    hasMixedOptions = true;
                }
            }
        }

        private void DrawToggleCell(
            Rect cellRect,
            TalesControlRow row,
            bool anyEnabled,
            bool allEnabled,
            bool isMixedToggle,
            int matchingCount,
            int totalPanelCount,
            int enabledCount)
        {
            bool hovered = Mouse.IsOver(cellRect);
            Color bg = hovered ? SlotMouseOverBg : SlotBaseBg;
            Color border = hovered
                ? SlotMouseOverBorder
                : (allEnabled ? SlotActiveBorder : (isMixedToggle ? SlotMixedBorder : SlotBaseBorder));

            Widgets.DrawBoxSolidWithOutline(cellRect, bg, border, 1);

            Rect iconRect = cellRect.ContractedBy(5f);
            GUI.color = anyEnabled ? Color.white : OffToggleIconColor;
            Widgets.DrawTextureFitted(iconRect, row.ToggleIcon ?? BaseContent.BadTex, 1f);

            Rect badgeRect = new(
                cellRect.xMax - StatusBadgeSize - 2f,
                cellRect.y + 2f,
                StatusBadgeSize,
                StatusBadgeSize);
            GUI.color = Color.white;
            Texture2D badgeTex = allEnabled
                ? Widgets.CheckboxOnTex
                : (isMixedToggle ? (Widgets.CheckboxPartialTex ?? Widgets.CheckboxOnTex) : Widgets.CheckboxOffTex);
            if (badgeTex != null)
            {
                GUI.DrawTexture(badgeRect, badgeTex);
            }

            TooltipHandler.TipRegion(
                cellRect,
                BuildGroupToggleTooltip(row, anyEnabled, allEnabled, matchingCount, totalPanelCount, enabledCount));
            MouseoverSounds.DoRegion(cellRect, SoundDefOf.Mouseover_Command);

            if (Widgets.ButtonInvisible(cellRect, doMouseoverSound: true) && Event.current.button == 0)
            {
                _pendingColumnId = row.Id;
                _pendingRow = InteractionRow.Toggle;
                _pendingMouseButton = Event.current.button;
                _pendingEvent = Event.current;
            }
        }

        private void DrawSelectionCell(
            Rect cellRect,
            TalesControlRow row,
            bool anyEnabled,
            bool hasMixedOptions,
            TalesControlOption currentOption,
            int matchingCount,
            int totalPanelCount)
        {
            bool hovered = anyEnabled && Mouse.IsOver(cellRect);

            Color bg;
            Color border;
            if (!anyEnabled)
            {
                bg = SlotDisabledBg;
                border = SlotDisabledBorder;
            }
            else if (hovered)
            {
                bg = SlotMouseOverBg;
                border = SlotMouseOverBorder;
            }
            else
            {
                bg = SlotBaseBg;
                border = SlotBaseBorder;
            }

            Widgets.DrawBoxSolidWithOutline(cellRect, bg, border, 1);

            Texture2D optionIcon = currentOption?.Icon ?? row.ToggleIcon ?? BaseContent.BadTex;
            string badgeText = hasMixedOptions && !string.IsNullOrEmpty(currentOption?.ShortBadge)
                ? "~"
                : currentOption?.ShortBadge;
            bool hasShortBadge = !string.IsNullOrEmpty(badgeText);
            Rect iconRect = cellRect.ContractedBy(5f);

            if (hasShortBadge)
            {
                GUI.color = anyEnabled
                    ? new Color(1f, 1f, 1f, 0.42f)
                    : new Color(0.42f, 0.42f, 0.42f, 0.22f);
                Widgets.DrawTextureFitted(iconRect, optionIcon, 1f);

                DrawShortBadge(cellRect, badgeText, anyEnabled);
            }
            else
            {
                GUI.color = anyEnabled ? Color.white : DisabledIconColor;
                Widgets.DrawTextureFitted(iconRect, optionIcon, 1f);
            }

            GUI.color = Color.white;
            TooltipHandler.TipRegion(
                cellRect,
                BuildGroupSelectionTooltip(row, anyEnabled, hasMixedOptions, currentOption, matchingCount, totalPanelCount));

            if (!anyEnabled)
            {
                return;
            }

            MouseoverSounds.DoRegion(cellRect, SoundDefOf.Mouseover_Command);

            if (Widgets.ButtonInvisible(cellRect, doMouseoverSound: true))
            {
                int button = Event.current.button;
                if (button == 0 || button == 1)
                {
                    _pendingColumnId = row.Id;
                    _pendingRow = InteractionRow.Selection;
                    _pendingMouseButton = button;
                    _pendingEvent = Event.current;
                }
            }
        }

        private static string BuildGroupToggleTooltip(
            TalesControlRow row,
            bool anyEnabled,
            bool allEnabled,
            int matchingCount,
            int totalPanelCount,
            int enabledCount)
        {
            if (totalPanelCount <= 1 && matchingCount <= 1)
            {
                return row.BuildToggleTooltip();
            }

            string header = "TalesUIFramework.UI.ToggleHeader".Translate(row.Title).ToString();
            string stateText = allEnabled
                ? "TalesUIFramework.UI.StateOn".Translate().ToString()
                : (anyEnabled
                    ? "TalesUIFramework.UI.StateMixed".Translate(enabledCount, matchingCount).ToString()
                    : "TalesUIFramework.UI.StateOff".Translate().ToString());
            string currentLine = "TalesUIFramework.UI.CurrentValue".Translate(stateText).ToString();

            if (matchingCount < totalPanelCount)
            {
                string appliesLine = "TalesUIFramework.UI.AppliesToSelected".Translate(matchingCount, totalPanelCount).ToString();
                return $"{header}: {row.ToggleTooltip}\n{currentLine}\n{appliesLine}";
            }

            return $"{header}: {row.ToggleTooltip}\n{currentLine}";
        }

        private static string BuildGroupSelectionTooltip(
            TalesControlRow row,
            bool anyEnabled,
            bool hasMixedOptions,
            TalesControlOption currentOption,
            int matchingCount,
            int totalPanelCount)
        {
            if (totalPanelCount <= 1 && matchingCount <= 1)
            {
                return row.BuildSelectionTooltip(anyEnabled, currentOption);
            }

            string currentLabel = hasMixedOptions
                ? "TalesUIFramework.UI.MixedValue".Translate().ToString()
                : (currentOption?.Label ?? "-");
            string currentLine = "TalesUIFramework.UI.CurrentValue".Translate(currentLabel).ToString();
            string hintLine = !anyEnabled
                ? (!string.IsNullOrEmpty(row.SelectionDisabledTooltip)
                    ? row.SelectionDisabledTooltip
                    : "TalesUIFramework.UI.DisabledHint".Translate().ToString())
                : "TalesUIFramework.UI.ClickHint".Translate().ToString();

            if (matchingCount < totalPanelCount)
            {
                string appliesLine = "TalesUIFramework.UI.AppliesToSelected".Translate(matchingCount, totalPanelCount).ToString();
                return $"{row.Title}\n{row.SelectionTooltip}\n{currentLine}\n{appliesLine}\n{hintLine}";
            }

            return $"{row.Title}\n{row.SelectionTooltip}\n{currentLine}\n{hintLine}";
        }

        private static void DrawShortBadge(Rect cellRect, string badgeText, bool isEnabled)
        {
            GameFont prevFont = Text.Font;
            TextAnchor prevAnchor = Text.Anchor;
            Color prevColor = GUI.color;

            Text.Font = badgeText.Length <= 1 ? GameFont.Medium : GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;

            Rect shadowRect = new(cellRect.x + 1f, cellRect.y + 1f, cellRect.width, cellRect.height);
            GUI.color = ShadowColor;
            Widgets.Label(shadowRect, badgeText);

            GUI.color = isEnabled ? Color.white : DisabledBadgeTextColor;
            Widgets.Label(cellRect, badgeText);

            GUI.color = prevColor;
            Text.Anchor = prevAnchor;
            Text.Font = prevFont;
        }

        public override void ProcessGroupInput(Event ev, List<Gizmo> group)
        {
            HandleInteraction(ev, group);
        }

        public override void ProcessInput(Event ev)
        {
            HandleInteraction(ev, new List<Gizmo> { this });
        }

        private void HandleInteraction(Event ev, List<Gizmo> group)
        {
            if (string.IsNullOrEmpty(_pendingColumnId) || _pendingRow == InteractionRow.None)
            {
                return;
            }

            List<Gizmo_TalesControlPanel> targetPanels = CollectTargetPanels(group);
            if (targetPanels.Count == 0)
            {
                ClearPendingInteraction();
                return;
            }

            int button = ev != null && ev.button >= 0 ? ev.button : _pendingMouseButton;

            if (_pendingRow == InteractionRow.Toggle)
            {
                int matchingCount = 0;
                int enabledCount = 0;
                for (int i = 0; i < targetPanels.Count; i++)
                {
                    TalesControlRow ownRow = targetPanels[i].FindOwnRow(_pendingColumnId);
                    if (ownRow != null)
                    {
                        matchingCount++;
                        if (ownRow.IsEnabled())
                        {
                            enabledCount++;
                        }
                    }
                }

                if (matchingCount > 0)
                {
                    bool allEnabled = enabledCount == matchingCount;
                    bool targetState = !allEnabled;
                    ApplyToPanels(targetPanels, _pendingColumnId, row => row.SetEnabled(targetState));
                    SoundDef sound = targetState ? SoundDefOf.Checkbox_TurnedOn : SoundDefOf.Checkbox_TurnedOff;
                    sound.PlayOneShotOnCamera();
                }
            }
            else if (_pendingRow == InteractionRow.Selection)
            {
                TalesControlRow representativeRow = null;
                bool hasMixedOptions = false;
                string firstOptionId = null;

                for (int i = 0; i < targetPanels.Count; i++)
                {
                    TalesControlRow ownRow = targetPanels[i].FindOwnRow(_pendingColumnId);
                    if (ownRow == null || !ownRow.IsEnabled())
                    {
                        continue;
                    }

                    representativeRow ??= ownRow;
                    string optId = ownRow.GetSelectedOptionId() ?? string.Empty;
                    if (firstOptionId == null)
                    {
                        firstOptionId = optId;
                    }
                    else if (firstOptionId != optId)
                    {
                        hasMixedOptions = true;
                    }
                }

                if (representativeRow != null)
                {
                    if (button == 0)
                    {
                        TalesControlOption nextOption = representativeRow.GetNextOption();
                        if (nextOption != null)
                        {
                            ApplyToPanels(targetPanels, _pendingColumnId, row =>
                            {
                                if (row.IsEnabled())
                                {
                                    row.SelectOption(nextOption.Id);
                                }
                            });
                            SoundDefOf.Click.PlayOneShotOnCamera();
                        }
                    }
                    else if (button == 1)
                    {
                        OpenSelectionMenu(representativeRow, hasMixedOptions, targetPanels);
                    }
                }
            }

            ClearPendingInteraction();
        }

        private List<Gizmo_TalesControlPanel> CollectTargetPanels(List<Gizmo> group)
        {
            List<Gizmo_TalesControlPanel> result = new(_mergedPanels.Count);

            void AddUnique(Gizmo_TalesControlPanel panel)
            {
                if (panel == null)
                {
                    return;
                }

                for (int i = 0; i < result.Count; i++)
                {
                    if (ReferenceEquals(result[i], panel)
                        || (result[i]._pawn != null && panel._pawn != null && result[i]._pawn == panel._pawn))
                    {
                        return;
                    }
                }
                result.Add(panel);
            }

            if (group != null)
            {
                for (int i = 0; i < group.Count; i++)
                {
                    if (group[i] is Gizmo_TalesControlPanel panel)
                    {
                        AddUnique(panel);
                    }
                }
            }

            for (int i = 0; i < _mergedPanels.Count; i++)
            {
                AddUnique(_mergedPanels[i]);
            }

            return result;
        }

        private static void OpenSelectionMenu(
            TalesControlRow sourceRow,
            bool hasMixedOptions,
            List<Gizmo_TalesControlPanel> targetPanels)
        {
            if (sourceRow.Options == null || sourceRow.Options.Count == 0)
            {
                return;
            }

            string currentId = hasMixedOptions ? null : sourceRow.GetSelectedOptionId();
            string rowId = sourceRow.Id;
            List<Gizmo_TalesControlPanel> panelsSnapshot = new(targetPanels);

            List<FloatMenuOption> menuOptions = new(sourceRow.Options.Count);
            for (int i = 0; i < sourceRow.Options.Count; i++)
            {
                TalesControlOption option = sourceRow.Options[i];
                bool isSelected = currentId != null && option.Id == currentId;
                string label = isSelected
                    ? "TalesUIFramework.UI.CurrentOptionMarker".Translate(option.Label).ToString()
                    : option.Label;

                string optionId = option.Id;
                FloatMenuOption menuOption = option.Icon != null
                    ? new FloatMenuOption(label, () => ApplyOptionToPanels(panelsSnapshot, rowId, optionId), option.Icon, Color.white)
                    : new FloatMenuOption(label, () => ApplyOptionToPanels(panelsSnapshot, rowId, optionId));

                menuOptions.Add(menuOption);
            }

            SoundDefOf.FloatMenu_Open.PlayOneShotOnCamera();
            Find.WindowStack.Add(new FloatMenu(menuOptions));
        }

        private static void ApplyOptionToPanels(List<Gizmo_TalesControlPanel> panels, string rowId, string optionId)
        {
            ApplyToPanels(panels, rowId, row =>
            {
                if (row.IsEnabled())
                {
                    row.SelectOption(optionId);
                }
            });
        }

        private static void ApplyToPanels(
            List<Gizmo_TalesControlPanel> panels,
            string rowId,
            Action<TalesControlRow> action)
        {
            if (panels == null)
            {
                return;
            }

            for (int i = 0; i < panels.Count; i++)
            {
                TalesControlRow row = panels[i].FindOwnRow(rowId);
                if (row != null)
                {
                    action(row);
                }
            }
        }

        private TalesControlRow FindOwnRow(string rowId)
        {
            return !string.IsNullOrEmpty(rowId) && _ownRowsById.TryGetValue(rowId, out TalesControlRow row)
                ? row
                : null;
        }

        private void ClearPendingInteraction()
        {
            _pendingColumnId = null;
            _pendingRow = InteractionRow.None;
            _pendingMouseButton = -1;
            _pendingEvent = null;
        }
    }
}
