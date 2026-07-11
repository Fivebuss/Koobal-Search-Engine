using PartSearchSuggest.PostV1.Apply;
using PartSearchSuggest.PostV1.Categories;
using PartSearchSuggest.PostV1.GlobalSearch;
using PartSearchSuggest.PostV1.Safety;
using PartSearchSuggest.PostV1.SearchHistory;
using PartSearchSuggest.PostV1.Subassemblies;
using PartSearchSuggest.PostV1.V2;

namespace PartSearchSuggest.PostV1
{
    /// <summary>
    /// Composition root for post-v1 category / subcategory / subassembly services
    /// (and optional Global Search Halt composition for architecture self-checks).
    /// Constructs concrete snapshot-backed services + unwired UI ports.
    /// <b>Unused by shipping code</b> — do not call from EditorSearchHook / GameLoad / Harmony.
    /// </summary>
    [PostV1Phase(PostV1Phase.A_SuggestOnlyCategories)]
    internal sealed class PostV1Services
    {
        public CategorySuggestionIndexConfig CategoryConfig { get; }

        public CategorySuggestionIndex CategoryIndex { get; }

        public ICategoryTabMatcher CategoryMatcher { get; }

        public IEditorCategoryUi EditorCategoryUi { get; }

        public ICategoryTabNavigator CategoryNavigator { get; }

        public SubassemblySuggestionIndex SubassemblyIndex { get; }

        public ISubassemblyMatcher SubassemblyMatcher { get; }

        public ISubassemblyLifecycleEvents SubassemblyEvents { get; }

        public ISubassemblyApplyPort SubassemblyApply { get; }

        public ISubassemblyDeleteRefreshHook SubassemblyDeleteRefresh { get; }

        public IApplySafetyRails SafetyRails { get; }

        /// <summary>
        /// Phase D composition (halt + tight Enter). Always gate-off; for architecture only.
        /// </summary>
        public GlobalSearchServices GlobalSearch { get; }

        /// <summary>
        /// Target ~0.9 per-item history delete. Always gate-off; for architecture only.
        /// </summary>
        public SearchHistoryServices SearchHistory { get; }

        /// <summary>
        /// V2 slide-expand + Settings (+ optional Track R ports). Always gate-off; architecture only.
        /// </summary>
        public V2Services V2 { get; }

        /// <summary>
        /// Default composition: real indexes + recording/unwired UI depending on
        /// <paramref name="useRecordingUiDoubles"/>.
        /// </summary>
        /// <param name="useRecordingUiDoubles">
        /// When true, uses <see cref="RecordingEditorCategoryUi"/> /
        /// <see cref="RecordingSubassemblyEditorUi"/> so navigator/apply algorithms are exercisable
        /// without Unity. When false, uses unwired NotImplemented ports (compile-check only).
        /// </param>
        public PostV1Services(bool useRecordingUiDoubles = true)
        {
            CategoryConfig = new CategorySuggestionIndexConfig();
            CategoryIndex = new CategorySuggestionIndex(CategoryConfig);
            CategoryMatcher = CategoryIndex.Matcher;

            SafetyRails = new ApplySafetyRails();

            EditorCategoryUi = useRecordingUiDoubles
                ? (IEditorCategoryUi)new RecordingEditorCategoryUi()
                : new UnwiredEditorCategoryUi();

            CategoryNavigator = new CategoryTabNavigator(EditorCategoryUi, SafetyRails);

            SubassemblyIndex = new SubassemblySuggestionIndex();
            SubassemblyMatcher = SubassemblyIndex.Matcher;
            SubassemblyEvents = new SubassemblyLifecycleEventBus();

            ISubassemblyEditorUi saUi = useRecordingUiDoubles
                ? (ISubassemblyEditorUi)new RecordingSubassemblyEditorUi()
                : new UnwiredSubassemblyEditorUi();

            SubassemblyApply = new SubassemblyApplyPort(saUi, SubassemblyMatcher, SafetyRails);
            SubassemblyDeleteRefresh = new SubassemblyDeleteRefreshService(
                SubassemblyIndex,
                SubassemblyEvents,
                SubassemblyApply);

            GlobalSearch = new GlobalSearchServices(
                useRecordingPorts: useRecordingUiDoubles,
                fullKoobalPresent: true);

            SearchHistory = new SearchHistoryServices(useRecordingPorts: useRecordingUiDoubles);

            V2 = new V2Services(useRecordingPorts: useRecordingUiDoubles);

            // Feature always off — shipping must not read this gate.
            // PostV1FeatureGate.* / V2FeatureGate.* remain false.
        }

        /// <summary>Install delete/save listeners on the in-process event bus.</summary>
        public void InstallLifecycleHooks()
        {
            SubassemblyDeleteRefresh.Install();
        }

        public void UninstallLifecycleHooks()
        {
            SubassemblyDeleteRefresh.Uninstall();
        }

        /// <summary>
        /// Stub-only composition (empty indexes, stub navigator/apply) — documents legacy scaffold path.
        /// </summary>
        public static PostV1Services CreateStubsOnly()
        {
            // Still uses concrete services; stubs remain available as types for gradual migration.
            return new PostV1Services(useRecordingUiDoubles: true);
        }
    }
}
