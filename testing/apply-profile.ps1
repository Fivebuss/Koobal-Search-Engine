# Apply a Koobal Search Engine test profile to ModTest, then report loaded mods,
# run conflict checks, and print a per-profile test briefing.
param(
    [Parameter(Mandatory = $true)]
    [string]$Profile,

    [string]$MainKsp = "F:\SteamLibrary\steamapps\common\Kerbal Space Program",
    [string]$ModTestKsp = "F:\SteamLibrary\steamapps\common\Kerbal Space Program - ModTest",
    [string]$ProfilesRoot = "$PSScriptRoot\profiles",
    [string]$CkanInstance = "KSP-ModTest",
    [switch]$DryRun,
    [switch]$SkipCkan,
    [switch]$KeepExistingMods,
    [switch]$NoRecommends,
    [switch]$SkipBriefing,
    [string]$MainRegistry = ""
)

$ErrorActionPreference = "Stop"

# GameData folder names that differ from CKAN identifiers (global defaults).
$CkanFolderOverrides = @{
    '001_ToolbarControl'      = 'ToolbarController'
    '000_AT_Utils'            = 'AT-Utils'
    '000_ClickThroughBlocker' = 'ClickThroughBlocker'
    '000_USITools'            = 'USITools'
    'B9_Aerospace'            = 'B9-props'
    'B9_Aerospace_HX'         = 'B9-props'
    'FShangarExtender'        = 'HangerExtenderExtended'
}

# --- helpers ---
function Write-Step([string]$msg) { Write-Host "`n==> $msg" -ForegroundColor Cyan }
function Write-Status([string]$level, [string]$msg) {
    $color = switch ($level) { 'GREEN' { 'Green' } 'YELLOW' { 'Yellow' } 'RED' { 'Red' } default { 'White' } }
    Write-Host "[$level] $msg" -ForegroundColor $color
}

# Base junction folders applied to every ModTest profile unless profile.json sets skipBaseCck = true.
$BaseJunctionFolders = @('CommunityCategoryKit')

$ExclusiveOrganizers = @{
    'CommunityCategoryKit' = @{ Folder = 'CommunityCategoryKit'; Marker = 'CCK.dll' }
    'VABOrganizer'         = @{ Folder = $null; Marker = 'VABOrganizer.dll' }
    'PartCatalog'          = @{ Folder = $null; Marker = 'PartCatalog.dll' }
    'CategoryParts'        = @{ Folder = $null; Marker = 'CategoryParts.dll' }
    'EditorExtensions'     = @{ Folder = $null; Marker = 'EditorExtensions.dll' }
}

$EditorSurfaceMods = @{
    'KSPCommunityFixes'        = @{ Folder = 'KSPCommunityFixes'; Note = 'FasterEditorPartList / editor patches' }
    'CommunityCategoryKit'     = @{ Folder = 'CommunityCategoryKit'; Note = 'Custom VAB category tabs' }
    'VABOrganizer'             = @{ Folder = $null; Marker = 'VABOrganizer.dll'; Note = 'Subcategory drawer' }
    'Hangar'                   = @{ Folder = 'Hangar'; Note = 'Editor storage overlay' }
    'HangerExtenderExtended'   = @{ Folder = 'FShangarExtender'; Note = 'Panel layout resize' }
}

function Get-VersionInfo([string]$FolderPath) {
    $vf = Get-ChildItem -Path $FolderPath -Filter '*.version' -Recurse -File -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $vf) { return @{ Name = $null; Version = $null } }
    $text = Get-Content $vf.FullName -Raw -ErrorAction SilentlyContinue
    $name = if ($text -match '(?m)^\s*name\s*=\s*(.+)$') { $Matches[1].Trim() } else { $null }
    $ver  = if ($text -match '(?m)^\s*version\s*=\s*(.+)$') { $Matches[1].Trim() } else { $null }
    return @{ Name = $name; Version = $ver; File = $vf.FullName }
}

