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
        public const float ContentPadding = 4f;
        public const float IconSize = 32f;
        public const float ColumnGap = 3f;
        public const float RowGap = 3f;
        public const float PanelWidth = ContentPadding * 2f + IconSize * 2f + ColumnGap; // 75f
        public const float StatusBadgeSize = 12f;

        private static readonly Color SlotBaseBg = new(0.11f, 0.11f, 0.13f, 0.92f);
        private static readonly Color SlotMouseOverBg = new(0.24f, 0.23f, 0.17f, 0.96f);
        private static readonly Color SlotDisabledBg = new(0.08f, 0.08f, 0.09f, 0.80f);

        private static readonly Color SlotBaseBorder = new(0.36f, 0.36f, 0.40f, 0.95f);
        private static readonly Color SlotActiveBorder = new(0.38f, 0.65f, 0.40f, 0.95f);
        private static readonly Color SlotMouseOverBorder = new(0.82f, 0.78f, 0.48f, 1.0f);
        private static readonly Color SlotDisabledBorder = new(0.22f, 0.22f, 0.24f, 0.70f);

        private static readonly Color DisabledIconColor = new(0.42f, 0.42f, 0.42f, 0.45f);
        private static readonly Color OffToggleIconColor = new(0.70f, 0.70f, 0.70f, 0.90f);
        private static readonly Color DisabledBadgeTextColor = new(0.55f, 0.55f, 0.55f, 0.55f);
        private static readonly Color ShadowColor = new(0f, 0f, 0f, 0.85f);

        private enum InteractionColumn
        {
            None,
            Toggle,
            Selection
        }

        private readonly Pawn _pawn;
        private readonly List<TalesControlRow> _rows;

        private string _pendingRowId;
        private InteractionColumn _pendingColumn = InteractionColumn.None;
        private int _pendingMouseButton = -1;
        private Event _pendingEvent;

        public string RowSignature { get; }

        public Gizmo_TalesControlPanel(Pawn pawn, List<TalesControlRow> rows)
        {
            _pawn = pawn;
            _rows = rows ?? new List<TalesControlRow>();

            StringBuilder sig = new();
            for (int i = 0; i < _rows.Count; i++)
            {
                if (i > 0)
                {
                    sig.Append('|');
                }
                sig.Append(_rows[i].Id);
            }
            RowSignature = sig.ToString();

            defaultLabel = "TalesUIFramework.UI.PanelLabel".Translate();
            tutorTag = "TalesUIFramework_ControlPanel";
            shrinkable = false;
            groupable = true;
            activateSound = null;
            Order = 0f;
        }

        public override float GetWidth(float maxWidth)
        {
            return Mathf.Min(PanelWidth, maxWidth);
        }

        public override bool GroupsWith(Gizmo other)
        {
            return other is Gizmo_TalesControlPanel otherPanel && otherPanel.RowSignature == RowSignature;
        }

        public override bool InheritInteractionsFrom(Gizmo other)
        {
            return false;
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            _pendingRowId = null;
            _pendingColumn = InteractionColumn.None;
            _pendingMouseButton = -1;
            _pendingEvent = null;

            if (_rows.Count == 0)
            {
                return new GizmoResult(GizmoState.Clear);
            }

            float width = GetWidth(maxWidth);
            float contentHeight = _rows.Count * IconSize + (_rows.Count - 1) * RowGap;
            float totalHeight = Mathf.Max(MinGizmoSize, ContentPadding * 2f + contentHeight);

            // Keep bottom edge aligned with the standard 75f gizmo row; expand upward for 3-4 rows.
            float bottomY = topLeft.y + MinGizmoSize;
            Rect gizmoRect = new(topLeft.x, bottomY - totalHeight, width, totalHeight);

            Widgets.DrawWindowBackground(gizmoRect);

            float startX = gizmoRect.x + (gizmoRect.width - (IconSize * 2f + ColumnGap)) * 0.5f;
            float startY = gizmoRect.y + (gizmoRect.height - contentHeight) * 0.5f;

            for (int i = 0; i < _rows.Count; i++)
            {
                TalesControlRow row = _rows[i];
                float rowY = startY + i * (IconSize + RowGap);

                Rect toggleRect = new(startX, rowY, IconSize, IconSize);
                Rect selectionRect = new(startX + IconSize + ColumnGap, rowY, IconSize, IconSize);

                bool isEnabled = row.IsEnabled();
                TalesControlOption currentOption = row.GetCurrentOption();

                DrawToggleCell(toggleRect, row, isEnabled);
                DrawSelectionCell(selectionRect, row, isEnabled, currentOption);
            }

            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            return _pendingColumn != InteractionColumn.None
                ? new GizmoResult(GizmoState.Interacted, _pendingEvent ?? Event.current)
                : new GizmoResult(GizmoState.Clear);
        }

        private void DrawToggleCell(Rect cellRect, TalesControlRow row, bool isEnabled)
        {
            bool hovered = Mouse.IsOver(cellRect);
            Color bg = hovered ? SlotMouseOverBg : SlotBaseBg;
            Color border = hovered
                ? SlotMouseOverBorder
                : (isEnabled ? SlotActiveBorder : SlotBaseBorder);

            Widgets.DrawBoxSolidWithOutline(cellRect, bg, border, 1);

            Rect iconRect = cellRect.ContractedBy(3f);
            GUI.color = isEnabled ? Color.white : OffToggleIconColor;
            Widgets.DrawTextureFitted(iconRect, row.ToggleIcon ?? BaseContent.BadTex, 1f);

            Rect badgeRect = new(
                cellRect.xMax - StatusBadgeSize - 1f,
                cellRect.y + 1f,
                StatusBadgeSize,
                StatusBadgeSize);
            GUI.color = Color.white;
            Texture2D badgeTex = isEnabled ? Widgets.CheckboxOnTex : Widgets.CheckboxOffTex;
            if (badgeTex != null)
            {
                GUI.DrawTexture(badgeRect, badgeTex);
            }

            TooltipHandler.TipRegion(cellRect, row.BuildToggleTooltip());
            MouseoverSounds.DoRegion(cellRect, SoundDefOf.Mouseover_Command);

            if (Widgets.ButtonInvisible(cellRect, doMouseoverSound: true) && Event.current.button == 0)
            {
                _pendingRowId = row.Id;
                _pendingColumn = InteractionColumn.Toggle;
                _pendingMouseButton = Event.current.button;
                _pendingEvent = Event.current;
            }
        }

        private void DrawSelectionCell(Rect cellRect, TalesControlRow row, bool isEnabled, TalesControlOption currentOption)
        {
            bool hovered = isEnabled && Mouse.IsOver(cellRect);

            Color bg;
            Color border;
            if (!isEnabled)
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
            bool hasShortBadge = !string.IsNullOrEmpty(currentOption?.ShortBadge);
            Rect iconRect = cellRect.ContractedBy(3f);

            if (hasShortBadge)
            {
                GUI.color = isEnabled
                    ? new Color(1f, 1f, 1f, 0.42f)
                    : new Color(0.42f, 0.42f, 0.42f, 0.22f);
                Widgets.DrawTextureFitted(iconRect, optionIcon, 1f);

                DrawShortBadge(cellRect, currentOption.ShortBadge, isEnabled);
            }
            else
            {
                GUI.color = isEnabled ? Color.white : DisabledIconColor;
                Widgets.DrawTextureFitted(iconRect, optionIcon, 1f);
            }

            GUI.color = Color.white;
            TooltipHandler.TipRegion(cellRect, row.BuildSelectionTooltip(isEnabled, currentOption));

            if (!isEnabled)
            {
                return;
            }

            MouseoverSounds.DoRegion(cellRect, SoundDefOf.Mouseover_Command);

            if (Widgets.ButtonInvisible(cellRect, doMouseoverSound: true))
            {
                int button = Event.current.button;
                if (button == 0 || button == 1)
                {
                    _pendingRowId = row.Id;
                    _pendingColumn = InteractionColumn.Selection;
                    _pendingMouseButton = button;
                    _pendingEvent = Event.current;
                }
            }
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
            if (string.IsNullOrEmpty(_pendingRowId) || _pendingColumn == InteractionColumn.None)
            {
                return;
            }

            TalesControlRow sourceRow = FindRow(_pendingRowId);
            if (sourceRow == null)
            {
                ClearPendingInteraction();
                return;
            }

            int button = ev != null && ev.button >= 0 ? ev.button : _pendingMouseButton;

            if (_pendingColumn == InteractionColumn.Toggle)
            {
                bool targetState = !sourceRow.IsEnabled();
                ApplyToGroup(group, sourceRow.Id, row => row.SetEnabled(targetState));
                SoundDef sound = targetState ? SoundDefOf.Checkbox_TurnedOn : SoundDefOf.Checkbox_TurnedOff;
                sound.PlayOneShotOnCamera();
            }
            else if (_pendingColumn == InteractionColumn.Selection && sourceRow.IsEnabled())
            {
                if (button == 0)
                {
                    TalesControlOption nextOption = sourceRow.GetNextOption();
                    if (nextOption != null)
                    {
                        ApplyToGroup(group, sourceRow.Id, row =>
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
                    OpenSelectionMenu(sourceRow, group);
                }
            }

            ClearPendingInteraction();
        }

        private static void OpenSelectionMenu(TalesControlRow sourceRow, List<Gizmo> group)
        {
            if (sourceRow.Options == null || sourceRow.Options.Count == 0)
            {
                return;
            }

            string currentId = sourceRow.GetSelectedOptionId();
            string rowId = sourceRow.Id;
            List<Gizmo> groupSnapshot = group != null ? new List<Gizmo>(group) : new List<Gizmo>();

            List<FloatMenuOption> menuOptions = new(sourceRow.Options.Count);
            for (int i = 0; i < sourceRow.Options.Count; i++)
            {
                TalesControlOption option = sourceRow.Options[i];
                bool isSelected = option.Id == currentId;
                string label = isSelected
                    ? "TalesUIFramework.UI.CurrentOptionMarker".Translate(option.Label).ToString()
                    : option.Label;

                string optionId = option.Id;
                FloatMenuOption menuOption = option.Icon != null
                    ? new FloatMenuOption(label, () => ApplyOptionToGroup(groupSnapshot, rowId, optionId), option.Icon, Color.white)
                    : new FloatMenuOption(label, () => ApplyOptionToGroup(groupSnapshot, rowId, optionId));

                menuOptions.Add(menuOption);
            }

            SoundDefOf.FloatMenu_Open.PlayOneShotOnCamera();
            Find.WindowStack.Add(new FloatMenu(menuOptions));
        }

        private static void ApplyOptionToGroup(List<Gizmo> group, string rowId, string optionId)
        {
            ApplyToGroup(group, rowId, row =>
            {
                if (row.IsEnabled())
                {
                    row.SelectOption(optionId);
                }
            });
        }

        private static void ApplyToGroup(List<Gizmo> group, string rowId, Action<TalesControlRow> action)
        {
            if (group == null)
            {
                return;
            }

            for (int i = 0; i < group.Count; i++)
            {
                if (group[i] is Gizmo_TalesControlPanel panel)
                {
                    TalesControlRow row = panel.FindRow(rowId);
                    if (row != null)
                    {
                        action(row);
                    }
                }
            }
        }

        private TalesControlRow FindRow(string rowId)
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                if (_rows[i].Id == rowId)
                {
                    return _rows[i];
                }
            }
            return null;
        }

        private void ClearPendingInteraction()
        {
            _pendingRowId = null;
            _pendingColumn = InteractionColumn.None;
            _pendingMouseButton = -1;
            _pendingEvent = null;
        }
    }
}
