using System;
using System.Collections.Generic;
using Verse;

namespace TalesUIFramework
{
    public static class TalesUIRegistry
    {
        private static readonly object SyncRoot = new();
        private static readonly List<IPawnControlProvider> RegisteredProviders = new();

        public static IReadOnlyList<IPawnControlProvider> Providers
        {
            get
            {
                lock (SyncRoot)
                {
                    return RegisteredProviders.ToArray();
                }
            }
        }

        public static void RegisterProvider(IPawnControlProvider provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            if (string.IsNullOrEmpty(provider.ProviderId))
            {
                throw new ArgumentException("ProviderId must not be null or empty.", nameof(provider));
            }

            lock (SyncRoot)
            {
                RegisteredProviders.RemoveAll(p => p.ProviderId == provider.ProviderId);
                RegisteredProviders.Add(provider);
                RegisteredProviders.Sort((a, b) =>
                {
                    int cmp = a.Order.CompareTo(b.Order);
                    return cmp != 0 ? cmp : string.CompareOrdinal(a.ProviderId, b.ProviderId);
                });
            }
        }

        public static bool UnregisterProvider(string providerId)
        {
            if (string.IsNullOrEmpty(providerId))
            {
                return false;
            }

            lock (SyncRoot)
            {
                return RegisteredProviders.RemoveAll(p => p.ProviderId == providerId) > 0;
            }
        }

        public static List<TalesControlRow> CollectControlsForPawn(Pawn pawn)
        {
            List<TalesControlRow> rows = new();
            if (pawn == null)
            {
                return rows;
            }

            IPawnControlProvider[] snapshot;
            lock (SyncRoot)
            {
                if (RegisteredProviders.Count == 0)
                {
                    return rows;
                }

                snapshot = RegisteredProviders.ToArray();
            }

            for (int i = 0; i < snapshot.Length; i++)
            {
                try
                {
                    snapshot[i].CollectControls(pawn, rows);
                }
                catch (Exception ex)
                {
                    Log.ErrorOnce(
                        $"[Tales UI Framework] Error collecting controls from provider '{snapshot[i].ProviderId}': {ex}",
                        snapshot[i].ProviderId.GetHashCode());
                }
            }

            if (rows.Count > 1)
            {
                rows.Sort((a, b) =>
                {
                    int cmp = a.Order.CompareTo(b.Order);
                    return cmp != 0 ? cmp : string.CompareOrdinal(a.Id, b.Id);
                });
            }

            return rows;
        }
    }
}