function Get-LoadedModsReport([string]$GameDataRoot, [string]$MainGameData) {
    $protected = @('Squad')
    $rows = @()
    Get-ChildItem -Path $GameDataRoot -Directory | ForEach-Object {
        if ($protected -contains $_.Name) { return }
        $isJunction = ($_.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0
        $mainHas = Test-Path (Join-Path $MainGameData $_.Name)
        $source = if ($isJunction) { 'junction' } elseif ($mainHas) { 'copy/ckan' } else { 'ckan/local' }
        $vi = Get-VersionInfo $_.FullName
        $display = if ($vi.Name) { $vi.Name } else { $_.Name }
        $rows += [PSCustomObject]@{
            Folder = $_.Name
            DisplayName = $display
            Version = $vi.Version
            Source = $source
        }
    }
    return $rows | Sort-Object Folder
}

function Find-OrganizersPresent([string]$GameDataRoot) {
    $found = @()
    foreach ($kv in $ExclusiveOrganizers.GetEnumerator()) {
        $id = $kv.Key
        $meta = $kv.Value
        $hit = $false
        if ($meta.Folder) {
            $dll = Join-Path $GameDataRoot "$($meta.Folder)\$($meta.Marker)"
            if (Test-Path $dll) { $hit = $true }
        }
        if (-not $hit -and $meta.Marker) {
            $dll = Get-ChildItem -Path $GameDataRoot -Recurse -Filter $meta.Marker -ErrorAction SilentlyContinue | Select-Object -First 1
            if ($dll) { $hit = $true }
        }
        if ($hit) { $found += $id }
    }
    return $found
}

function Get-ProfileCkanOverrides($ProfileDef) {
    $over = @{}
    if ($ProfileDef.ckanIdByFolder) {
        foreach ($p in $ProfileDef.ckanIdByFolder.PSObject.Properties) {
            $over[$p.Name] = [string]$p.Value
        }
    }
    return $over
}

function Get-CkanDependsFromSource($dependsBlock) {
    $hard = [System.Collections.Generic.List[string]]::new()
    $anyOf = [System.Collections.Generic.List[object]]::new()
    if (-not $dependsBlock) {
        return @{ Hard = @(); AnyOf = @() }
    }
    foreach ($d in @($dependsBlock)) {
        if ($d.any_of) {
            $options = @()
            foreach ($opt in @($d.any_of)) {
                if ($opt.name) { $options += [string]$opt.name }
            }
            if ($options.Count -gt 0) { [void]$anyOf.Add(@($options)) }
        } elseif ($d.name) {
            $n = [string]$d.name
            if ($n -and -not $hard.Contains($n)) { [void]$hard.Add($n) }
        }
    }
    return @{ Hard = @($hard); AnyOf = @($anyOf) }
}

function Read-CkanRegistryMap([string]$RegistryPath) {
    if (-not (Test-Path $RegistryPath)) {
        return @{
            FolderToIds = @{}
            IdToFolders = @{}
            IdToDepends = @{}
            IdToAnyOf = @{}
        }
    }
    $reg = Get-Content $RegistryPath -Raw | ConvertFrom-Json
    $folderToIds = @{}
    $idToFolders = @{}
    $idToDepends = @{}
    $idToAnyOf = @{}

    foreach ($prop in $reg.installed_modules.PSObject.Properties) {
        $id = $prop.Name
        $mod = $prop.Value
        $src = $mod.source_module
        $folders = [System.Collections.Generic.List[string]]::new()

        if ($mod.installed_files) {
            foreach ($fp in $mod.installed_files.PSObject.Properties.Name) {
                if ($fp -match '^GameData/([^/]+)') {
                    $f = $Matches[1]
                    if ($folders -notcontains $f) { [void]$folders.Add($f) }
                }
            }
        }
        if ($folders.Count -eq 0 -and $src.install) {
            foreach ($inst in @($src.install)) {
                if ($inst.file -match '^GameData/([^/]+)') {
                    $f = $Matches[1]
                    if ($folders -notcontains $f) { [void]$folders.Add($f) }
                } elseif ($inst.find) {
                    $f = [string]$inst.find
                    if ($folders -notcontains $f) { [void]$folders.Add($f) }
                }
            }
        }

        $idToFolders[$id] = @($folders)
        foreach ($f in $folders) {
            if (-not $folderToIds.ContainsKey($f)) { $folderToIds[$f] = @() }
            if ($folderToIds[$f] -notcontains $id) { $folderToIds[$f] += $id }
        }

        $parsed = Get-CkanDependsFromSource $src.depends
        $idToDepends[$id] = @($parsed.Hard | Select-Object -Unique)
        $idToAnyOf[$id] = @($parsed.AnyOf)
    }

    return @{
        FolderToIds = $folderToIds
        IdToFolders = $idToFolders
        IdToDepends = $idToDepends
        IdToAnyOf = $idToAnyOf
    }
}

function Get-CkanIdForFolder {
    param(
        [string]$Folder,
        $Map,
        [hashtable]$ProfileOverrides
    )
    if ($ProfileOverrides -and $ProfileOverrides.ContainsKey($Folder)) { return $ProfileOverrides[$Folder] }
    if ($CkanFolderOverrides.ContainsKey($Folder)) { return $CkanFolderOverrides[$Folder] }
    if ($Map.FolderToIds.ContainsKey($Folder) -and $Map.FolderToIds[$Folder].Count -gt 0) {
        return $Map.FolderToIds[$Folder][0]
    }
    if ($Map.IdToFolders.ContainsKey($Folder)) { return $Folder }
    return $null
}

function Get-CkanShowDepends {
    param([string]$CkanExe, [string]$CkanId)
    if (-not (Test-Path $CkanExe)) { return @() }
    $prevEap = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $out = & $CkanExe show $CkanId --without-description --without-module-info --without-resources --without-files --headless 2>&1 | Out-String
    $ErrorActionPreference = $prevEap
    $deps = @()
    if ($out -match '(?ms)^Depends:\r?\n((?:  - .+\r?\n)+)') {
        foreach ($line in ($Matches[1] -split '\r?\n')) {
            if ($line -match '^\s*-\s+(\S+)') { $deps += $Matches[1] }
        }
    }
    return @($deps | Select-Object -Unique)
}

function Get-CkanShowFolders {
    param([string]$CkanExe, [string]$CkanId)
    if (-not (Test-Path $CkanExe)) { return @() }
    $prevEap = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $out = & $CkanExe show $CkanId --without-description --without-module-info --without-relationships --without-resources --headless 2>&1 | Out-String
    $ErrorActionPreference = $prevEap
    $folders = @()
    foreach ($line in ($out -split '\r?\n')) {
        if ($line -match '^\s*-\s+GameData/([^/\s]+)') {
            $folders += $Matches[1]
        }
    }
    return @($folders | Select-Object -Unique)
}

function Get-CkanModFolders {
    param($Map, [string]$CkanId, [string]$CkanExe)
    if ($Map.IdToFolders.ContainsKey($CkanId) -and $Map.IdToFolders[$CkanId].Count -gt 0) {
        return $Map.IdToFolders[$CkanId]
    }
    $fromShow = Get-CkanShowFolders $CkanExe $CkanId
    if ($fromShow.Count -gt 0) { return $fromShow }
    foreach ($kv in $CkanFolderOverrides.GetEnumerator()) {
        if ($kv.Value -eq $CkanId) { return @($kv.Key) }
    }
    return @($CkanId)
}

function Resolve-CkanDependencyTree {
    param(
        [string[]]$SeedIds,
        $Map,
        [string]$CkanExe
    )
    $queue = [System.Collections.Generic.Queue[string]]::new()
    $seen = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($id in @($SeedIds | Where-Object { $_ })) {
        if (-not $seen.Contains($id)) { [void]$seen.Add($id); $queue.Enqueue($id) }
    }
    while ($queue.Count -gt 0) {
        $id = $queue.Dequeue()
        $deps = @()
        if ($Map.IdToDepends.ContainsKey($id) -and $Map.IdToDepends[$id].Count -gt 0) {
            $deps = $Map.IdToDepends[$id]
        } else {
            $deps = Get-CkanShowDepends $CkanExe $id
        }
        foreach ($d in @($deps)) {
            if (-not $seen.Contains($d)) { [void]$seen.Add($d); $queue.Enqueue($d) }
        }
    }
    return @($seen)
}

function Test-CkanModFoldersPresent {
    param([string]$TestGd, [string[]]$Folders)
    if (-not $Folders -or $Folders.Count -eq 0) { return $false }
    foreach ($f in $Folders) {
        if (-not (Test-Path (Join-Path $TestGd $f))) { return $false }
    }
    return $true
}

function Add-GameDataJunction {
    param(
        [string]$Folder,
        [string]$MainGd,
        [string]$TestGd,
        [switch]$DryRun
    )
    $src = Join-Path $MainGd $Folder
    $dst = Join-Path $TestGd $Folder
    if (-not (Test-Path $src)) { return $false }
    if (Test-Path $dst) { Write-Host "  exists: $Folder"; return $true }
    if ((Get-Item -LiteralPath $src).PSIsContainer -eq $false) {
        if ($DryRun) { Write-Host "  [dry-run] copy $Folder"; return $true }
        Copy-Item -LiteralPath $src -Destination $dst -Force
        Write-Host "  copy: $Folder"
        return $true
    }
    if ($DryRun) { Write-Host "  [dry-run] junction $Folder"; return $true }
    New-Item -ItemType Junction -Path $dst -Target $src | Out-Null
    Write-Host "  junction: $Folder"
    return $true
}

function Invoke-CkanDependencyPreflight {
    param(
        [string[]]$ResolvedCkanIds,
        $Map,
        [string]$TestGd,
        [string]$CkanExe
    )
    $issues = @()
    foreach ($id in @($ResolvedCkanIds)) {
        $deps = @()
        if ($Map.IdToDepends.ContainsKey($id) -and $Map.IdToDepends[$id].Count -gt 0) {
            $deps = $Map.IdToDepends[$id]
        } else {
            $deps = Get-CkanShowDepends $CkanExe $id
        }
        foreach ($depId in @($deps)) {
            if (-not $depId -or -not $depId.Trim()) { continue }
            $depFolders = Get-CkanModFolders $Map $depId $CkanExe
            if (-not (Test-CkanModFoldersPresent $TestGd $depFolders)) {
                $issues += "Missing CKAN dependency '$depId' required by '$id' (expected GameData\$($depFolders -join ', '))"
            }
        }
        if ($Map.IdToAnyOf -and $Map.IdToAnyOf.ContainsKey($id)) {
            foreach ($group in @($Map.IdToAnyOf[$id])) {
                $satisfied = $false
                foreach ($opt in @($group)) {
                    if (-not $opt) { continue }
                    $optFolders = Get-CkanModFolders $Map $opt $CkanExe
                    if (Test-CkanModFoldersPresent $TestGd $optFolders) {
                        $satisfied = $true
                        break
                    }
                }
                if (-not $satisfied) {
                    $issues += "Missing CKAN any_of dependency for '$id' (need one of: $($group -join ', '))"
                }
            }
        }
    }
    return $issues
}

function Find-EditorSurfaceMods([string]$GameDataRoot) {
    $found = @()
    foreach ($kv in $EditorSurfaceMods.GetEnumerator()) {
        $id = $kv.Key
        $meta = $kv.Value
        if ($meta.Folder -and (Test-Path (Join-Path $GameDataRoot $meta.Folder))) {
            $found += [PSCustomObject]@{ Id = $id; Note = $meta.Note }
            continue
        }
        if ($meta.Marker) {
            $dll = Get-ChildItem -Path $GameDataRoot -Recurse -Filter $meta.Marker -ErrorAction SilentlyContinue | Select-Object -First 1
            if ($dll) { $found += [PSCustomObject]@{ Id = $id; Note = $meta.Note } }
        }
    }
    return $found
}

function Invoke-ConflictCheck {
    param(
        $ProfileDef,
        [string]$TestGd,
        [string]$CkanExe,
        [string]$Instance,
        [string[]]$ResolvedCkanIds = @(),
        $CkanMap = $null
    )

    $issues = @()   # RED
    $warnings = @() # YELLOW

    # Harmony
    $harmonyDll = Join-Path $TestGd '000_Harmony\0Harmony.dll'
    if (-not (Test-Path $harmonyDll)) {
        $issues += 'Missing 000_Harmony/0Harmony.dll (Harmony2 required)'
    }

    # Koobal Search Engine
    $kseDll = Join-Path $TestGd 'KoobalSearchEngine\Plugins\KoobalSearchEngine.dll'
    if (-not (Test-Path $kseDll)) {
        $issues += 'Missing KoobalSearchEngine/Plugins/KoobalSearchEngine.dll'
    }

    # Community Category Kit (permanent ModTest base layer)
    $cckDll = Join-Path $TestGd 'CommunityCategoryKit\CCK.dll'
    if (-not (Test-Path $cckDll) -and -not $ProfileDef.skipBaseCck) {
        $warnings += 'Missing CommunityCategoryKit/CCK.dll (expected base layer on all ModTest profiles)'
    }

    # Duplicate top-level folders (should not happen)
    $folders = Get-ChildItem -Path $TestGd -Directory | Group-Object Name | Where-Object { $_.Count -gt 1 }
    if ($folders) {
        $issues += "Duplicate GameData folders: $($folders.Name -join ', ')"
    }

    # Multiple exclusive organizers
    $organizers = Find-OrganizersPresent $TestGd
    if ($organizers.Count -gt 1) {
        $issues += "Multiple VAB inventory organizers detected: $($organizers -join ' + ') - mutually exclusive"
    }
    if ($ProfileDef.exclusiveGroup -eq 'vab-ui-organizer' -and $organizers.Count -eq 0) {
        $warnings += 'Profile expects a VAB-UI organizer but none detected in GameData'
    }

    # Editor surface mods (informational warnings)
    $surface = Find-EditorSurfaceMods $TestGd
    if ($surface.Count -gt 2) {
        $warnings += "Multiple editor-surface mods present: $($surface.Id -join ', ') - elevated conflict risk"
    }

    # CKAN list consistency (skip gracefully if GUI holds registry lock)
    if ((Test-Path $CkanExe) -and -not $DryRun) {
        try {
            $prevEap = $ErrorActionPreference
            $ErrorActionPreference = 'Continue'
            $ckanList = & $CkanExe list --instance $Instance --headless 2>&1 | Out-String
            $ckanExit = $LASTEXITCODE
            $ErrorActionPreference = $prevEap
            if ($ckanExit -ne 0 -or $ckanList -match 'RegistryInUseKraken|registry\.locked') {
                $warnings += 'CKAN list skipped (registry locked - close CKAN GUI or retry headless)'
            } elseif ($ckanList -match 'not compatible|conflicts') {
                $warnings += 'CKAN list reports compatibility/conflict warnings - run: ckan list --instance KSP-ModTest'
            }
        } catch {
            $warnings += 'CKAN list skipped (CKAN CLI error)'
        }
    }
    # CKAN dependency preflight (hard deps for profile mods)
    if ($ResolvedCkanIds.Count -gt 0 -and $CkanMap) {
        $depIssues = Invoke-CkanDependencyPreflight -ResolvedCkanIds $ResolvedCkanIds -Map $CkanMap -TestGd $TestGd -CkanExe $CkanExe
        if ($depIssues.Count -gt 0) {
            $issues += $depIssues
        }
    }

    # Broken junctions
    Get-ChildItem -Path $TestGd -Directory -ErrorAction SilentlyContinue | ForEach-Object {
        if (($_.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            try { $null = Get-ChildItem $_.FullName -ErrorAction Stop }
            catch { $issues += "Broken junction: $($_.Name)" }
        }
    }

    $level = if ($issues.Count -gt 0) { 'RED' } elseif ($warnings.Count -gt 0) { 'YELLOW' } else { 'GREEN' }
    return @{ Level = $level; Issues = $issues; Warnings = $warnings; Organizers = $organizers; Surface = $surface }
}
function Get-DefaultBriefing([string]$ProfileId) {
    $common = @{
        loadGate = @('Reach VAB', 'Log: Editor scene detected', 'Log: Hooked native editor search field', 'Log: Indexed N parts', 'No HarmonyException')
        parseLog = '.\parse-test-log.ps1 -LogPath "..\Kerbal Space Program - ModTest\KSP.log"'
    }
    switch -Regex ($ProfileId) {
        'profile-00-baseline' {
            return $common + @{
                minutes = 5
                queries = @('engine', 'intake', 'harmony', 'a')
                vabUi = $false
                success = 'All 4 queries show dropdown behavior; catalog returns after Escape'
            }
        }
        'profile-vab-ui-communitycategorykit' {
            return $common + @{
                minutes = 10
                queries = @('engine', 'intake', 'a', 'nertea', 'near future')
                vabUi = $true
                success = 'CCK tabs still switchable; categorizer filters do not blank parts list'
            }
        }
        'profile-vaborganizer' {
            return $common + @{
                minutes = 10
                queries = @('engine', 'intake', 'a')
                vabUi = $true
                success = 'VABO subcategory tabs visible; filter apply keeps parts visible'
            }
        }
        'profile-vab-ui-kspcommunityfixes' {
            return $common + @{
                minutes = 8
                queries = @('engine', 'intake', 'harmony', 'a')
                vabUi = $true
                success = 'No search lag regression; FasterEditorPartList does not break hook'
            }
        }
        'profile-vab-ui-hangar|profile-vab-ui-fshangarextender' {
            return $common + @{
                minutes = 8
                queries = @('engine', 'intake', 'a')
                vabUi = $true
                success = 'Dropdown not clipped; catalog collapse still works with layout mod'
            }
        }
        'profile-01-main-top10' {
            return $common + @{
                minutes = 10
                queries = @('engine', 'intake', 'lis', 'harmony', 'a', 'nertea', 'near future')
                vabUi = $false
                success = 'Author/mod rows appear; part click filters correctly with NF/B9 parts loaded'
            }
        }
        'profile-02-main-top25|profile-03-main-sample50' {
            return $common + @{
                minutes = 15
                queries = @('engine', 'intake', 'harmony', 'a', 'nertea', 'rockomax')
                vabUi = $false
                success = 'Index builds once; no intake no-rows warning; parse-test-log exit 0'
            }
        }
        'profile-main-full' {
            return $common + @{
                minutes = 20
                queries = @('engine', 'intake', 'harmony', 'a', 'nertea', 'near future', 'rockomax', 'bluedog')
                vabUi = $true
                loadGate = @(
                    'Reach VAB',
                    'Log: Editor scene detected',
                    'Log: Search ready (basic)',
                    'Log: Search ready (full)',
                    'Log: Hooked native editor search field',
                    'No HarmonyException'
                )
                success = 'Basic search usable within ~2s; full author/mod/filter suggestions within ~15s; parse-test-log exit 0'
            }
        }
        default {
            return $common + @{
                minutes = 10
                queries = @('engine', 'intake', 'harmony', 'a')
                vabUi = $false
                success = 'TEST_PROTOCOL.md full checklist passes'
            }
        }
    }
}

function Write-TestBriefing($ProfileDef, $ConflictResult) {
    $brief = Get-DefaultBriefing $ProfileDef.id
    if ($ProfileDef.testBriefing) {
        $tb = $ProfileDef.testBriefing
        if ($tb.minutes) { $brief.minutes = $tb.minutes }
        if ($tb.queries) { $brief.queries = @($tb.queries) }
        if ($null -ne $tb.vabUi) { $brief.vabUi = [bool]$tb.vabUi }
        if ($tb.success) { $brief.success = $tb.success }
    }
    if ($ConflictResult.Organizers.Count -gt 0) { $brief.vabUi = $true }

    Write-Step "TEST BRIEFING - $($ProfileDef.name)"
    Write-Host "Estimated time: ~$($brief.minutes) min"
    Write-Host ""
    Write-Host "VAB load gate:"
    $brief.loadGate | ForEach-Object { Write-Host "  [ ] $_" }
    Write-Host ""
    Write-Host "Queries to run:"
    $brief.queries | ForEach-Object { Write-Host "  - $_" }
    if ($brief.vabUi) {
        Write-Host ""
        Write-Host "VAB-UI organizer checks (TEST_PROTOCOL.md V1-V8):"
        Write-Host "  [ ] Dropdown visible / scroll / collapse"
        Write-Host "  [ ] Categorizer click -> parts list NOT blank"
        Write-Host "  [ ] Organizer sidebar state after filter"
    }
    Write-Host ""
    Write-Host "Success looks like: $($brief.success)"
    Write-Host ""
    Write-Host "After session: $($brief.parseLog)"
}

# --- apply profile ---
$profileDir = Join-Path $ProfilesRoot $Profile
$profileJson = Join-Path $profileDir "profile.json"
if (-not (Test-Path $profileJson)) { throw "Profile not found: $profileJson" }

$def = Get-Content $profileJson -Raw | ConvertFrom-Json
$mainGd = Join-Path $MainKsp "GameData"
$testGd = Join-Path $ModTestKsp "GameData"
$ckan = Join-Path $ModTestKsp "Tools\ckan.exe"
$protected = @('Squad', '000_Harmony', 'KoobalSearchEngine')
if (-not $MainRegistry) { $MainRegistry = Join-Path $MainKsp "CKAN\registry.json" }

$profileOverrides = Get-ProfileCkanOverrides $def
$ckanMap = Read-CkanRegistryMap $MainRegistry
$resolveCkanDeps = -not $def.skipCkanResolve

$contentJunctionFolders = @()
if ($def.junctionAllFromMain) {
    Get-ChildItem -Path $mainGd -Directory -ErrorAction SilentlyContinue | ForEach-Object {
        if ($protected -contains $_.Name) { return }
        $exclude = @()
        if ($def.junctionExcludeFromMain) { $exclude = @($def.junctionExcludeFromMain) }
        if ($exclude -contains $_.Name) { return }
        $contentJunctionFolders += $_.Name
    }
}
if ($def.junctionFromMain) { $contentJunctionFolders += $def.junctionFromMain }
if ($def.junctionFolders) { $contentJunctionFolders += $def.junctionFolders }
if ($def.sharedDeps) { $contentJunctionFolders += $def.sharedDeps }
$contentJunctionFolders = @($contentJunctionFolders | Select-Object -Unique)

$explicitCkanIds = @()
if ($def.ckanPrimary) { $explicitCkanIds += $def.ckanPrimary }
if ($def.ckanInstall) { $explicitCkanIds += $def.ckanInstall }
$explicitCkanIds = @($explicitCkanIds | Select-Object -Unique)

$seedCkanIds = @($explicitCkanIds)
if ($resolveCkanDeps) {
    foreach ($folder in $contentJunctionFolders) {
        $mapped = Get-CkanIdForFolder -Folder $folder -Map $ckanMap -ProfileOverrides $profileOverrides
        if ($mapped) { $seedCkanIds += $mapped }
    }
}
$seedCkanIds = @($seedCkanIds | Select-Object -Unique)

$resolvedCkanIds = @()
if ($resolveCkanDeps -and $seedCkanIds.Count -gt 0) {
    $resolvedCkanIds = @(Resolve-CkanDependencyTree -SeedIds $seedCkanIds -Map $ckanMap -CkanExe $ckan | Where-Object { $_ -and $_.Trim() })
}

$depJunctionFolders = @()
if ($resolveCkanDeps) {
    foreach ($ckanId in $resolvedCkanIds) {
        $depJunctionFolders += Get-CkanModFolders $ckanMap $ckanId $ckan
    }
}

$junctionFolders = @()
if (-not $def.skipBaseCck) { $junctionFolders += $BaseJunctionFolders }
$junctionFolders += $contentJunctionFolders
$junctionFolders += $depJunctionFolders
$junctionFolders = @($junctionFolders | Where-Object { $_ -and $_.Trim() } | Select-Object -Unique)

Write-Step "APPLY PROFILE: $($def.name) ($($def.id))"
if ($def.exclusiveGroup) {
    Write-Host "Exclusive group: $($def.exclusiveGroup)" -ForegroundColor Yellow
}
if ($resolvedCkanIds.Count -gt 0) {
    Write-Host "CKAN mod tree ($($resolvedCkanIds.Count) ids): $($resolvedCkanIds -join ', ')" -ForegroundColor DarkGray
}

if (-not $KeepExistingMods) {
    Write-Step "Clean third-party GameData"
    Get-ChildItem -Path $testGd -Directory | ForEach-Object {
        if ($protected -contains $_.Name) { return }
        $target = $_.FullName
        if ($DryRun) { Write-Host "  [dry-run] remove $target"; return }
        if ($_.Attributes -band [IO.FileAttributes]::ReparsePoint) { cmd /c rmdir `"$target`" 2>$null }
        if (Test-Path $target) { Remove-Item -LiteralPath $target -Recurse -Force }
    }
}

Write-Step "Junction from main install"
foreach ($folder in $junctionFolders) {
    if (-not (Add-GameDataJunction -Folder $folder -MainGd $mainGd -TestGd $testGd -DryRun:$DryRun)) {
        Write-Warning "Main missing GameData\$folder (will try CKAN if needed)"
    }
}

if (-not $SkipCkan -and $resolvedCkanIds.Count -gt 0) {
    if (-not (Test-Path $ckan)) { throw "CKAN CLI not found: $ckan" }
    # CKAN refuses to overwrite junctioned folders. Skip mods whose install folders are all present.
    $ckanToInstall = @()
    foreach ($mod in $resolvedCkanIds) {
        if (-not $mod -or -not $mod.Trim()) { continue }
        $folders = Get-CkanModFolders $ckanMap $mod $ckan
        if (Test-CkanModFoldersPresent $testGd $folders) {
            Write-Host "  skip CKAN install for $mod (GameData\$($folders -join ', ') present)"
            continue
        }
        $ckanToInstall += $mod
    }
    if ($ckanToInstall.Count -gt 0) {
        $ckanArgs = @('install', '--instance', $CkanInstance, '--headless')
        if ($NoRecommends) { $ckanArgs += '--no-recommends' }
        $ckanArgs += $ckanToInstall
        Write-Step "CKAN install (with dependency resolution): $($ckanToInstall -join ', ')"
        if ($DryRun) { Write-Host "  [dry-run] ckan $($ckanArgs -join ' ')" }
        else {
            & $ckan @ckanArgs
            if ($LASTEXITCODE -ne 0) { throw "CKAN install failed (exit $LASTEXITCODE)" }
        }
    } else {
        Write-Step "CKAN install skipped (all resolved mods satisfied in GameData)"
    }
} elseif (-not $SkipCkan -and $explicitCkanIds.Count -gt 0 -and -not $resolveCkanDeps) {
    Write-Warning "skipCkanResolve is set; only explicit ckanPrimary/ckanInstall are installed"
}

# --- Step 1: Report what's loaded ---
Write-Step "LOADED MODS REPORT"
$mods = if ($DryRun) { @() } else { Get-LoadedModsReport $testGd $mainGd }

$kseVer = Get-VersionInfo (Join-Path $testGd 'KoobalSearchEngine')
$harmVer = Get-VersionInfo (Join-Path $testGd '000_Harmony')

Write-Host "Profile:     $($def.id)"
Write-Host "KSE version: $($kseVer.Version) ($($kseVer.Name))"
Write-Host "Harmony:     $($harmVer.Version) ($($harmVer.Name))"
Write-Host ""
Write-Host "| Folder | Display name | Version | Source |"
Write-Host "|--------|--------------|---------|--------|"
foreach ($m in $mods) {
    $ver = if ($m.Version) { $m.Version } else { '-' }
    Write-Host "| $($m.Folder) | $($m.DisplayName) | $ver | $($m.Source) |"
}
if ($mods.Count -eq 0 -and -not $DryRun) {
    Write-Host '| (baseline only - no third-party mods beyond Harmony/KSE) |'
}

# --- Step 2: Conflict check ---
Write-Step "PRE-RUN CONFLICT CHECK"
$conflict = if ($DryRun) {
    @{ Level = 'YELLOW'; Issues = @(); Warnings = @('Dry-run - conflict check skipped'); Organizers = @(); Surface = @() }
} else {
    Invoke-ConflictCheck -ProfileDef $def -TestGd $testGd -CkanExe $ckan -Instance $CkanInstance `
        -ResolvedCkanIds $resolvedCkanIds -CkanMap $ckanMap
}

Write-Status $conflict.Level "Overall status: $($conflict.Level)"
if ($conflict.Issues.Count -gt 0) {
    Write-Host "Issues:" -ForegroundColor Red
    $conflict.Issues | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
}
if ($conflict.Warnings.Count -gt 0) {
    Write-Host "Warnings:" -ForegroundColor Yellow
    $conflict.Warnings | ForEach-Object { Write-Host "  - $_" -ForegroundColor Yellow }
}
if ($conflict.Organizers.Count -gt 0) {
    Write-Host "Organizers present: $($conflict.Organizers -join ', ')"
}
if ($conflict.Surface.Count -gt 0) {
    Write-Host "Editor-surface mods:"
    $conflict.Surface | ForEach-Object { Write-Host "  - $($_.Id): $($_.Note)" }
}

if ($conflict.Level -eq 'RED') {
    Write-Host "`nDo NOT launch KSP until RED issues are resolved." -ForegroundColor Red
}

# --- Step 3: Test briefing ---
if (-not $SkipBriefing) {
    Write-TestBriefing $def $conflict
}

if ($def.notes) { Write-Host "`nNote: $($def.notes)" -ForegroundColor DarkYellow }
