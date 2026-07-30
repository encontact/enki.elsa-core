param([string]$Version)

if (-not $Version) {
    Write-Host "Usage: .\scripts\set-version.ps1 -Version x.y.z"
    exit 1
}

$propsFile = Join-Path $PSScriptRoot '..\Directory.Build.props'
$content = Get-Content $propsFile -Raw

if ($content -match '<Version>.*?</Version>') {
    $content = $content -replace '<Version>.*?</Version>', "<Version>$Version</Version>"
    Write-Host "  Updated Directory.Build.props to version $Version"
} else {
    Write-Host "ERROR: <Version> tag not found in Directory.Build.props"
    exit 1
}

Set-Content $propsFile $content -NoNewline
Write-Host "Done! Version set to $Version."
