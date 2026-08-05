#!/usr/bin/env python3
"""Synchronize the legacy client's complete logical sound library into Unity.

The tool deliberately selects one physical file for each legacy logical key:
scene music prefers mp3, short effects prefer wav, then ogg/mp3 fallbacks.
It also owns deterministic .meta files, Addressables rows and the generated ledger.
Run with --check in CI/review to reject partial migrations or drift.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import subprocess
import sys
from collections import Counter, defaultdict
from pathlib import Path


CATEGORIES = ("novice_voice", "npc", "role", "scene", "skill", "ui")
AUDIO_EXTENSIONS = (".mp3", ".ogg", ".wav")
SOUND_LABELS = tuple(f"pack_resource_sound_{category}" for category in CATEGORIES)
SYNC_VERSION = 1


def md5_guid(asset_path: str) -> str:
    return hashlib.md5(("yu-audio:" + asset_path.lower()).encode("utf-8")).hexdigest()


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def normalize(path: Path) -> str:
    return path.as_posix()


def choose_file(category: str, candidates: list[Path]) -> Path:
    priority = (".mp3", ".ogg", ".wav") if category == "scene" else (".wav", ".ogg", ".mp3")
    by_extension = {path.suffix.lower(): path for path in candidates}
    for extension in priority:
        if extension in by_extension:
            return by_extension[extension]
    raise RuntimeError(f"unsupported sound candidates: {candidates}")


def audio_meta(guid: str, category: str) -> str:
    if category == "scene":
        load_type, compression, quality, preload, background = 2, 1, 0.7, 0, 1
    elif category in ("npc", "novice_voice"):
        load_type, compression, quality, preload, background = 1, 1, 0.75, 1, 1
    else:
        load_type, compression, quality, preload, background = 0, 2, 1, 1, 1
    return f"""fileFormatVersion: 2
guid: {guid}
AudioImporter:
  externalObjects: {{}}
  serializedVersion: 8
  defaultSettings:
    serializedVersion: 2
    loadType: {load_type}
    sampleRateSetting: 0
    sampleRateOverride: 44100
    compressionFormat: {compression}
    quality: {quality}
    conversionMode: 0
    preloadAudioData: {preload}
  platformSettingOverrides: {{}}
  forceToMono: 0
  normalize: 1
  loadInBackground: {background}
  ambisonic: 0
  3D: 1
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def text_meta(guid: str) -> str:
    return f"""fileFormatVersion: 2
guid: {guid}
TextScriptImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def folder_meta(guid: str) -> str:
    return f"""fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def find_client_root(unity_root: Path, explicit: str | None) -> Path:
    if explicit:
        root = Path(explicit).resolve()
        if (root / "cdn/resource/sound").is_dir():
            return root
        raise RuntimeError(f"invalid client root: {root}")
    candidates = [unity_root.parent / "yu_client"]
    if len(unity_root.parents) > 1:
        candidates.append(unity_root.parents[1] / "yu_client")
    for root in candidates:
        if (root / "cdn/resource/sound").is_dir():
            return root.resolve()
    raise RuntimeError("cannot locate sibling yu_client; pass --client-root")


def collect_resources(client_root: Path, unity_root: Path) -> list[dict]:
    source_root = client_root / "cdn/resource/sound"
    result: list[dict] = []
    for category in CATEGORIES:
        category_root = source_root / category
        groups: dict[str, list[Path]] = defaultdict(list)
        for path in sorted(category_root.rglob("*")):
            if path.is_file() and path.suffix.lower() in AUDIO_EXTENSIONS:
                logical = normalize(path.relative_to(category_root).with_suffix(""))
                groups[logical].append(path)
        for logical, candidates in sorted(groups.items()):
            chosen = choose_file(category, candidates)
            relative_asset = Path("Assets/GameRes/resource/sound") / category / (logical + chosen.suffix.lower())
            asset_path = normalize(relative_asset)
            key = f"resource/sound/{category}/{logical}".lower()
            result.append(
                {
                    "category": category,
                    "logicalName": logical,
                    "address": key,
                    "source": normalize(chosen.relative_to(client_root)),
                    "sourceBytes": chosen.stat().st_size,
                    "sourceSha256": sha256(chosen),
                    "candidates": [normalize(p.relative_to(client_root)) for p in sorted(candidates)],
                    "selectedFormat": chosen.suffix.lower().lstrip("."),
                    "assetPath": asset_path,
                    "guid": md5_guid(asset_path),
                    "label": f"pack_resource_sound_{category}",
                    "status": "done",
                    "_sourcePath": chosen,
                    "_destinationPath": unity_root / relative_asset,
                }
            )
    return result


