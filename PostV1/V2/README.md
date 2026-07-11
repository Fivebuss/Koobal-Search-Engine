# PostV1 V2 — Slide-expand parts list + Settings tab

**Status:** Architecture scaffolding — **excluded from the shipping `KoobalSearchEngine.dll` build.**  
**Band:** **V2 / post-v1** (after v1.0; schedule intentionally).

Plan: [`../../docs/V2_PARTS_LIST_AND_SETTINGS.md`](../../docs/V2_PARTS_LIST_AND_SETTINGS.md)

## Dual tracks (read first)

| Track | Name | Stance |
|-------|------|--------|
| **S** | **Slide expand** | **Preferred default** — slide-grow panel geometry like today’s dropdown shift; soft icon size/style; optimize stock list in place |
| **R** | **Rebuild parts list** | **Optional** after go/no-go — new list UI hosting icons/rows; higher risk/reward; **not** assumed, **not** forbidden |

**Not either track’s default UX:** stock maximize / fullscreen chrome-swap.

Settings tab is required for options on **both** tracks.

## Exclusion from shipping compile

Parent folder is covered by `PartSearchSuggest.csproj`:

```xml
<Compile Remove="PostV1\**\*.cs" />
```

Do not wire into `EditorSearchHook`, `PartsPanelCollapseHelper`, ModTest, or Main `GameData` until V2 is intentionally scheduled.

## Compile-check (does not deploy)

```powershell
dotnet build "Source\PartSearchSuggest\PostV1\PostV1.Architecture.csproj" -c Release
```

Outputs to `PostV1/bin/` — never GameData.

## Layout

| Area | Role |
|------|------|
| `ModSettings/` | `KoobalSettingsModel`, `IKoobalSettingsStore`, cfg codec, `ISettingsTabHost` (Unwired/Recording) |
| `PartsListExperience/` | Track S slide-expand + compose; Track R rebuild host; layout prefs; organizer; virtualization; go/no-go |
| `V2Services` | Composition root |
| `V2FeatureGate` | **All false** (includes `EnablePartsListRebuild`) |
| `WIRE_UP.md` | Shipping touch list (do not execute yet) |

## What’s done (architecture)

- Settings model + memory store + cfg codec + patch API.
- Settings IA sections: Parts List / Search / History / Advanced.
- Pure geometry compose: user expand + dropdown contribution.
- Effective track resolution (Force S / Allow R experimental).
- Go/no-go evaluator for Track R.
- Recording ports for self-check; Unwired boundaries for live wire-up.

## What’s left for wire-up

See [`WIRE_UP.md`](WIRE_UP.md). Summary: live PluginData file, Settings tab host, extend `PartsPanelCollapseHelper` into a composer, soft icon reflow, organizer probes, then optional Track R only after written go decision.

## Rules

1. **Track S first**; Track R only after go/no-go.
2. Slide-expand ≠ maximize/chrome-swap.
3. One geometry composer for dropdown + user expand.
4. Gates stay false until ModTest per phase.
5. No architecture DLL to GameData.

## Namespace

`PartSearchSuggest.PostV1.V2.*` — tagged `[PostV1Phase(PostV1Phase.E_V2PartsListAndSettings)]`.
