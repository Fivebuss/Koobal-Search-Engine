using System;
using System.Collections.Generic;
using System.Text;

namespace PartSearchSuggest.PostV1.SearchHistory
{
    /// <summary>
    /// Load/save port for history entries. Live wire-up writes
    /// <c>GameData/KoobalSearchEngine/PluginData/History.cfg</c>; architecture uses memory.
    /// </summary>
    [PostV1Phase(PostV1Phase.HistoryItemDelete_v09)]
    internal interface IHistoryPersistence
    {
        /// <summary>Load entries in display order (newest first). Never null.</summary>
        IReadOnlyList<SearchHistoryEntry> Load();

        /// <summary>Replace persisted history with <paramref name="entries"/> (newest first).</summary>
        void Save(IReadOnlyList<SearchHistoryEntry> entries);
    }

    /// <summary>
    /// In-memory persistence for architecture self-checks / Recording composition.
    /// </summary>
    [PostV1Phase(PostV1Phase.HistoryItemDelete_v09)]
    internal sealed class MemoryHistoryPersistence : IHistoryPersistence
    {
        private readonly List<SearchHistoryEntry> _stored = new List<SearchHistoryEntry>();

        public List<string> CallLog { get; } = new List<string>();

        public int SaveCount { get; private set; }

        public IReadOnlyList<SearchHistoryEntry> Load()
        {
            CallLog.Add("Load:" + _stored.Count);
            return _stored.ToArray();
        }

        public void Save(IReadOnlyList<SearchHistoryEntry> entries)
        {
            _stored.Clear();
            if (entries != null)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    SearchHistoryEntry entry = entries[i];
                    if (entry != null)
                    {
                        _stored.Add(entry);
                    }
                }
            }

            SaveCount++;
            CallLog.Add("Save:" + _stored.Count);
        }

        /// <summary>Seed without counting as a Save (test helper).</summary>
        public void Seed(IEnumerable<SearchHistoryEntry> entries)
        {
            _stored.Clear();
            if (entries == null)
            {
                return;
            }

            foreach (SearchHistoryEntry entry in entries)
            {
                if (entry != null)
                {
                    _stored.Add(entry);
                }
            }
        }
    }

    /// <summary>
    /// Throws — documents the live file boundary until wire-up.
    /// </summary>
    [PostV1Phase(PostV1Phase.HistoryItemDelete_v09)]
    internal sealed class UnwiredHistoryPersistence : IHistoryPersistence
    {
        public IReadOnlyList<SearchHistoryEntry> Load()
        {
            throw new NotImplementedException(
                "UnwiredHistoryPersistence.Load — wire PluginData/History.cfg (+ legacy migrate).");
        }

        public void Save(IReadOnlyList<SearchHistoryEntry> entries)
        {
            throw new NotImplementedException(
                "UnwiredHistoryPersistence.Save — wire File.WriteAllLines via HistoryCfgCodec.");
        }
    }

    /// <summary>
    /// Pure codec for History.cfg lines: <c>id\tquery</c>, with legacy bare-query migration.
    /// </summary>
    [PostV1Phase(PostV1Phase.HistoryItemDelete_v09)]
    internal static class HistoryCfgCodec
    {
        public const char Separator = '\t';

        /// <summary>
        /// Parse lines into entries. Bare lines (no tab) mint a new id.
        /// Sets <paramref name="legacyLinesMigrated"/> when any bare line was upgraded.
        /// </summary>
        public static List<SearchHistoryEntry> Parse(
            IEnumerable<string> lines,
            out bool legacyLinesMigrated,
            Func<string> idFactory = null)
        {
            legacyLinesMigrated = false;
            var result = new List<SearchHistoryEntry>();
            if (lines == null)
            {
                return result;
            }

            Func<string> mint = idFactory ?? NewId;
            var seenIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (string raw in lines)
            {
                if (raw == null)
                {
                    continue;
                }

                string line = raw.TrimEnd('\r');
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                int tab = line.IndexOf(Separator);
                if (tab <= 0)
                {
                    // Legacy bare query — mint id.
                    string query = line.Trim();
                    if (query.Length == 0)
                    {
                        continue;
                    }

                    string id = mint();
                    while (!seenIds.Add(id))
                    {
                        id = mint();
                    }

                    result.Add(new SearchHistoryEntry(id, query));
                    legacyLinesMigrated = true;
                    continue;
                }

                string parsedId = line.Substring(0, tab).Trim();
                string parsedQuery = line.Substring(tab + 1).Trim();
                if (parsedId.Length == 0 || parsedQuery.Length == 0)
                {
                    continue;
                }

                if (!seenIds.Add(parsedId))
                {
                    // Duplicate id in file — mint a fresh one so Remove(id) stays unambiguous.
                    parsedId = mint();
                    while (!seenIds.Add(parsedId))
                    {
                        parsedId = mint();
                    }

                    legacyLinesMigrated = true;
                }

                result.Add(new SearchHistoryEntry(parsedId, parsedQuery));
            }

            return result;
        }

        /// <summary>Format entries as <c>id\tquery</c> lines (no trailing blank).</summary>
        public static string[] Format(IReadOnlyList<SearchHistoryEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return Array.Empty<string>();
            }

            var lines = new string[entries.Count];
            for (int i = 0; i < entries.Count; i++)
            {
                SearchHistoryEntry entry = entries[i];
                lines[i] = entry == null
                    ? string.Empty
                    : entry.Id + Separator + entry.Query;
            }

            return lines;
        }

        /// <summary>Join formatted lines with newlines (test/debug helper).</summary>
        public static string FormatText(IReadOnlyList<SearchHistoryEntry> entries)
        {
            string[] lines = Format(entries);
            if (lines.Length == 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            for (int i = 0; i < lines.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append('\n');
                }

                sb.Append(lines[i]);
            }

            return sb.ToString();
        }

        public static string NewId()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}
