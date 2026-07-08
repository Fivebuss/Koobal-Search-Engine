# Profile 05 — Legacy-version parts mods

Older CKAN KSP version pins still present on main 1.12.5 install.

## Mods

| Folder | CKAN KSP pin | Notes |
|--------|--------------|-------|
| B9_Aerospace | 1.8.0–1.8.99 | Legacy B9 parts |
| B9_Aerospace_HX | 1.8.0–1.8.99 | HX pack |
| REPOSoftTech | 1.8.0–1.8.99 | ION RCS etc. |
| DaMichel | 1.7.1–1.9.9999 | Spherical tanks |
| NearFutureRovers | 1.3.1–1.12.5 | Rover parts |
| LithobrakeExplorationTechnologies | 1.3.1–1.12.5 | Landing parts |
| ArcAerospace | 1.4+ | Wyvern parts |

## Apply

```powershell
.\apply-profile.ps1 -Profile profile-05-legacy
```

## Gate

Confirm parts from each legacy mod appear in VAB part list before running full query matrix.
