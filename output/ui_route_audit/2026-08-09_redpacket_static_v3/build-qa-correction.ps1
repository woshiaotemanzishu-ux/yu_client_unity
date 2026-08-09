$ErrorActionPreference = 'Stop'
$sourcePath = Join-Path $PSScriptRoot 'results-static-qa-blockers.json'
$source = [IO.File]::ReadAllText($sourcePath) | ConvertFrom-Json
$sourceNodes = @($source.nodes)
if ($sourceNodes.Count -ne 40) { throw "expected 40 QA blocker nodes, got $($sourceNodes.Count)" }
$nodes = foreach ($sourceNode in $sourceNodes) {
  [ordered]@{
    id = [string]$sourceNode.id
    status = 'blocked'
    runtime_gap = $null
    note = 'QA correction: blocked leaves must not retain the previous needs-runtime runtime_gap.'
  }
}
$json = [ordered]@{ nodes = @($nodes) } | ConvertTo-Json -Depth 6
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'results-static-qa-correction.json'), $json + [Environment]::NewLine, $utf8NoBom)

