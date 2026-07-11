using System.Collections.Generic;

namespace PartSearchSuggest.PostV1.SearchHistory
{
    /// <summary>
    /// Composition root for ~0.9 per-item history delete.
    /// <b>Unused by shipping code</b> — do not call from EditorSearchHook / GameLoad / Harmony.
    /// </summary>
    [PostV1Phase(PostV1Phase.HistoryItemDelete_v09)]
    internal sealed class SearchHistoryServices
    {
        public IHistoryPersistence Persistence { get; }

        public ISearchHistoryStore Store { get; }

        public IHistoryRowChrome RowChrome { get; }

        /// <param name="useRecordingPorts">
        /// When true, uses memory persistence + recording row chrome (exercisable without Unity).
        /// When false, uses Unwired NotImplemented ports (compile-check / boundary docs).
        /// </param>
        /// <param name="seedEntries">Optional seed for recording/memory path (newest first).</param>
        public SearchHistoryServices(
            bool useRecordingPorts = true,
            IEnumerable<SearchHistoryEntry> seedEntries = null)
        {
            // Store is pure — always backed by memory in the architecture assembly.
            // UnwiredHistoryPersistence remains available as the file-boundary type for wire-up.
            var memory = new MemoryHistoryPersistence();
            if (seedEntries != null)
            {
                memory.Seed(seedEntries);
            }

            Persistence = memory;
            Store = new SearchHistoryStore(memory);

            RowChrome = useRecordingPorts
                ? (IHistoryRowChrome)new RecordingHistoryRowChrome()
                : new UnwiredHistoryRowChrome();

            // SearchHistoryFeatureGate.* remain false — shipping must not flip.
            WireChromeToStore();
        }

        /// <summary>
        /// Recording/self-check: chrome remove clicks call <see cref="ISearchHistoryStore.Remove"/>.
        /// Live wire-up will do the same from EditorSearchHook instead.
        /// </summary>
        private void WireChromeToStore()
        {
            if (RowChrome is RecordingHistoryRowChrome recording)
            {
                recording.OnRemoveRequested += entryId =>
                {
                    Store.Remove(entryId);
                };
            }
        }

        /// <summary>Bind chrome from the current store snapshot (architecture helper).</summary>
        public void BindChromeFromSnapshot()
        {
            IReadOnlyList<SearchHistoryEntry> snap = Store.Snapshot;
            var requests = new List<HistoryRowChromeRequest>(snap.Count);
            for (int i = 0; i < snap.Count; i++)
            {
                SearchHistoryEntry entry = snap[i];
                requests.Add(new HistoryRowChromeRequest
                {
                    EntryId = entry.Id,
                    DisplayText = entry.Query,
                    VisibleIndex = i
                });
            }

            RowChrome.BindRows(requests);
        }
    }
}
