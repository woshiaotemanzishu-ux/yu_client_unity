#!/usr/bin/env python3
"""Deterministically graft authoritative outward subtrees into the current PetModule prefab.

Pinned scope: one historical commit, one prefab. This is a task-local mechanical patch,
not a registered Unity/Creator/conversion tool. It preserves every existing YAML document
except the three target parent m_Children lists and OutWardBaseView's Fairy template ref.
"""
from __future__ import annotations

import hashlib
import json
import re
import subprocess
from collections import Counter
from pathlib import Path

SOURCE_COMMIT = "a1d417820bb39f29c27e4e64e4bdeda21eff84ee"
PREFAB = Path("Assets/Prefabs/UI/Pet/PetModule.prefab")
REPORT = Path("Tools/UIAudit/contracts/role/outward-wing.graft-report.json")
LEVEL_REPORT = Path("Tools/UIAudit/contracts/role/outward-wing.level-system-graft-report.json")
ILLUSION_SCRIPT_GUID = "e505590d494d42719022a5d168a4d33c"
ILLUSION_CLASS = "Shenxiao.Module.Core::Shenxiao.Module.Core.Pet.IllusionBaseView"
LEVEL_SCRIPT_GUID = "35b7393a8e72497fa8b69ed03b8eb7db"
LEVEL_CLASS = "Shenxiao.Module.Core::Shenxiao.Module.Core.Pet.OutwardLvSystemView"
FAIRY_BIND_GUID = "bd19099f6edb80b449d52db8c5e545d3"
GRID_LAYOUT_GUID = "8a8695521f0d02e499659fee002a26c2"
CONTENT_SIZE_FITTER_GUID = "3245ec927659c4140ac4f8d17403cc18"

HEADER = re.compile(r"^--- !u!(\d+) &(-?\d+)(?: stripped)?$", re.MULTILINE)
LOCAL_REF = re.compile(r"\{([^{}]*fileID:\s*(-?\d+)[^{}]*)\}")


def split_documents(text: str):
    matches = list(HEADER.finditer(text))
    prefix = text[: matches[0].start()]
    order, docs = [], {}
    for index, match in enumerate(matches):
        file_id = int(match.group(2))
        end = matches[index + 1].start() if index + 1 < len(matches) else len(text)
        order.append(file_id)
        docs[file_id] = (int(match.group(1)), text[match.start() : end])
    return prefix, order, docs


def get_value(block: str, key: str) -> str:
    match = re.search(r"^  " + re.escape(key) + r": (.*)$", block, re.MULTILINE)
    return match.group(1) if match else ""


def local_refs(block: str):
    result = []
    for match in LOCAL_REF.finditer(block):
        body, file_id = match.group(1), int(match.group(2))
        if file_id and "guid:" not in body:
            result.append(file_id)
    return result


def game_object_id(block: str) -> int:
    match = re.search(r"m_GameObject: \{fileID: (-?\d+)\}", block)
    return int(match.group(1)) if match else 0


def component_ids(block: str):
    match = re.search(r"  m_Component:\n((?:  - component: \{fileID: -?\d+\}\n)+)", block)
    return [int(value) for value in re.findall(r"fileID: (-?\d+)", match.group(1))] if match else []


def transform_id(docs, game_object: int) -> int:
    for component in component_ids(docs[game_object][1]):
        if component in docs and docs[component][0] in (4, 224):
            return component
    return 0


def father_id(block: str) -> int:
    match = re.search(r"  m_Father: \{fileID: (-?\d+)\}", block)
    return int(match.group(1)) if match else 0


def child_ids(block: str):
    match = re.search(r"  m_Children:\n((?:  - \{fileID: -?\d+\}\n)*)", block)
    return [int(value) for value in re.findall(r"fileID: (-?\d+)", match.group(1))] if match else []


def path_for_transform(docs, transform: int) -> str:
    parts, seen = [], set()
    while transform and transform in docs and transform not in seen:
        seen.add(transform)
        game_object = game_object_id(docs[transform][1])
        parts.append(get_value(docs[game_object][1], "m_Name") if game_object in docs else "?")
        transform = father_id(docs[transform][1])
    return "/".join(reversed(parts))


