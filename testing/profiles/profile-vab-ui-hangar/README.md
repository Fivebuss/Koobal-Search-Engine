# Profile — Hangar (main install)

**Exclusive group:** `vab-ui-organizer` — test alone, not with VABOrganizer/PartCatalog.

## Required dependencies

Hangar is junctioned from the main install; `apply-profile.ps1` skips CKAN install for junctioned primary mods, so **deps must be installed explicitly**.

| CKAN id | GameData folder | Required for |
|---------|-----------------|--------------|
| `ModuleManager` | `ModuleManager.*.dll` (GameData root) | MM patches; auto-installed by CKAN |
| `AT-Utils` | `000_AT_Utils` | **Hangar.dll load** — hard dependency |
| `CommunityResourcePack` | `CommunityResourcePack` | Resource definitions for Hangar parts |

Also present on every ModTest profile: `000_Harmony`, `CommunityCategoryKit` (base junction), `PartSearchSuggest`.

### Not required for basic Hangar function

- **B9 / B9PartSwitch** — not CKAN depends for Hangar
- **CommunityCategoryKit** — base ModTest layer; Hangar does **not** ship a CCK category patch. The Hangar VAB tab and `HangarCategory` icons come from `Hangar.dll` itself once AT-Utils is present.
- **FilterExtensions** — optional MM patch in `Hangar/MM/FilterExtensions.cfg` only
- **EditorExtensions** — CKAN *suggest* only

## Known profile pitfall (fixed)

If only `Hangar` is junctioned without `AT-Utils`, KSP.log shows:

```
ADDON BINDER: Cannot resolve assembly: 000_AT_Utils
AssemblyLoader: Exception loading 'Hangar'
```

Symptoms: hangar parts have no `Hangar` module behavior, no Hangar sidebar tab, parts may appear as inert shells.

## Apply

```powershell
.\apply-profile.ps1 -Profile profile-vab-ui-hangar
```

Re-apply after profile changes so CKAN installs `AT-Utils` and `CommunityResourcePack` on ModTest.

## Test focus

1. **PartSearchSuggest** — search dropdown, categorizer, collapse (primary PSS matrix goal)
2. **Hangar mod** — only after deps present: Hangar tab in VAB, place hangar part, open/close doors, store vessel

If Hangar still fails after deps are installed, treat as Hangar/mod issue — not PartSearchSuggest — and note INCONCLUSIVE for Hangar-specific behavior.
