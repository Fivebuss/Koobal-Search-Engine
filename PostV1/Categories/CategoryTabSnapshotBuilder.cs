using System;
using System.Collections.Generic;

namespace PartSearchSuggest.PostV1.Categories
{
    /// <summary>
    /// Builds flat <see cref="CategoryTabEntry"/> rows from injected snapshots (no PartCategorizer).
    /// Dedups by FilterKey (OrdinalIgnoreCase); prefers higher item counts / more specific kinds.
    /// </summary>
    [PostV1Phase(PostV1Phase.A_SuggestOnlyCategories)]
    internal static class CategoryTabSnapshotBuilder
    {
        public static List<CategoryTabEntry> BuildEntries(CategoryTabSnapshotSet set)
        {
            var result = new List<CategoryTabEntry>();
            if (set?.Tabs == null || set.Tabs.Count == 0)
            {
                return result;
            }

            var byKey = new Dictionary<string, CategoryTabEntry>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < set.Tabs.Count; i++)
            {
                AppendSnapshot(byKey, set.Tabs[i], set.SourceTag, parentDisplayOverride: null);
            }

            foreach (KeyValuePair<string, CategoryTabEntry> pair in byKey)
            {
                result.Add(pair.Value);
            }

            result.Sort(CompareEntries);
            return result;
        }

        /// <summary>Convenience: build from a flat list of snapshots.</summary>
        public static List<CategoryTabEntry> BuildEntries(IEnumerable<CategoryTabSnapshot> tabs, string sourceTag = "Injected")
        {
            var set = new CategoryTabSnapshotSet { SourceTag = sourceTag ?? "Injected" };
            if (tabs != null)
            {
                set.Tabs.AddRange(tabs);
            }

            return BuildEntries(set);
        }

        private static void AppendSnapshot(
            Dictionary<string, CategoryTabEntry> byKey,
            CategoryTabSnapshot snap,
            string defaultSourceTag,
            string parentDisplayOverride)
        {
            if (snap == null)
            {
                return;
            }

            string filterKey = (snap.FilterKey ?? string.Empty).Trim();
            string displayName = (snap.DisplayName ?? string.Empty).Trim();
            if (filterKey.Length == 0 || displayName.Length == 0)
            {
                return;
            }

            string parent = !string.IsNullOrWhiteSpace(parentDisplayOverride)
                ? parentDisplayOverride.Trim()
                : (snap.ParentDisplayName ?? string.Empty).Trim();

            bool isSub = snap.IsSubcategory || !string.IsNullOrEmpty(parent);
            CategoryTabKind kind = snap.Kind;
            if (kind == 0 && isSub)
            {
                kind = CategoryTabKind.StockSubcategoryTab;
            }

            var entry = new CategoryTabEntry
            {
                Kind = kind,
                FilterKey = filterKey,
                ParentDisplayName = string.IsNullOrEmpty(parent) ? null : parent,
                DisplayName = displayName,
                SearchTerms = BuildSearchTerms(displayName, parent, snap.Aliases),
                IconName = snap.IconName,
                VerifiedItemCount = snap.ItemCount,
                IsSubcategory = isSub,
                SourceTag = string.IsNullOrWhiteSpace(snap.SourceTag) ? defaultSourceTag : snap.SourceTag
            };

            if (byKey.TryGetValue(filterKey, out CategoryTabEntry existing))
            {
                byKey[filterKey] = Prefer(existing, entry);
            }
            else
            {
                byKey[filterKey] = entry;
            }

            if (snap.Subcategories != null)
            {
                for (int i = 0; i < snap.Subcategories.Count; i++)
                {
                    CategoryTabSnapshot child = snap.Subcategories[i];
                    if (child == null)
                    {
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(child.ParentDisplayName))
                    {
                        child.ParentDisplayName = displayName;
                    }

                    child.IsSubcategory = true;
                    AppendSnapshot(byKey, child, defaultSourceTag, displayName);
                }
            }
        }

        private static CategoryTabEntry Prefer(CategoryTabEntry a, CategoryTabEntry b)
        {
            // Prefer known counts over unknown; then higher count; then subcategory specificity.
            int scoreA = PreferScore(a);
            int scoreB = PreferScore(b);
            return scoreB >= scoreA ? b : a;
        }

        private static int PreferScore(CategoryTabEntry e)
        {
            int score = 0;
            if (e.VerifiedItemCount >= 0)
            {
                score += 1000 + e.VerifiedItemCount;
            }

            if (e.IsSubcategory)
            {
                score += 10;
            }

            return score;
        }

        private static string[] BuildSearchTerms(string displayName, string parent, string[] aliases)
        {
            var terms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AddTerm(terms, displayName);
            AddTerm(terms, SplitCamelCase(displayName));
            AddTerm(terms, parent);
            if (aliases != null)
            {
                for (int i = 0; i < aliases.Length; i++)
                {
                    AddTerm(terms, aliases[i]);
                }
            }

            var array = new string[terms.Count];
            terms.CopyTo(array);
            return array;
        }

        private static void AddTerm(HashSet<string> terms, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            terms.Add(value.Trim());
        }

        private static string SplitCamelCase(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            var chars = new List<char>(value.Length + 8);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (i > 0 && char.IsUpper(c) && !char.IsUpper(value[i - 1]))
                {
                    chars.Add(' ');
                }

                chars.Add(c);
            }

            return new string(chars.ToArray());
        }

        private static int CompareEntries(CategoryTabEntry a, CategoryTabEntry b)
        {
            int kind = ((int)a.Kind).CompareTo((int)b.Kind);
            if (kind != 0)
            {
                return kind;
            }

            return string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