def is_active_ts_line(line: str) -> bool:
    stripped = line.strip()
    return bool(stripped) and not stripped.startswith("//") and not stripped.startswith("*")


def relative_legacy(path: Path, src_root: Path) -> str:
    return normalize(path.relative_to(src_root))


def classify_direct(path: str, line: str) -> tuple[str, str]:
    if path == "util/Util.ts" or path.startswith("newgameui/") or path in (
        "common/BaseWindowComponent.ts", "fashion/FashionMainView.ts"
    ):
        return "done", "covered_by_global_button"
    mapping = {
        "common/CommonManager.ts": "runtime_fighting_voice_helper",
        "commonController/GoodsController.ts": "pickup_success",
        "funcOpen/FunctionOpenAutoView.ts": "function_open",
        "scene/SceneController.ts": "role_level_up",
        "scene/sceneobj/Role.ts": "role_jump",
        "scene/fight/FightMovieInfo.ts": "skill_movie_config",
        "common/CongratulationObtainView.ts": "fighting_up_view",
    }
    if path in mapping:
        return "done", mapping[path]
    if path == "login/LoginBgView.ts" and 'PlaySoundEffect("2_dianji", true, 0)' in line:
        return "done", "legacy_explicit_zero_volume_noop"
    return "pending", "unity_module_or_transaction_consumer_missing"


def classify_fighting(path: str) -> tuple[str, str]:
    basename = Path(path).name
    if basename in ("DungeonVictoryView.ts", "DungeonFailureView.ts"):
        return "done", "DungeonResultView"
    if basename == "AutoBrushResultView.ts":
        return "done", "AutoBrushResultView"
    if basename == "MainUIReliveView.ts":
        return "done", "MainUIReliveView"
    return "pending", "corresponding_unity_result_consumer_missing"


def classify_scene_music(path: str) -> tuple[str, str]:
    if path.endswith("FightController.ts"):
        return "done", "SceneController.On12005"
    if path == "login/LoginBgView.ts":
        return "done", "LoginFlow.StartAsync"
    if path == "elim/ElimMainView.ts":
        return "done", "ElimMainView.OnShow/OnHide"
    return "pending", "login_or_special_scene_presenter_missing"


def collect_callsites(client_root: Path) -> list[dict]:
    src_root = client_root / "h5/src"
    rows: list[dict] = []
    direct_re = re.compile(r"PlaySoundEffect\s*\((.+)")
    fighting_re = re.compile(r"PlayFightingVoice\s*\(")
    scene_re = re.compile(r"PlaySceneSound\s*\(")
    control_re = re.compile(r"\.(PauseMusic|ResumeMusic)\s*\(")
    for path in sorted(src_root.rglob("*.ts")):
        rel = relative_legacy(path, src_root)
        for line_number, raw in enumerate(path.read_text(encoding="utf-8", errors="replace").splitlines(), 1):
            if not is_active_ts_line(raw):
                continue
            expression = raw.strip()
            if direct_re.search(raw):
                status, coverage = classify_direct(rel, expression)
                kind = "global_button" if coverage == "covered_by_global_button" else "sound_effect"
            elif fighting_re.search(raw) and rel != "common/CommonManager.ts":
                status, coverage = classify_fighting(rel)
                kind = "fighting_voice"
            elif scene_re.search(raw) and rel != "common/SoundManager.ts":
                status, coverage = classify_scene_music(rel)
                kind = "scene_music"
            elif control_re.search(raw) and rel != "common/SoundManager.ts":
                status, coverage = "pending", "login_video_or_lifecycle_consumer_missing"
                kind = "music_control"
            else:
                continue
            rows.append(
                {
                    "kind": kind,
                    "legacyPath": rel,
                    "legacyLine": line_number,
                    "expression": expression,
                    "status": status,
                    "coverage": coverage,
                }
            )
    rows.sort(key=lambda row: (row["legacyPath"].lower(), row["legacyLine"], row["kind"]))
    return rows


