using System.Collections.Generic;

namespace PartSearchSuggest.PostV1.Categories
{
    /// <summary>
    /// Shared matcher for category tab identity, counts, and validity.
    /// Index build, subtitle, <c>IsValid</c>, dedup, and Phase B apply MUST all use this contract.
    /// </summary>
    [PostV1Phase(PostV1Phase.A_SuggestOnlyCategories)]
    internal interface ICategoryTabMatcher
    {
        /// <summary>
        /// Count of parts (or crafts for SA categories) that the live tab would show for <paramref name="filterKey"/>.
        /// Returns 0 when the tab cannot be resolved or is empty.
        /// </summary>
        int CountMatchingTab(string filterKey);

        /// <summary>
        /// True when the tab is resolvable and should be eligible for suggestion
        /// (subject to empty-category product policy).
        /// </summary>
        bool IsTabValid(string filterKey);

        /// <summary>
        /// True when <paramref name="filterKey"/> refers to the same stock/custom/CCK surface
        /// that a manual tab click would activate.
        /// </summary>
        bool MatchesTab(string filterKey, CategoryTabKind kind);
    }

    /// <summary>
    /// Architecture stub — no PartCategorizer or cfg reads. Prefer
    /// <see cref="SnapshotCategoryTabMatcher"/> for snapshot-backed tests / index builds.
    /// </summary>
    [PostV1Phase(PostV1Phase.A_SuggestOnlyCategories)]
    internal sealed class CategoryTabMatcherStub : ICategoryTabMatcher
    {
        public int CountMatchingTab(string filterKey)
        {
            return 0;
        }

        public bool IsTabValid(string filterKey)
        {
            return false;
        }

        public bool MatchesTab(string filterKey, CategoryTabKind kind)
        {
            return false;
        }
    }

    /// <summary>
    /// Phase A read-only category/subcategory suggestion index.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Timing:</b> prefer editor entry once PartCategorizer is ready (+ optional cfg bootstrap).
    /// Do not build this from <c>GameLoadIndexService</c> hangar path — PartCategorizer is often absent at save-load.
    /// </para>
    /// <para>
    /// <b>Out of scope for Phase A:</b> Harmony, displayType writes, subassembly craft apply.
    /// </para>
    /// </remarks>
    [PostV1Phase(PostV1Phase.A_SuggestOnlyCategories)]
    internal interface ICategorySuggestionIndex
    {
        int EntryCount { get; }

        /// <summary>
        /// Rebuild from live categorizer when wired. Snapshot-backed
        /// <see cref="CategorySuggestionIndex"/> no-ops here — use <see cref="ICategorySuggestionIndexBuild.BuildFromSnapshot"/>.
        /// </summary>
        void Build();

        /// <summary>Clear indexed rows without touching shipping part/metadata indexes.</summary>
        void Clear();

        /// <summary>
        /// Ranked matches for <paramref name="query"/>. Must respect
        /// <c>PostV1QueryGuards.MinSuggestionQueryLength</c> (shipping
        /// <c>SuggestionQueryGuards.MinSuggestionQueryLength</c> when wired).
        /// </summary>
        IEnumerable<CategoryTabSuggestion> Match(string query, int maxResults);
    }

    /// <summary>Optional snapshot build surface for concrete indexes.</summary>
    [PostV1Phase(PostV1Phase.A_SuggestOnlyCategories)]
    internal interface ICategorySuggestionIndexBuild
    {
        void BuildFromSnapshot(CategoryTabSnapshotSet set);

        void ReplaceEntries(IEnumerable<CategoryTabEntry> entries);
    }

    /// <summary>
    /// Architecture stub — empty index, no KSP API calls.
    /// </summary>
    [PostV1Phase(PostV1Phase.A_SuggestOnlyCategories)]
    internal sealed class CategorySuggestionIndexStub : ICategorySuggestionIndex, ICategorySuggestionIndexBuild
    {
        private readonly List<CategoryTabEntry> _entries = new List<CategoryTabEntry>();

        public int EntryCount => _entries.Count;

        public void Build()
        {
            _entries.Clear();
        }

        public void BuildFromSnapshot(CategoryTabSnapshotSet set)
        {
            _entries.Clear();
            if (set != null)
            {
                _entries.AddRange(CategoryTabSnapshotBuilder.BuildEntries(set));
            }
        }

        public void ReplaceEntries(IEnumerable<CategoryTabEntry> entries)
        {
            _entries.Clear();
            if (entries != null)
            {
                _entries.AddRange(entries);
            }
        }

        public void Clear()
        {
            _entries.Clear();
        }

        public IEnumerable<CategoryTabSuggestion> Match(string query, int maxResults)
        {
            yield break;
        }
    }
}
