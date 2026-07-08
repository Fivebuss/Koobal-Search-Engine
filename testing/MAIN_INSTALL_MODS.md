# Main Install Mod Inventory

Generated: 2026-07-07 01:26
Source: `F:\SteamLibrary\steamapps\common\Kerbal Space Program\GameData` + CKAN `registry.json` (compat/deps source of truth)

## Summary

| Metric | Count |
|--------|------:|
| Top-level GameData folders (excl. Squad) | 131 |
| VAB-UI / organizer (high priority) |  |
| VABO-patch-only folders | 26 |
| Parts mods (profile-01+) | 37 |

## Mutual exclusivity - inventory organizers

Users typically run **ONE** inventory/organizer mod. **Never** install two of these in the same ModTest profile:

| CKAN id | On main install? | Profile |
|---------|:----------------:|---------|
| `CommunityCategoryKit` | **YES** | profile-vab-ui-communitycategorykit |
| `VABOrganizer` | no | profile-vaborganizer |
| `PartCatalog` | no | (profile not created - CKAN-only matrix pass) |
| `CategoryParts` | no | (profile not created - CKAN-only matrix pass) |
| `EditorExtensions` | no | (profile not created - CKAN-only matrix pass) |

**Main install organizer:** ``CommunityCategoryKit`` (CCK). VABOrganizer/PartCatalog/CategoryParts/EditorExtensions are **not** installed but many mods ship dormant VABO patches.

**Test order:** baseline -> VAB-UI profiles (one at a time) -> parts-mod sweeps (profile-01+).

## VAB-UI / ORGANIZER - high priority (test before parts sweeps)

| Priority | GameData | CKAN id | Downloads | KSP compat (CKAN) | Role |
|----------|----------|---------|----------:|-------------------|------|
| P0/P1 | `CommunityCategoryKit` | CommunityCategoryKit | 1,276,301 | 1.8.0-1.99.99 | VAB-UI / ORGANIZER |
| P0/P1 | `KSPCommunityFixes` | KSPCommunityFixes | 990,871 | 1.8.0-1.12.5 | VAB-UI / EDITOR-PATCH |
| P0/P1 | `FShangarExtender` | HangerExtenderExtended | 928,850 | 1.12.0-1.12.99 | VAB-UI / LAYOUT |
| P0/P1 | `Hangar` | Hangar | 137,643 | - | VAB-UI / LAYOUT |

### VABO-patch mods on main (need VABOrganizer CKAN install to activate)

| GameData folder | CKAN id(s) |
|-----------------|------------|
| `ReStockPlus` | ReStockPlus |
| `NearFutureSolar` | NearFutureSolar-Core, NearFutureSolar |
| `NearFutureElectrical` | NearFutureElectrical, NearFutureElectrical-Core |
| `NearFuturePropulsion` | NearFuturePropulsion |
| `NearFutureSpacecraft` | NearFutureSpacecraft |
| `NearFutureConstruction` | NearFutureConstruction |
| `StationPartsExpansionRedux` | StationPartsExpansionRedux |
| `KerbalAtomics` | KerbalAtomics |
| `NearFutureLaunchVehicles` | NearFutureLaunchVehicles |
| `CryoEngines` | CryoEngines |
| `CryoTanks` | CryoTanks, CryoTanks-Core |
| `HeatControl` | HeatControl |
| `SystemHeat` | SystemHeat |
| `NearFutureAeronautics` | NearFutureAeronautics |
| `NearFutureExploration` | NearFutureExploration |
| `MarkIVSystem` | MarkIVSpaceplaneSystem |
| `SterlingSystems` | SterlingSystemsElectricsPhotoVoltaic, SterlingSystemsEngines, SterlingSystemsThermalsPower, SterlingSystemsThermalsExotic, SterlingSystemsEnginesAntimatter, SterlingSystemsAgency, SterlingSystemsUtilitiesConstruction, SterlingSystemsElectrics, SterlingSystemsThermals, SterlingSystemsUtilities |
| `SpaceDust` | SpaceDust |
| `FarFutureTechnologies` | FarFutureTechnologies |
| `LithobrakeExplorationTechnologies` | LithobrakeExplorationTechnologies |
| ... | 26 total folders with VABO configs |

## Parts mods (download count descending)

