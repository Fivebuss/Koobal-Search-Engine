using System;
using System.Collections.Generic;

namespace PartSearchSuggest.PostV1.V2.ModSettings
{
    /// <summary>Settings tab sections for the Koobal options page.</summary>
    [PostV1Phase(PostV1Phase.E_V2PartsListAndSettings)]
    internal enum SettingsSectionId
    {
        PartsList = 0,
        Search = 1,
        History = 2,
        Advanced = 3
    }

    /// <summary>
    /// Host for a first-class KSP Settings tab (GameSettings / DialogGUI / etc. at wire-up).
    /// Unwired in architecture — no Unity.
    /// </summary>
    [PostV1Phase(PostV1Phase.E_V2PartsListAndSettings)]
    internal interface ISettingsTabHost
    {
        /// <summary>Whether the host believes the tab is registered / visible.</summary>
        bool IsTabRegistered { get; }

        /// <summary>Register the Koobal page with stock/mod settings UI.</summary>
        void RegisterTab(string displayName);

        /// <summary>Unregister on teardown.</summary>
        void UnregisterTab();

        /// <summary>Rebuild section controls from current store snapshot.</summary>
        void BindFromStore(IKoobalSettingsStore store);

        /// <summary>Notify that a section's controls should refresh.</summary>
        void RefreshSection(SettingsSectionId section);
    }

    /// <summary>Descriptor for one settings section (IA only until live UI).</summary>
    [PostV1Phase(PostV1Phase.E_V2PartsListAndSettings)]
    internal sealed class SettingsSectionDescriptor
    {
        public SettingsSectionId Id { get; set; }

        public string Title { get; set; }

        public string Summary { get; set; }

        public IReadOnlyList<string> OptionKeys { get; set; }
    }

    /// <summary>Static IA for the Settings tab — shared by Track S and Track R.</summary>
    [PostV1Phase(PostV1Phase.E_V2PartsListAndSettings)]
    internal static class SettingsTabInformationArchitecture
    {
        public static IReadOnlyList<SettingsSectionDescriptor> Sections { get; } =
            new[]
            {
                new SettingsSectionDescriptor
                {
                    Id = SettingsSectionId.PartsList,
                    Title = "Parts List",
                    Summary =
                        "Slide-expand (Track S), icon size, list style, organizer compatibility; "
                        + "Rebuild track experimental only after go/no-go.",
                    OptionKeys = new[]
                    {
                        "SlideExpandEnabled",
                        "ExpandWidthAmount",
                        "ExpandHeightAmount",
                        "IconSize",
                        "ListStyle",
                        "OrganizerCompatibilityMode",
                        "ArchitectureTrack",
                        "VirtualizationExperimental"
                    }
                },
                new SettingsSectionDescriptor
                {
                    Id = SettingsSectionId.Search,
                    Title = "Search",
                    Summary = "Suggestion density and related search UX (placeholders until wired).",
                    OptionKeys = new[] { "SearchSuggestionDensity" }
                },
                new SettingsSectionDescriptor
                {
                    Id = SettingsSectionId.History,
                    Title = "History",
                    Summary = "History cap; per-row delete ships ~0.9 separately.",
                    OptionKeys = new[] { "HistoryMaxEntries" }
                },
                new SettingsSectionDescriptor
                {
                    Id = SettingsSectionId.Advanced,
                    Title = "Advanced",
                    Summary = "Force Track S, allow Track R experimental, diagnostics.",
                    OptionKeys = new[]
                    {
                        "ForceSlideExpandTrack",
                        "AllowRebuildExperimental"
                    }
                }
            };
    }

    /// <summary>Throws — documents GameSettings / DialogGUI boundary.</summary>
    [PostV1Phase(PostV1Phase.E_V2PartsListAndSettings)]
    internal sealed class UnwiredSettingsTabHost : ISettingsTabHost
    {
        public bool IsTabRegistered => false;

        public void RegisterTab(string displayName)
        {
            throw new NotImplementedException(
                "UnwiredSettingsTabHost.RegisterTab(" + displayName
                + ") — wire GameSettings page or DialogGUI options host.");
        }

        public void UnregisterTab()
        {
            throw new NotImplementedException(
                "UnwiredSettingsTabHost.UnregisterTab — wire settings teardown.");
        }

        public void BindFromStore(IKoobalSettingsStore store)
        {
            throw new NotImplementedException(
                "UnwiredSettingsTabHost.BindFromStore — wire section controls from KoobalSettingsModel.");
        }

        public void RefreshSection(SettingsSectionId section)
        {
            throw new NotImplementedException(
                "UnwiredSettingsTabHost.RefreshSection(" + section + ") — wire section rebuild.");
        }
    }

    /// <summary>Recording double for architecture self-check (no Unity).</summary>
    [PostV1Phase(PostV1Phase.E_V2PartsListAndSettings)]
    internal sealed class RecordingSettingsTabHost : ISettingsTabHost
    {
        public bool IsTabRegistered { get; private set; }

        public string LastRegisteredName { get; private set; }

        public int BindCount { get; private set; }

        public List<SettingsSectionId> RefreshedSections { get; } = new List<SettingsSectionId>();

        public KoobalSettingsModel LastBoundSnapshot { get; private set; }

        public void RegisterTab(string displayName)
        {
            LastRegisteredName = displayName ?? string.Empty;
            IsTabRegistered = true;
        }

        public void UnregisterTab()
        {
            IsTabRegistered = false;
        }

        public void BindFromStore(IKoobalSettingsStore store)
        {
            if (store == null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            LastBoundSnapshot = store.Current;
            BindCount++;
        }

        public void RefreshSection(SettingsSectionId section)
        {
            RefreshedSections.Add(section);
        }
    }
}
