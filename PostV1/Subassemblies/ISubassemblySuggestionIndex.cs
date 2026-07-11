using System;
using System.Collections.Generic;
using PartSearchSuggest.PostV1.Shared;

namespace PartSearchSuggest.PostV1.Subassemblies
{
    /// <summary>
    /// Shared matcher for subassembly craft identity and validity.
    /// Index, <c>IsValid</c>, apply, and subtitle MUST share this contract.
    /// </summary>
    [PostV1Phase(PostV1Phase.C_Subassemblies)]
    internal interface ISubassemblyMatcher
    {
        /// <summary>True when the craft file exists and ShipTemplate (or equivalent) validates.</summary>
        bool IsCraftValid(string craftPath);

        /// <summary>Title used for display/ranking; null if unreadable.</summary>
        string TryGetCraftTitle(string craftPath);
    }

    /// <summary>Architecture stub — always invalid; no filesystem or KSP craft API calls.</summary>
    [PostV1Phase(PostV1Phase.C_Subassemblies)]
    internal sealed class SubassemblyMatcherStub : ISubassemblyMatcher
    {
        public bool IsCraftValid(string craftPath)
        {
            return false;
        }

        public string TryGetCraftTitle(string craftPath)
        {
            return null;
        }
    }

    /// <summary>
    /// Snapshot-backed matcher: validity = entry present + IsValidated in the index dictionary.
    /// </summary>
    [PostV1Phase(PostV1Phase.C_Subassemblies)]
    internal sealed class SnapshotSubassemblyMatcher : ISubassemblyMatcher
    {
        private readonly Dictionary<string, SubassemblyCraftEntry> _byPath =
            new Dictionary<string, SubassemblyCraftEntry>(StringComparer.OrdinalIgnoreCase);

        public void ReplaceEntries(IEnumerable<SubassemblyCraftEntry> entries)
        {
            _byPath.Clear();
            if (entries == null)
            {
                return;
            }

            foreach (SubassemblyCraftEntry entry in entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.CraftPath))
                {
                    continue;
                }

