using System;
using UnityEngine;

namespace TalesUIFramework
{
    public sealed class TalesControlOption
    {
        public string Id { get; }
        public string Label { get; }
        public Texture2D Icon { get; }
        public string ShortBadge { get; }
        public string TooltipDetail { get; }

        public TalesControlOption(
            string id,
            string label,
            Texture2D icon,
            string shortBadge = null,
            string tooltipDetail = null)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("Option id must not be null or empty.", nameof(id));
            }

            Id = id;
            Label = label ?? id;
            Icon = icon;
            ShortBadge = shortBadge;
            TooltipDetail = tooltipDetail;
        }
    }
}