| Rank | GameData | CKAN id(s) | Downloads | KSP compat | Class |
|-----:|----------|------------|----------:|------------|-------|
| 1 | `B9PartSwitch` | B9PartSwitch | 1,927,687 | 1.12.0-1.12.99 | parts / editor-workflow |
| 2 | `ReStock` | ReStock | 1,777,072 | 1.12.2-1.12.99 | parts / editor-workflow |
| 3 | `TweakScale` | TweakScale | 1,476,663 | 1.3.0-1.12.5 | parts / editor-workflow |
| 4 | `InterstellarFuelSwitch` | InterstellarFuelSwitch, InterstellarFuelSwitch-Core | 1,312,725 | 1.8.1-1.12.5 | parts |
| 5 | `B9_Aerospace` | B9-props, B9 | 1,160,710 | 1.8.0-1.8.99 | parts |
| 6 | `B9_Aerospace_HX` | B9AerospaceHX | 1,160,126 | 1.8.0-1.8.99 | parts |
| 7 | `ProceduralParts` | ProceduralParts | 1,045,135 | 1.8.1-1.12.99 | parts / editor-workflow |
| 8 | `WarpPlugin` | KSPInterstellarExtended | 986,093 | 1.8.1-1.12.5 | parts |
| 9 | `UmbraSpaceIndustries` | Konstruction, USI-Core, USI-FTT, UKS, USITools, USI-ART, USI-EXP, USI-LS | 751,522 | 1.8.0-1.12.99 | parts |
| 10 | `KAS` | KAS | 567,068 | 1.11.0-1.99.99 | parts / editor-workflow |
| 11 | `KIS` | KIS | 550,192 | 1.11.0-1.99.99 | parts / editor-workflow |
| 12 | `WildBlueIndustries` | KerbalActuators, WildBlueCore, WildBlue-PlayMode-CRP, WildBlueTools, Blueshift | 469,680 | 1.12.2- | parts |
| 13 | `Mk2Expansion` | Mk2Expansion | 458,706 | - | parts |
| 14 | `B9_Aerospace_ProceduralWings` | B9-PWings-Fork | 450,513 | 1.10.0-1.12.99 | parts |
| 15 | `REPOSoftTech` | IONRCS, REPOSoftTech-Agencies | 436,400 | 1.8.0-1.8.99 | parts |
| 16 | `Mk3Expansion` | Mk3Expansion | 273,959 | - | parts |
| 17 | `PhotonSail` | PhotonSailor | 230,745 | 1.8.1-1.12.5 | parts |
| 18 | `VanguardTechnologies` | EVAParachutes | 197,388 | 1.8.0-1.12.99 | parts |
| 19 | `UniversalStorage2` | UniversalStorage2 | 186,497 | 1.12.0-1.12.99 | parts |
| 20 | `OPT` | OPTSpacePlaneMain | 170,594 | - | parts |
| 21 | `MiningExpansion` | StockalikeMiningExtension | 140,551 | 1.12.0-1.12.99 | parts |
| 22 | `NearFutureRovers` | RoverPack | 128,627 | 1.3.1-1.12.5 | parts |
| 23 | `RaginCaucasian` | RaginCaucasian | 128,193 | 1.8.0-1.12.99 | parts |
| 24 | `BetterScienceLabsContinued` | BetterScienceLabsContinued | 108,224 | 1.8.0-1.12.99 | parts |
| 25 | `ExtraplanetaryLaunchpads` | ExtraPlanetaryLaunchpads | 101,547 | 1.12.0-1.12.99 | parts / editor-workflow |
| 26 | `SpaceTuxIndustries` | RecycledPartsLVNClusters, RecycledPartsMk2SolarBatteries, RecycledPartsMk2KISContainers, RecycledPartsMk2Essentials, RecycledPartsOrgamiFoldableAssets, RecycledPartsMk2Lightning | 96,825 | 1.12.0-1.12.99 | parts |
| 27 | `Mk3HypersonicSystems` | Mk3HypersonicSystems | 46,435 | - | parts |
| 28 | `DaMichel` | DMTanks-SphericalTanks | 43,491 | 1.7.1-1.9.9999 | parts |
| 29 | `SHED` | REKT | 36,072 | 1.8.0-1.12.5 | parts |
| 30 | `PrakasaAeroworks` | PrakasaAeroworks | 34,512 | 1.9.1-1.12.99 | parts |
| 31 | `AtomicTech` | AtomicTechnologiesIncorporated, AtomicTechIncJunkyards | 30,737 | - | parts |
| 32 | `Landertron` | Landertron | 27,193 | 1.12.0-1.12.99 | parts |
| 33 | `ArcAerospace` | Wyvern-5 | 17,028 | 1.4- | parts |
| 34 | `KerbalExpeditionaryGroup` | KerbalExpeditionaryGroup, MK3Science | 10,045 | - | parts |
| 35 | `CEDA_ProjectAethon` | CEDA-Aethon-ITS | 9,626 | - | parts |
| 36 | `XwinKieFi` | XwinkvsKiefighter | 4,446 | - | parts |
| 37 | `XwinKTransport` | XwinkTransports | 2,320 | - | parts |

## Skipped (reference)

