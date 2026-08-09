# Team UI route static audit

## Completion boundary

- Audit date: 2026-08-09.
- Static inventory and schema-6 ledger only. No Unity, browser, foreground application, account write, build, or runtime snapshot was used.
- No leaf is `done`. `TeamHallItem` pure rendering is `needs-runtime-verify`; every other leaf is explicitly `blocked`.

## Old-client page and component inventory

The current old client has 15 Team UI definitions under `cdn/resource/game/team`:

1. `TeamView`
2. `TeamHallItem`
3. `TeamRoleItem`
4. `TeamTabItem`
5. `TeamApplyView`
6. `TeamApplyRoleItem`
7. `TeamBeInvitedView`
8. `TeamBeInvitedRoleItem`
9. `TeamInviteView`
10. `TeamInviteRoleItem`
11. `TeamChangeTargetView`
12. `TeamChangeTargetTab`
13. `TeamMatchView`
14. `TeamVoteView`
15. `TeamSmallDescView`

`route-manifest.json` expands these into page, control, conditional-state,
popup, list-row, scrolling, return, sound, protocol-boundary and resource
leaves. Page `control_inventory[]` entries map every direct child exactly once.

## Unity inventory and static change

- Existing Team C#: `TeamController`, `TeamModel`, `TeamMainRoleItem`.
- Existing editable Team prefab: only `Assets/Prefabs/UI/Team/TeamHallItem.prefab`.
- Missing page prefabs: `TeamView`, `TeamApplyView`, `TeamBeInvitedView`,
  `TeamInviteView`, `TeamChangeTargetView`, `TeamMatchView`, `TeamVoteView`,
  `TeamSmallDescView`, and their remaining list/tab item prefabs.
- The existing `TeamHallItem.prefab` now binds a data-only `TeamHallItem` view.
  It consumes the authoritative 24012 hall snapshot for member count, leader
  name/level, online state and shared `CustomHeadItem` portrait rendering.
- No apply/invite/join/menu click handler was added. `applyBtn` remains without
  a new write binding in this change.

## Component dependency inventory

- `TeamHallItem` reuses the existing shared `Common/CustomHeadItem` prefab/view
  for the portrait; the Team host only supplies role data and presentation
  state.
- Runtime verification must cover empty/populated rows, same-scene/remote/offline
  state, async portrait arrival, row reuse, and at least two viewports.
- No shared component, generated bind, Common view, MainUI or HUD file was edited.

## Resource closure

The old-client Team directory contains the full UI JSON/scene set and Team
textures. The Unity Team resource directory currently contains only
`team_texture.spriteatlas` plus three source textures (`ui_dw_zdyq003`,
`uizd_001`, `com_head_border`). This is not a complete static conversion
closure. Missing page prefabs must follow `convert-module`, but conversion is
blocked in this run because it requires Unity plus runtime snapshots and the
task explicitly forbids both.

## Protocol boundaries

- 24011 remains absent. The old avatar-menu path is nonfunctional
  (`ShowPlayerMenu` is empty / member click is unbound); the server-side leader
  transaction being live does not authorize reviving it. Authoritative leader
  changes continue to arrive through 24015.
- 24042 remains absent. The old client has no sender and discards the decoded
  response, despite a server read handler existing.
- All real state-changing leaves (24000, 24002, 24004, 24005, 24006/24057,
  24008, 24009, 24017/24018, 24021, 24048, 24055 and related external
  transitions) are enumerated but blocked. No account transaction was sent.

## Required runtime follow-up

After page prefabs and resource closure exist, use the original manifest path
and run old H5 and a source/catalog-matched Unity Web build with the same account
and state. Verify GraphicRaycaster clicks, scroll-to-last-row and clipping,
popup identity/close chains, conditional leader/team/empty states, authoritative
success and failure refresh, close/reopen consistency, sound lifecycle, cold/warm
timing, two viewports, and immutable evidence hashes before any leaf can become
`done`.
