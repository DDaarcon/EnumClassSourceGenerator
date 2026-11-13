[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Position = 0)]
    [string] $Version
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$packageProjectRelativePath = 'EnumClasses.Package/EnumClasses.Package.csproj'
$shippedRelativePath = 'EnumClasses.SourceGenerators/AnalyzerReleases.Shipped.md'
$unshippedRelativePath = 'EnumClasses.SourceGenerators/AnalyzerReleases.Unshipped.md'
$packageProjectPath = Join-Path $repositoryRoot $packageProjectRelativePath
$shippedPath = Join-Path $repositoryRoot $shippedRelativePath
$unshippedPath = Join-Path $repositoryRoot $unshippedRelativePath
$utf8WithoutBom = [System.Text.UTF8Encoding]::new($false)

function Invoke-Git {
    param([Parameter(Mandatory)][string[]] $Arguments)

    $previousErrorActionPreference = $ErrorActionPreference
    try {
        # Windows PowerShell represents native stderr as ErrorRecord objects. Git uses
        # stderr for successful status messages too, so rely on its exit code instead.
        $ErrorActionPreference = 'Continue'
        $output = @(& git -C $repositoryRoot @Arguments 2>&1)
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }

    if ($exitCode -ne 0) {
        $details = ($output | Out-String).Trim()
        throw "Git command failed: git $($Arguments -join ' ')`n$details"
    }

    return $output
}

function Get-NewLine {
    param([Parameter(Mandatory)][string] $Content)

    if ($Content.Contains("`r`n")) {
        return "`r`n"
    }

    return "`n"
}

function Write-Utf8WithoutBom {
    param(
        [Parameter(Mandatory)][string] $Path,
        [Parameter(Mandatory)][string] $Content
    )

    [System.IO.File]::WriteAllText($Path, $Content, $utf8WithoutBom)
}

$branch = ((Invoke-Git -Arguments @('branch', '--show-current')) | Out-String).Trim()
if ($branch -ne 'master') {
    $branchDescription = if ([string]::IsNullOrWhiteSpace($branch)) { 'detached HEAD' } else { "branch '$branch'" }
    throw "Release preparation must run on branch 'master'; the repository is on $branchDescription."
}

$changes = @(Invoke-Git -Arguments @('status', '--porcelain=v1', '--untracked-files=all'))
if ($changes.Count -gt 0) {
    $changeList = ($changes -join [Environment]::NewLine)
    throw "Release preparation requires a clean working tree. Commit, stash, or remove these changes first:`n$changeList"
}

$packageProject = [System.IO.File]::ReadAllText($packageProjectPath)
$shipped = [System.IO.File]::ReadAllText($shippedPath)
$unshipped = [System.IO.File]::ReadAllText($unshippedPath)

$versionElementPattern = '<Version>(?<Version>[^<]+)</Version>'
$versionMatches = [regex]::Matches($packageProject, $versionElementPattern)
if ($versionMatches.Count -ne 1) {
    throw "Expected exactly one <Version> element in '$packageProjectPath', but found $($versionMatches.Count)."
}

$currentVersion = $versionMatches[0].Groups['Version'].Value
$previousVersions = @(
    [regex]::Matches($shipped, '(?m)^## Release (?<Version>\S+)\s*$') |
        ForEach-Object { $_.Groups['Version'].Value }
)
$previousReleaseNumbers = @(
    $previousVersions | ForEach-Object {
        $releaseNumberMatch = [regex]::Match($_, '^\d+\.\d+\.\d+')
        if ($releaseNumberMatch.Success) {
            $releaseNumberMatch.Value
        }
    }
)

Write-Host "Current package version: $currentVersion"
if ($previousVersions.Count -gt 0) {
    Write-Host 'Shipped analyzer releases:'
    $previousVersions | ForEach-Object { Write-Host "  - $_" }
}
else {
    Write-Host 'Shipped analyzer releases: none'
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    $Version = Read-Host 'New release version'
}

