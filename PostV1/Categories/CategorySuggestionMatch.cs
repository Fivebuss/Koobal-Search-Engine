using System;
using System.Collections.Generic;
using PartSearchSuggest.PostV1.Shared;

namespace PartSearchSuggest.PostV1.Categories
{
    /// <summary>
    /// Pure match / score / dedup helpers for category tab suggestions.
    /// Unit-testable without Unity / PartCategorizer.
    /// </summary>
    [PostV1Phase(PostV1Phase.A_SuggestOnlyCategories)]
    internal static class CategorySuggestionMatch
    {
        /// <summary>Short kind tags for dropdown rows / diagnostics.</summary>
        public static string GetKindTag(CategoryTabKind kind)
        {
            switch (kind)
            {
                case CategoryTabKind.StockCategoryTab:
                    return "StockCategory";
                case CategoryTabKind.StockSubcategoryTab:
                    return "Subcategory";
                case CategoryTabKind.CustomPartCategory:
                case CategoryTabKind.CustomSubassemblyCategory:
                    return "CustomCategory";
                case CategoryTabKind.CckCategoryTab:
                    return "CckCategory";
                default:
                    return kind.ToString();
            }
        }

        public static string FormatDisplayTitle(CategoryTabEntry entry)
        {
            if (entry == null)
            {
                return string.Empty;
            }

            if (entry.IsSubcategory
                && !string.IsNullOrWhiteSpace(entry.ParentDisplayName)
                && !string.Equals(entry.ParentDisplayName, entry.DisplayName, StringComparison.OrdinalIgnoreCase))
            {
                return entry.ParentDisplayName.Trim() + " › " + entry.DisplayName.Trim();
            }

            return entry.DisplayName ?? string.Empty;
        }

        public static string FormatSubtitle(CategoryTabEntry entry, string kindTag)
        {
            if (entry == null)
            {
                return kindTag ?? string.Empty;
            }

            string countPart;
            if (entry.VerifiedItemCount < 0)
            {
                countPart = null;
            }
            else if (entry.Kind == CategoryTabKind.CustomSubassemblyCategory)
            {
                countPart = entry.VerifiedItemCount + " crafts";
            }
            else
            {
                countPart = entry.VerifiedItemCount + " parts";
            }

            string tag = kindTag ?? GetKindTag(entry.Kind);
            if (countPart == null)
            {
                return tag;
            }

            return tag + " · " + countPart;
        }

        public static bool IsDenylisted(CategoryTabEntry entry, string[] denylist)
        {
            if (entry == null || denylist == null || denylist.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < denylist.Length; i++)
            {
                string banned = denylist[i];
                if (string.IsNullOrWhiteSpace(banned))
                {
                    continue;
                }

                if (string.Equals(entry.FilterKey, banned, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(entry.DisplayName, banned, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool PassesEmptyPolicy(CategoryTabEntry entry, CategoryEmptyRowPolicy policy)
        {
            if (entry == null)
            {
                return false;
            }

            if (policy == CategoryEmptyRowPolicy.ShowZeroCount)
            {
                return true;
            }

            // HideWhenZero: unknown (-1) allowed; zero hidden.
            return entry.VerifiedItemCount != 0;
        }

        public static int ScoreEntry(CategoryTabEntry entry, string[] words)
        {
            if (entry == null)
            {
                return TextMatchScoring.NoMatch;
            }

            return TextMatchScoring.AggregateWordScores(
                words,
                word => TextMatchScoring.ScoreDisplayFirst(
                    word,
                    entry.DisplayName,
                    entry.ParentDisplayName,
                    entry.SearchTerms));
        }

        public static CategoryTabSuggestion ToSuggestion(CategoryTabEntry entry, int rankScore)
        {
            string kindTag = GetKindTag(entry.Kind);
            string title = FormatDisplayTitle(entry);
            return new CategoryTabSuggestion
            {
                Kind = entry.Kind,
                KindTag = kindTag,
                QueryText = entry.DisplayName,
                DisplayText = title,
                MatchReason = FormatSubtitle(entry, kindTag),
                FilterKey = entry.FilterKey,
                ApplyPayloadId = entry.FilterKey,
                IconName = entry.IconName,
                RankScore = rankScore,
                SourceEntry = entry
            };
        }

        /// <summary>
        /// Dedup by FilterKey then by display title; keep best (lowest) RankScore, then Stock over Custom.
        /// </summary>
        public static List<CategoryTabSuggestion> Dedup(IReadOnlyList<CategoryTabSuggestion> suggestions)
        {
            if (suggestions == null || suggestions.Count == 0)
            {
                return new List<CategoryTabSuggestion>();
            }

            if (suggestions.Count == 1)
            {
                return new List<CategoryTabSuggestion> { suggestions[0] };
            }

            var sorted = new List<CategoryTabSuggestion>(suggestions);
            sorted.Sort(ComparePriority);

            var kept = new List<CategoryTabSuggestion>(sorted.Count);
            var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var seenTitles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < sorted.Count; i++)
            {
                CategoryTabSuggestion candidate = sorted[i];
                if (candidate == null)
                {
                    continue;
                }

                string key = candidate.FilterKey ?? candidate.ApplyPayloadId;
                if (!string.IsNullOrEmpty(key) && !seenKeys.Add(key))
                {
                    continue;
                }

                string title = candidate.DisplayText ?? string.Empty;
                if (title.Length > 0 && !seenTitles.Add(title))
                {
                    continue;
                }

                kept.Add(candidate);
            }

            return kept;
        }

        private static int ComparePriority(CategoryTabSuggestion left, CategoryTabSuggestion right)
        {
            int rank = left.RankScore.CompareTo(right.RankScore);
            if (rank != 0)
            {
                return rank;
            }

            int kind = GetKindPriority(left.Kind).CompareTo(GetKindPriority(right.Kind));
            if (kind != 0)
            {
                return kind;
            }

            return string.Compare(left.DisplayText, right.DisplayText, StringComparison.OrdinalIgnoreCase);
        }

        private static int GetKindPriority(CategoryTabKind kind)
        {
            switch (kind)
            {
                case CategoryTabKind.StockCategoryTab:
                    return 0;
                case CategoryTabKind.StockSubcategoryTab:
                    return 1;
                case CategoryTabKind.CustomPartCategory:
                    return 2;
                case CategoryTabKind.CustomSubassemblyCategory:
                    return 3;
                case CategoryTabKind.CckCategoryTab:
                    return 4;
                default:
                    return 9;
            }
        }
    }
}
