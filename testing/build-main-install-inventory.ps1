# Builds MAIN_INSTALL_MODS.md from main KSP GameData + CKAN registry + download counts.
param(
    [string]$MainKsp = "F:\SteamLibrary\steamapps\common\Kerbal Space Program",
    [string]$Output = "$PSScriptRoot\MAIN_INSTALL_MODS.md",
    [string]$DownloadCountsUrl = "https://raw.githubusercontent.com/KSP-CKAN/CKAN-meta/master/download_counts.json"
)

$ErrorActionPreference = "Stop"
$mainGd = Join-Path $MainKsp "GameData"
$registryPath = Join-Path $MainKsp "CKAN\registry.json"

$countsCache = Join-Path $env:TEMP "ckan-download_counts.json"
if (-not (Test-Path $countsCache) -or ((Get-Date) - (Get-Item $countsCache).LastWriteTime).TotalDays -gt 7) {
    Invoke-WebRequest -Uri $DownloadCountsUrl -OutFile $countsCache -UseBasicParsing
}
$downloadCounts = Get-Content $countsCache -Raw | ConvertFrom-Json
$countLookup = @{}
foreach ($p in $downloadCounts.PSObject.Properties) { $countLookup[$p.Name] = [int]$p.Value }

$reg = Get-Content $registryPath -Raw | ConvertFrom-Json
$installedIds = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($prop in $reg.installed_modules.PSObject.Properties) { [void]$installedIds.Add($prop.Name) }

# Mutually exclusive inventory organizers (ONE per ModTest profile)
$exclusiveOrganizers = [ordered]@{
    'CommunityCategoryKit' = @{ Name = 'Community Category Kit'; Folder = 'CommunityCategoryKit'; Role = 'custom VAB category tabs (CCK.dll)' }
    'VABOrganizer' = @{ Name = 'VAB Organizer'; Folder = $null; Role = 'subcategory drawer + size tags' }
    'PartCatalog' = @{ Name = 'Part Catalog'; Folder = $null; Role = 'alternative parts browser' }
    'CategoryParts' = @{ Name = 'Category Parts'; Folder = $null; Role = 'category-based parts list' }
    'EditorExtensions' = @{ Name = 'Editor Extensions'; Folder = $null; Role = 'editor UI extensions' }
}

# VAB-UI mods on main (test separately; some overlap organizer role)
$vabUiInstalled = [ordered]@{
    'CommunityCategoryKit' = @{ Folder = 'CommunityCategoryKit'; Priority = 'P0'; Note = 'Primary category system on main install' }
    'Hangar' = @{ Folder = 'Hangar'; Priority = 'P1'; Note = 'Editor storage overlay' }
    'HangerExtenderExtended' = @{ Folder = 'FShangarExtender'; Priority = 'P1'; Note = 'Panel resize / layout' }
    'KSPCommunityFixes' = @{ Folder = 'KSPCommunityFixes'; Priority = 'P0'; Note = 'FasterEditorPartList patches search pipeline' }
}

$folderInfo = @{}
foreach ($prop in $reg.installed_modules.PSObject.Properties) {
    $id = $prop.Name
    $mod = $prop.Value
    $src = $mod.source_module
    $tags = @()
    if ($src.tags) { $tags = @($src.tags) }
    if (-not $mod.installed_files) { continue }
    foreach ($fp in $mod.installed_files.PSObject.Properties.Name) {
        if ($fp -match '^GameData/([^/]+)') {
            $folder = $Matches[1]
            if (-not $folderInfo.ContainsKey($folder)) {
                $folderInfo[$folder] = @{
                    CkanIds = [System.Collections.Generic.List[string]]::new()
                    Tags = [System.Collections.Generic.HashSet[string]]::new()
                    KspMin = $src.ksp_version_min
                    KspMax = $src.ksp_version_max
                    DisplayName = $src.name
                }
            }
            $info = $folderInfo[$folder]
            if ($info.CkanIds -notcontains $id) { [void]$info.CkanIds.Add($id) }
            foreach ($t in $tags) { [void]$info.Tags.Add($t) }
            if ($src.name) { $info.DisplayName = $src.name }
        }
    }
}

