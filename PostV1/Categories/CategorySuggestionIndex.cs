using System;
using System.Collections.Generic;
using PartSearchSuggest.PostV1.Shared;

namespace PartSearchSuggest.PostV1.Categories
{
    /// <summary>
    /// Concrete Phase A category/subcategory suggestion index built from injected snapshots.
    /// Does not call PartCategorizer; call <see cref="BuildFromSnapshot"/> (or replace entries)
    /// from a future editor-entry adapter.
    /// </summary>
    [PostV1Phase(PostV1Phase.A_SuggestOnlyCategories)]
    internal sealed class CategorySuggestionIndex : ICategorySuggestionIndex, ICategorySuggestionIndexBuild
    {
        private readonly List<CategoryTabEntry> _entries = new List<CategoryTabEntry>();
        private readonly CategorySuggestionIndexConfig _config;
        private readonly SnapshotCategoryTabMatcher _matcher;

        public CategorySuggestionIndex(CategorySuggestionIndexConfig config = null)
        {
            _config = config ?? new CategorySuggestionIndexConfig();
            _matcher = new SnapshotCategoryTabMatcher(_config);
        }

        public ICategoryTabMatcher Matcher => _matcher;

        public CategorySuggestionIndexConfig Config => _config;

        public int EntryCount => _entries.Count;

        /// <summary>
        /// No-op for live UI. Prefer <see cref="BuildFromSnapshot"/>. Kept to satisfy the interface
        /// until an editor-entry PartCategorizer adapter exists at wire-up.
        /// </summary>
        public void Build()
        {
            // Intentionally does not touch PartCategorizer. Wire-up adapter should:
            // 1) Enumerate live tabs → CategoryTabSnapshotSet
            // 2) Call BuildFromSnapshot
        }

        public void BuildFromSnapshot(CategoryTabSnapshotSet set)
        {
            _entries.Clear();
            List<CategoryTabEntry> built = CategoryTabSnapshotBuilder.BuildEntries(set);
            _entries.AddRange(built);
            _matcher.ReplaceEntries(_entries);
        }

        public void ReplaceEntries(IEnumerable<CategoryTabEntry> entries)
        {
            _entries.Clear();
            if (entries != null)
            {
                _entries.AddRange(entries);
            }

            _matcher.ReplaceEntries(_entries);
        }

        public void Clear()
        {
            _entries.Clear();
            _matcher.ReplaceEntries(_entries);
        }

        public IEnumerable<CategoryTabSuggestion> Match(string query, int maxResults)
        {
            string trimmed = (query ?? string.Empty).Trim();
            if (PostV1QueryGuards.IsTooShortForSuggestions(trimmed) || maxResults <= 0)
            {
                yield break;
            }

            string[] words = TextMatchScoring.SplitQueryWords(trimmed);
            if (words.Length == 0)
            {
                yield break;
            }

            var raw = new List<CategoryTabSuggestion>();
            for (int i = 0; i < _entries.Count; i++)
            {
                CategoryTabEntry entry = _entries[i];
                if (entry == null)
                {
                    continue;
                }

                if (_config.SuppressSingleCharacterKeys && PostV1QueryGuards.IsSingleCharacter(entry.FilterKey))
                {
                    continue;
                }

                if (CategorySuggestionMatch.IsDenylisted(entry, _config.DenylistKeys))
                {
                    continue;
                }

                if (!CategorySuggestionMatch.PassesEmptyPolicy(entry, _config.EmptyRowPolicy))
                {
                    continue;
                }

                if (!_matcher.IsTabValid(entry.FilterKey))
                {
                    continue;
                }

                int rank = CategorySuggestionMatch.ScoreEntry(entry, words);
                if (rank < 0)
                {
                    continue;
                }

                // Short-query title-first: display-prefix ranks already win via ScoreDisplayFirst.
                raw.Add(CategorySuggestionMatch.ToSuggestion(entry, rank));
            }

            List<CategoryTabSuggestion> deduped = CategorySuggestionMatch.Dedup(raw);
            deduped.Sort(CompareSuggestions);

            int count = 0;
            for (int i = 0; i < deduped.Count; i++)
            {
                CategoryTabSuggestion suggestion = deduped[i];
                if (suggestion == null || !suggestion.IsValid(_matcher, _config.EmptyRowPolicy))
                {
                    continue;
                }

                yield return suggestion;
                count++;
                if (count >= maxResults)
                {
                    yield break;
                }
            }
        }

        private static int CompareSuggestions(CategoryTabSuggestion a, CategoryTabSuggestion b)
        {
            int rank = a.RankScore.CompareTo(b.RankScore);
            if (rank != 0)
            {
                return rank;
            }

            return string.Compare(a.DisplayText, b.DisplayText, StringComparison.OrdinalIgnoreCase);
        }
    }
}
