using PartSearchSuggest.PostV1.V2.ModSettings;
using PartSearchSuggest.PostV1.V2.PartsListExperience;

namespace PartSearchSuggest.PostV1.V2
{
    /// <summary>
    /// Composition root for V2 settings + parts-list experience (Track S + Track R ports).
    /// <b>Unused by shipping code</b> — do not call from EditorSearchHook / GameLoad / Harmony.
    /// </summary>
    [PostV1Phase(PostV1Phase.E_V2PartsListAndSettings)]
    internal sealed class V2Services
    {
        public ISettingsPersistence SettingsPersistence { get; }

        public IKoobalSettingsStore SettingsStore { get; }

        public ISettingsTabHost SettingsTabHost { get; }

        public IPartsListSlideExpandController SlideExpand { get; }

        public IPartsListLayout Layout { get; }

        public IPartsListRebuildHost RebuildHost { get; }

        public IPartsListOrganizerBridge OrganizerBridge { get; }

        public IPartsListVirtualizationPort Virtualization { get; }

        /// <param name="useRecordingPorts">
        /// When true, memory settings + recording UI/list ports (exercisable without Unity).
        /// When false, Unwired NotImplemented ports (compile-check / boundary docs).
        /// </param>
        public V2Services(bool useRecordingPorts = true)
        {
            var memory = new MemorySettingsPersistence();
            SettingsPersistence = memory;
            SettingsStore = new KoobalSettingsStore(memory);

            if (useRecordingPorts)
            {
                SettingsTabHost = new RecordingSettingsTabHost();
                SlideExpand = new RecordingPartsListSlideExpandController();
                Layout = new RecordingPartsListLayout();
                RebuildHost = new RecordingPartsListRebuildHost();
                OrganizerBridge = new RecordingPartsListOrganizerBridge();
                Virtualization = new RecordingPartsListVirtualizationPort();
            }
            else
            {
                SettingsTabHost = new UnwiredSettingsTabHost();
                SlideExpand = new UnwiredPartsListSlideExpandController();
                Layout = new UnwiredPartsListLayout();
                RebuildHost = new UnwiredPartsListRebuildHost();
                OrganizerBridge = new UnwiredPartsListOrganizerBridge();
                Virtualization = new UnwiredPartsListVirtualizationPort();
            }

            // V2FeatureGate.* remain false — shipping must not flip.
        }

        /// <summary>
        /// Architecture helper: resolve prefs, compose geometry with dropdown, apply Track S ports.
        /// Does not activate Track R unless effective track is Rebuild (still gate-off in shipping).
        /// </summary>
        public PartsListPanelGeometryIntent ApplySlideExpandFromStore(DropdownSlideContribution dropdown)
        {
            PartsListLayoutPreferences prefs = PartsListLayoutPreferences.FromSettings(SettingsStore.Current);
            PartsListPanelGeometryIntent intent = PartsListLayoutPreferences.Compose(prefs, dropdown);
            SlideExpand.ApplyGeometry(intent);
            Layout.ApplyLayout(prefs);

            if (prefs.VirtualizationExperimental && Virtualization is RecordingPartsListVirtualizationPort recording)
            {
                recording.IsEnabled = true;
            }

            if (prefs.EffectiveTrack == PartsListArchitectureTrack.Rebuild)
            {
                RebuildHost.Activate(prefs);
            }
            else if (RebuildHost.IsActive)
            {
                RebuildHost.Deactivate();
            }

            return intent;
        }
    }
}
