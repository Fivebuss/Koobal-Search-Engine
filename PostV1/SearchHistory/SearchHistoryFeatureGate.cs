namespace PartSearchSuggest.PostV1.SearchHistory
{
    /// <summary>
    /// Feature gates for per-item search history delete (target ~0.9.x).
    /// </summary>
    /// <remarks>
    /// <b>Always false in this scaffolding.</b> Shipping must not reference this type until
    /// ~0.9 wire-up is intentional and ModTest smoke passes. Not a v1.0 blocker.
    /// </remarks>
    [PostV1Phase(PostV1Phase.HistoryItemDelete_v09)]
    internal static class SearchHistoryFeatureGate
    {
        /// <summary>Master enable for per-row history delete chrome + Remove wiring. Scaffold default: off.</summary>
        public const bool Enabled = false;

        /// <summary>Show per-row ✕ / trash on history rows. Off.</summary>
        public const bool EnablePerRowDeleteChrome = false;

        /// <summary>
        /// Persist ids in History.cfg (id\tquery). When off at wire-up, store may still run in-memory only.
        /// Scaffold default: off.
        /// </summary>
        public const bool EnableIdPersistenceFormat = false;
    }
}
