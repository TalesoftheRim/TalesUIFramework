using RimWorld;
using UnityEngine;
using Verse;

namespace TalesUIFramework
{
    [StaticConstructorOnStartup]
    public static class TalesGraphics
    {
        public static readonly Texture2D TogglePanelIcon;

        static TalesGraphics()
        {
            TogglePanelIcon =
                ContentFinder<Texture2D>.Get("UI/Icons/TalesUIFramework/TalesUI_Toggle", reportFailure: false)
                ?? TexCommand.ToggleVent
                ?? BaseContent.WhiteTex;
        }
    }
}
