using System;

namespace PartSearchSuggest.PostV1.SearchHistory
{
    /// <summary>
    /// One remembered search query with an id stable across sessions (once persisted).
    /// </summary>
    [PostV1Phase(PostV1Phase.HistoryItemDelete_v09)]
    internal sealed class SearchHistoryEntry
    {
        public SearchHistoryEntry(string id, string query)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("History entry id must be non-empty.", nameof(id));
            }

            Id = id;
            Query = query ?? string.Empty;
        }

        /// <summary>Opaque id (GUID "N" preferred). Stable for the life of this remembered query.</summary>
        public string Id { get; }

        /// <summary>Trimmed query text shown in the history dropdown and used for Remember dedupe.</summary>
        public string Query { get; }

        public override string ToString()
        {
            return Id + "\t" + Query;
        }
    }
}