def public_resource(row: dict) -> dict:
    return {key: value for key, value in row.items() if not key.startswith("_")}


def json_text(value: object) -> str:
    return json.dumps(value, ensure_ascii=False, indent=2) + "\n"


def render_ledger(resources: list[dict], callsites: list[dict]) -> str:
    category_counts = Counter(row["category"] for row in resources)
    format_counts = Counter(row["selectedFormat"] for row in resources)
    callsite_counts = Counter(row["status"] for row in callsites)
    total_bytes = sum(row["sourceBytes"] for row in resources)
    lines = [
        "# 老客户端声音迁移台账",
        "",
        "> 本文由 `Tools/Audio/sync_legacy_audio.py` 生成。不要手工改单行状态；修改迁移规则后重跑工具。",
        "",
        "## 总览",
        "",
        f"- 老端物理文件：676（原始多格式保留在老端，不重复导入 Unity）",
        f"- Unity 逻辑声音：{len(resources)}/310，状态全部 `done`，选中体积 {total_bytes:,} bytes",
        f"- 分类：" + "，".join(f"{key} {category_counts[key]}" for key in CATEGORIES),
        f"- 格式：" + "，".join(f"{key} {format_counts[key]}" for key in sorted(format_counts)),
        f"- 老端运行调用点：{len(callsites)}，已覆盖 {callsite_counts['done']}，待对应 Unity 业务页面/事务补齐 {callsite_counts['pending']}",
        "- 全局按钮：运行时自动绑定现有 `Button` 的 pointer-down，播放 `resource/sound/ui/2_dianji`；不重写人工 Prefab。",
        "- 场景音乐：按 `ConfigSound.Scene → DefaultSceneType → DefaultScene` 解析，保留老端 0.3 音量语义。",
        "",
        "## 状态定义",
        "",
        "- `done`：资源、地址和当前 Unity 消费链均已落地。",
        "- `pending`：声音资源已齐，但老端调用所在的 Unity 模块、权威事务成功点或专属演出尚不存在；不得提前在按钮点击时伪播成功音。",
        "",
        "## 资源清单（310/310）",
        "",
        "| 分类 | 逻辑名 | 选中格式 | Unity 地址 | 字节 | 状态 |",
        "|---|---|---:|---|---:|---|",
    ]
    for row in resources:
        lines.append(
            f"| {row['category']} | `{row['logicalName']}` | {row['selectedFormat']} | `{row['address']}` | {row['sourceBytes']:,} | {row['status']} |"
        )
    lines.extend(
        [
            "",
            "## 老端调用点清单",
            "",
            "| 类型 | 老端位置 | 表达式 | 状态 | Unity 覆盖/未补原因 |",
            "|---|---|---|---|---|",
        ]
    )
    for row in callsites:
        expression = row["expression"].replace("|", "\\|").replace("`", "'")
        lines.append(
            f"| {row['kind']} | `{row['legacyPath']}:{row['legacyLine']}` | `{expression}` | {row['status']} | `{row['coverage']}` |"
        )
    lines.extend(
        [
            "",
            "## 复验命令",
            "",
            "```powershell",
            "python Tools/Audio/sync_legacy_audio.py --check --client-root E:\\GitProject\\yu_client",
            "```",
            "",
            "`--check` 同时核对 310 个选中资源的哈希、`.meta` GUID/导入配置、ConfigSound、Addressables 条目/标签和两份 JSON 台账。",
            "",
        ]
    )
    return "\n".join(lines)


def replace_addressables(group_text: str, generated_entries: list[str]) -> str:
    marker = "  m_ReadOnly: 0\n  m_Settings:"
    if marker not in group_text:
        raise RuntimeError("Remote_resource.asset tail marker not found")
    head, tail = group_text.split(marker, 1)
    entry_re = re.compile(r"(?ms)^  - m_GUID: .*?(?=^  - m_GUID:|\Z)")
    prefix_end = head.find("  m_SerializeEntries:\n")
    if prefix_end < 0:
        raise RuntimeError("Remote_resource.asset entry marker not found")
    prefix_end += len("  m_SerializeEntries:\n")
    prefix = head[:prefix_end]
    body = head[prefix_end:]
    kept = []
    for match in entry_re.finditer(body):
        block = match.group(0)
        if "m_Address: resource/sound/" in block or "m_Address: resource/config/client/configsound" in block:
            continue
        kept.append(block.rstrip("\n") + "\n")
    return prefix + "".join(kept) + "".join(generated_entries) + marker + tail


