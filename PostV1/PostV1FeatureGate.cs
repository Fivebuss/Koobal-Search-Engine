namespace PartSearchSuggest.PostV1
{
    /// <summary>
    /// Compile-time / future PluginData gate for post-v1 category and subassembly features.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Always false in this scaffolding.</b> Shipping code must not reference this type.
    /// When Phase A is later included in the project, keep this gate off until ModTest validation,
    /// and never flip it from <c>EditorSearchHook</c> / <c>GameLoadIndexService</c> without an
    /// explicit post-v1 release decision (plan open question #5).
    /// </para>
    /// </remarks>
    [PostV1Phase(PostV1Phase.A_SuggestOnlyCategories)]
    internal static class PostV1FeatureGate
    {
        /// <summary>Master enable for any post-v1 category/SA suggestion surface. Scaffold default: off.</summary>
        public const bool Enabled = false;

        /// <summary>Phase A suggest-only category/subcategory index. Scaffold default: off.</summary>
        public const bool EnableCategorySuggestions = false;

        /// <summary>Phase B tab navigate/apply. Scaffold default: off.</summary>
        public const bool EnableCategoryTabNavigate = false;

        /// <summary>Phase C subassembly index/apply/delete-refresh. Scaffold default: off.</summary>
        public const bool EnableSubassemblySuggestions = false;

        /// <summary>
        /// Phase D global Enter-halt + tight matching (no chrome). Scaffold default: off.
        /// Prefer GlobalSearchFeatureGate for per-surface flags.
        /// </summary>
        public const bool EnableGlobalSearchHalt = false;

        /// <summary>
        /// Target ~0.9 — per-row search history delete. Scaffold default: off.
        /// Prefer SearchHistoryFeatureGate. Not a v1.0 blocker.
        /// </summary>
        public const bool EnableHistoryItemDelete = false;

        /// <summary>
        /// V2 — slide-expand parts list + Settings tab (optional Track R rebuild).
        /// Prefer V2FeatureGate. Scaffold default: off.
        /// </summary>
        public const bool EnableV2PartsListAndSettings = false;
    }
}
