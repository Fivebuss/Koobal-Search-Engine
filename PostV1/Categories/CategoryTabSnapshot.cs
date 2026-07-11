using System.Collections.Generic;

namespace PartSearchSuggest.PostV1.Categories
{
    /// <summary>
    /// Injected snapshot of one PartCategorizer (or cfg/CCK) tab — no live Unity references.
    /// Phase A builds the suggestion index from these; live PartCategorizer adapters come at wire-up.
    /// </summary>
    [PostV1Phase(PostV1Phase.A_SuggestOnlyCategories)]
    internal sealed class CategoryTabSnapshot
    {
        public CategoryTabKind Kind { get; set; }

        /// <summary>Stable key used as <see cref="CategoryTabEntry.FilterKey"/>.</summary>
        public string FilterKey { get; set; }

        public string ParentDisplayName { get; set; }

        public string DisplayName { get; set; }

        /// <summary>Extra search aliases (cfg names, camel splits, etc.).</summary>
        public string[] Aliases { get; set; }

        public string IconName { get; set; }

        /// <summary>Parts or crafts the tab would show; -1 unknown / deferred.</summary>
        public int ItemCount { get; set; } = -1;

        public bool IsSubcategory { get; set; }

        /// <summary>LivePartCategorizer, CfgBootstrap, Cck, TestFixture, …</summary>
        public string SourceTag { get; set; }

        /// <summary>Child subcategory snapshots under this parent (optional nesting).</summary>
        public List<CategoryTabSnapshot> Subcategories { get; set; }
    }

    /// <summary>Root payload for building a category suggestion index without live UI.</summary>
    [PostV1Phase(PostV1Phase.A_SuggestOnlyCategories)]
    internal sealed class CategoryTabSnapshotSet
    {
        public List<CategoryTabSnapshot> Tabs { get; set; } = new List<CategoryTabSnapshot>();

        public string SourceTag { get; set; } = "Injected";
    }
}