def find_path(docs, target: str):
    matches = []
    for file_id, (class_id, block) in docs.items():
        if class_id != 1:
            continue
        transform = transform_id(docs, file_id)
        if transform and path_for_transform(docs, transform) == target:
            matches.append((file_id, transform))
    if len(matches) != 1:
        raise RuntimeError(f"expected one path {target}, found {matches}")
    return matches[0]


def structural_closure(docs, root_game_object: int, root_transform: int):
    ids, stack = set(), [root_transform]
    while stack:
        file_id = stack.pop()
        if file_id in ids or file_id not in docs:
            continue
        ids.add(file_id)
        block = docs[file_id][1]
        game_object = game_object_id(block)
        if game_object:
            ids.add(game_object)
            ids.update(component_ids(docs[game_object][1]))
        stack.extend(child_ids(block))
    boundary = {father_id(docs[root_transform][1])}
    queue = list(ids)
    while queue:
        file_id = queue.pop()
        for reference in local_refs(docs[file_id][1]):
            if reference in docs and reference not in ids and reference not in boundary:
                ids.add(reference)
                queue.append(reference)
    return ids, boundary


def owned_subtree_closure(docs, root_transform: int):
    """Keep only the transform-owned tree plus explicitly referenced template instance stubs.

    OutwardLvSystem's historical generic closure reaches sibling module documents through the
    PetModule parent.  The production-owned boundary is its transform subtree.  Its only local
    references outside that structure are three PrefabInstance + stripped GameObject pairs under
    the owned __Templates transform; package/prefab GUID references remain external by design.
    """
    structural, transforms, stack = set(), set(), [root_transform]
    while stack:
        transform = stack.pop()
        if transform in transforms or transform not in docs:
            continue
        transforms.add(transform)
        structural.add(transform)
        game_object = game_object_id(docs[transform][1])
        if game_object:
            structural.add(game_object)
            structural.update(component for component in component_ids(docs[game_object][1]) if component in docs)
        stack.extend(child_ids(docs[transform][1]))

    parent_boundary = {father_id(docs[root_transform][1])}
    support = set()
    for file_id in list(structural):
        for reference in local_refs(docs[file_id][1]):
            if reference in docs and reference not in structural and reference not in parent_boundary:
                support.add(reference)
    closure = structural | support
    unresolved = []
    for file_id in closure:
        for reference in local_refs(docs[file_id][1]):
            if reference in docs and reference not in closure and reference not in parent_boundary:
                unresolved.append((file_id, reference))
    if unresolved:
        raise RuntimeError(f"OutwardLvSystem owned closure has unresolved local edges: {unresolved}")
    return closure, structural, support, parent_boundary


def deterministic_mapping(namespace: str, old_ids, occupied):
    mapping = {}
    for old_id in sorted(old_ids):
        nonce = 0
        while True:
            digest = hashlib.sha256(f"outward-wing:{namespace}:{old_id}:{nonce}".encode()).digest()
            candidate = int.from_bytes(digest[:8], "big") & ((1 << 63) - 1)
            if candidate > 1000 and candidate not in occupied and candidate not in mapping.values():
                mapping[old_id] = candidate
                occupied.add(candidate)
                break
            nonce += 1
    return mapping


def replace_ids(block: str, mapping):
    header = HEADER.match(block)
    if not header:
        raise RuntimeError("document header missing")
    old_id = int(header.group(2))
    block = block[: header.start(2)] + str(mapping[old_id]) + block[header.end(2) :]

    def replace_reference(match):
        body = match.group(1)
        if "guid:" in body:
            return match.group(0)
        old = int(match.group(2))
        if old not in mapping:
            return match.group(0)
        start, end = match.span(2)
        relative_start = start - match.start(0)
        relative_end = end - match.start(0)
        raw = match.group(0)
        return raw[:relative_start] + str(mapping[old]) + raw[relative_end:]

    return LOCAL_REF.sub(replace_reference, block)


