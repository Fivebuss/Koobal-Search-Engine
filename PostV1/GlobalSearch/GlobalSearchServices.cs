namespace PartSearchSuggest.PostV1.GlobalSearch
{
    /// <summary>
    /// Composition root for global search halt + tight Enter matching.
    /// <b>Unused by shipping code</b> — do not call from EditorSearchHook / GameLoad / Harmony.
    /// </summary>
    [PostV1Phase(PostV1Phase.D_GlobalSearchHalt)]
    internal sealed class GlobalSearchServices
    {
        public ITightSearchMatcher Matcher { get; }

        public ISearchExecutionHalt Halt { get; }

        public ISearchResultApplier Applier { get; }

        public GlobalSearchOrchestrator Orchestrator { get; }

        /// <param name="useRecordingPorts">
        /// When true, uses Recording halt/applier so orchestrator is exercisable without Unity.
        /// When false, uses Unwired NotImplemented ports (compile-check / boundary docs).
        /// </param>
        /// <param name="fullKoobalPresent">
        /// When true, orchestrator skips <see cref="SearchBarSurface.EditorPartList"/>
        /// (dropdown product already owns halt + Enter there).
        /// </param>
        public GlobalSearchServices(bool useRecordingPorts = true, bool fullKoobalPresent = true)
        {
            Matcher = new TightSearchMatcher();

            Halt = useRecordingPorts
                ? (ISearchExecutionHalt)new RecordingSearchExecutionHalt()
                : new UnwiredSearchExecutionHalt();

            Applier = useRecordingPorts
                ? (ISearchResultApplier)new RecordingSearchResultApplier()
                : new UnwiredSearchResultApplier();

            Orchestrator = new GlobalSearchOrchestrator(
                Halt,
                Matcher,
                Applier,
                fullKoobalPresent: fullKoobalPresent,
                skipEditorWhenFullKoobalPresent: true);

            // GlobalSearchFeatureGate.Enabled remains false — shipping must not flip.
        }

        /// <summary>Load a snapshot into the shared matcher (per-surface session build).</summary>
        public void LoadSnapshot(SearchableItemSnapshotSet set)
        {
            Matcher.BuildFromSnapshot(set);
        }
    }
}
