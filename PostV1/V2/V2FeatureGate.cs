namespace PartSearchSuggest.PostV1.V2
{
    /// <summary>
    /// Feature gates for V2 slide-expand / settings / optional rebuild.
    /// </summary>
    /// <remarks>
    /// <b>Always false in this scaffolding.</b> Shipping must not reference this type until
    /// after v1 and an intentional V2 schedule. Track R (<see cref="EnablePartsListRebuild"/>)
    /// stays off until go/no-go criteria pass — see docs/V2_PARTS_LIST_AND_SETTINGS.md §0.
    /// </remarks>
    [PostV1Phase(PostV1Phase.E_V2PartsListAndSettings)]
    internal static class V2FeatureGate
    {
        /// <summary>Master enable. Scaffold default: off.</summary>
        public const bool Enabled = false;

        /// <summary>V2-1 Settings tab host. Off.</summary>
        public const bool EnableSettingsTab = false;

        /// <summary>Track S — slide-expand geometry. Off.</summary>
        public const bool EnablePartsListSlideExpand = false;

        /// <summary>Track S — soft icon size / list style. Off.</summary>
        public const bool EnablePartsListLayout = false;

        /// <summary>Organizer detect / yield. Off.</summary>
        public const bool EnableOrganizerBridge = false;

        /// <summary>Cache / defer / in-place virtualization attempt. Off.</summary>
        public const bool EnablePerfSoft = false;

        /// <summary>
        /// Track R — owned parts list rebuild. Off until go/no-go.
        /// Not the default path; not forbidden once criteria pass.
        /// </summary>
        public const bool EnablePartsListRebuild = false;
    }
}
