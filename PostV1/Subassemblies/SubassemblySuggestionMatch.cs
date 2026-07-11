using System;
using System.Collections.Generic;
using PartSearchSuggest.PostV1.Shared;

namespace PartSearchSuggest.PostV1.Subassemblies
{
    /// <summary>
    /// Pure match/rank helpers for subassembly craft suggestions (name / description / author).
    /// </summary>
    [PostV1Phase(PostV1Phase.C_Subassemblies)]
    internal static class SubassemblySuggestionMatch
    {
        public static int ScoreEntry(SubassemblyCraftEntry entry, string[] words, bool preferTitle)
        {
            if (entry == null || !entry.IsValidated)
            {
                return TextMatchScoring.NoMatch;
            }

            return TextMatchScoring.AggregateWordScores(
                words,
                word => TextMatchScoring.ScoreTitleDescriptionAuthor(
                    word,
                    entry.Title,
                    entry.Description,
                    entry.Author,
                    preferTitle));
        }

        public static SubassemblyCraftSuggestion ToSuggestion(SubassemblyCraftEntry entry, int rankScore)
        {
            string facility = string.IsNullOrWhiteSpace(entry.FacilityFolder)
                ? "Subassembly"
                : entry.FacilityFolder.Trim();

            return new SubassemblyCraftSuggestion
            {
                QueryText = entry.Title,
                DisplayText = entry.Title,
                MatchReason = facility
                    + (string.IsNullOrWhiteSpace(entry.Author) ? string.Empty : " · " + entry.Author.Trim()),
                FilterKey = entry.CraftPath,
                ApplyPayloadId = entry.CraftPath,
                RankScore = rankScore,
                SourceEntry = entry
            };
        }

        public static List<SubassemblyCraftSuggestion> Dedup(IReadOnlyList<SubassemblyCraftSuggestion> suggestions)
        {
            if (suggestions == null || suggestions.Count == 0)
            {
                return new List<SubassemblyCraftSuggestion>();
            }

            var sorted = new List<SubassemblyCraftSuggestion>(suggestions);
            sorted.Sort((a, b) =>
            {
                int rank = a.RankScore.CompareTo(b.RankScore);
                if (rank != 0)
                {
                    return rank;
                }

                return string.Compare(a.DisplayText, b.DisplayText, StringComparison.OrdinalIgnoreCase);
            });

            var kept = new List<SubassemblyCraftSuggestion>(sorted.Count);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < sorted.Count; i++)
            {
                SubassemblyCraftSuggestion s = sorted[i];
                if (s == null)
                {
                    continue;
                }

                string key = s.FilterKey ?? s.ApplyPayloadId;
                if (string.IsNullOrEmpty(key) || !seen.Add(key))
                {
                    continue;
                }

                kept.Add(s);
            }

            return kept;
        }

        public static List<SubassemblyCraftEntry> BuildEntries(SubassemblyCraftSnapshotSet set)
        {
            var result = new List<SubassemblyCraftEntry>();
            if (set?.Crafts == null)
            {
                return result;
            }

            var byPath = new Dictionary<string, SubassemblyCraftEntry>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < set.Crafts.Count; i++)
            {
                SubassemblyCraftSnapshot snap = set.Crafts[i];
                if (snap == null || string.IsNullOrWhiteSpace(snap.CraftPath))
                {
                    continue;
                }

                string title = string.IsNullOrWhiteSpace(snap.Title)
                    ? System.IO.Path.GetFileNameWithoutExtension(snap.CraftPath)
                    : snap.Title.Trim();

                byPath[snap.CraftPath] = new SubassemblyCraftEntry
                {
                    CraftPath = snap.CraftPath.Trim(),
                    Title = title,
                    Description = snap.Description,
                    Author = snap.Author,
                    FacilityFolder = snap.FacilityFolder,
                    IsValidated = snap.IsValidated,
                    IconName = snap.IconName
                };
            }

            foreach (KeyValuePair<string, SubassemblyCraftEntry> pair in byPath)
            {
                result.Add(pair.Value);
            }

            return result;
        }
    }
}
