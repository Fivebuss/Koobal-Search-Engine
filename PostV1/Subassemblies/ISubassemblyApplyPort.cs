using System;
using PartSearchSuggest.PostV1.Safety;

namespace PartSearchSuggest.PostV1.Subassemblies
{
    /// <summary>
    /// Clear-matrix reasons for transient craft-path filters (risk R4 / historical lessons).
    /// </summary>
    [PostV1Phase(PostV1Phase.C_Subassemblies)]
    internal enum SubassemblyFilterClearReason
    {
        BeforeApply = 0,
        AfterApplySuccess = 1,
        ApplyFailed = 2,
        DropdownDismiss = 3,
        TabChanged = 4,
        SearchFocus = 5,
        Timeout = 6,
        BeforeDeleteDialog = 7,
        Manual = 8
    }

    /// <summary>Result codes for Phase C apply.</summary>
    [PostV1Phase(PostV1Phase.C_Subassemblies)]
    internal enum SubassemblyApplyResultCode
    {
        Succeeded = 0,
        NullRequest = 1,
        EmptyCraftPath = 2,
        EditorNotReady = 3,
        CraftInvalid = 4,
        FilterClearFailed = 5,
        SelectOrLoadFailed = 6,
        CancelledBySafetyRail = 7,
        UnwiredPort = 8,
        UnexpectedException = 99
    }

    /// <summary>
    /// Low-level UI/filesystem port for selecting/loading a subassembly craft.
    /// </summary>
    [PostV1Phase(PostV1Phase.C_Subassemblies)]
    internal interface ISubassemblyEditorUi
    {
        bool IsEditorReady();

        bool ClearTransientCraftPathFilter(SubassemblyFilterClearReason reason);

        /// <summary>Switch to stock subassembly tab (stock-equivalent; no displayType Harmony).</summary>
        bool EnsureSubassemblyTabActive();

        /// <summary>Select/load craft the way stock does (icon click / proven select API).</summary>
        bool SelectOrLoadCraft(string craftPath);

        void RecoverUiAfterFailedApply();
    }

    /// <summary>Unwired UI boundary — throws NotImplementedException.</summary>
    [PostV1Phase(PostV1Phase.C_Subassemblies)]
    internal sealed class UnwiredSubassemblyEditorUi : ISubassemblyEditorUi
    {
        public bool IsEditorReady()
        {
            throw new NotImplementedException("UnwiredSubassemblyEditorUi.IsEditorReady");
        }

        public bool ClearTransientCraftPathFilter(SubassemblyFilterClearReason reason)
        {
            throw new NotImplementedException("UnwiredSubassemblyEditorUi.ClearTransientCraftPathFilter");
        }

        public bool EnsureSubassemblyTabActive()
        {
            throw new NotImplementedException("UnwiredSubassemblyEditorUi.EnsureSubassemblyTabActive");
        }

        public bool SelectOrLoadCraft(string craftPath)
        {
            throw new NotImplementedException("UnwiredSubassemblyEditorUi.SelectOrLoadCraft");
        }

        public void RecoverUiAfterFailedApply()
        {
            throw new NotImplementedException("UnwiredSubassemblyEditorUi.RecoverUiAfterFailedApply");
        }
    }

    /// <summary>Recording double for workflow tests.</summary>
    [PostV1Phase(PostV1Phase.C_Subassemblies)]
    internal sealed class RecordingSubassemblyEditorUi : ISubassemblyEditorUi
    {
        public bool EditorReady { get; set; } = true;

        public bool ClearSucceeds { get; set; } = true;

        public bool TabSucceeds { get; set; } = true;

        public bool SelectSucceeds { get; set; } = true;

        public System.Collections.Generic.List<string> CallLog { get; } =
            new System.Collections.Generic.List<string>();

        public bool IsEditorReady()
        {
            CallLog.Add(nameof(IsEditorReady));
            return EditorReady;
        }

        public bool ClearTransientCraftPathFilter(SubassemblyFilterClearReason reason)
        {
            CallLog.Add("Clear:" + reason);
            return ClearSucceeds;
        }