                _byPath[entry.CraftPath] = entry;
            }
        }

        public bool IsCraftValid(string craftPath)
        {
            return !string.IsNullOrWhiteSpace(craftPath)
                && _byPath.TryGetValue(craftPath, out SubassemblyCraftEntry entry)
                && entry.IsValidated;
        }

        public string TryGetCraftTitle(string craftPath)
        {
            if (string.IsNullOrWhiteSpace(craftPath) || !_byPath.TryGetValue(craftPath, out SubassemblyCraftEntry entry))
            {
                return null;
            }

            return entry.Title;
        }
    }

    /// <summary>
    /// Phase C subassembly craft suggestion index (folder-scoped).
    /// </summary>
    /// <remarks>
    /// Incremental <c>AddOrUpdate</c> / <c>Remove</c> only on save/delete. Full scan at editor entry
    /// must be frame-budgeted for large libraries (plan acceptance ≥50–100 SAs).
    /// </remarks>
    [PostV1Phase(PostV1Phase.C_Subassemblies)]
    internal interface ISubassemblySuggestionIndex
    {
        int EntryCount { get; }

        /// <summary>Scan current save Subassemblies folders (editor entry) when wired.</summary>
        void Build();

        void Clear();

        /// <summary>Incremental upsert after stock save confirms a craft write.</summary>
        void AddOrUpdate(string craftPath);

        /// <summary>Incremental remove after stock confirms delete (prefer postfix timing).</summary>
        void Remove(string craftPath);

        IEnumerable<SubassemblyCraftSuggestion> Match(string query, int maxResults);
    }

    [PostV1Phase(PostV1Phase.C_Subassemblies)]
    internal interface ISubassemblySuggestionIndexBuild
    {
        void BuildFromSnapshot(SubassemblyCraftSnapshotSet set);

        void AddOrUpdateEntry(SubassemblyCraftEntry entry);
    }

    /// <summary>
    /// Concrete index from injected craft snapshots + incremental AddOrUpdate/Remove.
    /// </summary>
    [PostV1Phase(PostV1Phase.C_Subassemblies)]
    internal sealed class SubassemblySuggestionIndex : ISubassemblySuggestionIndex, ISubassemblySuggestionIndexBuild
    {
        private readonly Dictionary<string, SubassemblyCraftEntry> _byPath =
            new Dictionary<string, SubassemblyCraftEntry>(StringComparer.OrdinalIgnoreCase);

        private readonly SnapshotSubassemblyMatcher _matcher = new SnapshotSubassemblyMatcher();
        private readonly ISubassemblyMatcher _externalMatcher;

        /// <param name="externalMatcher">
        /// Optional live filesystem/ShipTemplate matcher. When null, snapshot matcher is used.
        /// </param>
        public SubassemblySuggestionIndex(ISubassemblyMatcher externalMatcher = null)
        {
            _externalMatcher = externalMatcher;
        }

        public ISubassemblyMatcher Matcher => _externalMatcher ?? _matcher;

        public int EntryCount => _byPath.Count;

        public void Build()
        {
            // Live folder scan belongs at wire-up. Prefer BuildFromSnapshot from an editor-entry adapter.
        }

        public void BuildFromSnapshot(SubassemblyCraftSnapshotSet set)
        {
            _byPath.Clear();
            List<SubassemblyCraftEntry> entries = SubassemblySuggestionMatch.BuildEntries(set);
            for (int i = 0; i < entries.Count; i++)
            {
                SubassemblyCraftEntry entry = entries[i];
                _byPath[entry.CraftPath] = entry;
            }

            SyncMatcher();
        }

        public void Clear()
        {
            _byPath.Clear();
            SyncMatcher();
        }

        public void AddOrUpdate(string craftPath)
        {
            if (string.IsNullOrWhiteSpace(craftPath))
            {
                return;
            }

            string title = Matcher.TryGetCraftTitle(craftPath);
            bool valid = Matcher.IsCraftValid(craftPath);
            if (!valid && _externalMatcher == null)
            {
                // Snapshot-only mode: keep path with filename title if already known; else upsert shell.
                if (!_byPath.ContainsKey(craftPath))
                {
                    _byPath[craftPath] = new SubassemblyCraftEntry
                    {
                        CraftPath = craftPath,
                        Title = System.IO.Path.GetFileNameWithoutExtension(craftPath),
                        IsValidated = true
                    };
                    SyncMatcher();
                }

                return;
            }

            if (!valid)
            {
                _byPath.Remove(craftPath);
                SyncMatcher();
                return;
            }

            if (_byPath.TryGetValue(craftPath, out SubassemblyCraftEntry existing))
            {
                if (!string.IsNullOrWhiteSpace(title))
                {
                    existing.Title = title;
                }

                existing.IsValidated = true;
            }
            else
            {
                _byPath[craftPath] = new SubassemblyCraftEntry
                {
                    CraftPath = craftPath,
                    Title = string.IsNullOrWhiteSpace(title)
                        ? System.IO.Path.GetFileNameWithoutExtension(craftPath)
                        : title,
                    IsValidated = true
                };
            }

            SyncMatcher();
        }

        public void AddOrUpdateEntry(SubassemblyCraftEntry entry)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.CraftPath))
            {
                return;
            }

            _byPath[entry.CraftPath] = entry;
            SyncMatcher();
        }

        public void Remove(string craftPath)
        {
            if (!string.IsNullOrEmpty(craftPath))
            {
                _byPath.Remove(craftPath);
                SyncMatcher();
            }
        }

        public IEnumerable<SubassemblyCraftSuggestion> Match(string query, int maxResults)
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

            bool preferTitle = trimmed.Length <= PostV1QueryGuards.TitleFirstMaxQueryLength;
            var raw = new List<SubassemblyCraftSuggestion>();

            foreach (KeyValuePair<string, SubassemblyCraftEntry> pair in _byPath)
            {
                SubassemblyCraftEntry entry = pair.Value;
                if (entry == null || !entry.IsValidated)
                {
                    continue;
                }

                if (!Matcher.IsCraftValid(entry.CraftPath))
                {
                    continue;
                }

                int rank = SubassemblySuggestionMatch.ScoreEntry(entry, words, preferTitle);
                if (rank < 0)
                {
                    continue;
                }

                raw.Add(SubassemblySuggestionMatch.ToSuggestion(entry, rank));
            }

            List<SubassemblyCraftSuggestion> deduped = SubassemblySuggestionMatch.Dedup(raw);
            deduped.Sort((a, b) =>
            {
                int rank = a.RankScore.CompareTo(b.RankScore);
                if (rank != 0)
                {
                    return rank;
                }

                return string.Compare(a.DisplayText, b.DisplayText, StringComparison.OrdinalIgnoreCase);
            });

            int count = 0;
            for (int i = 0; i < deduped.Count; i++)
            {
                SubassemblyCraftSuggestion suggestion = deduped[i];
                if (suggestion == null || !suggestion.IsValid(Matcher))
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

        private void SyncMatcher()
        {
            _matcher.ReplaceEntries(_byPath.Values);
        }
    }

    /// <summary>Architecture stub — empty index; no folder IO.</summary>
    [PostV1Phase(PostV1Phase.C_Subassemblies)]
    internal sealed class SubassemblySuggestionIndexStub : ISubassemblySuggestionIndex
    {
        private readonly Dictionary<string, SubassemblyCraftEntry> _byPath =
            new Dictionary<string, SubassemblyCraftEntry>(StringComparer.OrdinalIgnoreCase);

        public int EntryCount => _byPath.Count;

        public void Build()
        {
            _byPath.Clear();
        }

        public void Clear()
        {
            _byPath.Clear();
        }

        public void AddOrUpdate(string craftPath)
        {
            // No-op until Phase C live folder adapter; signature documents incremental contract.
        }

        public void Remove(string craftPath)
        {
            if (!string.IsNullOrEmpty(craftPath))
            {
                _byPath.Remove(craftPath);
            }
        }

        public IEnumerable<SubassemblyCraftSuggestion> Match(string query, int maxResults)
        {
            yield break;
        }
    }
}
