namespace PartSearchSuggest.PostV1.Categories
{
    /// <summary>
    /// Dropdown DTO for a Phase A category/subcategory suggestion.
    /// Parallel to shipping <c>PartSuggestion</c> but kept separate until kinds are merged post-v1.
    /// Ready to map into future dropdown rows: display title, subtitle, rank, apply payload id.
    /// </summary>
    [PostV1Phase(PostV1Phase.A_SuggestOnlyCategories)]
    internal sealed class CategoryTabSuggestion
    {
        public CategoryTabKind Kind { get; set; }

        /// <summary>Short tag for UI / diagnostics: StockCategory, Subcategory, CustomCategory, …</summary>
        public string KindTag { get; set; }

        /// <summary>Text applied to the search field if product chooses fill-only before Phase B.</summary>
        public string QueryText { get; set; }

        /// <summary>Primary label (use parent › child when names collide).</summary>
        public string DisplayText { get; set; }

        /// <summary>Subtitle fragment e.g. <c>StockCategory · N parts</c>.</summary>
        public string MatchReason { get; set; }

        /// <summary>Same key as <see cref="CategoryTabEntry.FilterKey"/> — apply must resolve this key.</summary>
        public string FilterKey { get; set; }

        /// <summary>
        /// Opaque apply payload id for Phase B navigator (today identical to FilterKey;
        /// kept separate so row factories can diverge later without breaking match).
        /// </summary>
        public string ApplyPayloadId { get; set; }

        public string IconName { get; set; }

        /// <summary>Lower is stronger (align with categorizer/metadata RankScore bands).</summary>
        public int RankScore { get; set; } = 999;

        /// <summary>Back-reference for apply; may be null for ephemeral match results.</summary>
        public CategoryTabEntry SourceEntry { get; set; }

        /// <summary>
        /// Structural validity (key + title). Prefer <see cref="IsValid(ICategoryTabMatcher, CategoryEmptyRowPolicy)"/>.
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(FilterKey ?? ApplyPayloadId)
                && !string.IsNullOrWhiteSpace(DisplayText);
        }

        /// <summary>
        /// Full validity gate: structural + shared matcher + empty-row policy.
        /// Index predicate === apply predicate via <paramref name="matcher"/>.
        /// </summary>
        public bool IsValid(ICategoryTabMatcher matcher, CategoryEmptyRowPolicy emptyPolicy)
        {
            if (!IsValid())
            {
                return false;
            }

            string key = FilterKey ?? ApplyPayloadId;
            if (matcher != null && !matcher.IsTabValid(key))
            {
                return false;
            }

            if (SourceEntry != null
                && !CategorySuggestionMatch.PassesEmptyPolicy(SourceEntry, emptyPolicy))
            {
                return false;
            }

            return true;
        }
    }
}