| GameData | CKAN id(s) | Downloads | Reason |
|----------|------------|----------:|--------|
| `CommunityResourcePack` | CommunityResourcePack | 2,247,380 | skip-folder |
| `JSI` | RasterPropMonitor, RasterPropMonitor-Core | 1,968,080 | skip-ckan-id |
| `KerbalEngineer` | KerbalEngineerRedux | 1,869,018 | skip-ckan-id |
| `000_ClickThroughBlocker` | ClickThroughBlocker | 1,807,059 | skip-folder |
| `Firespitter` | FirespitterCore | 1,706,667 | no-parts |
| `001_ToolbarControl` | ToolbarController | 1,692,695 | skip-ckan-id |
| `CommunityTechTree` | CommunityTechTree | 1,443,093 | skip-folder |
| `Waterfall` | Waterfall | 1,224,053 | skip-ckan-id |
| `NearFutureProps` | NearFutureProps | 917,260 | no-parts |
| `000_Harmony` | Harmony2 | 903,213 | skip-ckan-id |
| `KerbalAtomicsLH2NTRModSupport` | KerbalAtomics-NTRModSupport | 899,649 | no-parts |
| `NearFutureElectricaNTRs` | KerbalAtomics-NFECompatibility | 897,262 | no-parts |
| `000_TexturesUnlimited` | TexturesUnlimited | 867,044 | skip-ckan-id |
| `SpaceTuxLibrary` | SpaceTuxLibrary | 856,213 | skip-folder |
| `DynamicBatteryStorage` | DynamicBatteryStorage | 821,613 | no-parts |
| `CryoEnginesNFAero` | CryoEngines-NFAero | 796,688 | no-parts |
| `000_USITools` | USITools | 751,522 | skip-folder |
| `KerbalJointReinforcement` | KerbalJointReinforcementNext | 747,070 | skip-folder |
| `DMagicOrbitalScience` | DMagicOrbitalScience | 739,489 | no-parts |
| `999_KSP-Recall` | KSP-Recall | 661,097 | skip-folder |
| `KerbalReusabilityExpansion` | SpaceXLegs | 654,543 | no-parts |
| `DeployableEngines` | DeployableEngines | 643,361 | no-parts |
| `PatchManager` | PatchManager | 584,184 | skip-ckan-id |
| `PhysicsRangeExtender` | PhysicsRangeExtender | 558,575 | skip-ckan-id |
| `BahaSP` | BDAnimationModules | 549,148 | skip-folder |
| `000_AT_Utils` | AT-Utils | 448,318 | skip-folder |
| `KerbalChangelog` | KerbalChangelog | 431,158 | skip-ckan-id |
| `KSPWheel` | KSPWheel | 430,338 | no-parts |
| `RetractableLiftingSurface` | RetractableLiftingSurface | 416,700 | no-parts |
| `StockWaterfallEffects` | StockWaterfallEffects | 400,314 | skip-ckan-id |
| `B9AnimationModules` | B9AnimationModules | 357,462 | skip-folder |
| `IndicatorLights` | IndicatorLights | 346,414 | skip-ckan-id |
| `ASET` | ASETAgency, ASETProps | 338,114 | no-parts |
| `RocketSoundEnhancement` | RocketSoundEnhancement | 333,489 | skip-ckan-id |
| `FMRS` | FMRSContinued | 329,665 | skip-folder |
| `FireflyAPI` | FireflyAPI | 309,513 | skip-ckan-id |
| `OPT_Reconfig` | OPTReconfig | 299,197 | no-parts |
| `RocketSoundEnhancementDefault` | RocketSoundEnhancement-Config-Default | 290,358 | skip-folder |
| `MechJebForAll` | MechJebForAll | 289,748 | no-parts |
| `PersistentRotation` | PersistentRotation | 280,152 | skip-ckan-id |
| `ROUtils` | ROUtils | 277,467 | skip-ckan-id |
| `RecoveryController` | RecoveryController | 265,146 | skip-ckan-id |
| `VesselMover` | VesselMoverContinued | 245,334 | skip-folder |
| `SpaceTuxSA` | SpacetuxSA | 206,402 | skip-folder |
| `TweakScaleCompanion` | TweakScaleCompanion | 202,717 | no-parts |
| `ShipManifest` | ShipManifest | 202,315 | skip-ckan-id |
| `JX2Antenna` | JX2Antenna | 183,389 | no-parts |
| `ModuleAnimateGenericEffects` | ModuleAnimateGenericEffects | 68,495 | skip-folder |
| `SSTOProject` | SSTOProject | 44,426 | no-parts |
| `ModuleSequentialAnimateGeneric` | ModuleSequentialAnimateGeneric | 33,842 | skip-folder |
| `BonVoyage` | BonVoyage | 32,069 | skip-ckan-id |
| `SpaceDustUnbound` | SpaceDustUnbound | 27,888 | skip-ckan-id |
| `PWBFuelBalancerRestored` | PWBFuelBalancerRestored | 21,788 | skip-ckan-id |
| `Mk2Rebalance` | Mk2Rebalance | 19,871 | no-parts |
| `Missing_robotics` | MissingRobotics | 18,664 | skip-folder |
| `RRPT` | RealisticReStockAndProceduralTanks | 17,605 | no-parts |
| `SmokeScreen` | SmokeScreen | 7,510 | skip-ckan-id |
| `DaedalusConsortium` | DaedalusConsortiumMegastationsContinued | 5,225 | no-parts |
| `JxFab_UtilitySystems` | JxFabUtilitySystems | 4,908 | no-parts |
| `Kerbaltek` | HyperEdit | 135 | skip-ckan-id |
| `SquadExpansion` |  | 0 | skip-folder |
| `MechJeb2` | MechJeb2 | 0 | skip-ckan-id |
| `PartSearchSuggest` |  | 0 | skip-folder |
| `TitanStarship` |  | 0 | no-parts |

