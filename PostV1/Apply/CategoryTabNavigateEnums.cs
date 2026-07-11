namespace PartSearchSuggest.PostV1.Apply
{
    /// <summary>
    /// Result codes for Phase B tab navigation. Callers map these to logging / RecoverAfterFailedApply.
    /// </summary>
    [PostV1Phase(PostV1Phase.B_CategoryTabNavigate)]
    internal enum CategoryTabNavigateResultCode
    {
        Succeeded = 0,
        NullRequest = 1,
        EmptyFilterKey = 2,
        EditorNotReady = 3,
        TabNotFound = 4,
        TabNotVisible = 5,
        ActivationFailed = 6,
        StateVerifyFailed = 7,
        FilterClearFailed = 8,
        CancelledBySafetyRail = 9,
        UnwiredPort = 10,
        UnexpectedException = 99
    }

    /// <summary>High-level request intent for the navigator.</summary>
    [PostV1Phase(PostV1Phase.B_CategoryTabNavigate)]
    internal enum CategoryTabNavigateIntent
    {
        /// <summary>Activate parent category tab only.</summary>
        ActivateParent = 0,

        /// <summary>Activate parent then subcategory (stock radio path).</summary>
        ActivateParentThenSubcategory = 1,

        /// <summary>Activate a custom / CCK tab by FilterKey.</summary>
        ActivateCustomOrCck = 2
    }
}
