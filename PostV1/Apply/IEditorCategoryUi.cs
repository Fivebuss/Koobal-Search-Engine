namespace PartSearchSuggest.PostV1.Apply
{
    /// <summary>
    /// Port over PartCategorizer / stock category buttons. Implementations at wire-up talk to
    /// live UI; <see cref="UnwiredEditorCategoryUi"/> throws at the UI boundary for compile-check.
    /// </summary>
    /// <remarks>
    /// <para><b>FORBIDDEN in any implementation (plan + transcript lessons):</b></para>
    /// <list type="bullet">
    /// <item>Do NOT permanently patch / rewrite <c>Category.displayType</c> (v0.8.3.16 blank-list).</item>
    /// <item>Do NOT Harmony Prefix <c>OnTrueCATEGORY</c> / <c>OnTrueSUB</c> to mutate displayType.</item>
    /// <item>Do NOT Prefix-skip <c>SearchRoutine</c> (returns null → StartCoroutine NRE).</item>
    /// <item>Do NOT leave Harmony writers active on the stock click path after apply.</item>
    /// </list>
    /// Preferred: invoke the same managed activation stock uses (radio SetState / button OnTrue
    /// equivalent) inside a scoped apply; restore any captured state on dispose / failure.
    /// </remarks>
    [PostV1Phase(PostV1Phase.B_CategoryTabNavigate)]
    internal interface IEditorCategoryUi
    {
        /// <summary>True when editor + PartCategorizer are ready for navigation.</summary>
        bool IsEditorReady();

        /// <summary>Locate tab by FilterKey; false if missing.</summary>
        bool TryFindTab(string filterKey, Categories.CategoryTabKind kind, out CategoryUiTabHandle handle);

        /// <summary>Ensure the tab's parent strip / page is scrolled/visible enough to activate.</summary>
        bool EnsureTabVisible(CategoryUiTabHandle handle);

        /// <summary>
        /// Stock-equivalent activation (parent, then subcategory when handle is a sub).
        /// Must not write displayType except via stock's own activation path.
        /// </summary>
        bool ActivateTab(CategoryUiTabHandle handle);

        /// <summary>Verify the active category/sub matches the handle after activation.</summary>
        bool VerifyActiveTab(CategoryUiTabHandle handle);

        /// <summary>Clear Koobal-owned custom SearchFilterResult / transient filters (0.8.5.1 pattern).</summary>
        bool ClearKoobalCustomFilters();

        /// <summary>Clear stock search field text / dismiss typed filter without skipping SearchRoutine.</summary>
        bool ClearStockSearchFilter();

        /// <summary>Best-effort restore list usability after failed apply.</summary>
        void RecoverUiAfterFailedApply();
    }

    /// <summary>Opaque tab identity resolved by the UI port (no Unity object pinned in index).</summary>
    [PostV1Phase(PostV1Phase.B_CategoryTabNavigate)]
    internal sealed class CategoryUiTabHandle
    {
        public string FilterKey { get; set; }

        public Categories.CategoryTabKind Kind { get; set; }

        public string ParentFilterKey { get; set; }

        public bool IsSubcategory { get; set; }

        public string DisplayText { get; set; }
    }

    /// <summary>
    /// Sealed unwired port — UI methods throw <see cref="System.NotImplementedException"/>.
    /// Used by composition root until live PartCategorizer adapter exists.
    /// </summary>
    [PostV1Phase(PostV1Phase.B_CategoryTabNavigate)]
    internal sealed class UnwiredEditorCategoryUi : IEditorCategoryUi
    {
        public bool IsEditorReady()
        {
            throw new System.NotImplementedException(
                "UnwiredEditorCategoryUi.IsEditorReady — wire PartCategorizer / EditorLogic adapter.");
        }

        public bool TryFindTab(string filterKey, Categories.CategoryTabKind kind, out CategoryUiTabHandle handle)
        {
            handle = null;
            throw new System.NotImplementedException(
                "UnwiredEditorCategoryUi.TryFindTab — wire live category button lookup.");
        }

        public bool EnsureTabVisible(CategoryUiTabHandle handle)
        {
            throw new System.NotImplementedException(
                "UnwiredEditorCategoryUi.EnsureTabVisible — wire scroll/page visibility.");
        }

        public bool ActivateTab(CategoryUiTabHandle handle)
        {
            // FORBIDDEN reminder: do not implement via permanent displayType Harmony Prefix / OnTrue rewrite.
            throw new System.NotImplementedException(
                "UnwiredEditorCategoryUi.ActivateTab — wire stock-equivalent radio/button activation only.");
        }

        public bool VerifyActiveTab(CategoryUiTabHandle handle)
        {
            throw new System.NotImplementedException(
                "UnwiredEditorCategoryUi.VerifyActiveTab — wire active category state read.");
        }

        public bool ClearKoobalCustomFilters()
        {
            throw new System.NotImplementedException(
                "UnwiredEditorCategoryUi.ClearKoobalCustomFilters — wire StockSearchHelper clear path.");
        }

        public bool ClearStockSearchFilter()
        {
            throw new System.NotImplementedException(
                "UnwiredEditorCategoryUi.ClearStockSearchFilter — wire searchField clear without skipping SearchRoutine.");
        }

        public void RecoverUiAfterFailedApply()
        {
            throw new System.NotImplementedException(
                "UnwiredEditorCategoryUi.RecoverUiAfterFailedApply — wire RecoverAfterFailedApply.");
        }
    }

    /// <summary>
    /// Test / snapshot double: records calls and returns configurable results without Unity.
    /// </summary>
    [PostV1Phase(PostV1Phase.B_CategoryTabNavigate)]
    internal sealed class RecordingEditorCategoryUi : IEditorCategoryUi
    {
        public bool EditorReady { get; set; } = true;

        public bool FindSucceeds { get; set; } = true;

        public bool VisibleSucceeds { get; set; } = true;

        public bool ActivateSucceeds { get; set; } = true;

        public bool VerifySucceeds { get; set; } = true;

        public bool ClearCustomSucceeds { get; set; } = true;

        public bool ClearStockSucceeds { get; set; } = true;

        public System.Collections.Generic.List<string> CallLog { get; } =
            new System.Collections.Generic.List<string>();

        public bool IsEditorReady()
        {
            CallLog.Add(nameof(IsEditorReady));
            return EditorReady;
        }

        public bool TryFindTab(string filterKey, Categories.CategoryTabKind kind, out CategoryUiTabHandle handle)
        {
            CallLog.Add("TryFindTab:" + filterKey);
            if (!FindSucceeds || string.IsNullOrWhiteSpace(filterKey))
            {
                handle = null;
                return false;
            }

            handle = new CategoryUiTabHandle
            {
                FilterKey = filterKey,
                Kind = kind,
                DisplayText = filterKey,
                IsSubcategory = kind == Categories.CategoryTabKind.StockSubcategoryTab
            };
            return true;
        }

        public bool EnsureTabVisible(CategoryUiTabHandle handle)
        {
            CallLog.Add(nameof(EnsureTabVisible));
            return VisibleSucceeds;
        }

        public bool ActivateTab(CategoryUiTabHandle handle)
        {
            CallLog.Add(nameof(ActivateTab));
            return ActivateSucceeds;
        }

        public bool VerifyActiveTab(CategoryUiTabHandle handle)
        {
            CallLog.Add(nameof(VerifyActiveTab));
            return VerifySucceeds;
        }

        public bool ClearKoobalCustomFilters()
        {
            CallLog.Add(nameof(ClearKoobalCustomFilters));
            return ClearCustomSucceeds;
        }

        public bool ClearStockSearchFilter()
        {
            CallLog.Add(nameof(ClearStockSearchFilter));
            return ClearStockSucceeds;
        }

        public void RecoverUiAfterFailedApply()
        {
            CallLog.Add(nameof(RecoverUiAfterFailedApply));
        }
    }
}