$skipIds = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
@(
    'Scatterer', 'Scatterer-config', 'Scatterer-sunflare',
    'EnvironmentalVisualEnhancements', 'EnvironmentalVisualEnhancements-HR',
    'Parallax', 'Parallax-StockTextures', 'Parallax-StockScatterTextures',
    'Waterfall', 'StockWaterfallEffects', 'SmokeScreen',
    'RocketSoundEnhancement', 'RocketSoundEnhancementDefault',
    'ToolbarController', 'MechJeb2', 'KerbalEngineerRedux', 'KerbalEngineer',
    'Harmony2', 'ModuleManager', 'KerbalChangelog',
    'PhysicsRangeExtender', 'VesselMover', 'HyperEdit',
    'FMRS', 'RecoveryController', 'PersistentRotation',
    'TextureReplacer', 'TexturesUnlimited',
    'IndicatorLights', 'SpaceDustUnbound', 'SpaceDust',
    'PatchManager', 'FireflyAPI', 'ROUtils',
    'PWBFuelBalancerRestored', 'ShipManifest', 'BonVoyage',
    'KerbalJointReinforcement', 'Missing_robotics', 'KoobalSearchEngine',
    'RasterPropMonitor', 'RasterPropMonitor-Core', 'JSI'
) | ForEach-Object { [void]$skipIds.Add($_) }

$skipFolderPatterns = @(
    'Scatterer', 'Waterfall', 'SmokeScreen', 'RocketSound', 'IndicatorLights',
    'KerbalEngineer', 'MechJeb2', 'ToolbarControl', 'ClickThroughBlocker',
    'Harmony', 'ModuleManager', 'KerbalChangelog',
    'PhysicsRangeExtender', 'VesselMover', 'HyperEdit', 'FMRS', 'RecoveryController',
    'PersistentRotation', 'TexturesUnlimited', 'SpaceDust', 'PatchManager',
    'FireflyAPI', 'ROUtils', 'PWBFuelBalancer', 'ShipManifest', 'BonVoyage',
    'KerbalJointReinforcement', 'Missing_robotics', 'KoobalSearchEngine',
    'SquadExpansion', 'Squad', 'StockWaterfallEffects', 'SpaceTuxLibrary',
    'SpaceTuxSA', 'BahaSP', 'BDAnimationModules', 'B9AnimationModules',
    'ModuleAnimateGenericEffects', 'ModuleSequentialAnimateGeneric',
    'Kerbaltek', '000_AT_Utils', 'AT-Utils', '000_ClickThroughBlocker',
    '000_USITools', 'USITools', 'CommunityResourcePack', 'CommunityTechTree',
    '999_KSP-Recall', 'KSP-Recall', 'JSI'
)

function Get-VabUiClass {
    param([string]$Folder, [hashtable]$Info)
    $ids = if ($Info) { @($Info.CkanIds) } else { @() }
    foreach ($id in $ids) {
        if ($exclusiveOrganizers.Contains($id)) { return 'VAB-UI / ORGANIZER' }
        if ($id -eq 'Hangar' -or $id -eq 'HangerExtenderExtended') { return 'VAB-UI / LAYOUT' }
        if ($id -eq 'KSPCommunityFixes') { return 'VAB-UI / EDITOR-PATCH' }
    }
    if ($Folder -eq 'CommunityCategoryKit') { return 'VAB-UI / ORGANIZER' }
    if ($Folder -in @('Hangar', 'FShangarExtender')) { return 'VAB-UI / LAYOUT' }
    if ($Folder -eq 'KSPCommunityFixes') { return 'VAB-UI / EDITOR-PATCH' }
    $folderPath = Join-Path $mainGd $Folder
    if (Test-Path $folderPath) {
        $hasVabo = Get-ChildItem -Path $folderPath -Recurse -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -match 'VABOrganizer|VABO' } | Select-Object -First 1
        if ($hasVabo) { return 'VABO-PATCH (needs VABOrganizer)' }
    }
    return $null
}