$Version = $Version.Trim()
$nugetVersionPattern = '^(?<Release>\d+\.\d+\.\d+)(?:-[0-9A-Za-z]+(?:[.-][0-9A-Za-z]+)*)?(?:\+[0-9A-Za-z]+(?:[.-][0-9A-Za-z]+)*)?$'
$versionMatch = [regex]::Match($Version, $nugetVersionPattern)
if (-not $versionMatch.Success) {
    throw "'$Version' is not a supported NuGet semantic version (for example, 1.2.3 or 1.2.3-preview.1)."
}
$analyzerReleaseVersion = $versionMatch.Groups['Release'].Value

if ($Version -eq $currentVersion) {
    throw "The package already uses version '$Version'."
}

$lastAnalyzerReleaseVersion = if ($previousReleaseNumbers.Count -gt 0) {
    $previousReleaseNumbers[-1]
}
else {
    $null
}
$appendToLastAnalyzerRelease = $analyzerReleaseVersion -eq $lastAnalyzerReleaseVersion

if (($previousReleaseNumbers -contains $analyzerReleaseVersion) -and -not $appendToLastAnalyzerRelease) {
    throw "Analyzer release '$analyzerReleaseVersion' already exists, but it is not the last release in AnalyzerReleases.Shipped.md."
}

$releaseBranch = "release/$Version"
$matchingBranches = @(
    Invoke-Git -Arguments @(
        'branch', '--all', '--list',
        $releaseBranch,
        "remotes/origin/$releaseBranch")
)
if ($matchingBranches.Count -gt 0) {
    throw "Release branch '$releaseBranch' already exists. No branch was checked out and no release files were changed."
}

$unshippedHeaderPattern = '\A; Unshipped analyzer release\s*'
if ($unshipped -notmatch $unshippedHeaderPattern) {
    throw "'$unshippedPath' does not start with the expected unshipped release header."
}

$unshippedBody = ([regex]::Replace($unshipped, $unshippedHeaderPattern, '')).Trim()
$shippedNewLine = Get-NewLine $shipped
$unshippedNewLine = Get-NewLine $unshipped

$updatedPackageProject = [regex]::Replace(
    $packageProject,
    $versionElementPattern,
    "<Version>$Version</Version>")

$updatedShipped = $shipped.TrimEnd()
if (-not [string]::IsNullOrWhiteSpace($unshippedBody)) {
    $normalizedUnshippedBody = $unshippedBody -replace "`r?`n", $shippedNewLine
    if ($appendToLastAnalyzerRelease) {
        $updatedShipped += $shippedNewLine + $shippedNewLine + $normalizedUnshippedBody
    }
    else {
        $updatedShipped += $shippedNewLine + $shippedNewLine +
            "## Release $analyzerReleaseVersion" + $shippedNewLine + $shippedNewLine +
            $normalizedUnshippedBody
    }
}
$updatedShipped += $shippedNewLine

$updatedUnshipped = '; Unshipped analyzer release' + $unshippedNewLine

if ($PSCmdlet.ShouldProcess($repositoryRoot, "prepare release $Version")) {
    [void](Invoke-Git -Arguments @('switch', '--quiet', '--create', $releaseBranch))

    Write-Utf8WithoutBom $packageProjectPath $updatedPackageProject
    Write-Utf8WithoutBom $shippedPath $updatedShipped
    Write-Utf8WithoutBom $unshippedPath $updatedUnshipped

    Write-Host "Prepared release $Version."
    Write-Host "  Updated: $packageProjectPath"
    if ([string]::IsNullOrWhiteSpace($unshippedBody)) {
        Write-Host '  Analyzer releases: no unshipped entries to migrate'
    }
    else {
        $migrationAction = if ($appendToLastAnalyzerRelease) { 'Appended analyzer entries to' } else { 'Migrated analyzer entries into' }
        Write-Host "  $migrationAction release ${analyzerReleaseVersion}: $unshippedPath -> $shippedPath"
    }

    [void](Invoke-Git -Arguments @(
        'add', '--',
        $packageProjectRelativePath,
        $shippedRelativePath,
        $unshippedRelativePath))

    $commitMessage = "release $Version"
    $commitOutput = Invoke-Git -Arguments @('commit', '-m', $commitMessage)
    $commitOutput | ForEach-Object { Write-Host $_ }
    Write-Host "Committed release changes with message '$commitMessage'."
}
