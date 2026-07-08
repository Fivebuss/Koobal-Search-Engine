# Profile 02 — Main install top 25

Extends profile-01 with ranks 11–25 from `MAIN_INSTALL_MODS.md`.

## Apply

```powershell
.\apply-profile.ps1 -Profile profile-02-main-top25
```

Adds: NearFutureConstruction, ProceduralParts, WarpPlugin, StationPartsExpansionRedux, FShangarExtender, KerbalAtomics, NearFutureLaunchVehicles, CryoEngines, UmbraSpaceIndustries, CryoTanks, HeatControl, SystemHeat, KAS, KIS, NearFutureAeronautics.

**Blueshift / FireflyAPI (deferred):** Not included in this profile — saved for a later mod pack. The `WildBlueIndustries` junction from main may still contain Blueshift cfg files, but without `FireflyAPI` the mod does not load at runtime.

Shared deps include USI/WildBlue/NF support libraries and `TweakScaleCompanion` — all junctioned from main.