function Test-VabRelevant {
    param([string]$Folder, [hashtable]$Info, [string]$GdRoot)
    $ui = Get-VabUiClass -Folder $Folder -Info $Info
    if ($ui) { return $true, $ui }

    $folderPath = Join-Path $GdRoot $Folder
    if (-not (Test-Path $folderPath)) { return $false, 'missing' }

    if ($Folder -in @('ProceduralParts', 'B9PartSwitch', 'TweakScale', 'KIS', 'KAS', 'ExtraplanetaryLaunchpads', 'ReStock', 'ReStockPlus')) {
        return $true, 'parts / editor-workflow'
    }

    foreach ($id in $Info.CkanIds) { if ($skipIds.Contains($id)) { return $false, 'skip-ckan-id' } }
    foreach ($pat in $skipFolderPatterns) { if ($Folder -like "*$pat*") { return $false, 'skip-folder' } }

    $hasParts = Test-Path (Join-Path $folderPath 'Parts')
    $hasPartCfgs = Get-ChildItem -Path $folderPath -Filter '*.cfg' -Recurse -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -match '\\Parts\\' -or $_.Name -eq 'part.cfg' } | Select-Object -First 1

    if ($hasParts -or $hasPartCfgs) { return $true, 'parts' }
    return $false, 'no-parts'
}