        public bool EnsureSubassemblyTabActive()
        {
            CallLog.Add(nameof(EnsureSubassemblyTabActive));
            return TabSucceeds;
        }

        public bool SelectOrLoadCraft(string craftPath)
        {
            CallLog.Add("Select:" + craftPath);
            return SelectSucceeds;
        }

        public void RecoverUiAfterFailedApply()
        {
            CallLog.Add(nameof(RecoverUiAfterFailedApply));
        }
    }

    /// <summary>
    /// Request to select/place a subassembly the way stock does (tab switch + craft focus).
    /// </summary>
    [PostV1Phase(PostV1Phase.C_Subassemblies)]
    internal sealed class SubassemblyApplyRequest
    {
        /// <summary>Craft path FilterKey from the suggestion.</summary>
        public string CraftPath { get; set; }

        public string DisplayTitle { get; set; }

        /// <summary>
        /// When true, clear any transient craft-path filter before apply and ensure clear matrix
        /// on dismiss / tab change / search focus / timeout / before delete dialog (historical lessons).
        /// </summary>
        public bool UseTransientCraftPathFilter { get; set; } = true;
    }

    /// <summary>Outcome of a Phase C subassembly apply.</summary>
    [PostV1Phase(PostV1Phase.C_Subassemblies)]
    internal sealed class SubassemblyApplyResult
    {
        public bool Succeeded { get; set; }

        public SubassemblyApplyResultCode Code { get; set; }

        public string FailureReason { get; set; }

        public static SubassemblyApplyResult Ok()
        {
            return new SubassemblyApplyResult
            {
                Succeeded = true,
                Code = SubassemblyApplyResultCode.Succeeded
            };
        }

        public static SubassemblyApplyResult Fail(SubassemblyApplyResultCode code, string reason)
        {
            return new SubassemblyApplyResult
            {
                Succeeded = false,
                Code = code,
                FailureReason = reason ?? code.ToString()
            };
        }

        public static SubassemblyApplyResult Fail(string reason)
        {
            return Fail(SubassemblyApplyResultCode.UnexpectedException, reason);
        }
    }

    /// <summary>
    /// Phase C apply port: switch to subassembly tab and select/place via stock-equivalent path.
    /// </summary>
    [PostV1Phase(PostV1Phase.C_Subassemblies)]
    internal interface ISubassemblyApplyPort
    {
        SubassemblyApplyResult Apply(SubassemblyApplyRequest request);

        /// <summary>Clear any Koobal-owned transient SA filter without touching stock delete UI.</summary>
        void ClearTransientCraftFilter(SubassemblyFilterClearReason reason = SubassemblyFilterClearReason.Manual);
    }

    /// <summary>
    /// Full apply workflow: safety rails → clear matrix → SA tab → select/load → result.
    /// </summary>
    [PostV1Phase(PostV1Phase.C_Subassemblies)]
    internal sealed class SubassemblyApplyPort : ISubassemblyApplyPort
    {
        private readonly ISubassemblyEditorUi _ui;
        private readonly ISubassemblyMatcher _matcher;
        private readonly IApplySafetyRails _safety;

        /// <summary>Historical transient filter timeout (seconds) — wire-up schedules clear.</summary>
        public const int TransientFilterTimeoutSeconds = 45;

        public SubassemblyApplyPort(
            ISubassemblyEditorUi ui,
            ISubassemblyMatcher matcher = null,
            IApplySafetyRails safety = null)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _matcher = matcher;
            _safety = safety ?? new NullApplySafetyRails();
        }

