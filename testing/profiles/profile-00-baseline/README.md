# Profile 00 — Baseline



Harmony2 + Koobal Search Engine + **CommunityCategoryKit** (permanent ModTest base layer).



## Apply



```powershell

cd "F:\SteamLibrary\steamapps\common\Kerbal Space Program\Source\PartSearchSuggest\testing"

.\apply-profile.ps1 -Profile profile-00-baseline

```



Removes all third-party GameData except `Squad`, `000_Harmony`, `KoobalSearchEngine`, then junctions CCK from main.



## Mods present



| Folder | Source |

|--------|--------|

| Squad | stock |

| 000_Harmony | local copy (main install) |

| Koobal Search Engine | robocopy from main |

| CommunityCategoryKit | junction from main |



## CKAN



No CKAN installs. Instance stays `KSP-ModTest`.



## Test gate



Pass TEST_PROTOCOL baseline queries before advancing to VAB-UI profiles.

