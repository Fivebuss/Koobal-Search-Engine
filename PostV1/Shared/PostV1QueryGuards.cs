namespace PartSearchSuggest.PostV1.Shared
{
    /// <summary>
    /// PostV1-local mirror of shipping <c>SuggestionQueryGuards</c> constants/logic that do not
    /// require PartLoader / editor part counts. Used so the architecture project typechecks
    /// without referencing shipping types until wire-up.
    /// </summary>
    [PostV1Phase(PostV1Phase.A_SuggestOnlyCategories)]
    internal static class PostV1QueryGuards
    {
        internal const int MinSuggestionQueryLength = 2;

        /// <summary>
        /// Aligns with shipping title-first short-query behaviour
        /// (<c>SuggestionIndex.TitleFirstMaxQueryLength</c>).
        /// </summary>
        internal const int TitleFirstMaxQueryLength = 2;

        /// <summary>
        /// Fraction threshold for overly-broad rows. Full editor-part-count check stays in
        /// shipping <c>SuggestionQueryGuards.IsOverlyBroad</c> at wire-up; here we only expose
        /// the constant for documentation and optional snapshot-based policies.
        /// </summary>
        internal const float MaxBroadMatchFraction = 0.90f;

        internal static bool IsTooShortForSuggestions(string query)
        {
            return (query ?? string.Empty).Trim().Length < MinSuggestionQueryLength;
        }

        internal static bool IsTooShortForBroadSuggestions(string query)
        {
            return IsTooShortForSuggestions(query);
        }

        internal static bool IsSingleCharacter(string value)
        {
            string trimmed = (value ?? string.Empty).Trim();
            return trimmed.Length == 1;
        }

        /// <summary>
        /// Suppress single-character keys and queries below min length.
        /// Part-count broadness (vs editor inventory) is applied at wire-up via shipping guards.
        /// </summary>
        internal static bool ShouldSuppressByKeyOrQuery(string query, string filterKey)
        {
            return IsTooShortForBroadSuggestions(query) || IsSingleCharacter(filterKey);
        }
    }
}
