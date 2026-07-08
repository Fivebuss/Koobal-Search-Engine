# Profile 01 — Main install top 10 (CKAN + junction)

Run **after** VAB-UI organizer profiles (`profile-vab-ui-*`, `profile-vaborganizer`).

## Primary mods (CKAN installs with auto dependency resolution)

| # | CKAN id | GameData folder |
|---|---------|-----------------|
| 1 | ReStockPlus | ReStockPlus |
| 2 | TweakScale | TweakScale |
| 3 | NearFutureSolar | NearFutureSolar |
| 4 | NearFutureElectrical | NearFutureElectrical |
| 5 | NearFuturePropulsion | NearFuturePropulsion |
| 6 | InterstellarFuelSwitch | InterstellarFuelSwitch |
| 7 | NearFutureSpacecraft | NearFutureSpacecraft |
| 8 | B9-props | B9_Aerospace (+ HX via CKAN deps) |
| 9 | NearFutureConstruction | NearFutureConstruction |
| 10 | ProceduralParts | ProceduralParts |

CKAN pulls transitive dependencies (ModuleManager, CommunityResourcePack, B9PartSwitch, etc.) — do not hand-maintain dep lists.

## Apply

```powershell
cd "F:\SteamLibrary\steamapps\common\Kerbal Space Program\Source\PartSearchSuggest\testing"
.\apply-profile.ps1 -Profile profile-01-main-top10
```

Junctions matching folders from main install first; CKAN install fills any missing dependencies.

## Verify compat

```powershell
$ckan = "..\Kerbal Space Program - ModTest\Tools\ckan.exe"
& $ckan show ReStockPlus --headless
```
