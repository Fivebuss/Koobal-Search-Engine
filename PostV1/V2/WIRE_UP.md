# V2 wire-up checklist

**Do not execute until after v1.0 and an intentional V2 schedule.**

Feature gates stay **false** until ModTest. Prefer **Track S** end-to-end before any Track R prototype.

## Preconditions

1. Keep `V2FeatureGate.*` **false** (especially `EnablePartsListRebuild`).
2. Keep `<Compile Remove="PostV1\**\*.cs" />` until intentionally including V2 sources.
3. Deploy to **ModTest** only — never Main until Settings + slide-expand acceptance.
4. Do **not** call V2 from shipping paths while gates are off.

## Track choice

1. Ship **Track S** (slide-expand + settings + soft layout + perf soft).
2. Run go/no-go (`PartsListTrackGoNoGo` + measured evidence) before enabling Track R.
3. Only then flip `EnablePartsListRebuild` on ModTest.

## Wire-up order

### 1. Settings persistence + tab (shared)

| File / work | Change |
|-------------|--------|
| New `FileSettingsPersistence` | `PluginData/KoobalSettings.cfg` via `SettingsCfgCodec` |
| Live `ISettingsTabHost` | GameSettings page or DialogGUI; sections from `SettingsTabInformationArchitecture` |
| Optional toolbar | Mirror `SlideExpandEnabled` only (geometry), not maximize |

### 2. Track S — geometry composer

| File / work | Change |
|-------------|--------|
| Extend `PartsPanelCollapseHelper` (or new live adapter) | Implement `IPartsListSlideExpandController`; **compose** user expand + dropdown — restore must return to user-expand rest when expand on |
| `SearchDropdownPanel` open/close | Call composer `NotifyDropdownOpen` / recompose instead of Restore-to-stock-only |
| `PartsPanelTransitionGuard` | Ensure stock transitions don’t wipe expand rest |

### 3. Track S — soft layout + organizers + perf

| File / work | Change |
|-------------|--------|
| Live `IPartsListLayout` | Icon size / list style soft reflow |
| Live `IPartsListOrganizerBridge` | Detect CCK/filters/reskins; layout-only when compatibility on |
| Live `IPartsListVirtualizationPort` | Defer Refresh; attempt in-place window if feasible |

### 4. Track R — only after go/no-go

| File / work | Change |
|-------------|--------|
| Written decision | Lag/UX evidence vs plan §0 criteria |
| Live `IPartsListRebuildHost` | Owned list; consume same filters/predicates; ModTest only |
| Fallback | Keep ability to force Track S via settings |

## Verification before Main

- [ ] Settings tab loads/saves; reset defaults works.
- [ ] Slide-expand widens list; dropdown open/close still slides without fighting expand.
- [ ] Icon size / style apply without blanking stock categories.
- [ ] CCK / filter compatibility mode does not break tabs.
- [ ] Track R off by default; if prototyped, ModTest-only + force-S escape hatch.
- [ ] Gate-off path: zero behaviour change vs core-only.

## Explicit non-goals at wire-up

- No stock maximize / F11 chrome-swap UI.
- No architecture-only DLL copy to Main/`GameData/KoobalSearchEngine`.
- No Track R as silent default.
