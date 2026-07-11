using System.Collections.Generic;

namespace PartSearchSuggest.PostV1.GlobalSearch
{
    /// <summary>
    /// Pure id-set list filter used by Recording applier and future live adapters.
    /// Separated so non-editor surfaces can reuse the same apply math without PartCategorizer.
    /// </summary>
    [PostV1Phase(PostV1Phase.D_GlobalSearchHalt)]
    internal static class SnapshotListFilter
    {
        /// <summary>
        /// Returns items whose <see cref="SearchableItemSnapshot.Id"/> is in <paramref name="matchIds"/>.
        /// </summary>
        public static List<SearchableItemSnapshot> Filter(
            IReadOnlyList<SearchableItemSnapshot> candidates,
            ISet<string> matchIds)
        {
            var result = new List<SearchableItemSnapshot>();
            if (candidates == null || matchIds == null || matchIds.Count == 0)
            {
                return result;
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                SearchableItemSnapshot item = candidates[i];
                if (item != null && !string.IsNullOrEmpty(item.Id) && matchIds.Contains(item.Id))
                {
                    result.Add(item);
                }
            }

            return result;
        }

        /// <summary>
        /// Build match id set from tight results (editor SearchFilterResult pattern).
        /// </summary>
        public static HashSet<string> ToIdSet(IReadOnlyList<TightMatchResult> matches)
        {
            var set = new HashSet<string>(System.StringComparer.Ordinal);
            if (matches == null)
            {
                return set;
            }

            for (int i = 0; i < matches.Count; i++)
            {
                TightMatchResult m = matches[i];
                if (m != null && !string.IsNullOrEmpty(m.Id))
                {
                    set.Add(m.Id);
                }
            }

            return set;
        }
    }
}
