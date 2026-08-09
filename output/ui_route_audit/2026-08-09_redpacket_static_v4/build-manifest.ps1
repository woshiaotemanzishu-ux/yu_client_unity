$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$sourcePath = Join-Path $repo 'output\ui_route_audit\2026-08-09_redpacket_static_v3\route-manifest.json'
$manifest = [IO.File]::ReadAllText($sourcePath) | ConvertFrom-Json
if ($manifest.route -ne 'mainui.red-packet' -or @($manifest.nodes).Count -ne 62) { throw 'unexpected RedPacket v3 manifest topology' }
$routeState = @($manifest.nodes | Where-Object id -eq 'mainui.red-packet.route-state')
if ($routeState.Count -ne 1) { throw 'route-state page missing' }
$routeState[0].control_inventory += @(
  [ordered]@{ id = 'disconnect-reset'; kind = 'lifecycle'; child = 'mainui.red-packet.route-state.disconnect-reset' },
  [ordered]@{ id = 'late-arrival'; kind = 'lifecycle'; child = 'mainui.red-packet.route-state.late-arrival' },
  [ordered]@{ id = 'subscription-unbind'; kind = 'lifecycle'; child = 'mainui.red-packet.route-state.subscription-unbind' }
)
$manifest.nodes += @(
  [ordered]@{ id = 'mainui.red-packet.route-state.disconnect-reset'; type = 'read'; risk = 'read-only'; parent = 'mainui.red-packet.route-state'; note = '真断线 Reset 递增 generation，并在 Release 模块前调用 MainView.PrepareForRelease 幂等解绑。' },
  [ordered]@{ id = 'mainui.red-packet.route-state.late-arrival'; type = 'read'; risk = 'read-only'; parent = 'mainui.red-packet.route-state'; note = 'Reset 后 await 晚到实例按 generation 判旧并立即 Release，禁止回填 _moduleRoot 或 Show。' },
  [ordered]@{ id = 'mainui.red-packet.route-state.subscription-unbind'; type = 'read'; risk = 'read-only'; parent = 'mainui.red-packet.route-state'; note = 'OnShow 订阅，OnHide/OnDispose/PrepareForRelease 解绑，冷/热开关和断线释放不得泄漏。' }
)
$manifest.baseline | Add-Member -NotePropertyName superseded_by_v4 -NotePropertyValue @(
  'output/ui_route_audit/2026-08-09_redpacket_static_v3'
) -Force
$manifest.baseline | Add-Member -NotePropertyName v4_reason -NotePropertyValue 'Lifecycle QA added disconnect reset, stale await release, and subscription unbind as three independent leaves; schema6 topology is immutable.' -Force
$json = $manifest | ConvertTo-Json -Depth 14
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'route-manifest.json'), $json + [Environment]::NewLine, $utf8NoBom)

