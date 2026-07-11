# PostV1 Global Search Halt — architecture scaffolding

**Status:** Near-implementation architecture — **excluded from the shipping `KoobalSearchEngine.dll` build.** Not wired into live Native or full Koobal yet.

Enter-to-search **halt** + **tightened matching** on every KSP search bar — part of Koobal’s **native/stock search improvements** core (same functional family as Native Enter path), **without** branding, dropdowns, or predictive UI on those bars.

**Product relationship:** See [`../../docs/MISSION.md`](../../docs/MISSION.md).

| Product | Role for this feature |
|---------|----------------------|
| **Full Koobal** | Full UI product **plus** this native-search core (editor already has halt+Enter; global bars get halt+tighten only). |
| **Koobal Native Search** | Standalone slice of that native-search core. Global-bar halt/tighten fits this slice when shipped. Branding stays on the main editor bar only. Kept in parity with **any relevant** full-mod fixes. |

Native does **not** exclusively own global search. Scaffold stays under `PartSearchSuggest/PostV1` until extracted (see [`../../../KoobalNativeSearch/docs/FUTURE_GLOBAL_SEARCH.md`](../../../KoobalNativeSearch/docs/FUTURE_GLOBAL_SEARCH.md)).

Plan: [`../../docs/POST_V1_GLOBAL_SEARCH_HALT.md`](../../docs/POST_V1_GLOBAL_SEARCH_HALT.md)

Unrelated ~0.9 QoL (editor history per-row delete): [`../SearchHistory/README.md`](../SearchHistory/README.md).

## Exclusion from shipping compile

Parent folder is covered by `PartSearchSuggest.csproj`:

```xml
<Compile Remove="PostV1\**\*.cs" />
```

Do not remove that exclude for GlobalSearch alone. Do not wire into `EditorSearchHook`, Harmony, ModTest, Main `GameData`, or Native Search until after v1 **and** after categories/subassemblies PostV1 work.

## Compile-check (does not deploy)

```powershell
dotnet build "Source\PartSearchSuggest\PostV1\PostV1.Architecture.csproj" -c Release
```

Outputs to `PostV1/bin/` — never GameData.

## Layout

| File / type | Role |
|-------------|------|
| `SearchBarSurface` + `SearchSurfaceRegistry` | Inventory + confidence + coexistence rules |
| `ISearchExecutionHalt` + Unwired/Recording | Cancel live-as-you-type (no IEnumerator Prefix-skip) |
| `ITightSearchMatcher` + `TightSearchMatcher` | NativeEnterMatcher-style pure scoring on snapshots |
| `ISearchResultApplier` + Unwired/Recording | Id-set filter apply bookkeeping |
| `SnapshotListFilter` | Pure list filter helper |
| `SearchableItemSnapshot*` / `TightMatchResult` | Snapshot DTOs |
| `GlobalSearchOrchestrator` | Type→halt; Enter→match→apply |
| `GlobalSearchServices` | Composition root |
| `GlobalSearchFeatureGate` | **All false** |
| `WIRE_UP.md` | Shipping touch list (do not execute yet) |

## What’s done (nearly ready)

- Surface registry with Proven vs NeedsRuntimeConfirmation tags.
- Full tight-match algorithm (score bands, title-first, no description-only Enter hits).
- Orchestrator coexistence rule: skip editor when full Koobal dropdown present.
- Recording ports exercisable via `ArchitectureSelfCheck`.
- Part-like + browser-row snapshot factories on `TightSearchMatcher`.

## What’s left for wire-up

See [`WIRE_UP.md`](WIRE_UP.md). Summary:

1. Live halt adapters per surface (editor already proven — reuse `StockSearchHelper` cancel).
2. Snapshot builders from stock list rows (RD / TS / craft browser / …).
3. Live appliers (editor: `SearchFilterResult`; others: confirmed filter APIs).
4. Scene hooks + runtime audit for non-editor surfaces.
5. Feature gates still off until ModTest smoke.
6. Native standalone parity: port global halt+tighten (+ **any relevant** core fixes) into Native when shipping.

## Rules

1. **No chrome** on global bars — no branding panel, no suggestion dropdown (Native branding stays on the main editor bar only).
2. **Match on Enter only** — never on `onValueChanged`.
3. **Never Prefix-skip** IEnumerator search routines.
4. **No loose stock fallback** on zero tight matches.
5. **Do not double-hook** editor when full Koobal’s dropdown path is active.
6. **Native = standalone slice** of the native-search core; keep parity with **any relevant** full-mod fixes. Do not treat Native as exclusive owner of global search. Do not change shipping behaviour in this band.

## Namespace

`PartSearchSuggest.PostV1.GlobalSearch` — tagged `[PostV1Phase(PostV1Phase.D_GlobalSearchHalt)]`.
