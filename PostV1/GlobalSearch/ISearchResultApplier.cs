using System;
using System.Collections.Generic;

namespace PartSearchSuggest.PostV1.GlobalSearch
{
    /// <summary>
    /// Applies tightened Enter matches to the surface list (id-set filter).
    /// Editor live path: SearchFilterResult + EditorPartListFilter; other surfaces TBD at wire-up.
    /// </summary>
    [PostV1Phase(PostV1Phase.D_GlobalSearchHalt)]
    internal interface ISearchResultApplier
    {
        /// <summary>Currently applied custom filter id, if any.</summary>
        string ActiveFilterId { get; }

        /// <summary>
        /// Apply id-set filter for <paramref name="matches"/>. Returns false when zero matches
        /// (caller must leave list unchanged — no loose stock fallback).
        /// </summary>
        bool Apply(
            SearchBarSurface surface,
            IReadOnlyList<TightMatchResult> matches,
            string queryDisplayText);

        /// <summary>Clear custom filter / restore unfiltered list for the surface.</summary>
        bool ClearFilter(SearchBarSurface surface);
    }

    /// <summary>Throws — live apply is surface-specific.</summary>
    [PostV1Phase(PostV1Phase.D_GlobalSearchHalt)]
    internal sealed class UnwiredSearchResultApplier : ISearchResultApplier
    {
        public string ActiveFilterId => null;

        public bool Apply(
            SearchBarSurface surface,
            IReadOnlyList<TightMatchResult> matches,
            string queryDisplayText)
        {
            throw new NotImplementedException(
                "UnwiredSearchResultApplier.Apply(" + surface
                + ") — wire SearchFilterResult / list filter; no stock fallback on zero matches.");
        }

        public bool ClearFilter(SearchBarSurface surface)
        {
            throw new NotImplementedException(
                "UnwiredSearchResultApplier.ClearFilter(" + surface + ").");
        }
    }

    /// <summary>
    /// Records apply/clear and tracks active id set for self-checks.
    /// Includes pure id-set helper used by future live adapters.
    /// </summary>
    [PostV1Phase(PostV1Phase.D_GlobalSearchHalt)]
    internal sealed class RecordingSearchResultApplier : ISearchResultApplier
    {
        public const string FilterIdPrefix = "KoobalGlobalSearch_";

        public List<string> CallLog { get; } = new List<string>();

        public string ActiveFilterId { get; private set; }

        public SearchBarSurface? ActiveSurface { get; private set; }

        public HashSet<string> ActiveMatchIds { get; } = new HashSet<string>(StringComparer.Ordinal);

        public bool Apply(
            SearchBarSurface surface,
            IReadOnlyList<TightMatchResult> matches,
            string queryDisplayText)
        {
            if (matches == null || matches.Count == 0)
            {
                CallLog.Add("Apply:empty:" + surface);
                return false;
            }

            ActiveMatchIds.Clear();
            for (int i = 0; i < matches.Count; i++)
            {
                TightMatchResult m = matches[i];
                if (m != null && !string.IsNullOrEmpty(m.Id))
                {
                    ActiveMatchIds.Add(m.Id);
                }
            }

            if (ActiveMatchIds.Count == 0)
            {
                CallLog.Add("Apply:no-ids:" + surface);
                return false;
            }

            ActiveSurface = surface;
            ActiveFilterId = FilterIdPrefix + surface;
            CallLog.Add(
                "Apply:" + surface + ":" + ActiveMatchIds.Count + ":" + (queryDisplayText ?? string.Empty));
            return true;
        }

        public bool ClearFilter(SearchBarSurface surface)
        {
            bool had = ActiveFilterId != null && ActiveSurface == surface;
            if (ActiveSurface == surface)
            {
                ActiveFilterId = null;
                ActiveSurface = null;
                ActiveMatchIds.Clear();
            }

            CallLog.Add("ClearFilter:" + surface + ":" + had);
            return true;
        }

        /// <summary>
        /// Pure helper: keep candidates whose id is in <paramref name="matchIds"/>.
        /// Live editor adapters build EditorPartListFilter predicates the same way.
        /// </summary>
        public static List<string> FilterIds(IEnumerable<string> candidateIds, ISet<string> matchIds)
        {
            var result = new List<string>();
            if (candidateIds == null || matchIds == null || matchIds.Count == 0)
            {
                return result;
            }

            foreach (string id in candidateIds)
            {
                if (!string.IsNullOrEmpty(id) && matchIds.Contains(id))
                {
                    result.Add(id);
                }
            }

            return result;
        }
    }
}
