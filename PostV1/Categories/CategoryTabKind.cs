namespace PartSearchSuggest.PostV1.Categories
{
    /// <summary>
    /// Post-v1 suggestion kinds for <b>tab navigation surfaces</b> — not the same as shipping
    /// <c>SuggestionKind.FilterCategory</c> (which is a predicate on <c>AvailablePart.category</c>).
    /// </summary>
    /// <remarks>
    /// When Phase A is wired, these will become first-class organic rows alongside parts /
    /// FilterFunction / ModName / etc. Dedup vs <c>FilterCategory</c> / <c>FilterFunction</c>
    /// is a Phase B product decision (plan open question / risk R9).
    /// </remarks>
    [PostV1Phase(PostV1Phase.A_SuggestOnlyCategories)]
    internal enum CategoryTabKind
    {
        /// <summary>Stock PartCategorizer parent category tab (e.g. Structural, Engines).</summary>
        StockCategoryTab,

        /// <summary>Stock subcategory under a parent category tab.</summary>
        StockSubcategoryTab,

        /// <summary>Custom part category from cfg / live PartCategorizer merge (Squad PartCategories.cfg style).</summary>
        CustomPartCategory,

        /// <summary>Custom subassembly-category tab (SubassemblyCategories.cfg style).</summary>
        CustomSubassemblyCategory,

        /// <summary>Optional stretch: Community Category Kit registered tab label.</summary>
        CckCategoryTab
    }
}
