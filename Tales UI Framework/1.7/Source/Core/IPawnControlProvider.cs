using System.Collections.Generic;
using Verse;

namespace TalesUIFramework
{
    public interface IPawnControlProvider
    {
        string ProviderId { get; }
        string DisplayName { get; }
        int Order { get; }

        void CollectControls(Pawn pawn, List<TalesControlRow> rows);
    }
}
