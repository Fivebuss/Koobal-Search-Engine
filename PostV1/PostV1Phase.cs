namespace PartSearchSuggest.PostV1
{
    /// <summary>
    /// Implementation phases from <c>docs/POST_V1_CATEGORIES_SUBASSEMBLIES_PLAN.md</c>.
    /// Scaffold types are tagged with the phase they belong to; none are active in the shipping DLL.
    /// </summary>
    internal enum PostV1Phase
    {
        /// <summary>
        /// Phase A — read-only category/subcategory index and suggestion DTOs.
        /// Click may no-op; no tab navigation, no Harmony, no <c>displayType</c> writes.
        /// </summary>
        A_SuggestOnlyCategories = 1,

        /// <summary>
        /// Phase B — apply/navigate port: switch stock/custom/CCK tabs without nuking stock UI.
        /// Thin navigator only; fail-open; scoped restore; no delete-dialog Harmony.
        /// </summary>
        B_CategoryTabNavigate = 2,

        /// <summary>
        /// Phase C — subassembly craft index + apply + delete-refresh hook (postfix-only when implemented).
        /// Folder-scoped incremental index; never rebuild part/metadata/categorizer indexes on SA lifecycle.
        /// </summary>
        C_Subassemblies = 3,

        /// <summary>
        /// Phase D — global Enter-to-search halt + tightened matching on every KSP search bar
        /// (no branding, no dropdowns). After v1 and after categories/SA band.
        /// See <c>docs/POST_V1_GLOBAL_SEARCH_HALT.md</c> and <c>PostV1/GlobalSearch/</c>.
        /// </summary>
        D_GlobalSearchHalt = 4,

        /// <summary>
        /// Target ~0.9.x — delete individual search-history rows in the editor dropdown.
        /// Pre-full-v1 QoL; <b>not</b> a v1.0 blocker. May ship before Phase A–D.
        /// Folder lives under PostV1/ for compile exclusion only.
        /// See <c>docs/V0_9_HISTORY_ITEM_DELETE.md</c> and <c>PostV1/SearchHistory/</c>.
        /// </summary>
        HistoryItemDelete_v09 = 9,

        /// <summary>
        /// V2 / post-v1 — slide-expanded parts list (geometry, same family as dropdown slide)
        /// + first-class mod Settings tab. Not a fullscreen maximize / chrome-swap.
        /// After v1; schedule after or alongside earlier PostV1 bands as product decides.
        /// See <c>docs/V2_PARTS_LIST_AND_SETTINGS.md</c> and <c>PostV1/V2/</c>.
        /// </summary>
        E_V2PartsListAndSettings = 5
    }

    /// <summary>
    /// Marks a type as belonging to a specific post-v1 phase. Documentation aid only —
    /// not read by shipping code and not a runtime feature switch.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Interface | System.AttributeTargets.Enum | System.AttributeTargets.Struct, Inherited = false)]
    internal sealed class PostV1PhaseAttribute : System.Attribute
    {
        public PostV1PhaseAttribute(PostV1Phase phase)
        {
            Phase = phase;
        }

        public PostV1Phase Phase { get; }
    }
}
