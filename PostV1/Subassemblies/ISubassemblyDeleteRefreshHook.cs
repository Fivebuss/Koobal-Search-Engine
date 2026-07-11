using System;

namespace PartSearchSuggest.PostV1.Subassemblies
{
    /// <summary>
    /// Port events for subassembly lifecycle — no Harmony in this scaffold.
    /// Wire-up may forward stock save/delete observe points (prefer postfix-only for delete).
    /// </summary>
    [PostV1Phase(PostV1Phase.C_Subassemblies)]
    internal interface ISubassemblyLifecycleEvents
    {
        event Action<string> SubassemblySaved;

        event Action<string> SubassemblyDeleted;

        /// <summary>Optional: folder resync when path unknown.</summary>
        event Action FolderResyncRequested;

        void RaiseSaved(string craftPath);

        void RaiseDeleted(string craftPath);

        void RaiseFolderResync();
    }

    /// <summary>In-process event bus for delete-refresh / incremental index.</summary>
    [PostV1Phase(PostV1Phase.C_Subassemblies)]
    internal sealed class SubassemblyLifecycleEventBus : ISubassemblyLifecycleEvents
    {
        public event Action<string> SubassemblySaved;

        public event Action<string> SubassemblyDeleted;

        public event Action FolderResyncRequested;

        public void RaiseSaved(string craftPath)
        {
            SubassemblySaved?.Invoke(craftPath);
        }

        public void RaiseDeleted(string craftPath)
        {
            SubassemblyDeleted?.Invoke(craftPath);
        }

        public void RaiseFolderResync()
        {
            FolderResyncRequested?.Invoke();
        }
    }

    /// <summary>
    /// Phase C hook contract for refreshing the subassembly suggestion index after stock delete.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>No Harmony in this scaffold.</b> When implemented, prefer <b>postfix-only</b> index updates
    /// after stock confirms delete — do not Prefix-delete, and do not call
    /// <c>RefreshSubassemblies</c> mid-delete-dialog (risk R3 / historical popup NRE).
    /// </para>
    /// <para>
    /// Implementations should call <see cref="ISubassemblySuggestionIndex.Remove"/> for the
    /// deleted craft path only (folder-scoped). Never rebuild part/metadata/categorizer indexes here.
    /// </para>
    /// </remarks>
    [PostV1Phase(PostV1Phase.C_Subassemblies)]
    internal interface ISubassemblyDeleteRefreshHook
    {
        /// <summary>
        /// Register listeners / Harmony postfixes. Must be idempotent and fail-open.
        /// </summary>
        void Install();

        /// <summary>Unregister listeners. Safe to call when not installed.</summary>
        void Uninstall();

        /// <summary>
        /// Invoked after stock delete is confirmed (or equivalent observe point).
        /// <paramref name="craftPath"/> may be null when only a full folder resync is safe.
        /// </summary>
        void OnSubassemblyDeleted(string craftPath);

        /// <summary>Incremental upsert after save confirm.</summary>
        void OnSubassemblySaved(string craftPath);
    }

    /// <summary>
    /// Complete refresh API: listens via <see cref="ISubassemblyLifecycleEvents"/>,
    /// rebuilds incremental index — no Harmony class yet.
    /// </summary>
    [PostV1Phase(PostV1Phase.C_Subassemblies)]
    internal sealed class SubassemblyDeleteRefreshService : ISubassemblyDeleteRefreshHook
    {
        private readonly ISubassemblySuggestionIndex _index;
        private readonly ISubassemblyLifecycleEvents _events;
        private readonly ISubassemblyApplyPort _applyPort;
        private bool _installed;

        public SubassemblyDeleteRefreshService(
            ISubassemblySuggestionIndex index,
            ISubassemblyLifecycleEvents events = null,
            ISubassemblyApplyPort applyPort = null)
        {
            _index = index ?? throw new ArgumentNullException(nameof(index));
            _events = events;
            _applyPort = applyPort;
        }

        public bool IsInstalled => _installed;

        public void Install()
        {
            if (_installed || _events == null)
            {
                return;
            }

            _events.SubassemblyDeleted += OnSubassemblyDeleted;
            _events.SubassemblySaved += OnSubassemblySaved;
            _events.FolderResyncRequested += OnFolderResync;
            _installed = true;
        }

        public void Uninstall()
        {
            if (!_installed || _events == null)
            {
                _installed = false;
                return;
            }

            _events.SubassemblyDeleted -= OnSubassemblyDeleted;
            _events.SubassemblySaved -= OnSubassemblySaved;
            _events.FolderResyncRequested -= OnFolderResync;
            _installed = false;
        }

        public void OnSubassemblyDeleted(string craftPath)
        {
            // Clear transient SA filter BEFORE any delete-dialog interaction at wire-up call sites.
            _applyPort?.ClearTransientCraftFilter(SubassemblyFilterClearReason.BeforeDeleteDialog);

            if (_index == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(craftPath))
            {
                // Postfix-only timing: remove this path only — never full part/metadata rebuild.
                _index.Remove(craftPath);
            }
        }

        public void OnSubassemblySaved(string craftPath)
        {
            if (_index == null || string.IsNullOrEmpty(craftPath))
            {
                return;
            }

            _index.AddOrUpdate(craftPath);
        }

        private void OnFolderResync()
        {
            // Full folder resync is editor-entry BuildFromSnapshot responsibility at wire-up.
            // Here we only clear when path-unknown delete left the index dirty — prefer path Remove.
        }
    }

    /// <summary>
    /// Architecture stub — documents the delete-refresh contract without Harmony patches.
    /// </summary>
    [PostV1Phase(PostV1Phase.C_Subassemblies)]
    internal sealed class SubassemblyDeleteRefreshHookStub : ISubassemblyDeleteRefreshHook
    {
        private readonly ISubassemblySuggestionIndex _index;

        public SubassemblyDeleteRefreshHookStub(ISubassemblySuggestionIndex index)
        {
            _index = index;
        }

        public void Install()
        {
            // No Harmony / no stock hooks in scaffolding.
        }

        public void Uninstall()
        {
            // No-op.
        }

        public void OnSubassemblyDeleted(string craftPath)
        {
            if (_index == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(craftPath))
            {
                _index.Remove(craftPath);
            }
        }

        public void OnSubassemblySaved(string craftPath)
        {
            // Stub: no upsert.
        }
    }
}