def addressable_entry(guid: str, address: str, label: str) -> str:
    return f"""  - m_GUID: {guid}
    m_Address: {address}
    m_ReadOnly: 0
    m_SerializedLabels:
    - {label}
    FlaggedDuringContentUpdateRestriction: 0
"""


def ensure_setting_labels(settings_text: str) -> str:
    anchor = "    - pack_resource_config\n"
    if anchor not in settings_text:
        raise RuntimeError("Addressable settings pack_resource_config label not found")
    missing = [label for label in SOUND_LABELS if f"    - {label}\n" not in settings_text]
    if not missing:
        return settings_text
    addition = "".join(f"    - {label}\n" for label in missing)
    return settings_text.replace(anchor, anchor + addition, 1)


def verify_guid_collisions(unity_root: Path, owned_paths: set[Path], desired: dict[Path, bytes]) -> None:
    desired_guids: dict[str, Path] = {}
    for path, content in desired.items():
        if path.suffix != ".meta":
            continue
        match = re.search(rb"^guid: ([0-9a-f]{32})$", content, re.MULTILINE)
        if not match:
            continue
        guid = match.group(1).decode("ascii")
        if guid in desired_guids and desired_guids[guid] != path:
            raise RuntimeError(f"generated GUID collision: {path} and {desired_guids[guid]}")
        desired_guids[guid] = path
    # The project contains more than 70k metadata files. Walking them through Python on Windows
    # takes over a minute; let Git scan all existing tracked metadata blobs in one pass.
    tracked = subprocess.run(
        ["git", "grep", "-n", "-E", r"^guid: [0-9a-f]{32}$", "--", "*.meta"],
        cwd=unity_root, stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=False,
    )
    if tracked.returncode not in (0, 1):
        raise RuntimeError("git grep metadata failed: " + tracked.stderr.decode("utf-8", errors="replace"))
    for raw in tracked.stdout.decode("utf-8", errors="replace").splitlines():
        match = re.match(r"^(.+):\d+:guid: ([0-9a-f]{32})$", raw)
        if not match:
            continue
        meta = (unity_root / match.group(1)).resolve()
        guid = match.group(2)
        if meta not in owned_paths and guid in desired_guids:
            raise RuntimeError(f"generated GUID collides with existing file: {meta}")