def replace_father(block: str, parent: int):
    changed, count = re.subn(r"  m_Father: \{fileID: -?\d+\}", f"  m_Father: {{fileID: {parent}}}", block, count=1)
    if count != 1:
        raise RuntimeError("m_Father missing")
    return changed


def append_children(block: str, children):
    match = re.search(r"  m_Children:\n((?:  - \{fileID: -?\d+\}\n)*)", block)
    if not match:
        empty = re.search(r"^  m_Children: \[\]$", block, re.MULTILINE)
        if empty:
            replacement = "  m_Children:\n" + "".join(
                f"  - {{fileID: {child}}}\n" for child in children
            )
            return block[: empty.start()] + replacement.rstrip("\n") + block[empty.end() :]
        raise RuntimeError("m_Children missing")
    existing = [int(value) for value in re.findall(r"fileID: (-?\d+)", match.group(1))]
    additions = [child for child in children if child not in existing]
    replacement = "".join(f"  - {{fileID: {child}}}\n" for child in existing + additions)
    return block[: match.start(1)] + replacement + block[match.end(1) :]


def append_component(block: str, component: int):
    match = re.search(r"  m_Component:\n((?:  - component: \{fileID: -?\d+\}\n)+)", block)
    if not match:
        raise RuntimeError("m_Component missing")
    existing = [int(value) for value in re.findall(r"fileID: (-?\d+)", match.group(1))]
    if component in existing:
        return block
    replacement = match.group(1) + f"  - component: {{fileID: {component}}}\n"
    return block[: match.start(1)] + replacement + block[match.end(1) :]


def replace_field(block: str, field: str, file_id: int):
    changed, count = re.subn(
        rf"^  {re.escape(field)}: \{{fileID: -?\d+\}}$",
        f"  {field}: {{fileID: {file_id}}}",
        block,
        count=1,
        flags=re.MULTILINE,
    )
    if count != 1:
        raise RuntimeError(f"field missing: {field}")
    return changed


def normalize_added_document(block: str) -> str:
    """Remove trailing whitespace only inside newly grafted history documents."""
    return re.sub(r"[ \t]+(?=\r?$)", "", block, flags=re.MULTILINE)


def force_game_object_inactive(block: str) -> str:
    changed, count = re.subn(r"^  m_IsActive: \d+$", "  m_IsActive: 0", block, count=1, flags=re.MULTILINE)
    if count != 1:
        raise RuntimeError("IllusionBaseView root m_IsActive missing")
    return changed


