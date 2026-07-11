namespace PartSearchSuggest.PostV1.Categories
{
    /// <summary>
    /// Indexed category / subcategory tab row for Phase A suggest-only matching.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Contract:</b> every field used for ranking / subtitle / <c>IsValid</c> must be the same
    /// identity the Phase B navigator uses to activate the tab (index predicate === apply predicate).
    /// </para>
    /// <para>
    /// Prefer opaque string keys over live Unity button references in the index so editor-entry
    /// rebuilds stay frame-sliceable and do not pin destroyed UI objects.
    /// </para>
    /// </remarks>
    [PostV1Phase(PostV1Phase.A_SuggestOnlyCategories)]
    internal sealed class CategoryTabEntry
    {
        public CategoryTabKind Kind { get; set; }

        /// <summary>Stable identity for dedup / apply resolve (e.g. parentKey + "›" + subKey).</summary>
        public string FilterKey { get; set; }

        /// <summary>Parent category display name; null/empty for root-only rows.</summary>
        public string ParentDisplayName { get; set; }

        /// <summary>Category or subcategory display label shown in the dropdown.</summary>
        public string DisplayName { get; set; }

        /// <summary>Search tokens (display + aliases); index-time only.</summary>
        public string[] SearchTerms { get; set; }

        /// <summary>Cfg / stock icon name for display; resolve via existing icon helpers when wired.</summary>
        public string IconName { get; set; }

        /// <summary>
        /// Cached count for subtitle (<c>N parts</c> or <c>N crafts</c>). Must come from the shared matcher.
        /// Use -1 when unknown / deferred.
        /// </summary>
        public int VerifiedItemCount { get; set; } = -1;

        /// <summary>True when this row is a subcategory under a parent tab.</summary>
        public bool IsSubcategory { get; set; }

        /// <summary>Source tag for diagnostics (LivePartCategorizer, CfgBootstrap, Cck).</summary>
        public string SourceTag { get; set; }
    }
}
