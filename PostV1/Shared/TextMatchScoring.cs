using System;

namespace PartSearchSuggest.PostV1.Shared
{
    /// <summary>
    /// Pure text match/score helpers aligned with categorizer suggestion quality patterns:
    /// prefix beats contains; title/display-first for short queries; lower score = stronger.
    /// </summary>
    [PostV1Phase(PostV1Phase.A_SuggestOnlyCategories)]
    internal static class TextMatchScoring
    {
        // Rank bands for category / subcategory / custom tab rows (first-class, ~0–20 like categorizer).
        public const int DisplayPrefixRank = 0;
        public const int DisplayTokenPrefixRank = 1;
        public const int ParentPrefixRank = 2;
        public const int AliasPrefixRank = 3;
        public const int DisplayContainsRank = 8;
        public const int ParentContainsRank = 9;
        public const int AliasContainsRank = 10;
        public const int DescriptionPrefixRank = 14;
        public const int DescriptionContainsRank = 15;
        public const int AuthorPrefixRank = 16;
        public const int AuthorContainsRank = 17;

        /// <summary>No match.</summary>
        public const int NoMatch = -1;

        public static int ScoreDisplayFirst(string queryWord, string displayName, string parentDisplayName, string[] searchTerms)
        {
            if (string.IsNullOrWhiteSpace(queryWord))
            {
                return NoMatch;
            }

            int best = NoMatch;
            best = MinRank(best, ScoreAgainst(displayName, queryWord, DisplayPrefixRank, DisplayContainsRank, DisplayTokenPrefixRank));
            best = MinRank(best, ScoreAgainst(parentDisplayName, queryWord, ParentPrefixRank, ParentContainsRank, ParentPrefixRank + 1));

            if (searchTerms != null)
            {
                for (int i = 0; i < searchTerms.Length; i++)
                {
                    string term = searchTerms[i];
                    if (string.Equals(term, displayName, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(term, parentDisplayName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    best = MinRank(best, ScoreAgainst(term, queryWord, AliasPrefixRank, AliasContainsRank, AliasPrefixRank + 1));
                }
            }

            return best;
        }

        /// <summary>
        /// Score craft name / description / author. Title-first for short queries
        /// (caller passes <paramref name="preferTitle"/> = query length ≤ TitleFirstMaxQueryLength).
        /// </summary>
        public static int ScoreTitleDescriptionAuthor(
            string queryWord,
            string title,
            string description,
            string author,
            bool preferTitle)
        {
            if (string.IsNullOrWhiteSpace(queryWord))
            {
                return NoMatch;
            }

            int titlePrefix = preferTitle ? DisplayPrefixRank : 4;
            int titleContains = preferTitle ? DisplayContainsRank : 12;
            int titleToken = preferTitle ? DisplayTokenPrefixRank : 5;

            int descPrefix = preferTitle ? DescriptionPrefixRank : DisplayPrefixRank + 2;
            int descContains = preferTitle ? DescriptionContainsRank : DisplayContainsRank + 2;
            int authorPrefix = preferTitle ? AuthorPrefixRank : DisplayPrefixRank + 3;
            int authorContains = preferTitle ? AuthorContainsRank : DisplayContainsRank + 3;

            int best = NoMatch;
            best = MinRank(best, ScoreAgainst(title, queryWord, titlePrefix, titleContains, titleToken));
            best = MinRank(best, ScoreAgainst(description, queryWord, descPrefix, descContains, descPrefix + 1));
            best = MinRank(best, ScoreAgainst(author, queryWord, authorPrefix, authorContains, authorPrefix + 1));
            return best;
        }

        /// <summary>
        /// Multi-word aggregate: every word must match; worst (highest) rank wins — same as
        /// <c>CategorizerSuggestionIndex.ScoreEntry</c>.
        /// </summary>
        public static int AggregateWordScores(string[] words, Func<string, int> scoreWord)
        {
            if (words == null || words.Length == 0 || scoreWord == null)
            {
                return NoMatch;
            }

            int worst = NoMatch;
            for (int i = 0; i < words.Length; i++)
            {
                int wordRank = scoreWord(words[i]);
                if (wordRank < 0)
                {
                    return NoMatch;
                }

                if (worst < 0 || wordRank > worst)
                {
                    worst = wordRank;
                }
            }

            return worst;
        }

        public static string[] SplitQueryWords(string query)
        {
            string trimmed = (query ?? string.Empty).Trim();
            if (trimmed.Length == 0)
            {
                return Array.Empty<string>();
            }

            return trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static int ScoreAgainst(string candidate, string word, int prefixRank, int containsRank, int tokenPrefixRank)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                return NoMatch;
            }

            string text = candidate.Trim();
            if (text.StartsWith(word, StringComparison.OrdinalIgnoreCase))
            {
                return prefixRank;
            }

            if (text.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // Prefer word-boundary-ish token prefix over mid-string contains when possible.
                foreach (string token in SplitTokens(text))
                {
                    if (token.StartsWith(word, StringComparison.OrdinalIgnoreCase))
                    {
                        return tokenPrefixRank;
                    }
                }

                return containsRank;
            }

            foreach (string token in SplitTokens(text))
            {
                if (token.StartsWith(word, StringComparison.OrdinalIgnoreCase))
                {
                    return tokenPrefixRank;
                }
            }

            return NoMatch;
        }

        private static string[] SplitTokens(string text)
        {
            return text.Split(new[] { ' ', '/', '-', '_', '›', '>', '.', ',' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static int MinRank(int current, int candidate)
        {
            if (candidate < 0)
            {
                return current;
            }

            if (current < 0 || candidate < current)
            {
                return candidate;
            }

            return current;
        }
    }
}
