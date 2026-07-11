using System;
using System.Collections.Generic;
using System.Linq;

namespace PartSearchSuggest.PostV1.SearchHistory
{
    /// <summary>
    /// Search history store: remember, per-item remove, clear-all, snapshot for dropdown binding.
    /// Mirrors shipping <c>SearchHistory</c> semantics with stable ids.
    /// </summary>
    [PostV1Phase(PostV1Phase.HistoryItemDelete_v09)]
    internal interface ISearchHistoryStore
    {
        /// <summary>Newest-first snapshot for dropdown binding. Never null.</summary>
        IReadOnlyList<SearchHistoryEntry> Snapshot { get; }

        int Count { get; }

        /// <summary>
        /// Remember a query (min length 2). Dedupes case-insensitively, moves to top,
        /// preserves existing id when deduping. Persists on change.
        /// </summary>
        /// <returns>True when the list changed.</returns>
        bool Remember(string query);

        /// <summary>Remove by stable id. Persists when found.</summary>
        /// <returns>True when an entry was removed.</returns>
        bool Remove(string entryId);

        /// <summary>Remove by current snapshot index (0 = newest). Persists when in range.</summary>
        /// <returns>True when an entry was removed.</returns>
        bool RemoveAt(int index);

        /// <summary>Clear all entries (maps to existing header trashcan). Persists when non-empty.</summary>
        void ClearAll();

        /// <summary>
        /// Filter snapshot for dropdown: empty query → all (capped); else substring match.
        /// </summary>
        IEnumerable<SearchHistoryEntry> Match(string query, int maxResults);

        /// <summary>Reload from persistence (e.g. after external file change). Rare.</summary>
        void Reload();
    }

    /// <summary>
    /// Concrete store against an injected <see cref="IHistoryPersistence"/>.
    /// Pure list mutation + save — no Unity / KSP types.
    /// </summary>
    [PostV1Phase(PostV1Phase.HistoryItemDelete_v09)]
    internal sealed class SearchHistoryStore : ISearchHistoryStore
    {
        /// <summary>Matches shipping <c>SearchHistory.MaxEntries</c>.</summary>
        public const int DefaultMaxEntries = 12;

        /// <summary>Matches shipping min length for Remember / commit.</summary>
        public const int MinQueryLength = 2;

        private readonly IHistoryPersistence _persistence;
        private readonly int _maxEntries;
        private readonly Func<string> _idFactory;
        private readonly List<SearchHistoryEntry> _entries = new List<SearchHistoryEntry>();

        public SearchHistoryStore(
            IHistoryPersistence persistence,
            int maxEntries = DefaultMaxEntries,
            Func<string> idFactory = null,
            bool loadOnConstruct = true)
        {
            _persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
            if (maxEntries < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxEntries), "Max entries must be >= 1.");
            }

            _maxEntries = maxEntries;
            _idFactory = idFactory ?? HistoryCfgCodec.NewId;

            if (loadOnConstruct)
            {
                Reload();
            }
        }

        public IReadOnlyList<SearchHistoryEntry> Snapshot => _entries.ToArray();

        public int Count => _entries.Count;

        public bool Remember(string query)
        {
            string trimmed = NormalizeQuery(query);
            if (trimmed.Length < MinQueryLength)
            {
                return false;
            }

            int existing = FindIndexByQuery(trimmed);
            if (existing >= 0)
            {
                SearchHistoryEntry keep = _entries[existing];
                if (existing == 0 && string.Equals(keep.Query, trimmed, StringComparison.Ordinal))
                {
                    // Already newest with identical text — no persist churn.
                    return false;
                }

                _entries.RemoveAt(existing);
                // Preserve id; refresh query casing to the newly typed form.
                _entries.Insert(0, new SearchHistoryEntry(keep.Id, trimmed));
                TrimToMax();
                Persist();
                return true;
            }

            _entries.Insert(0, new SearchHistoryEntry(_idFactory(), trimmed));
            TrimToMax();
            Persist();
            return true;
        }

        public bool Remove(string entryId)
        {
            if (string.IsNullOrEmpty(entryId))
            {
                return false;
            }

            int index = FindIndexById(entryId);
            if (index < 0)
            {
                return false;
            }

            _entries.RemoveAt(index);
            Persist();
            return true;
        }

        public bool RemoveAt(int index)
        {
            if (index < 0 || index >= _entries.Count)
            {
                return false;
            }

            _entries.RemoveAt(index);
            Persist();
            return true;
        }

        public void ClearAll()
        {
            if (_entries.Count == 0)
            {
                return;
            }

            _entries.Clear();
            Persist();
        }

        public IEnumerable<SearchHistoryEntry> Match(string query, int maxResults)
        {
            if (maxResults < 0)
            {
                maxResults = 0;
            }

            string trimmed = (query ?? string.Empty).Trim();
            IEnumerable<SearchHistoryEntry> source = _entries;

            if (!string.IsNullOrEmpty(trimmed))
            {
                source = _entries.Where(
                    e => e.Query.IndexOf(trimmed, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            return source.Take(maxResults);
        }

        public void Reload()
        {
            _entries.Clear();
            IReadOnlyList<SearchHistoryEntry> loaded = _persistence.Load()
                ?? Array.Empty<SearchHistoryEntry>();

            // Re-parse via codec path is caller's job for raw lines; here we accept DTOs
            // but still enforce max + normalize + unique ids.
            var seenIds = new HashSet<string>(StringComparer.Ordinal);
            bool dirty = false;

            for (int i = 0; i < loaded.Count; i++)
            {
                SearchHistoryEntry entry = loaded[i];
                if (entry == null)
                {
                    continue;
                }

                string query = NormalizeQuery(entry.Query);
                if (query.Length == 0)
                {
                    dirty = true;
                    continue;
                }

                string id = entry.Id;
                if (string.IsNullOrEmpty(id) || !seenIds.Add(id))
                {
                    id = _idFactory();
                    while (!seenIds.Add(id))
                    {
                        id = _idFactory();
                    }

                    dirty = true;
                }

                if (!string.Equals(query, entry.Query, StringComparison.Ordinal)
                    || !string.Equals(id, entry.Id, StringComparison.Ordinal))
                {
                    dirty = true;
                }

                _entries.Add(new SearchHistoryEntry(id, query));
            }

            if (_entries.Count > _maxEntries)
            {
                _entries.RemoveRange(_maxEntries, _entries.Count - _maxEntries);
                dirty = true;
            }

            // If persistence returned legacy bare lines via a file adapter that already
            // minted ids, Save is optional; if we had to repair, persist once.
            if (dirty)
            {
                Persist();
            }
        }

        private void TrimToMax()
        {
            while (_entries.Count > _maxEntries)
            {
                _entries.RemoveAt(_entries.Count - 1);
            }
        }

        private void Persist()
        {
            _persistence.Save(_entries.ToArray());
        }

        private int FindIndexById(string entryId)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (string.Equals(_entries[i].Id, entryId, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        private int FindIndexByQuery(string trimmedQuery)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (string.Equals(_entries[i].Query, trimmedQuery, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>Trim; strip tabs so cfg lines stay unambiguous.</summary>
        internal static string NormalizeQuery(string query)
        {
            if (query == null)
            {
                return string.Empty;
            }

            string trimmed = query.Trim();
            if (trimmed.IndexOf(HistoryCfgCodec.Separator) >= 0)
            {
                trimmed = trimmed.Replace(HistoryCfgCodec.Separator.ToString(), " ");
                trimmed = trimmed.Trim();
            }

            return trimmed;
        }
    }
}
