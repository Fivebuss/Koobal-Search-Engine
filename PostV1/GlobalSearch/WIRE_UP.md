# Global Search Halt — wire-up checklist

**Do not execute until after v1.0 ships, categories/SA PostV1 is done (or explicitly deferred), and Phase G0 architecture self-check is green.**

Plan: [`../../docs/POST_V1_GLOBAL_SEARCH_HALT.md`](../../docs/POST_V1_GLOBAL_SEARCH_HALT.md)  
Mission / products: [`../../docs/MISSION.md`](../../docs/MISSION.md)

**Product relationship:** Full Koobal = full UI + native-search core. Native Search = standalone slice of that native-search core (halt, tighten, guards; branding on main editor bar only). Global-bar halt/tighten is more of that core — both products get it when shipped; Native does not exclusively own it. Scaffold remains under PostV1 until extracted; see [`../../../KoobalNativeSearch/docs/FUTURE_GLOBAL_SEARCH.md`](../../../KoobalNativeSearch/docs/FUTURE_GLOBAL_SEARCH.md).

## Preconditions

1. Keep `GlobalSearchFeatureGate.*` **false** until ModTest smoke passes.
2. Keep `<Compile Remove="PostV1\**\*.cs" />` until intentionally enabling PostV1 compile include.
3. Deploy experiments to **ModTest only** — never Main.
4. Do **not** attach global halt listeners to editor when full Koobal dropdown is active (`SearchSurfaceRegistry` / orchestrator skip).
5. Do **not** wire GlobalSearch into live Native or full Koobal shipping builds in this band. Later: ship in full Koobal’s native-search core and port into Native for standalone parity.

## Wire-up order

### 1. Shared core bridge (optional but recommended)

| File | Change |
|------|--------|
| New shared lib **or** shipping copy of `TightSearchMatcher` | Prefer one matcher used by NativeEnterMatcher parity tests + full Koobal Enter. |
| `StockSearchHelper.CancelPendingStockSearchForTyping` | Live `ISearchExecutionHalt` for `EditorPartList` only. |
| `StockSearchGuard` | Bridge suppress depth — never skip SearchRoutine. |

### 2. Phase G1 — editor (proven; careful coexistence)

| File | Change |
|------|--------|
| Full Koobal `EditorSearchHook` | **No second halt** — dropdown path already includes native-search core. Global module skips editor bar. |
| Optional shared-core swap | Replace duplicated Enter scoring with `TightSearchMatcher` behind gate; parity tests required. |
| Native Search | Editor native-search core already proven; keep parity with relevant full-mod fixes. |

### 3. Phase G2 — R&D

| Step | Change |
|------|--------|
| Runtime audit | Dump TMP/InputField + subscribers in RD scene; confirm live filter. |
| Snapshot builder | Tech nodes → `SearchableItemSnapshot` (title, tech id, tags). |
| Halt adapter | Cancel RD live filter debounce / rebuild on type. |
| Apply adapter | Filter visible nodes by tight id set on Enter. |
| Fail-open | Log once; leave stock alone on reflection failure. |

### 4. Phase G3 — Tracking Station

Same pattern as G2 for vessel list filter.

### 5. Phase G4 — Craft browser

Same pattern for `CraftBrowserDialog` (or confirmed type) name filter.

### 6. Phase G5 — remaining confirmed surfaces

KSPedia, settings/mods, AG, roster, agencies — **only** after audit finds a real search bar. Skip empty inventory rows.

### 7. Phase G6 — Native standalone parity

| Product | Action |
|---------|--------|
| Full Koobal | Ships global halt+tighten as native-search core on non-editor bars (editor stays dropdown + core). |
| Native Search | Port the same core improvements into the standalone slice; still no dropdown/history/other full UI. Ongoing: any relevant full-mod core search fixes → Native. |

## Verification before Main

- [ ] Architecture self-check: Match + Halt + Apply + editor-skip when full Koobal present.
- [ ] ModTest G2: RD typing no lag; Enter tight; Escape/clear restores.
- [ ] ModTest: full Koobal editor dropdown still works (no double halt).
- [ ] KSP.log clean of SearchStart / coroutine null errors on editor regression.
- [ ] Feature gate off path: zero behaviour change.
- [ ] When Native ships parity: global bars work without predictive UI; soft exclusivity with full Koobal still holds.

## Explicit non-touch

- Do not add branding or dropdowns for global bars.
- Do not Prefix-skip SearchRoutine / IEnumerator search.
- Do not fall back to loose stock search on zero matches.
- Do not hook every TMP_InputField in a scene.
- Do not deploy architecture-only DLL to GameData as an enable.
- Do not describe Native as exclusive owner of global search — it is the standalone slice of the native-search core.