$versionFiles = Get-ChildItem -Path $mainGd -Filter '*.version' -Recurse -File
$folderVersions = @{}
foreach ($vf in $versionFiles) {
    $top = (($vf.FullName.Substring($mainGd.Length).TrimStart('\')) -split '\\')[0]
    if (-not $folderVersions.ContainsKey($top)) { $folderVersions[$top] = $true }
}

$rows = @()
Get-ChildItem -Path $mainGd -Directory | ForEach-Object {
    if ($_.Name -eq 'Squad') { return }
    $folder = $_.Name
    $info = $folderInfo[$folder]
    $ckanIds = if ($info) { @($info.CkanIds) } else { @() }
    $dl = 0
    foreach ($id in $ckanIds) { if ($countLookup.ContainsKey($id)) { $dl = [Math]::Max($dl, $countLookup[$id]) } }
    $relevant, $reason = Test-VabRelevant -Folder $folder -Info $info -GdRoot $mainGd
    $rows += [PSCustomObject]@{
        Folder = $folder
        CkanId = ($ckanIds -join ', ')
        DisplayName = if ($info -and $info.DisplayName) { $info.DisplayName } else { $folder }
        Downloads = $dl
        KspCompat = if ($info) { "$($info.KspMin)-$($info.KspMax)" } else { '?' }
        Versioned = $folderVersions.ContainsKey($folder)
        VabRelevant = $relevant
        Reason = $reason
    }
}

$vabUiRows = $rows | Where-Object { $_.Reason -like 'VAB-UI*' -or $_.Reason -like 'VABO-PATCH*' } | Sort-Object @{e={$_.Reason -like 'VAB-UI*'};desc=$true}, Downloads -Descending
$partsRows = $rows | Where-Object { $_.VabRelevant -and $_.Reason -notlike 'VAB-UI*' -and $_.Reason -notlike 'VABO-PATCH*' } | Sort-Object Downloads -Descending
$skipRows = $rows | Where-Object { -not $_.VabRelevant } | Sort-Object Downloads -Descending

$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('# Main Install Mod Inventory')
[void]$sb.AppendLine('')
[void]$sb.AppendLine("Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm')")
[void]$sb.AppendLine("Source: ``$mainGd`` + CKAN ``registry.json`` (compat/deps source of truth)")
[void]$sb.AppendLine('')
[void]$sb.AppendLine('## Summary')
[void]$sb.AppendLine('')
[void]$sb.AppendLine('| Metric | Count |')
[void]$sb.AppendLine('|--------|------:|')
[void]$sb.AppendLine("| Top-level GameData folders (excl. Squad) | $($rows.Count) |")
[void]$sb.AppendLine("| VAB-UI / organizer (high priority) | $(($vabUiRows | Where-Object { $_.Reason -like 'VAB-UI / ORGANIZER*' }).Count) |")
[void]$sb.AppendLine("| VABO-patch-only folders | $(($vabUiRows | Where-Object { $_.Reason -like 'VABO-PATCH*' }).Count) |")
[void]$sb.AppendLine("| Parts mods (profile-01+) | $($partsRows.Count) |")
[void]$sb.AppendLine('')
[void]$sb.AppendLine('## Mutual exclusivity - inventory organizers')
[void]$sb.AppendLine('')
[void]$sb.AppendLine('Users typically run **ONE** inventory/organizer mod. **Never** install two of these in the same ModTest profile:')
[void]$sb.AppendLine('')
[void]$sb.AppendLine('| CKAN id | On main install? | Profile |')
[void]$sb.AppendLine('|---------|:----------------:|---------|')
foreach ($kv in $exclusiveOrganizers.GetEnumerator()) {
    $onMain = $installedIds.Contains($kv.Key)
    $profile = switch ($kv.Key) {
        'CommunityCategoryKit' { 'profile-vab-ui-communitycategorykit' }
        'VABOrganizer' { 'profile-vaborganizer' }
        default { '(profile not created - CKAN-only matrix pass)' }
    }
    [void]$sb.AppendLine("| ``$($kv.Key)`` | $(if ($onMain) { '**YES**' } else { 'no' }) | $profile |")
}
[void]$sb.AppendLine('')
[void]$sb.AppendLine('**Main install organizer:** ``CommunityCategoryKit`` (CCK). VABOrganizer/PartCatalog/CategoryParts/EditorExtensions are **not** installed but many mods ship dormant VABO patches.')
[void]$sb.AppendLine('')
[void]$sb.AppendLine('**Test order:** baseline -> VAB-UI profiles (one at a time) -> parts-mod sweeps (profile-01+).')
[void]$sb.AppendLine('')
[void]$sb.AppendLine('## VAB-UI / ORGANIZER - high priority (test before parts sweeps)')
[void]$sb.AppendLine('')
[void]$sb.AppendLine('| Priority | GameData | CKAN id | Downloads | KSP compat (CKAN) | Role |')
[void]$sb.AppendLine('|----------|----------|---------|----------:|-------------------|------|')
foreach ($r in ($vabUiRows | Where-Object { $_.Reason -like 'VAB-UI*' })) {
    [void]$sb.AppendLine("| P0/P1 | ``$($r.Folder)`` | $($r.CkanId) | $($r.Downloads.ToString('N0')) | $($r.KspCompat) | $($r.Reason) |")
}
[void]$sb.AppendLine('')
[void]$sb.AppendLine('### VABO-patch mods on main (need VABOrganizer CKAN install to activate)')
[void]$sb.AppendLine('')
[void]$sb.AppendLine('| GameData folder | CKAN id(s) |')
[void]$sb.AppendLine('|-----------------|------------|')
foreach ($r in ($vabUiRows | Where-Object { $_.Reason -like 'VABO-PATCH*' } | Select-Object -First 20)) {
    [void]$sb.AppendLine("| ``$($r.Folder)`` | $($r.CkanId) |")
}
$vaboCount = ($vabUiRows | Where-Object { $_.Reason -like 'VABO-PATCH*' }).Count
[void]$sb.AppendLine("| ... | $vaboCount total folders with VABO configs |")
[void]$sb.AppendLine('')
[void]$sb.AppendLine('## Parts mods (download count descending)')
[void]$sb.AppendLine('')
[void]$sb.AppendLine('| Rank | GameData | CKAN id(s) | Downloads | KSP compat | Class |')
[void]$sb.AppendLine('|-----:|----------|------------|----------:|------------|-------|')
$rank = 1
foreach ($r in $partsRows) {
    [void]$sb.AppendLine("| $rank | ``$($r.Folder)`` | $($r.CkanId) | $($r.Downloads.ToString('N0')) | $($r.KspCompat) | $($r.Reason) |")
    $rank++
}
[void]$sb.AppendLine('')
[void]$sb.AppendLine('## Skipped (reference)')
[void]$sb.AppendLine('')
[void]$sb.AppendLine('| GameData | CKAN id(s) | Downloads | Reason |')
[void]$sb.AppendLine('|----------|------------|----------:|--------|')
foreach ($r in $skipRows) {
    [void]$sb.AppendLine("| ``$($r.Folder)`` | $($r.CkanId) | $($r.Downloads.ToString('N0')) | $($r.Reason) |")
}

$sb.ToString() | Set-Content -Path $Output -Encoding UTF8
Write-Host "Wrote $Output"
