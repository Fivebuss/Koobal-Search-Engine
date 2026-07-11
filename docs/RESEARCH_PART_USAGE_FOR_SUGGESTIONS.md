# Research: Part usage signals for suggestion ranking

**Status:** Research memo only — **no ranking code, configs, or GameData changes**.  
**Date:** 2026-07-11  
**Goal:** Find public / practical data that could later bias Koobal Search Engine suggestions toward parts average KSP players actually use.

---

## Executive verdict

There is **no ready-made public dataset** of “most used KSP stock parts” (community-wide frequency from crafts). The best *latent* source is **KerbalX** (per-part “N craft use this part” + craft `.craft` bodies), but there is **no documented open bulk/export API** for aggregate rankings. **SpaceDock is not a craft-sharing site** — it hosts **mods** (zipballs via a documented HTTP API); `.craft` files appear only as occasional **bundled demos** inside part packs / replica mods and are **heavily mod-biased**. On this research install, **mod example crafts landed in root `Ships/`**, not under `GameData/<mod>/` (zero third-party `.craft` under GameData despite ~130 mod folders) — still a useful local signal, with clear showcase bias. `.craft` files themselves remain **easy to parse** (`part = InternalName_uid`). Practical next steps are: (1) a small **opt-in / respectful** KerbalX sample of pure-stock crafts, (2) optional **local Saves + Ships craft mining**, and (3) a **hand-curated stock prior** from tutorial meta + stock Ships analysis — shipped later as an **optional weight file**, not hard-coded ranking.

---

## 1. Public craft repositories

| Source | URL | What exists | Usable for part frequency? |
|--------|-----|-------------|----------------------------|
| **KerbalX** | https://kerbalx.com/ | Primary KSP craft-sharing site; scans uploads for mods/parts; search by stock / SPH / VAB / class / part count; per-part pages list crafts using that part | **Best candidate.** Per-part usage lists exist; **no public aggregate “top parts” dump** found. Site API via **KXAPI** is for **authorized mods**, not open bulk scrape. |
| KerbalX Parts index | https://kerbalx.com/parts | Filter “are used in craft”, sort by name, group by mod | UI exists; HTML is JS-heavy — hard to scrape without respectful rate-limited tooling / permission. |
| KerbalX About / PartMapper | https://kerbalx.com/about | Crowd-sourced part→mod mapping + CKAN | Explains how KX builds part knowledge; not a frequency dataset. |
| KerbalX stats | https://kerbalx.com/statistics | Site statistics page (fetch timed out in this research pass) | Possibly craft/user totals; **not confirmed** as part-frequency. |
| KerbalX mod / Craft Manager / KXAPI | https://kerbalx.com/mod · https://kerbalx.com/CraftManager · https://github.com/Sujimichi/KXAPI | In-game upload/download; authorized API | Useful for players; **not** an open research corpus. |
| **Forum Craft Repository** | https://forum.kerbalspaceprogram.com/forum/81-craft-repository/ | Classic forum craft threads | Crafts attached case-by-case; **no structured corpus**. |
| **Reddit** r/KerbalSpaceProgram | https://www.reddit.com/r/KerbalSpaceProgram/ | Craft screenshots / occasional `.craft` pastes | Unstructured; poor for frequency mining. |
| **SpaceDock** | https://spacedock.info/ | **Mod host** (not craft hangar). Documented `/api/*` for mods; downloads are version zipballs. Bundled `.craft` demos exist inside some mods | **Poor for stock part-frequency.** No craft category, no pure-stock filter, no per-part craft index. See **§1a**. |
| **CurseForge Shareables** | https://www.curseforge.com/kerbal/search?class=shareables&categories=planes-and-ships | Dedicated **Shareables** class (~1k+ Planes and Ships); some stock replicas | Better than SpaceDock for *craft* packs; still showcase/replica-biased; CurseForge API needs auth key. |
| **Steam Workshop (KSP)** | Steam app `220200` workshop | In-game craft subscribe; files land under `steamapps/workshop/content/220200/` | Usable locally after subscribe; **no research-friendly bulk API**; ToS/Steam limits apply. |
| **spacedock.ru** (unrelated) | https://spacedock.ru/ | RU community portal with **Сохранения** (saves/crafts) posts — **not** spacedock.info | Forum/blog style; unstructured; language/ToS separate; not a structured corpus. |
| **KSP Builds** | https://kspbuilds.com/ | Hangars / playlists for **KSP2** builds | Wrong game generation for KSP1 stock priors. |
| **GitHub craft dumps** | e.g. https://github.com/edlund/ksp-craftfiles | Small personal collections (often outdated / MechJeb) | Too small / biased for “average player”. |
| Mod packs with demo crafts | SpaceDock / CurseForge part packs | Ships showcase **that mod’s** parts | Biases toward modded parts — avoid for stock priors. |
| KSP Wiki / Fandom “Parts” | https://kerbalspaceprogram.fandom.com/wiki/Parts | Category taxonomy, not usage ranks | Useful for **category priors**, not frequency. Official wiki craft-file page blocked by anti-bot (Anubis) during this pass. |

