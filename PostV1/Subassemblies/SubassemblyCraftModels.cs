using System.Collections.Generic;

namespace PartSearchSuggest.PostV1.Subassemblies
{
    /// <summary>
    /// Indexed subassembly craft under the current save's Subassemblies folders (VAB/SPH layout).
    /// </summary>
    /// <remarks>
    /// Index at editor entry + incremental save/delete only. Never rebuild part/metadata/categorizer
    /// indexes on subassembly lifecycle events.
    /// </remarks>
    [PostV1Phase(PostV1Phase.C_Subassemblies)]
    internal sealed class SubassemblyCraftEntry
    {
        /// <summary>Full craft path used as stable FilterKey for match / apply / delete remove.</summary>
        public string CraftPath { get; set; }

        /// <summary>Player-facing craft title from ShipTemplate / craft file.</summary>
        public string Title { get; set; }

        /// <summary>Optional craft description for ranking (when available in ShipTemplate).</summary>
        public string Description { get; set; }

        /// <summary>Optional author / ship designer string when present.</summary>
        public string Author { get; set; }

        /// <summary>VAB, SPH, or other facility folder tag for scoped refresh.</summary>
        public string FacilityFolder { get; set; }

        /// <summary>True when craft/ShipTemplate validated at index time.</summary>
        public bool IsValidated { get; set; }

        /// <summary>Optional icon/thumbnail key; thumbnails remain a separate deferred track.</summary>
        public string IconName { get; set; }
    }

    /// <summary>
    /// Injected craft list snapshot for building <see cref="SubassemblySuggestionIndex"/> without folder IO.
    /// </summary>
    [PostV1Phase(PostV1Phase.C_Subassemblies)]
    internal sealed class SubassemblyCraftSnapshot
    {
        public string CraftPath { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public string Author { get; set; }

        public string FacilityFolder { get; set; }

        public bool IsValidated { get; set; } = true;

        public string IconName { get; set; }
    }

    [PostV1Phase(PostV1Phase.C_Subassemblies)]
    internal sealed class SubassemblyCraftSnapshotSet
    {
        public List<SubassemblyCraftSnapshot> Crafts { get; set; } = new List<SubassemblyCraftSnapshot>();

        public string SourceTag { get; set; } = "Injected";
    }

    /// <summary>
    /// Dropdown DTO for a Phase C subassembly craft suggestion.
    /// </summary>
    [PostV1Phase(PostV1Phase.C_Subassemblies)]
    internal sealed class SubassemblyCraftSuggestion
    {
        public string QueryText { get; set; }

        public string DisplayText { get; set; }

        public string MatchReason { get; set; }

        /// <summary>Must equal <see cref="SubassemblyCraftEntry.CraftPath"/> for apply.</summary>
        public string FilterKey { get; set; }

        public string ApplyPayloadId { get; set; }

        public int RankScore { get; set; } = 999;

        public SubassemblyCraftEntry SourceEntry { get; set; }

        /// <summary>
        /// Drop missing files from suggestions. Prefer overload with <see cref="ISubassemblyMatcher"/>.
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(FilterKey ?? ApplyPayloadId)
                && !string.IsNullOrWhiteSpace(DisplayText);
        }

        public bool IsValid(ISubassemblyMatcher matcher)
        {
            if (!IsValid())
            {
                return false;
            }

            string path = FilterKey ?? ApplyPayloadId;
            if (matcher != null && !matcher.IsCraftValid(path))
            {
                return false;
            }

            return true;
        }
    }
}