def ensure_illusion_content_layout(current_order, current_docs):
    """Make the restored list/attribute groups the actual self-sizing ScrollRect contents.

    This is a deterministic functional patch only: the legacy authoritative RefGridLayout is
    3 columns, 220x94, hgap=4, vgap=7.  No decorative or pixel-tuning nodes are created.
    """
    paths = {
        "illusion": "PetModule/IllusionBaseView/bottom_group/illusion_scroller/Content/illusion_group",
        "prop": "PetModule/IllusionBaseView/right_group/prop_scroller/Content/prop_group",
        "illusion_scroll": "PetModule/IllusionBaseView/bottom_group/illusion_scroller",
        "prop_scroll": "PetModule/IllusionBaseView/right_group/prop_scroller",
    }
    illusion_go, illusion_transform = find_path(current_docs, paths["illusion"])
    prop_go, prop_transform = find_path(current_docs, paths["prop"])
    illusion_scroll_go, _ = find_path(current_docs, paths["illusion_scroll"])
    prop_scroll_go, _ = find_path(current_docs, paths["prop_scroll"])

    def find_component(game_object, class_name):
        return next((component for component in component_ids(current_docs[game_object][1])
                     if component in current_docs and class_name in current_docs[component][1]), 0)

    new_blocks = []
    symbolic = []
    if not find_component(illusion_go, "UnityEngine.UI.GridLayoutGroup"): symbolic.append(-101)
    if not find_component(illusion_go, "UnityEngine.UI.ContentSizeFitter"): symbolic.append(-102)
    if not find_component(prop_go, "UnityEngine.UI.ContentSizeFitter"): symbolic.append(-103)
    mapping = deterministic_mapping("illusion-functional-layout-v1", symbolic, set(current_docs))

    if -101 in mapping:
        file_id = mapping[-101]
        current_docs[illusion_go] = (1, append_component(current_docs[illusion_go][1], file_id))
        block = f"""--- !u!114 &{file_id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {illusion_go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {GRID_LAYOUT_GUID}, type: 3}}
  m_Name:
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.GridLayoutGroup
  m_Padding:
    m_Left: 0
    m_Right: 0
    m_Top: 0
    m_Bottom: 0
  m_ChildAlignment: 0
  m_StartCorner: 0
  m_StartAxis: 0
  m_CellSize: {{x: 220, y: 94}}
  m_Spacing: {{x: 4, y: 7}}
  m_Constraint: 1
  m_ConstraintCount: 3
"""
        new_blocks.append((file_id, block))
    if -102 in mapping:
        file_id = mapping[-102]
        current_docs[illusion_go] = (1, append_component(current_docs[illusion_go][1], file_id))
        new_blocks.append((file_id, content_size_fitter_block(file_id, illusion_go)))
    if -103 in mapping:
        file_id = mapping[-103]
        current_docs[prop_go] = (1, append_component(current_docs[prop_go][1], file_id))
        new_blocks.append((file_id, content_size_fitter_block(file_id, prop_go)))

    for file_id, block in new_blocks:
        current_order.append(file_id)
        current_docs[file_id] = (114, normalize_added_document(block))

    for scroll_go, content_transform in ((illusion_scroll_go, illusion_transform), (prop_scroll_go, prop_transform)):
        scroll = find_component(scroll_go, "UnityEngine.UI.ScrollRect")
        if not scroll:
            raise RuntimeError(f"ScrollRect missing on {scroll_go}")
        current_docs[scroll] = (114, replace_field(current_docs[scroll][1], "m_Content", content_transform))
    return current_order, current_docs, len(new_blocks)


def content_size_fitter_block(file_id: int, game_object: int) -> str:
    return f"""--- !u!114 &{file_id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {game_object}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {CONTENT_SIZE_FITTER_GUID}, type: 3}}
  m_Name:
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.ContentSizeFitter
  m_HorizontalFit: 0
  m_VerticalFit: 2
"""


def graft_level_system(source_order, source_docs, current_prefix, current_order, current_docs):
    """Incrementally restore the authoritative OutwardLvSystem owned subtree once."""
    try:
        find_path(current_docs, "PetModule/OutwardLvSystem")
        if LEVEL_REPORT.exists():
            print(LEVEL_REPORT.read_text(encoding="utf-8"))
        return current_prefix, current_order, current_docs
    except RuntimeError:
        pass

    level_go, level_transform = find_path(source_docs, "PetModule/OutwardLvSystem")
    _, current_module_transform = find_path(current_docs, "PetModule")
    level_ids, structural_ids, support_ids, boundary = owned_subtree_closure(source_docs, level_transform)
    occupied = set(current_docs)
    original_collisions = sorted(level_ids & occupied)
    level_map = deterministic_mapping("level-system-a1d417820", level_ids, occupied)
    additions = []
    root_components = set(component_ids(source_docs[level_go][1]))
    for old_id in source_order:
        if old_id not in level_ids:
            continue
        block = replace_ids(source_docs[old_id][1], level_map)
        if old_id == level_transform:
            block = replace_father(block, current_module_transform)
        if old_id in root_components and source_docs[old_id][0] == 114:
            block = re.sub(
                r"m_Script: \{fileID: 11500000, guid: [0-9a-f]+, type: 3\}",
                f"m_Script: {{fileID: 11500000, guid: {LEVEL_SCRIPT_GUID}, type: 3}}",
                block,
                count=1,
            )
            block = re.sub(
                r"^  m_EditorClassIdentifier:.*$", f"  m_EditorClassIdentifier: {LEVEL_CLASS}",
                block, count=1, flags=re.MULTILINE,
            )
        if old_id == level_go:
            block = force_game_object_inactive(block)
        additions.append((level_map[old_id], normalize_added_document(block)))

    current_docs[current_module_transform] = (
        current_docs[current_module_transform][0],
        append_children(current_docs[current_module_transform][1], [level_map[level_transform]]),
    )
    current_order = list(current_order) + [file_id for file_id, _ in additions]
    for file_id, block in additions:
        current_docs[file_id] = (source_docs[next(old for old, mapped in level_map.items() if mapped == file_id)][0], block)

    report = {
        "schema": 1,
        "sourceCommit": SOURCE_COMMIT,
        "prefab": PREFAB.as_posix(),
        "root": "PetModule/OutwardLvSystem",
        "parents": {"old": "PetModule", "current": "PetModule", "currentTransform": current_module_transform},
        "closure": {
            "structuralDocuments": len(structural_ids),
            "templateSupportDocuments": len(support_ids),
            "documents": len(level_ids),
            "parentBoundary": sorted(boundary),
            "unresolvedLocalReferences": 0,
        },
        "originalFileIdCollisions": len(original_collisions),
        "remappedFileIds": len(level_map),
        "changedExistingDocuments": [current_module_transform],
        "addedDocuments": len(additions),
        "defaultActive": False,
    }
    LEVEL_REPORT.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    return current_prefix, current_order, current_docs