### 1a. SpaceDock deep dive (2026-07-11 follow-up)

**What SpaceDock is:** Multi-game **mod repository** (KerbalStuff lineage). Browse UI is “Popular Mods” / search mods — there is **no hangar, craft class, or Ships category**. “Hangar” on the site is a **parts mod** (mod id 1000), not craft sharing.

**Public API** (docs: https://github.com/KSP-SpaceDock/SpaceDock/blob/master/api.md · footer link https://spacedock.info → GitHub `api.md`):

| Endpoint | Role |
|----------|------|
| `GET /api/browse` | Paginated mods (`page`, `count` 1–500, `orderby` name/updated/created, `game_id`) |
| `GET /api/browse/{new\|featured\|top}` | Curated lists |
| `GET /api/search/mod?query=` | Text search (also supports UI tips: `user:`, `ver:`, `game:`, `downloads:>N`, …) |
| `GET /api/mod/<id>` | Mod metadata + `versions[]` with `download_path` |
| `GET /api/mod/<id>/latest` | Latest version stub |
| `GET /api/games` | Games (KSP id **3102**, short `kerbal-space-program`; also KSP2, KSA, …) |
| `POST /api/download_counts` | Batch download counts |
| Auth `POST /api/login` + create/update | Uploader workflows only |

**Download URL pattern:** `https://spacedock.info` + `versions[].download_path`  
Example: `/mod/2176/Shuttle%20Orbiter%20Construction%20Kit/download/1.1.8` → full zipball (GameData + optional Ships/`.craft`).

**API etiquette (from api.md):** Set a descriptive **User-Agent** with contact info. Responses are JSON. No official bulk-export of craft corpora (there is none to export).

**robots.txt:** `https://spacedock.info/robots.txt` → **404** (no crawl policy published). Do **not** treat missing robots as a license to mass-mirror; still respect ToS + rate limits.

**Terms / license (https://spacedock.info/privacy):**

- Be responsible; no illegal uploads; no harassment; upload only content you can license.
- Uploaders grant SpaceDock a **royalty-free worldwide license to distribute their mods**.
- Site content under **German copyright**; private use reproductions allowed under §44a-ish framing; **commercial / bulk redistribution** of others’ works needs rights-holder consent.
- **Per-mod license** still applies to zip contents (GPL, CC-BY-*, ARR, etc.). Example: Shuttle Orbiter Construction Kit is **ARR** — fine to download for personal play; poor basis for redistributing derived stats dumps without care.
- **Practical research stance:** Small manual samples of published download links + local parse = aligned with personal/research use. **Mass automated scraping of all zips** is discouraged (load, ToS spirit, mixed ARR). Prefer API metadata browsing over downloading every mod.

**Where `.craft` files appear:** Only inside some mod zipballs (`Ships/`, `GameData/.../VAB|SPH/`, “sample craft” folders). Search queries like `craft`, `sample craft`, `craft files included` return **part packs that mention crafts**, not a craft index — e.g. Near Future Spacecraft, Airplane Plus, SOCK, Thunderbirds InterKerbin Rescue.

**Tiny SpaceDock craft sample (not representative):**

Downloaded **2** craft-oriented packs with permissive licenses (CC-BY-SA / GPLv3), **6** `.craft` files total:

| Pack | SpaceDock id | Crafts | Notes |
|------|--------------|--------|-------|
| Thunderbirds InterKerbin Rescue | 3088 | 5 (SPH/VAB) | Replica set; almost all parts are mod (`CJThunder*`, `CJMole*`, …) |
| Flying Wing Cargo Hauler | 1173 | 1 (SPH) | SSTO showcase; parts are pack-specific (`FlyingWing*`) |

**Presence leaders in this 6-craft sample:** mod-unique names dominate (`CJThunderbird*`, `FlyingWing*`). Stock-ish names barely appear (`liquidEngine1-2`, `commDish`, `dockingPort2`, `wingShuttleRudder`, `Size3EngineCluster`) and only on individual crafts. **Conclusion:** SpaceDock-bundled crafts measure **“parts this pack wants you to try”**, not average stock career usage.

**Biases vs KerbalX:**

| Dimension | SpaceDock | KerbalX |
|-----------|-----------|---------|
| Primary content | Mods (GameData zips) | Crafts (`.craft` + metadata) |
| Pure-stock filter | None | Yes (“Only Pure Stock”) |
| Per-part “N crafts use this” | No | Yes |
| SPH/VAB / class / downloads on craft | N/A (mod downloads) | First-class craft fields |
| API for listing units of interest | Strong **mod** API | Craft Manager / KXAPI for authorized clients; no open bulk craft dump |
| Stock part-frequency usability | **Low** (mod demos) | **Highest known** (with sampling constraints) |
| Adjacent “shareables” | Accidental crafts in zips | Entire product |

**Verdict for popularity weights:** Do **not** use SpaceDock as a primary stock part-usage corpus. Optional secondary use: CKAN/SpaceDock **mod download ranks** as a proxy for *mod* popularity only (already noted in §2) — never confuse with stock `part =` frequency.

### 1b. Local GameData / Ships craft sample (this install)

**Install scanned:** `F:\SteamLibrary\steamapps\common\Kerbal Space Program\`  
**Scope:** all `*.craft` under `GameData/` and root `Ships/` (backup-like paths excluded by name heuristic). Saves and Steam Workshop not included in the primary count (Workshop folder absent; Saves had ~36 crafts noted only).

**Finding — where mod crafts actually live:** Despite **~133** `GameData` top-level folders (heavy modded install), **`GameData` contained 0 third-party `.craft` files**. All GameData crafts were Squad / SquadExpansion (contracts, missions, tutorials, MH/Serenity ships). Third-party **example crafts were installed into root `Ships/VAB` and `Ships/SPH`** (common mod packaging pattern: unzip demos next to stock ships). SpaceDock zipballs that ship `Ships/` folders would land the same way.

| Bucket | Crafts | Notes |
|--------|--------|-------|
| **Total** | **186** | GameData 96 + Ships 90 |
| Official-ish (Squad/DLC + stock Ships w/o third-party parts) | **144** | Contracts, MH missions/ships, Serenity ships, tutorials, remaining stock Ships |
| Mod demos in root Ships (third-party `part =` prefixes) | **42** | Inferred packs below |
| Non-Squad under GameData | **0** | Important for future local miners: scan **Ships** as well as GameData |

**Who contributed most (craft counts):**

| Source | # |
|--------|---|
| `[stock Ships]` (no third-party part prefixes detected) | 48 |
| Squad Contracts PrebuiltCraft | 48 |
| Making History Missions | 31 |
| MarkTwo **M2X** demos in Ships | 9 |
| WildBlue **Buffalo** demos in Ships | 8 |
| Making History Ships | 7 |
| MarkThree **M3X** demos in Ships | 7 |
| Breaking Ground Serenity Ships | 6 |
| **ChopShop** demos in Ships | 6 |
| CJ*/Uwing/Xwing demos | 4 |
| Squad Missions + Tutorials | 4 |
| **OPT** demos | 3 |
| B9 Procedural Wings demos | 2 |
| EL / MRS? / TP? demos | 1 each |

**Top parts by craft presence (all 186)** — prefer this over instances:

| Presence | Internal name | Notes |
|----------|---------------|-------|
| 65/186 | `strutConnector` | Ubiquitous structural spam |
| 42/186 | `longAntenna` | Communotron-class |
| 39/186 | `solarPanels5` | OX-STAT-ish |
| 37/186 | `batteryPack` | Z-100 |
| 36/186 | `noseCone` | |
| 34/186 | `launchClamp1`, `sensorThermometer` | |
| 33/186 | `RCSBlock` | |
| 32/186 | `SmallGearBay` | Plane-heavy Ships bias |
| 31/186 | `fuelLine`, `strutCube` | |
| 29/186 | `Decoupler.1`, `GooExperiment` | `Decoupler.*` = stock rename family |
| 27/186 | `linearRcs`, `radialDecoupler` | |
| 21/186 | `liquidEngine3.v2`, `ksp.r.largeBatteryPack` | Stock `.v2` / dotted names |

**Official-ish only (144)** — similar leaders (`strutConnector` 60/144, `batteryPack` / `solarPanels5` 36, `longAntenna` 34, `RCSBlock` 31, `Decoupler.1` 28, `liquidEngine3.v2` 21, `liquidEngine2` 19). Career essentials appear but do **not** dominate over struts/solar/RCS.

**Mod-demo Ships only (42)** — presence skewed to gear/aero + pack parts (`GearSmall` 11/42, `SmallGearBay` 10/42, `wbiBuffaloCommandPod` 5/42, `b2Chassis` 4/42). Stock antennas/gear still show up as supporting parts.

**Instance counts (all 186) — strut spam warning:**  
`strutConnector` 474, `solarPanels5` 275, `strutCube` 219, `linearRcs` 191, `RCSBlock` 158, … — **do not** use raw instances for suggestion priors.

**Stock vs modded mix:**

- **Official-ish crafts:** effectively **stock + DLC** (Making History / Breaking Ground). Many internal names use dots / `.v2` suffixes that a naïve Squad-cfg scrape may miss; treat as stock families for ranking maps.
- **Full 186:** **42/186 (~23%)** crafts are clear third-party demos; they inject pack-specific names (`wbi*`, `M2X.*`, `M3X.*`, `opt.*`, `ChopShop.*`, `B9.*`, `CJ*`) into global tallies.
- **Caveat (critical):** Mod-pack / stock Ships crafts ≠ average player Saves. This corpus is **showcase + contract + mission + pack demo** biased (SPH/plane heavy in Ships; struts/solar/RCS inflated). Still a **useful local signal** for: (a) which stock utilities co-occur, (b) how mod demos pollute unfiltered scans, (c) validating the `part =` parser on a large real folder.

**Implication for later mining:** Local opt-in scanners should walk `Ships/**/*.craft` **and** `GameData/**/*.craft`, optionally `saves/*/Ships/**/*.craft`, then **exclude** crafts whose parts are outside the user’s installed PartLoader set *or* flag crafts with high unknown-part ratios as “mod demos.”

### KerbalX signals worth noting

- Craft pages expose: **Pure Stock**, **SPH/VAB**, **class** (ship / spaceplane / lander / …), **part count**, **downloads / views / popularity**, root part name, full parts list.
- Part pages (example pattern `https://kerbalx.com/parts/<id>`) show **“N Craft use this part (ordered by download count)”** or “no craft use this part”.
- Mod filters and “Only Pure Stock” make it possible (later) to sample **stock-only** crafts and reduce mod noise.

**Constraint:** Treat KerbalX as a **showcase / download-popularity** population, not telemetry of all installs. Do **not** bulk-download thousands of crafts without permission / rate limits / ToS review.

---

## 2. Existing analyses of “most used parts”

**Finding: none located** as a published aggregate study (no Kaggle dataset, no blog with “top 50 stock parts from N crafts,” no wiki “most used” list).

What *does* exist instead:

| Kind | Examples | Value |
|------|----------|--------|
| **Beginner / tutorial meta** | Career starter builds repeatedly use Mk1 Command Pod, Mk16 parachute, early solids (Flea/Hammer), Swivel/Reliant, FL-T tanks, Z-100 battery, Communotron 16, Goo, Science Jr., stack/radial decouplers, heat shield | Strong **qualitative prior** for career-early parts |
| **Example build lists** | e.g. https://coeleveld.com/kerbal-space-program/ · https://pinter.org/archives/12715 | Same pattern of “essentials” |
| **Part name lists** | Pastebin / gists of stock internal names (e.g. `mk1pod`, `parachuteSingle`, `batteryPack`, `liquidEngine3`) | Mapping titles ↔ `part =` names |
| **CKAN download ranks** | SpaceDock/CKAN mod popularity | Proxy for **mods**, not stock part usage |
| **CKAN issue discussion** | https://github.com/KSP-CKAN/CKAN/issues/3242 | Notes desire for usage-from-saves stats; **not implemented** as public data |
| **Tools that *could* produce analyses** (see §3) | Craft-File-Reader, KSPPartRemover, TuneKSP | Local analysis only unless someone publishes results |

### Qualitative “average player” stock patterns (not measured frequencies)

From tutorials + VAB-oriented community practice (consensus, **not** a dataset):

| Role | Common stock internal names (examples) | Display titles (approx.) |
|------|----------------------------------------|---------------------------|
| Command | `mk1pod`, `Mark1-2Pod` / Mk1-3, `probeCore*` | Mk1 / Mk1-3 pods, Stayputnik & probe cores |
| Recovery | `parachuteSingle`, `parachuteRadial*`, `HeatShield*` | Mk16 / radial chutes, heat shields |
| Early engines | `liquidEngine2` (Swivel), Reliant family, `solidBooster*`, `liquidEngine3` / `liquidEngine3.v2` (Terrier) | Atmosphere / vacuum staples |
| Fuel | `fuelTank*`, `Size*` / Rockomax tanks | FL-T / Rockomax lines |
| Power | `batteryPack`, `ksp.r.largeBatteryPack`, `solarPanels*` | Z-100 / Z-400, OX-STAT / Gigantor-class |
| Staging | `stackDecoupler*`, `radialDecoupler*`, `Decoupler.*`, `sepMotor1` | Decouplers / separators / sepratrons |
| Control / RCS | `RCSBlock`, `linearRcs`, `sasModule` | RV-105, Place-Anywhere, SAS |
| Comms / science | `longAntenna`, `HighGainAntenna*`, `GooExperiment`, `science.module`, sensors | Communotron / HG, Goo, Science Jr. |
| Structure spam | `strutConnector`, `fuelLine` | Very high **instance** counts when present |

**Caution:** Showcase crafts inflate **struts, solar panels, RCS thrusters, landing gear** instance counts. Prefer **craft presence** (binary: part appears on craft) over raw instance counts for “do players use this part at all?”

---

## 3. Craft file parseability & open datasets

### Format (confirmed from local stock Ships)

`.craft` is KSP **ConfigNode**-style text. Each placed part appears as:

```text
PART
{
	part = Mark1Cockpit_4294820102
	...
}
```

- **Internal name** = substring before the final `_` + numeric unique id (`Mark1Cockpit`).
- Names may contain dots / version suffixes: e.g. `liquidEngine3.v2`, `delta.small`, `Decoupler.1`.
- Header fields useful for filtering: `type = SPH|VAB`, `ship =`, `version =`, `description =`.

**Parseability: high.** A line regex on `^\s*part\s*=\s*([^\s_]+)` (or split on last `_` carefully for names with underscores — stock mostly uses dots) is enough for frequency counts. Full parsers exist for edge cases.

### Libraries / tools

| Tool | URL | Notes |
|------|-----|--------|
| `@kspcommunity/craft-file-reader` | https://github.com/kspcommunity/Craft-File-Reader | JS: `processCraftFile` → craft + parts details |
| Mod Parts Lister | https://github.com/kspcommunity/Mod-Parts-Lister · data endpoint mentioned as `https://mod-parts.kspcommunity.com/data.json` (403 in this pass) | Part ↔ mod catalog, not usage |
| KSPPartRemover | https://github.com/ChrisDeadman/KSPPartRemover | CLI `list-parts` / `list-mods` on `.craft` / `.sfs` |
| KML | https://github.com/my-th-os/KML | GUI/CLI editor for craft/saves |
| TuneKSP | https://github.com/Conti/TuneKSP | Scans crafts under Saves to find **used vs unused** parts (local optimization, not public stats) |
| confignode (Rust) | https://docs.rs/confignode | Generic ConfigNode parser |
| KSP_DataDump | https://github.com/linuxgurugamer/KSP_DataDump | Dumps part **definitions** to CSV (balance), not craft usage |

### Open datasets

**None found** that publish aggregated part frequencies. No Kaggle “KSP craft corpus.” GitHub craft repos are tiny personal sets.

Local install used for a **sanity sample** (not community-wide):

- Path: `Kerbal Space Program/Ships/` (stock demo crafts)
- **90** crafts (**34 VAB**, **56 SPH**) — already **plane-heavy**

**Top by instance count (biased toward multi-copy parts):**  
`strutConnector`, `solarPanels5`, `linearRcs`, `SmallGearBay`, `R8winglet`, `RCSBlock`, `fuelLine`, `launchClamp1`, …

**Top by craft presence (better “is this used?” signal):**  
`SmallGearBay` 27/90, `strutConnector` 23/90, `longAntenna` 19/90, `GearSmall` 19/90, `noseCone` 19/90, `R8winglet` 18/90, `solarPanels5` 17/90, `radialDecoupler` 16/90, …

**VAB-only presence (more rocket-like):**  
`noseCone`, `launchClamp1`, `strutConnector`, `radialDecoupler`, `longAntenna`, `solarPanels5`, `batteryPack`, `liquidEngine2`, `fuelLine`, `liquidEngine3.v2`, …

**SPH-only presence:**  
landing gear, elevons, fuselage, `Mark2Cockpit`, airbrakes, …

**Selected “career essentials” presence in this Ships set** (illustrates showcase ≠ starter meta):  
`mk1pod` 7/90, `parachuteSingle` 8/90, `batteryPack` 11/90, `liquidEngine2` 12/90, `liquidEngine3` 10/90 — present but **not** dominant because stock Ships are largely aircraft/rovers.

---

## 4. Practical constraints & biases

| Bias | Effect on ranking data |
|------|-------------------------|
| **Stock vs modded** | KerbalX / CurseForge heavily modded; pure-stock filter required for Koobal stock suggestions. Mod parts must not drown stock unless user has those mods. |
| **Career vs sandbox** | Shared crafts skew **sandbox / replica / challenge** (high part count). Career unlock order is underrepresented. Tutorial meta better for early-career priors. |
| **Plane vs rocket (KerbalX & stock Ships)** | SPH / spaceplanes / shuttles overrepresented in many galleries; boosts wings, gear, intakes. VAB-only samples needed for rocket priors. |
| **Showcase vs average play** | Download-popular craft ≠ median personal save craft. Struts/solar/RCS instance spam. Prefer **presence**, optionally **download-weighted presence**. |
| **DLC** | Making History / Breaking Ground parts appear “stock-like” on KX; gate by installed DLC. |
| **Version suffixes** | `liquidEngine3` vs `liquidEngine3.v2` — treat as same family or map via PartLoader titles. |
| **Restock / rename mods** | Visual overhauls can change titles; internal names usually stable — rank by **internal name**. |
| **ToS / scraping** | KerbalX and wikis may block aggressive scrapers; SpaceDock ToS + per-mod licenses constrain mass zip mirroring (API OK for metadata with UA). Prefer small samples, permission, or user-local Saves. |
| **SpaceDock mod demos** | Crafts inside part packs inflate **that mod’s** internal names; useless for stock suggestion priors. |
| **Local Ships + GameData demos** | Mod packs often install examples into root `Ships/` (not GameData). Unfiltered local scans mix stock Ships, MH contracts, and pack demos — filter by unknown-part ratio. |

---

## 5. Recommendations for later (not implemented)

These are design notes for a future ranking pass — **do not implement in this research task**.

1. **Optional weight file**  
   - e.g. `GameData/.../KoobalPartPopularity.cfg` or JSON: `internalName → weight` (0–1 or log-frequency).  
   - Version the file; ship a **conservative stock prior**; allow disable in Settings.  
   - Keep ranking formula so **exact title / exact internal-name matches** still outrank popularity boosts (avoid drowning niche exact matches).

2. **Signals to encode (priority order)**  
   - **A. Hand-curated career essentials** (pods, chutes, batteries, common engines/tanks, antennas) — low effort, high UX.  
   - **B. Category priors** (boost Pods/Engines/Fuel when query is ambiguous short tokens).  
   - **C. Craft-presence frequencies** from a pure-stock KerbalX sample (VAB-weighted) and/or local Saves.  
   - **D. Instance-rate** only within-category (e.g. among batteries), never globally (struts win otherwise).

3. **How to apply in suggestions**  
   - `score = textMatch + λ * popularityPrior + μ * categoryPrior + historyBoost`  
   - Cap `λ` so a rare part with perfect title match stays above a popular part with weak match.  
   - Optional: SPH vs VAB context (editor building) selects plane vs rocket prior tables.

4. **Data pipeline (future research/engineering)**  
   - Filter KerbalX: Pure Stock + VAB (+ optional SPH table).  
   - Download a **bounded** sample (e.g. top N by downloads **and** random mid-tier), parse `part =`, compute presence counts.  
   - Map internal names → available parts via PartLoader; drop missing/DLC-locked.  
   - Emit weight file; unit-test that exact matches dominate.  
   - **Optional adjacent:** CurseForge Shareables (Planes and Ships) micro-sample for stock-tagged packs; Steam Workshop only via user-local subscribed crafts. **Skip SpaceDock** for stock priors.

5. **Do not**  
   - Hard-code huge scrape results into C#.  
   - Use mod-pack demo crafts (SpaceDock / CurseForge part packs) as stock truth.  
   - Equate CKAN / SpaceDock mod downloads with stock part popularity.

---

## 6. Risks if data is used naively

- **Strut/solar supremacy** from instance counts → useless suggestion lists.  
- **Plane bias** → wings/gear outrank Terrier/Swivel for rocket players.  
- **Sandbox replicas** → obscure aesthetic parts rank above career staples.  
- **Stale KSP versions** on KerbalX → renamed/retired parts.  
- **Privacy / ToS** if scraping user crafts at scale.  
- **Overfitting** to stock Ships (56/90 SPH) if that sample is treated as “players.”

---

## 7. Recommended next research steps (data is thin)

1. **KerbalX outreach / ToS check** — ask whether a research dump of pure-stock part presence counts (no craft bodies) is acceptable; or use a tiny manual sample (50–100 crafts) with delays.  
2. **Manual micro-sample** — download ~30 pure-stock VAB crafts (mix of download tiers); tabulate presence; compare to tutorial essentials list.  
3. **Local Saves + Ships pilot** (opt-in later feature idea) — parse `saves/*/Ships/**/*.craft` **and** root `Ships/**/*.craft` / `GameData/**/*.craft` on the user’s machine; filter or down-weight mod-demo crafts (high unknown-part ratio). Strongest “this install” signal; still not community telemetry.  
4. **Build a curated v0 prior** — ~80–150 stock internal names tagged high/medium from tutorials + VAB stock Ships; no scrape required.  
5. **Normalize name families** — document `*.v2` / `Decoupler.*` / `Rockomax*.BW` / title remaps for engines and tanks (seen heavily in local sample).  
6. **Category-conditioned priors** — separate tables for Pods, Engines, Aerodynamics, Electrical.  
7. **Revisit KerbalX statistics / Parts UI** with an interactive browser session (JS-rendered lists did not load via simple fetch in this pass).  
8. **SpaceDock closed for stock priors** — API useful for mod metadata only; no further craft mining planned unless a true stock-craft-only upload corpus appears (unlikely given site design).  
9. **CurseForge Shareables (optional)** — if KerbalX access is blocked, sample stock-tagged shareables via CurseForge API (key required) with the same presence metrics and caveats.

---

## 8. Source URL list (quick index)

- https://kerbalx.com/  
- https://kerbalx.com/parts  
- https://kerbalx.com/about  
- https://kerbalx.com/statistics  
- https://kerbalx.com/dev_blog  
- https://kerbalx.com/mod · https://kerbalx.com/CraftManager  
- https://github.com/Sujimichi/KXAPI · https://github.com/Sujimichi/KerbalX  
- https://forum.kerbalspaceprogram.com/forum/81-craft-repository/  
- https://www.curseforge.com/kerbal/search?class=shareables&categories=planes-and-ships  
- https://docs.curseforge.com/rest-api/  
- https://spacedock.info/  
- https://spacedock.info/privacy  
- https://github.com/KSP-SpaceDock/SpaceDock/blob/master/api.md  
- https://spacedock.ru/ (unrelated RU portal)  
- https://kspbuilds.com/ (KSP2)  
- Steam Workshop KSP path pattern: `steamapps/workshop/content/220200/`  
- https://github.com/kspcommunity/Craft-File-Reader  
- https://github.com/kspcommunity/Mod-Parts-Lister  
- https://github.com/ChrisDeadman/KSPPartRemover  
- https://github.com/my-th-os/KML  
- https://github.com/Conti/TuneKSP  
- https://github.com/linuxgurugamer/KSP_DataDump  
- https://github.com/edlund/ksp-craftfiles  
- https://github.com/KSP-CKAN/CKAN/issues/3242  
- https://kerbalspaceprogram.fandom.com/wiki/Parts  
- https://coeleveld.com/kerbal-space-program/  
- https://pinter.org/archives/12715  
- https://docs.rs/confignode  

---

## 9. Sample findings snapshot

### 9a. Local stock Ships only (earlier pass)

*Not community telemetry — illustrates parse method and bias.*

| Metric | Result |
|--------|--------|
| Crafts | 90 (VAB 34 / SPH 56) |
| Unique internal names seen | 496 |
| Instance-count leaders | strutConnector, solarPanels5, linearRcs, SmallGearBay, … |
| Presence leaders | SmallGearBay, strutConnector, longAntenna, GearSmall, noseCone, … |
| VAB presence leaders | noseCone, launchClamp1, strutConnector, radialDecoupler, longAntenna, solarPanels5, batteryPack, liquidEngine2, … |
| Parse rule | `part = <internalName>_<uid>` inside `PART { }` blocks |

### 9b. Full local GameData + Ships (follow-up, this install)

*See §1b for full write-up. Summary:*

| Metric | Result |
|--------|--------|
| Crafts | **186** (96 GameData + 90 Ships) |
| Third-party `.craft` under GameData | **0** (mod demos in root Ships instead) |
| Mod-demo crafts in Ships | **42** (~23%) — M2X, Buffalo, M3X, ChopShop, OPT, … |
| Presence leaders (all) | strutConnector, longAntenna, solarPanels5, batteryPack, noseCone, … |
| Instance leaders | strutConnector 474, solarPanels5 275, strutCube 219, linearRcs 191 (spam) |
| Caveat | Showcase / contract / mission / pack-demo ≠ average player |

### 9c. SpaceDock bundled-craft micro-sample (follow-up)

*Tiny sample / not representative — 6 crafts from 2 permissive-license packs (ids 3088, 1173). Downloads removed after parse.*

| Metric | Result |
|--------|--------|
| Crafts | 6 |
| Unique internal names | 42 |
| Presence / instance leaders | Almost entirely **mod** parts (`CJThunderbird*`, `FlyingWing*`, `CJMole*`) |
| Stock-ish names seen | Sparse: `liquidEngine1-2`, `commDish`, `dockingPort2`, `wingShuttleRudder`, `Size3EngineCluster` |
| Implication | Confirms SpaceDock crafts ≠ stock usage corpus |

---

*End of research memo. No ranking implementation performed.*
