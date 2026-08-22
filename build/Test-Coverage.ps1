<#
    Runs the rewrite test suites with coverage and enforces the 100% line and branch gate.

    Every suite emits a report covering all included assemblies, and coverlet writes filenames
    relative to a per-report source root. Reports are therefore canonicalised to repo-relative paths
    and merged by taking the best result per line, rather than summed, which would count each line
    once per suite.

    Legacy projects are excluded by coverage.runsettings until p13-legacy-removal.
#>
[CmdletBinding()]
param(
    [double] $MinLineRate = 1.0,
    [double] $MinBranchRate = 1.0,
    [string] $Configuration = 'Release',
    [switch] $SkipTests
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Split-Path -Parent $PSScriptRoot)).Path
$results = Join-Path $root 'artifacts/coverage'

$projects = @(
    'tests/DupDetector.Core.Tests/DupDetector.Core.Tests.csproj'
    'tests/DupDetector.Sources.Tests/DupDetector.Sources.Tests.csproj'
    'tests/DupDetector.Reporting.Tests/DupDetector.Reporting.Tests.csproj'
    'tests/DupDetector.Cli.Tests/DupDetector.Cli.Tests.csproj'
)

if (-not $SkipTests) {
    if (Test-Path $results) { Remove-Item $results -Recurse -Force }
    New-Item -ItemType Directory -Path $results -Force | Out-Null

    foreach ($project in $projects) {
        Write-Host "==> $project" -ForegroundColor Cyan
        dotnet test (Join-Path $root $project) `
            --configuration $Configuration `
            --nologo `
            --settings (Join-Path $root 'build/coverage.runsettings') `
            --collect:'XPlat Code Coverage' `
            --results-directory $results
        if ($LASTEXITCODE -ne 0) { throw "Tests failed for $project" }
    }
}

$reports = @(Get-ChildItem $results -Recurse -Filter 'coverage.cobertura.xml' -ErrorAction SilentlyContinue)
if ($reports.Count -eq 0) { throw 'No coverage report was produced.' }

function Resolve-CoverageFile {
    param([string[]] $Roots, [string] $FileName, [string] $RepoRoot)

    foreach ($candidateRoot in $Roots) {
        $candidate = Join-Path $candidateRoot $FileName
        if (Test-Path $candidate) {
            $full = (Resolve-Path $candidate).Path
            if ($full.StartsWith($RepoRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
                return $full.Substring($RepoRoot.Length).TrimStart([char]92, [char]47).Replace([char]92, [char]47)
            }
            return $full.Replace([char]92, [char]47)
        }
    }

    return $FileName.Replace([char]92, [char]47)
}

# key -> @(hits, branchesCovered, branchesTotal)
$lines = @{}

foreach ($report in $reports) {
    $xml = [xml](Get-Content $report.FullName -Raw)
    $roots = @($xml.SelectNodes('//sources/source') | ForEach-Object { $_.InnerText })
    $canonical = @{}

    foreach ($class in $xml.SelectNodes('//class')) {
        $name = $class.filename
        if (-not $canonical.ContainsKey($name)) {
            $canonical[$name] = Resolve-CoverageFile -Roots $roots -FileName $name -RepoRoot $root
        }

        foreach ($line in $class.SelectNodes('lines/line')) {
            $key = '{0}|{1}' -f $canonical[$name], $line.number
            $hits = [int] $line.hits
            $covered = 0
            $total = 0
            if ($line.'condition-coverage' -match '\((\d+)/(\d+)\)') {
                $covered = [int] $Matches[1]
                $total = [int] $Matches[2]
            }

            if ($lines.ContainsKey($key)) {
                $existing = $lines[$key]
                $lines[$key] = @(
                    [Math]::Max($existing[0], $hits)
                    [Math]::Max($existing[1], $covered)
                    [Math]::Max($existing[2], $total)
                )
            }
            else {
                $lines[$key] = @($hits, $covered, $total)
            }
        }
    }
}

$linesValid = $lines.Count
$linesCovered = 0
$branchesCovered = 0
$branchesValid = 0
$uncovered = [System.Collections.Generic.List[string]]::new()

foreach ($key in $lines.Keys) {
    $entry = $lines[$key]
    if ($entry[0] -gt 0) { $linesCovered++ } else { $uncovered.Add(($key -replace '\|', ':')) }
    $branchesCovered += $entry[1]
    $branchesValid += $entry[2]
    if ($entry[2] -gt 0 -and $entry[1] -lt $entry[2]) {
        $uncovered.Add(('{0} branch {1}/{2}' -f ($key -replace '\|', ':'), $entry[1], $entry[2]))
    }
}

$lineRate = if ($linesValid -eq 0) { 0.0 } else { $linesCovered / $linesValid }
$branchRate = if ($branchesValid -eq 0) { 1.0 } else { $branchesCovered / $branchesValid }

Write-Host ''
Write-Host ('Line   : {0}/{1} = {2:P2}' -f $linesCovered, $linesValid, $lineRate)
Write-Host ('Branch : {0}/{1} = {2:P2}' -f $branchesCovered, $branchesValid, $branchRate)

if ($uncovered.Count -gt 0) {
    Write-Host ''
    Write-Host 'Uncovered:' -ForegroundColor Yellow
    $uncovered | Sort-Object -Unique | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }
}

if ($linesValid -eq 0) { throw 'Coverage report contains no lines; the Include filter matched nothing.' }
if ($lineRate -lt $MinLineRate) { throw ('Line coverage {0:P2} is below the required {1:P2}.' -f $lineRate, $MinLineRate) }
if ($branchRate -lt $MinBranchRate) { throw ('Branch coverage {0:P2} is below the required {1:P2}.' -f $branchRate, $MinBranchRate) }

Write-Host ''
Write-Host 'Coverage gate passed.' -ForegroundColor Green