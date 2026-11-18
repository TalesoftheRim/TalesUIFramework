using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TalesUIFramework
{
    public class TalesUIFrameworkMod : Mod
    {
        private readonly TalesUISettings _settings;

        public override string SettingsCategory() => "TalesUIFramework.SettingsCategory".Translate();

        public TalesUIFrameworkMod(ModContentPack pack) : base(pack)
        {
            _settings = GetSettings<TalesUISettings>();
            Log.Message("[Tales UI Framework] Assembly loaded.");
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new();
            listing.Begin(inRect);
            listing.Gap(6f);

            listing.CheckboxLabeled(
                "TalesUIFramework.Settings.ShowUI".Translate(),
                ref _settings.showUI,
                "TalesUIFramework.Settings.ShowUIDesc".Translate());

            listing.Gap(6f);

            listing.CheckboxLabeled(
                "TalesUIFramework.Settings.UseCompactPanel".Translate(),
                ref _settings.useCompactPanel,
                "TalesUIFramework.Settings.UseCompactPanelDesc".Translate());

            listing.Gap(12f);
            listing.GapLine();
            listing.Gap(6f);

            listing.Label("TalesUIFramework.Settings.RegisteredProvidersHeader".Translate());
            IReadOnlyList<IPawnControlProvider> providers = TalesUIRegistry.Providers;
            if (providers.Count == 0)
            {
                GUI.color = Color.gray;
                listing.Label("  - " + "TalesUIFramework.Settings.NoProvidersRegistered".Translate());
                GUI.color = Color.white;
            }
            else
            {
                for (int i = 0; i < providers.Count; i++)
                {
                    IPawnControlProvider provider = providers[i];
                    listing.Label($"  - {provider.DisplayName} ({provider.ProviderId})");
                }
            }

            listing.End();
        }
    }
}
