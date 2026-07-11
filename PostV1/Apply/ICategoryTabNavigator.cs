using System;
using PartSearchSuggest.PostV1.Categories;
using PartSearchSuggest.PostV1.Safety;

namespace PartSearchSuggest.PostV1.Apply
{
    /// <summary>
    /// Inputs for Phase B category/subcategory tab navigation.
    /// </summary>
    [PostV1Phase(PostV1Phase.B_CategoryTabNavigate)]
    internal sealed class CategoryTabApplyRequest
    {
        /// <summary>Same key produced by Phase A index / suggestion FilterKey.</summary>
        public string FilterKey { get; set; }

        public CategoryTabKind Kind { get; set; }

        /// <summary>Optional display label for logging / recovery messages.</summary>
        public string DisplayText { get; set; }

        /// <summary>When true, clear any Koobal custom filters before navigating (0.8.5.1 pattern).</summary>
        public bool ClearKoobalCustomFiltersFirst { get; set; } = true;

        public CategoryTabNavigateIntent Intent { get; set; } = CategoryTabNavigateIntent.ActivateParentThenSubcategory;
    }

    /// <summary>
    /// Result of a Phase B apply attempt. Callers should dismiss dropdown and, on failure,
    /// invoke shipping <c>RecoverAfterFailedApply</c> when wired.
    /// </summary>
    [PostV1Phase(PostV1Phase.B_CategoryTabNavigate)]
    internal sealed class CategoryTabApplyResult
    {
        public bool Succeeded { get; set; }

        public CategoryTabNavigateResultCode Code { get; set; }

        /// <summary>Human-readable failure reason for KSP.log; null on success.</summary>
        public string FailureReason { get; set; }

        public static CategoryTabApplyResult Ok()
        {
            return new CategoryTabApplyResult
            {
                Succeeded = true,
                Code = CategoryTabNavigateResultCode.Succeeded
            };
        }

        public static CategoryTabApplyResult Fail(CategoryTabNavigateResultCode code, string reason)
        {
            return new CategoryTabApplyResult
            {
                Succeeded = false,
                Code = code,
                FailureReason = reason ?? code.ToString()
            };
        }

        public static CategoryTabApplyResult Fail(string reason)
        {
            return Fail(CategoryTabNavigateResultCode.UnexpectedException, reason);
        }
    }

    /// <summary>
    /// Phase B port: navigate to the stock/custom/CCK tab matching a suggestion
    /// (spiritual successor to historical <c>StockTabNavigator</c>).
    /// </summary>
    /// <remarks>
    /// <para><b>Algorithm (documented + implemented against <see cref="IEditorCategoryUi"/>):</b></para>
    /// <list type="number">
    /// <item>Safety rails: null editor / empty query / apply-suppress → cancel.</item>
    /// <item>Pre-activate filter clear plan (Koobal custom + stock search field).</item>
    /// <item>Find tab by FilterKey + kind.</item>
    /// <item>Ensure tab visible.</item>
    /// <item>Activate via stock-equivalent path (NO displayType patch, NO OnTrue Prefix rewrite).</item>
    /// <item>Verify active state matches handle.</item>
    /// <item>On failure: recover UI + clear suppress; return result code.</item>
    /// </list>
    /// <para><b>Hard bans:</b> permanent displayType writers; OnTrue Prefix mutators; SearchRoutine skip;
    /// delete-dialog Harmony.</para>
    /// </remarks>
    [PostV1Phase(PostV1Phase.B_CategoryTabNavigate)]
    internal interface ICategoryTabNavigator
    {
        CategoryTabApplyResult Apply(CategoryTabApplyRequest request);

        void RecoverAfterFailedApply();
    }

    /// <summary>
    /// Full navigator algorithm against an <see cref="IEditorCategoryUi"/> port.
    /// </summary>
    [PostV1Phase(PostV1Phase.B_CategoryTabNavigate)]
    internal sealed class CategoryTabNavigator : ICategoryTabNavigator
    {
        private readonly IEditorCategoryUi _ui;
        private readonly IApplySafetyRails _safety;

        public CategoryTabNavigator(IEditorCategoryUi ui, IApplySafetyRails safety = null)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _safety = safety ?? new NullApplySafetyRails();
        }

