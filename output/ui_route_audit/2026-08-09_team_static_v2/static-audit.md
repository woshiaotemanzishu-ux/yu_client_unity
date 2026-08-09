# Team UI route static audit — topology revision 2

## Ledger authority

- `2026-08-09_team_static_v1` is preserved byte-for-byte as a historical,
  internally valid schema-6 ledger, but it is **superseded and must not be used
  for route acceptance** because read-only QA proved its topology omitted
  controls and conditional leaves.
- This v2 directory is a fresh schema-6 initialization from a corrected
  manifest. The old ledger tree and manifest hash were not edited or rebound.
- Audit mode remains static-only: no Unity, browser, foreground application,
  account write, runtime snapshot or real-Web comparison was used.
- No node is `done`. One implemented pure-display leaf is
  `needs-runtime-verify`; every other leaf is explicitly `blocked`.

## Corrected topology

The v2 topology contains 127 nodes and 102 leaves. In addition to the v1
inventory, it explicitly records:

1. `TeamView.LoadSuccess` query 24010.
2. Close-button and background-close paths separately.
3. Target summary text, with-team/without-team button and list matrices.
4. Target/world-shout/apply-list non-leader messages as three separate leaves.
5. Conditional create-on-open and world-shout countdown, blocked-click and
   expiry-reset states.
6. Member-row leader/self action visibility.
7. Invite tab-row rendering/selected state plus nearby/friend/guild row render.
8. `TeamApplyView.open_callback` query 24047 and `join_type` check-image state.
9. `TeamBeInvitedView` destroy/reset-list lifecycle.
10. Change-target role-level filtering, scroll, row rendering and selected
    state; distinct `down_level`/`up_level` clicks; calculator open, value
    preview and close callback; all four validation failures and value resets.
11. Sentient full-team Alert open, automatic match cancel, explicit confirm
    and cancel controls, then mutually exclusive current-scene `FIND_WAY` and
    off-scene 24108 confirm branches.

## Complete old-client module scan

All 15 Team definitions under `cdn/resource/game/team` and their TypeScript
consumers were rescanned: `TeamView`, `TeamHallItem`, `TeamRoleItem`,
`TeamTabItem`, `TeamApplyView`, `TeamApplyRoleItem`, `TeamBeInvitedView`,
`TeamBeInvitedRoleItem`, `TeamInviteView`, `TeamInviteRoleItem`,
`TeamChangeTargetView`, `TeamChangeTargetTab`, `TeamMatchView`, `TeamVoteView`
and `TeamSmallDescView`. The manifest includes pages, buttons, list/scroll
leaves, list-item render states, conditional display/message blocks, popups,
returns, lifecycle cleanup, sound, protocol boundaries and resource closure.
Every page `control_inventory[]` maps every direct child exactly once.

## Unity implementation boundary

- Existing editable Team prefab remains only
  `Assets/Prefabs/UI/Team/TeamHallItem.prefab`.
- Its data-only `TeamHallItem` view consumes 24012 for member count, leader
  name/level, online state and shared `CustomHeadItem`; no apply, join, invite
  or avatar-menu click binding was added.
- Empty/reused rows hide the cached portrait, and asynchronous portrait loads
  use a render version so stale data cannot overwrite the current row.
- No code or Prefab changed while correcting v2 topology.

## Protocol and authorization boundaries

- 24011 remains absent: old `ShowPlayerMenu` is empty / member avatar click is
  unbound. Server-side leader support does not authorize reviving this client
  route; authoritative leader updates continue through 24015.
- 24042 remains absent: the old client has no sender and discards its decoded
  response even though the server has a read handler.
- All state-changing leaves, including 24000, 24002, 24004, 24005,
  24006/24057, 24008, 24009, 24017/24018, 24021, 24048, 24055, 24108,
  calculator value/reset paths and external navigation, are enumerated but
  blocked. No protocol was sent and no account was mutated.

## Resource and runtime blocker

Page-level Team prefabs are missing. Unity's Team resource directory still has
only `team_texture.spriteatlas` and three source textures (`ui_dw_zdyq003`,
`uizd_001`, `com_head_border`), which is not the old module's conversion
closure. Missing pages therefore require `convert-module`, Unity import and
old/Unity runtime snapshots. Those operations are forbidden in this run and
the missing resource files are outside the Team write island.

Runtime follow-up must replay this v2 manifest against the old H5 and a
source/catalog-matched Unity Web build with the same account/state. It must
cover GraphicRaycaster clicks, popup identities, scroll-to-last-row, clipping,
all condition matrices above, authoritative success/failure and immediate
refresh, close/reopen consistency, sounds, cold/warm timing, two viewports and
immutable evidence hashes before any leaf can become `done`.
