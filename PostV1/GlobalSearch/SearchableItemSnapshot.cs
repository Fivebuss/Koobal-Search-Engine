using System.Collections.Generic;

namespace PartSearchSuggest.PostV1.GlobalSearch
{
    /// <summary>
    /// Field kinds for tightened Enter matching — mirrors NativeEnterMatcher / SuggestionIndex.
    /// Description may be indexed but must not qualify Enter hits alone.
    /// </summary>
    [PostV1Phase(PostV1Phase.D_GlobalSearchHalt)]
    internal enum TightSearchFieldKind
    {
        Title = 0,
        Name = 1,
        Tag = 2,
        Description = 3,
        Category = 4,
        Manufacturer = 5,
        Module = 6,
        AutoTag = 7,
        TechRequired = 8,
        /// <summary>Surface-specific secondary label (vessel type, tech id, craft author, …).</summary>
        Secondary = 9
    }

    /// <summary>One searchable text field on a snapshot item.</summary>
    [PostV1Phase(PostV1Phase.D_GlobalSearchHalt)]
    internal sealed class SearchableFieldSnapshot
    {
        public string Text { get; set; }

        public TightSearchFieldKind Kind { get; set; }

        public int PrefixScore { get; set; }

        public int ContainsScore { get; set; }
    }

    /// <summary>
    /// One list row / part / tech / vessel — pure data for <see cref="TightSearchMatcher"/>.
    /// No KSP types.
    /// </summary>
    [PostV1Phase(PostV1Phase.D_GlobalSearchHalt)]
    internal sealed class SearchableItemSnapshot
    {
        /// <summary>Stable id used by appliers (part name, tech id, craft path, vessel id, …).</summary>
        public string Id { get; set; }

        public string DisplayTitle { get; set; }

        public SearchBarSurface Surface { get; set; }

        public List<SearchableFieldSnapshot> Fields { get; } = new List<SearchableFieldSnapshot>();
    }

    /// <summary>Batch of items for one surface session.</summary>
    [PostV1Phase(PostV1Phase.D_GlobalSearchHalt)]
    internal sealed class SearchableItemSnapshotSet
    {
        public SearchBarSurface Surface { get; set; }

        public List<SearchableItemSnapshot> Items { get; } = new List<SearchableItemSnapshot>();
    }

    /// <summary>One tightened Enter match.</summary>
    [PostV1Phase(PostV1Phase.D_GlobalSearchHalt)]
    internal sealed class TightMatchResult
    {
        public string Id { get; set; }

        public string DisplayTitle { get; set; }

        /// <summary>Lower is stronger (NativeEnterMatcher aggregate score).</summary>
        public int Score { get; set; }

        public TightSearchFieldKind BestFieldKind { get; set; }
    }
}
