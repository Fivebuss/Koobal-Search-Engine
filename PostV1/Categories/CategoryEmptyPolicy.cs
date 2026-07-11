namespace PartSearchSuggest.PostV1.Categories
{
    /// <summary>
    /// Product policy for 0-item category rows (plan open question #1).
    /// Configurable so Phase A can ship either hide-empty or show-with-zero without code forks.
    /// </summary>
    [PostV1Phase(PostV1Phase.A_SuggestOnlyCategories)]
    internal enum CategoryEmptyRowPolicy
    {
        /// <summary>Hide rows when verified count is 0 (core filter rule). Unknown (-1) still shown if otherwise valid.</summary>
        HideWhenZero = 0,

        /// <summary>Show rows with <c>· 0 parts</c> subtitle (historical v0.8.1.6–7 behaviour).</summary>
        ShowZeroCount = 1
    }

    /// <summary>Index / match configuration for category suggestions.</summary>
    [PostV1Phase(PostV1Phase.A_SuggestOnlyCategories)]
    internal sealed class CategorySuggestionIndexConfig
    {
        public CategoryEmptyRowPolicy EmptyRowPolicy { get; set; } = CategoryEmptyRowPolicy.HideWhenZero;

        /// <summary>Default max rows when caller passes 0 / negative (safety).</summary>
        public int DefaultMaxResults { get; set; } = 12;

        /// <summary>
        /// When true, single-character filter keys are dropped (shipping guard parity).
        /// </summary>
        public bool SuppressSingleCharacterKeys { get; set; } = true;

        /// <summary>
        /// Optional denylist of display names / filter keys (OrdinalIgnoreCase).
        /// Useful for junk custom-category labels without depending on shipping tag junk lists.
        /// </summary>
        public string[] DenylistKeys { get; set; } = System.Array.Empty<string>();
    }
}
