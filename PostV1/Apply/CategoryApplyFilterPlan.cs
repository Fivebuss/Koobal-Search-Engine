using System.Collections.Generic;

namespace PartSearchSuggest.PostV1.Apply
{
    /// <summary>
    /// Pure filter-clear / scoped-state steps the UI port must support during apply.
    /// Navigator executes these in order; the port performs the side effects.
    /// </summary>
    [PostV1Phase(PostV1Phase.B_CategoryTabNavigate)]
    internal enum CategoryApplyFilterStep
    {
        /// <summary>Clear Koobal custom SearchFilterResult / race guards (0.8.5.1).</summary>
        ClearKoobalCustomFilters = 0,

        /// <summary>Clear stock searchField typed filter without Prefix-skipping SearchRoutine.</summary>
        ClearStockSearchFilter = 1,

        /// <summary>Optional: dismiss dropdown / suppress re-entry flags (wired via safety rail).</summary>
        RaiseApplySuppressFlag = 2,

        /// <summary>Optional: clear apply-suppress after success / recovery.</summary>
        ClearApplySuppressFlag = 3
    }

    /// <summary>Ordered plan of filter-clear steps for a navigate apply.</summary>
    [PostV1Phase(PostV1Phase.B_CategoryTabNavigate)]
    internal static class CategoryApplyFilterPlan
    {
        /// <summary>
        /// Default pre-activate clear matrix when <c>ClearKoobalCustomFiltersFirst</c> is true.
        /// </summary>
        public static IReadOnlyList<CategoryApplyFilterStep> BuildPreActivateSteps(bool clearKoobalCustomFirst)
        {
            var steps = new List<CategoryApplyFilterStep>(4);
            if (clearKoobalCustomFirst)
            {
                steps.Add(CategoryApplyFilterStep.ClearKoobalCustomFilters);
                steps.Add(CategoryApplyFilterStep.ClearStockSearchFilter);
            }

            steps.Add(CategoryApplyFilterStep.RaiseApplySuppressFlag);
            return steps;
        }

        public static IReadOnlyList<CategoryApplyFilterStep> BuildPostSuccessSteps()
        {
            return new[] { CategoryApplyFilterStep.ClearApplySuppressFlag };
        }

        public static IReadOnlyList<CategoryApplyFilterStep> BuildFailureRecoverySteps()
        {
            return new[]
            {
                CategoryApplyFilterStep.ClearKoobalCustomFilters,
                CategoryApplyFilterStep.ClearApplySuppressFlag
            };
        }
    }
}