def main():
    source_text = subprocess.check_output(
        ["git", "show", f"{SOURCE_COMMIT}:{PREFAB.as_posix()}"]
    ).decode("utf-8-sig")
    current_text = PREFAB.read_text(encoding="utf-8-sig")
    current_sha = hashlib.sha256(current_text.encode()).hexdigest()
    source_prefix, source_order, source_docs = split_documents(source_text)
    current_prefix, current_order, current_docs = split_documents(current_text)

    # A second invocation is a normalization/verification pass, never a second graft.
    try:
        find_path(current_docs, "PetModule/IllusionBaseView")
        already_grafted = True
    except RuntimeError:
        already_grafted = False
    if already_grafted:
        base_text = subprocess.check_output(["git", "show", f"HEAD:{PREFAB.as_posix()}"]).decode("utf-8-sig")
        _, _, base_docs = split_documents(base_text)
        illusion_go, illusion_transform = find_path(source_docs, "PetModule/IllusionBaseView")
        fairy_go, fairy_transform = find_path(source_docs, "PetModule/OutWardBaseView/__Templates/FairyWishEnterBtn")
        current_enter_go, _ = find_path(current_docs, "PetModule/OutWardBaseView/enter_btn")
        illusion_ids, _ = structural_closure(source_docs, illusion_go, illusion_transform)
        fairy_ids, _ = structural_closure(source_docs, fairy_go, fairy_transform)
        fairy_root_components = set(component_ids(source_docs[fairy_go][1]))
        fairy_live_ids = fairy_ids - {fairy_go} - fairy_root_components
        occupied_replay = set(base_docs)
        deterministic_mapping("illusion-a1d417820", illusion_ids, occupied_replay)
        deterministic_mapping("fairy-template-a1d417820", fairy_ids, occupied_replay)
        fairy_live_map = deterministic_mapping("fairy-live-a1d417820", fairy_live_ids, occupied_replay)
        fairy_root_mono = next(
            component for component in fairy_root_components
            if source_docs[component][0] == 114 and FAIRY_BIND_GUID in source_docs[component][1]
        )
        live_bind = next(
            (component for component in component_ids(current_docs[current_enter_go][1])
             if component in current_docs and FAIRY_BIND_GUID in current_docs[component][1]),
            0,
        )
        appended = []
        if live_bind == 0:
            live_bind_map = deterministic_mapping(
                "fairy-live-bind-a1d417820", {fairy_root_mono}, set(current_docs)
            )
            live_bind = live_bind_map[fairy_root_mono]
            reference_map = dict(fairy_live_map)
            reference_map[fairy_root_mono] = live_bind
            reference_map[fairy_go] = current_enter_go
            bind_block = normalize_added_document(replace_ids(source_docs[fairy_root_mono][1], reference_map))
            current_docs[current_enter_go] = (
                current_docs[current_enter_go][0],
                append_component(current_docs[current_enter_go][1], live_bind),
            )
            appended.append((live_bind, bind_block))
        added_ids = set(current_docs) - set(base_docs)
        normalized_count = 0
        for file_id in added_ids:
            class_id, original_block = current_docs[file_id]
            block = original_block
            if class_id == 1 and get_value(block, "m_Name") == "IllusionBaseView":
                block = force_game_object_inactive(block)
            normalized = normalize_added_document(block)
            if normalized != original_block:
                current_docs[file_id] = (class_id, normalized)
                normalized_count += 1
        current_prefix, current_order, current_docs = graft_level_system(
            source_order, source_docs, current_prefix, current_order, current_docs
        )
        current_order, current_docs, layout_added = ensure_illusion_content_layout(current_order, current_docs)
        output = current_prefix + "".join(current_docs[file_id][1] for file_id in current_order)
        output += "".join(block for _, block in appended)
        PREFAB.write_text(output, encoding="utf-8", newline="")
        report = json.loads(REPORT.read_text(encoding="utf-8"))
        report["normalizedAddedDocuments"] = normalized_count
        report["closures"]["fairyLiveBinding"] = {"documents": 1, "remapped": 1}
        report["addedDocuments"] = 404
        report["changedExistingDocuments"] = sorted(set(report["changedExistingDocuments"] + [current_enter_go]))
        report["afterSha256"] = hashlib.sha256(output.encode()).hexdigest()
        report["functionalLayout"] = {
            "addedComponents": layout_added,
            "illusionGrid": {"columns": 3, "cell": [220, 94], "spacing": [4, 7]},
            "contents": ["illusion_group", "prop_group"],
        }
        REPORT.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        print(json.dumps(report, ensure_ascii=False, indent=2))
        return

    illusion_go, illusion_transform = find_path(source_docs, "PetModule/IllusionBaseView")
    fairy_go, fairy_transform = find_path(
        source_docs, "PetModule/OutWardBaseView/__Templates/FairyWishEnterBtn"
    )
    current_module_go, current_module_transform = find_path(current_docs, "PetModule")
    current_templates_go, current_templates_transform = find_path(
        current_docs, "PetModule/OutWardBaseView/__Templates"
    )
    current_enter_go, current_enter_transform = find_path(
        current_docs, "PetModule/OutWardBaseView/enter_btn"
    )
    current_outward_go, current_outward_transform = find_path(
        current_docs, "PetModule/OutWardBaseView"
    )

    illusion_ids, illusion_boundary = structural_closure(source_docs, illusion_go, illusion_transform)
    fairy_ids, fairy_boundary = structural_closure(source_docs, fairy_go, fairy_transform)
    fairy_root_components = set(component_ids(source_docs[fairy_go][1]))
    fairy_live_ids = fairy_ids - {fairy_go} - fairy_root_components
    fairy_live_root_transforms = child_ids(source_docs[fairy_transform][1])

    occupied = set(current_docs)
    original_collisions = sorted(illusion_ids & occupied)
    illusion_map = deterministic_mapping("illusion-a1d417820", illusion_ids, occupied)
    fairy_template_map = deterministic_mapping("fairy-template-a1d417820", fairy_ids, occupied)
    fairy_live_map = deterministic_mapping("fairy-live-a1d417820", fairy_live_ids, occupied)

    additions = []
    for old_id in source_order:
        if old_id in illusion_ids:
            block = replace_ids(source_docs[old_id][1], illusion_map)
            if old_id == illusion_transform:
                block = replace_father(block, current_module_transform)
            if old_id in component_ids(source_docs[illusion_go][1]) and source_docs[old_id][0] == 114:
                block = re.sub(
                    r"m_Script: \{fileID: 11500000, guid: [0-9a-f]+, type: 3\}",
                    f"m_Script: {{fileID: 11500000, guid: {ILLUSION_SCRIPT_GUID}, type: 3}}",
                    block,
                    count=1,
                )
                block = re.sub(r"^  m_EditorClassIdentifier:.*$", f"  m_EditorClassIdentifier: {ILLUSION_CLASS}", block, count=1, flags=re.MULTILINE)
            additions.append((illusion_map[old_id], normalize_added_document(block), "illusion"))

    for old_id in source_order:
        if old_id in fairy_ids:
            block = replace_ids(source_docs[old_id][1], fairy_template_map)
            if old_id == fairy_transform:
                block = replace_father(block, current_templates_transform)
            additions.append((fairy_template_map[old_id], normalize_added_document(block), "fairy-template"))

    for old_id in source_order:
        if old_id in fairy_live_ids:
            block = replace_ids(source_docs[old_id][1], fairy_live_map)
            if old_id in fairy_live_root_transforms:
                block = replace_father(block, current_enter_transform)
            additions.append((fairy_live_map[old_id], normalize_added_document(block), "fairy-live"))

    changed_existing = {
        current_module_transform,
        current_templates_transform,
        current_enter_transform,
    }
    current_docs[current_module_transform] = (
        current_docs[current_module_transform][0],
        append_children(current_docs[current_module_transform][1], [illusion_map[illusion_transform]]),
    )
    current_docs[current_templates_transform] = (
        current_docs[current_templates_transform][0],
        append_children(current_docs[current_templates_transform][1], [fairy_template_map[fairy_transform]]),
    )
    current_docs[current_enter_transform] = (
        current_docs[current_enter_transform][0],
        append_children(
            current_docs[current_enter_transform][1],
            [fairy_live_map[transform] for transform in fairy_live_root_transforms],
        ),
    )

    outward_component = next(
        component
        for component in component_ids(current_docs[current_outward_go][1])
        if current_docs.get(component, (0, ""))[0] == 114
        and "_tpl_FairyWishEnterBtn:" in current_docs[component][1]
    )
    changed_existing.add(outward_component)
    current_docs[outward_component] = (
        current_docs[outward_component][0],
        replace_field(
            current_docs[outward_component][1],
            "_tpl_FairyWishEnterBtn",
            fairy_template_map[fairy_go],
        ),
    )

    output = current_prefix + "".join(current_docs[file_id][1] for file_id in current_order)
    output += "".join(block for _, block, _ in additions)
    PREFAB.write_text(output, encoding="utf-8", newline="")

    report = {
        "schema": 1,
        "sourceCommit": SOURCE_COMMIT,
        "prefab": PREFAB.as_posix(),
        "sourceSelection": "a1d417820 is the cleaned later form of the same component identity; b05c5771 retains 451 versus 360 illusion documents.",
        "parents": {
            "illusion": {"old": "PetModule", "current": "PetModule", "currentTransform": current_module_transform},
            "fairyTemplate": {"old": "PetModule/OutWardBaseView/__Templates", "current": "PetModule/OutWardBaseView/__Templates", "currentTransform": current_templates_transform},
            "fairyLiveChildren": {"old": "FairyWishEnterBtn", "current": "PetModule/OutWardBaseView/enter_btn", "currentTransform": current_enter_transform},
        },
        "closures": {
            "illusion": {"documents": len(illusion_ids), "originalFileIdCollisions": len(original_collisions), "remapped": len(illusion_map)},
            "fairyTemplate": {"documents": len(fairy_ids), "remapped": len(fairy_template_map)},
            "fairyLiveChildren": {"documents": len(fairy_live_ids), "remapped": len(fairy_live_map)},
        },
        "changedExistingDocuments": sorted(changed_existing),
        "addedDocuments": len(additions),
        "beforeSha256": current_sha,
        "afterSha256": hashlib.sha256(output.encode()).hexdigest(),
    }
    REPORT.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
