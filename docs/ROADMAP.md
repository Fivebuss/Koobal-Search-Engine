# Roadmap (mission-ordered)

Canonical mission: see [`MISSION.md`](MISSION.md).

### Product relationship (short)

- **Full Koobal** = full product (dropdown, suggestions, history, later categories/V2, …) **plus** core native/stock search improvements.
- **Native Search** = standalone slice of that native-search core (halt, tighten, guards; branding only on the main editor bar). Kept in sync with **any relevant** full-mod fixes. Future global-bar halt/tighten fits this slice — not exclusive ownership of “global search.”

Ordered so each band deepens **organization + speed + universal access + usefulness** of the parts list:

1. **Current core** — Predictive dropdown on the native editor search bar; parts / filters / mods / authors; history; loading-screen index. *Makes the parts list quickly searchable without replacing stock UI.* (Native Search ships the stock-search improvement half of this without dropdown/history.)
2. **~0.9 — History item delete** — Per-row remove from recent searches. *Keeps history useful and organized instead of only clear-all.* See [`V0_9_HISTORY_ITEM_DELETE.md`](V0_9_HISTORY_ITEM_DELETE.md). *(Full Koobal only.)*
3. **Post-v1 — Categories / subassemblies** — Suggest and jump to category tabs and subassemblies from the same dropdown. *Organizes navigation through the editor’s own taxonomy.* See [`POST_V1_CATEGORIES_SUBASSEMBLIES_PLAN.md`](POST_V1_CATEGORIES_SUBASSEMBLIES_PLAN.md). *(Full Koobal only.)*
4. **Later — Global search halt** — Enter-halt + tight match on every KSP search bar. *Universal access: same fast, deliberate search behavior beyond the editor.* Part of the **native search improvements** core: both full Koobal and the Native standalone slice get it when shipped; architecture stays in PostV1 until extracted. See [`POST_V1_GLOBAL_SEARCH_HALT.md`](POST_V1_GLOBAL_SEARCH_HALT.md); Native notes: [`../../KoobalNativeSearch/docs/FUTURE_GLOBAL_SEARCH.md`](../../KoobalNativeSearch/docs/FUTURE_GLOBAL_SEARCH.md).
5. **V2 — Settings + slide-expand** (Track R rebuild gated) — Layout/settings for the parts list experience; preferred Track S slide-expand; optional Track R only with go/no-go. *Makes the list itself more useful and controllable.* See [`V2_PARTS_LIST_AND_SETTINGS.md`](V2_PARTS_LIST_AND_SETTINGS.md). *(Full Koobal.)*

Longer / partially stale feature notes remain in [`../ROADMAP.md`](../ROADMAP.md); this file is the mission-aligned order of work.