        public CategoryTabApplyResult Apply(CategoryTabApplyRequest request)
        {
            if (request == null)
            {
                return CategoryTabApplyResult.Fail(
                    CategoryTabNavigateResultCode.NullRequest,
                    "CategoryTabApplyRequest is null.");
            }

            if (string.IsNullOrWhiteSpace(request.FilterKey))
            {
                return CategoryTabApplyResult.Fail(
                    CategoryTabNavigateResultCode.EmptyFilterKey,
                    "FilterKey is empty.");
            }

            if (_safety.ShouldBlockApply(out string blockReason))
            {
                return CategoryTabApplyResult.Fail(
                    CategoryTabNavigateResultCode.CancelledBySafetyRail,
                    blockReason ?? "Blocked by safety rail.");
            }

            try
            {
                if (!_ui.IsEditorReady())
                {
                    return CategoryTabApplyResult.Fail(
                        CategoryTabNavigateResultCode.EditorNotReady,
                        "Editor / PartCategorizer not ready.");
                }

                // --- Pre-activate filter clear (pure step list → port) ---
                foreach (CategoryApplyFilterStep step in CategoryApplyFilterPlan.BuildPreActivateSteps(
                             request.ClearKoobalCustomFiltersFirst))
                {
                    CategoryTabApplyResult clearResult = ExecuteFilterStep(step);
                    if (!clearResult.Succeeded
                        && step != CategoryApplyFilterStep.RaiseApplySuppressFlag)
                    {
                        RecoverAfterFailedApply();
                        return clearResult;
                    }
                }

                // --- Find → ensure visible → activate → verify ---
                if (!_ui.TryFindTab(request.FilterKey, request.Kind, out CategoryUiTabHandle handle)
                    || handle == null)
                {
                    RecoverAfterFailedApply();
                    return CategoryTabApplyResult.Fail(
                        CategoryTabNavigateResultCode.TabNotFound,
                        "Tab not found for key '" + request.FilterKey + "'.");
                }

                if (!_ui.EnsureTabVisible(handle))
                {
                    RecoverAfterFailedApply();
                    return CategoryTabApplyResult.Fail(
                        CategoryTabNavigateResultCode.TabNotVisible,
                        "Tab not visible for key '" + request.FilterKey + "'.");
                }

                // FORBIDDEN: do not patch displayType here; ActivateTab must use stock-equivalent path only.
                if (!_ui.ActivateTab(handle))
                {
                    RecoverAfterFailedApply();
                    return CategoryTabApplyResult.Fail(
                        CategoryTabNavigateResultCode.ActivationFailed,
                        "Stock-equivalent activation failed for '" + request.FilterKey + "'.");
                }

                if (!_ui.VerifyActiveTab(handle))
                {
                    RecoverAfterFailedApply();
                    return CategoryTabApplyResult.Fail(
                        CategoryTabNavigateResultCode.StateVerifyFailed,
                        "Active tab verify failed for '" + request.FilterKey + "'.");
                }

                foreach (CategoryApplyFilterStep step in CategoryApplyFilterPlan.BuildPostSuccessSteps())
                {
                    ExecuteFilterStep(step);
                }

                return CategoryTabApplyResult.Ok();
            }
            catch (NotImplementedException ex)
            {
                // Unwired port boundary — fail open with explicit code.
                try
                {
                    RecoverAfterFailedApply();
                }
                catch (NotImplementedException)
                {
                    // Swallow nested unwired recover.
                }

                return CategoryTabApplyResult.Fail(
                    CategoryTabNavigateResultCode.UnwiredPort,
                    ex.Message);
            }
            catch (Exception ex)
            {
                try
                {
                    RecoverAfterFailedApply();
                }
                catch
                {
                    // Fail-open: never throw into UI thread from recovery.
                }

                return CategoryTabApplyResult.Fail(
                    CategoryTabNavigateResultCode.UnexpectedException,
                    ex.Message);
            }
        }

        public void RecoverAfterFailedApply()
        {
            try
            {
                foreach (CategoryApplyFilterStep step in CategoryApplyFilterPlan.BuildFailureRecoverySteps())
                {
                    ExecuteFilterStep(step);
                }

                _ui.RecoverUiAfterFailedApply();
                _safety.ClearApplySuppress();
            }
            catch (NotImplementedException)
            {
                _safety.ClearApplySuppress();
            }
            catch
            {
                // Fail-open.
                _safety.ClearApplySuppress();
            }
        }

        private CategoryTabApplyResult ExecuteFilterStep(CategoryApplyFilterStep step)
        {
            switch (step)
            {
                case CategoryApplyFilterStep.ClearKoobalCustomFilters:
                    return _ui.ClearKoobalCustomFilters()
                        ? CategoryTabApplyResult.Ok()
                        : CategoryTabApplyResult.Fail(
                            CategoryTabNavigateResultCode.FilterClearFailed,
                            "ClearKoobalCustomFilters failed.");

                case CategoryApplyFilterStep.ClearStockSearchFilter:
                    return _ui.ClearStockSearchFilter()
                        ? CategoryTabApplyResult.Ok()
                        : CategoryTabApplyResult.Fail(
                            CategoryTabNavigateResultCode.FilterClearFailed,
                            "ClearStockSearchFilter failed.");

                case CategoryApplyFilterStep.RaiseApplySuppressFlag:
                    _safety.SetApplySuppress();
                    return CategoryTabApplyResult.Ok();

                case CategoryApplyFilterStep.ClearApplySuppressFlag:
                    _safety.ClearApplySuppress();
                    return CategoryTabApplyResult.Ok();

                default:
                    return CategoryTabApplyResult.Ok();
            }
        }
    }

    /// <summary>
    /// Architecture stub — always fails open; no Unity / PartCategorizer calls.
    /// Prefer <see cref="CategoryTabNavigator"/> + <see cref="RecordingEditorCategoryUi"/> for tests.
    /// </summary>
    [PostV1Phase(PostV1Phase.B_CategoryTabNavigate)]
    internal sealed class CategoryTabNavigatorStub : ICategoryTabNavigator
    {
        public CategoryTabApplyResult Apply(CategoryTabApplyRequest request)
        {
            return CategoryTabApplyResult.Fail(
                CategoryTabNavigateResultCode.UnwiredPort,
                "PostV1 CategoryTabNavigatorStub: Phase B stub; no tab navigation performed.");
        }

        public void RecoverAfterFailedApply()
        {
            // No-op stub. Real implementation should mirror EditorSearchHook recovery paths.
        }
    }
}
