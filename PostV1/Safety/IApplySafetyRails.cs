namespace PartSearchSuggest.PostV1.Safety
{
    /// <summary>
    /// Shared safety rails for Phase B/C apply paths: null editor, empty query, apply-suppress flags.
    /// Shipping wire-up should bridge to StockSearchGuard / EditorSearchHook suppress state.
    /// </summary>
    [PostV1Phase(PostV1Phase.B_CategoryTabNavigate)]
    internal interface IApplySafetyRails
    {
        /// <summary>True when apply must not run (editor null, suppress flag, empty query context).</summary>
        bool ShouldBlockApply(out string reason);

        void SetApplySuppress();

        void ClearApplySuppress();

        bool IsApplySuppressActive { get; }

        /// <summary>True when editor context is usable (wired) or assumed ready (null double).</summary>
        bool IsEditorContextAvailable();

        /// <summary>Reject empty / whitespace queries at the apply boundary.</summary>
        bool IsQueryEligibleForApply(string query);
    }

    /// <summary>Default rails: only blocks on active suppress flag; editor assumed available.</summary>
    [PostV1Phase(PostV1Phase.B_CategoryTabNavigate)]
    internal sealed class NullApplySafetyRails : IApplySafetyRails
    {
        private bool _suppress;

        public bool IsApplySuppressActive => _suppress;

        public bool ShouldBlockApply(out string reason)
        {
            if (_suppress)
            {
                reason = "Apply suppress flag is active.";
                return true;
            }

            reason = null;
            return false;
        }

        public void SetApplySuppress()
        {
            _suppress = true;
        }

        public void ClearApplySuppress()
        {
            _suppress = false;
        }

        public bool IsEditorContextAvailable()
        {
            return true;
        }

        public bool IsQueryEligibleForApply(string query)
        {
            return !string.IsNullOrWhiteSpace(query);
        }
    }

    /// <summary>
    /// Configurable rails for tests and future EditorLogic bridge.
    /// </summary>
    [PostV1Phase(PostV1Phase.B_CategoryTabNavigate)]
    internal sealed class ApplySafetyRails : IApplySafetyRails
    {
        private bool _suppress;

        public bool EditorAvailable { get; set; } = true;

        public bool IsApplySuppressActive => _suppress;

        public bool ShouldBlockApply(out string reason)
        {
            if (!EditorAvailable)
            {
                reason = "Editor context is null / unavailable.";
                return true;
            }

            if (_suppress)
            {
                reason = "Apply suppress flag is active.";
                return true;
            }

            reason = null;
            return false;
        }

        public void SetApplySuppress()
        {
            _suppress = true;
        }

        public void ClearApplySuppress()
        {
            _suppress = false;
        }

        public bool IsEditorContextAvailable()
        {
            return EditorAvailable;
        }

        public bool IsQueryEligibleForApply(string query)
        {
            return !string.IsNullOrWhiteSpace(query)
                && query.Trim().Length >= Shared.PostV1QueryGuards.MinSuggestionQueryLength;
        }
    }
}
