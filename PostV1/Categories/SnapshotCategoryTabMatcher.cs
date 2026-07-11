using System;
using System.Collections.Generic;
using PartSearchSuggest.PostV1.Shared;

namespace PartSearchSuggest.PostV1.Categories
{
    /// <summary>
    /// Snapshot-backed matcher: counts and validity come from indexed entries (injected data),
    /// not live PartCategorizer. Wire-up replaces or wraps this with a live matcher that shares
    /// the same FilterKey contract (index predicate === apply predicate).
    /// </summary>
    [PostV1Phase(PostV1Phase.A_SuggestOnlyCategories)]
    internal sealed class SnapshotCategoryTabMatcher : ICategoryTabMatcher
    {
        private readonly Dictionary<string, CategoryTabEntry> _byKey =
            new Dictionary<string, CategoryTabEntry>(StringComparer.OrdinalIgnoreCase);

        private readonly CategorySuggestionIndexConfig _config;

        public SnapshotCategoryTabMatcher(CategorySuggestionIndexConfig config = null)
        {
            _config = config ?? new CategorySuggestionIndexConfig();
        }

        public void ReplaceEntries(IEnumerable<CategoryTabEntry> entries)
        {
            _byKey.Clear();
            if (entries == null)
            {
                return;
            }

            foreach (CategoryTabEntry entry in entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.FilterKey))
                {
                    continue;
                }

                _byKey[entry.FilterKey] = entry;
            }
        }

        public int CountMatchingTab(string filterKey)
        {
            if (string.IsNullOrWhiteSpace(filterKey))
            {
                return 0;
            }

            return _byKey.TryGetValue(filterKey, out CategoryTabEntry entry)
                ? Math.Max(0, entry.VerifiedItemCount)
                : 0;
        }

        public bool IsTabValid(string filterKey)
        {
            if (string.IsNullOrWhiteSpace(filterKey) || !_byKey.TryGetValue(filterKey, out CategoryTabEntry entry))
            {
                return false;
            }

            if (_config.SuppressSingleCharacterKeys && PostV1QueryGuards.IsSingleCharacter(filterKey))
            {
                return false;
            }

            if (CategorySuggestionMatch.IsDenylisted(entry, _config.DenylistKeys))
            {
                return false;
            }

            return CategorySuggestionMatch.PassesEmptyPolicy(entry, _config.EmptyRowPolicy);
        }

        public bool MatchesTab(string filterKey, CategoryTabKind kind)
        {
            if (string.IsNullOrWhiteSpace(filterKey) || !_byKey.TryGetValue(filterKey, out CategoryTabEntry entry))
            {
                return false;
            }

            return entry.Kind == kind;
        }
    }
}