        public SubassemblyApplyResult Apply(SubassemblyApplyRequest request)
        {
            if (request == null)
            {
                return SubassemblyApplyResult.Fail(
                    SubassemblyApplyResultCode.NullRequest,
                    "SubassemblyApplyRequest is null.");
            }

            if (string.IsNullOrWhiteSpace(request.CraftPath))
            {
                return SubassemblyApplyResult.Fail(
                    SubassemblyApplyResultCode.EmptyCraftPath,
                    "CraftPath is empty.");
            }

            if (_safety.ShouldBlockApply(out string blockReason))
            {
                return SubassemblyApplyResult.Fail(
                    SubassemblyApplyResultCode.CancelledBySafetyRail,
                    blockReason ?? "Blocked by safety rail.");
            }

            try
            {
                if (!_ui.IsEditorReady())
                {
                    return SubassemblyApplyResult.Fail(
                        SubassemblyApplyResultCode.EditorNotReady,
                        "Editor not ready for subassembly apply.");
                }

                if (_matcher != null && !_matcher.IsCraftValid(request.CraftPath))
                {
                    return SubassemblyApplyResult.Fail(
                        SubassemblyApplyResultCode.CraftInvalid,
                        "Craft invalid or missing: " + request.CraftPath);
                }

                _safety.SetApplySuppress();

                // Clear matrix BEFORE apply (and always before delete dialog — separate call site).
                if (request.UseTransientCraftPathFilter)
                {
                    if (!_ui.ClearTransientCraftPathFilter(SubassemblyFilterClearReason.BeforeApply))
                    {
                        Recover();
                        return SubassemblyApplyResult.Fail(
                            SubassemblyApplyResultCode.FilterClearFailed,
                            "Pre-apply filter clear failed.");
                    }
                }

                if (!_ui.EnsureSubassemblyTabActive())
                {
                    Recover();
                    return SubassemblyApplyResult.Fail(
                        SubassemblyApplyResultCode.SelectOrLoadFailed,
                        "Could not activate subassembly tab.");
                }

                if (!_ui.SelectOrLoadCraft(request.CraftPath))
                {
                    Recover();
                    return SubassemblyApplyResult.Fail(
                        SubassemblyApplyResultCode.SelectOrLoadFailed,
                        "Select/load failed for " + request.CraftPath);
                }

                // Do not leave transient filters stuck — clear after success if we used one.
                if (request.UseTransientCraftPathFilter)
                {
                    _ui.ClearTransientCraftPathFilter(SubassemblyFilterClearReason.AfterApplySuccess);
                }

                _safety.ClearApplySuppress();
                return SubassemblyApplyResult.Ok();
            }
            catch (NotImplementedException ex)
            {
                try { Recover(); } catch (NotImplementedException) { }
                return SubassemblyApplyResult.Fail(SubassemblyApplyResultCode.UnwiredPort, ex.Message);
            }
            catch (Exception ex)
            {
                try { Recover(); } catch { }
                return SubassemblyApplyResult.Fail(SubassemblyApplyResultCode.UnexpectedException, ex.Message);
            }
        }

        public void ClearTransientCraftFilter(SubassemblyFilterClearReason reason = SubassemblyFilterClearReason.Manual)
        {
            try
            {
                _ui.ClearTransientCraftPathFilter(reason);
            }
            catch (NotImplementedException)
            {
                // Unwired — ignore.
            }
            catch
            {
                // Fail-open.
            }
        }

        private void Recover()
        {
            try
            {
                _ui.ClearTransientCraftPathFilter(SubassemblyFilterClearReason.ApplyFailed);
                _ui.RecoverUiAfterFailedApply();
            }
            catch
            {
                // Fail-open.
            }

            _safety.ClearApplySuppress();
        }
    }

    /// <summary>Architecture stub — always fails; no EditorPartList / filter calls.</summary>
    [PostV1Phase(PostV1Phase.C_Subassemblies)]
    internal sealed class SubassemblyApplyPortStub : ISubassemblyApplyPort
    {
        public SubassemblyApplyResult Apply(SubassemblyApplyRequest request)
        {
            return SubassemblyApplyResult.Fail(
                SubassemblyApplyResultCode.UnwiredPort,
                "PostV1 SubassemblyApplyPortStub: Phase C not implemented; no subassembly apply performed.");
        }

        public void ClearTransientCraftFilter(SubassemblyFilterClearReason reason = SubassemblyFilterClearReason.Manual)
        {
            // No-op stub.
        }
    }
}