def build_desired(unity_root: Path, client_root: Path, resources: list[dict], callsites: list[dict]) -> dict[Path, bytes]:
    desired: dict[Path, bytes] = {}
    folder_paths = [Path("Assets/GameRes/resource/sound")]
    folder_paths.extend(Path("Assets/GameRes/resource/sound") / category for category in CATEGORIES)
    for folder in folder_paths:
        asset_path = normalize(folder)
        desired[(unity_root / (asset_path + ".meta")).resolve()] = folder_meta(md5_guid(asset_path)).encode("utf-8")

    for row in resources:
        destination = row["_destinationPath"].resolve()
        desired[destination] = row["_sourcePath"].read_bytes()
        desired[Path(str(destination) + ".meta")] = audio_meta(row["guid"], row["category"]).encode("utf-8")

    config_source = client_root / "cdn/resource/config/client/ConfigSound.json"
    config_asset = Path("Assets/GameRes/resource/config/client/ConfigSound.json")
    config_destination = (unity_root / config_asset).resolve()
    config_guid = md5_guid(normalize(config_asset))
    config_data = json.loads(config_source.read_text(encoding="utf-8"))
    desired[config_destination] = json_text(config_data).encode("utf-8")
    desired[Path(str(config_destination) + ".meta")] = text_meta(config_guid).encode("utf-8")

    resource_manifest = {
        "schemaVersion": SYNC_VERSION,
        "policy": "scene: mp3>ogg>wav; other categories: wav>ogg>mp3",
        "sourceRoot": "yu_client/cdn/resource/sound",
        "logicalCount": len(resources),
        "physicalCount": sum(len(row["candidates"]) for row in resources),
        "resources": [public_resource(row) for row in resources],
    }
    callsite_manifest = {
        "schemaVersion": SYNC_VERSION,
        "sourceRoot": "yu_client/h5/src",
        "total": len(callsites),
        "done": sum(row["status"] == "done" for row in callsites),
        "pending": sum(row["status"] == "pending" for row in callsites),
        "callsites": callsites,
    }
    desired[(unity_root / "Schemas/Audio/audio_manifest.json").resolve()] = json_text(resource_manifest).encode("utf-8")
    desired[(unity_root / "Schemas/Audio/callsite_manifest.json").resolve()] = json_text(callsite_manifest).encode("utf-8")
    desired[(unity_root / "Docs/声音迁移台账.md").resolve()] = render_ledger(resources, callsites).encode("utf-8")

    group_path = unity_root / "Assets/AddressableAssetsData/AssetGroups/Remote_resource.asset"
    group_text = group_path.read_text(encoding="utf-8")
    entries = [addressable_entry(config_guid, "resource/config/client/configsound", "pack_resource_config")]
    entries.extend(addressable_entry(row["guid"], row["address"], row["label"]) for row in resources)
    desired[group_path.resolve()] = replace_addressables(group_text, entries).encode("utf-8")

    settings_path = unity_root / "Assets/AddressableAssetsData/AddressableAssetSettings.asset"
    settings_text = settings_path.read_text(encoding="utf-8")
    desired[settings_path.resolve()] = ensure_setting_labels(settings_text).encode("utf-8")
    return desired


def validate_resource_set(unity_root: Path, resources: list[dict]) -> None:
    sound_root = unity_root / "Assets/GameRes/resource/sound"
    actual = {
        path.resolve()
        for path in sound_root.rglob("*")
        if path.is_file() and path.suffix.lower() in AUDIO_EXTENSIONS
    } if sound_root.exists() else set()
    expected = {row["_destinationPath"].resolve() for row in resources}
    extra = sorted(actual - expected)
    if extra:
        raise RuntimeError("unexpected audio files under managed sound root:\n" + "\n".join(map(str, extra[:20])))


def sync(args: argparse.Namespace) -> int:
    unity_root = Path(args.unity_root).resolve() if args.unity_root else Path(__file__).resolve().parents[2]
    client_root = find_client_root(unity_root, args.client_root)
    resources = collect_resources(client_root, unity_root)
    if len(resources) != 310:
        raise RuntimeError(f"legacy logical sound count changed: expected 310, got {len(resources)}")
    physical = sum(len(row["candidates"]) for row in resources)
    if physical != 676:
        raise RuntimeError(f"legacy physical sound count changed: expected 676, got {physical}")
    callsites = collect_callsites(client_root)
    desired = build_desired(unity_root, client_root, resources, callsites)
    owned_paths = set(desired)
    verify_guid_collisions(unity_root, owned_paths, desired)

    drift: list[str] = []
    for path, content in desired.items():
        if not path.is_file() or path.read_bytes() != content:
            drift.append(str(path.relative_to(unity_root)))
            if not args.check:
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_bytes(content)
    if args.check:
        validate_resource_set(unity_root, resources)
        if drift:
            print("audio migration drift detected:", file=sys.stderr)
            print("\n".join(f"  {path}" for path in drift[:50]), file=sys.stderr)
            return 1
        print(f"audio migration check passed: resources={len(resources)} callsites={len(callsites)}")
        return 0

    validate_resource_set(unity_root, resources)
    print(f"audio migration synchronized: resources={len(resources)} physical={physical} callsites={len(callsites)} changed={len(drift)}")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--unity-root", help="Unity repository root; defaults from this script")
    parser.add_argument("--client-root", help="legacy yu_client root")
    parser.add_argument("--check", action="store_true", help="verify without writing")
    args = parser.parse_args()
    try:
        return sync(args)
    except Exception as exc:  # fail closed with one actionable message
        print(f"audio migration failed: {exc}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
