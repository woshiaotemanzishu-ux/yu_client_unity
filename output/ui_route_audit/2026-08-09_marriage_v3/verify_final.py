from __future__ import annotations

import json
from collections import Counter
from pathlib import Path


OUT = Path(__file__).resolve().parent
REPO = OUT.parents[2]
FIXED_IDS = {
    "marriage.main.mate.ask",
    "marriage.main.mate.again",
    "marriage.main.mate.break",
    "marriage.main.mate.flow",
}
VIEW_NAMES = [
    "MarriageAskView",
    "MarriageFriendView",
    "MarriageGiftView",
    "MarriageMainView",
    "MarriageRingView",
]


def load_json(path: Path) -> object:
    return json.loads(path.read_text(encoding="utf-8"))


manifest: dict = load_json(OUT / "route-manifest.json")
ledger: dict = load_json(OUT / "route-ledger.json")
correction: list[dict] = load_json(OUT / "correction-results-qa-final.json")

assert manifest["route"] == "marriage"
assert ledger["route"] == "marriage"
assert ledger["schema"] == 6
assert len(manifest["nodes"]) == 217
assert len(ledger["nodes"]) == 217
assert Counter(node["status"] for node in ledger["nodes"]) == Counter(
    {"blocked": 196, "needs-runtime-verify": 21}
)

parent_ids = {node["parent"] for node in ledger["nodes"] if node.get("parent")}
leaves = [node for node in ledger["nodes"] if node["id"] not in parent_ids]
assert len(leaves) == 187
assert Counter(node["status"] for node in leaves) == Counter(
    {"blocked": 166, "needs-runtime-verify": 21}
)
assert all(not node.get("runtime_gap") for node in ledger["nodes"] if node["status"] == "blocked")
assert all(node.get("blocked_reason") for node in ledger["nodes"] if node["status"] == "blocked")

by_id = {node["id"]: node for node in ledger["nodes"]}
assert set(item["id"] for item in correction) == FIXED_IDS
for item in correction:
    assert item["status"] == "blocked"
    assert item.get("runtime_gap") is None
    assert item.get("blocked_reason")
for node_id in FIXED_IDS:
    node = by_id[node_id]
    assert node["status"] == "blocked"
    assert node.get("runtime_gap") is None
    assert node.get("blocked_reason")

project = OUT / "view_compile" / "Marriage.Views.StaticCompile.csproj"
project_text = project.read_text(encoding="utf-8")
for view_name in VIEW_NAMES:
    assert f"{view_name}.cs" in project_text
assert "<EnableDefaultCompileItems>false</EnableDefaultCompileItems>" in project_text
assert "<TreatWarningsAsErrors>true</TreatWarningsAsErrors>" in project_text

compiled_dll = OUT / "view_compile" / "bin" / "Debug" / "Marriage.Views.StaticCompile.dll"
assert compiled_dll.is_file()
source_files = [
    REPO / "Assets" / "Scripts" / "Module" / "Core" / "Marriage" / "Views" / f"{name}.cs"
    for name in VIEW_NAMES
]
assert compiled_dll.stat().st_mtime_ns >= max(path.stat().st_mtime_ns for path in source_files)

structure_project = OUT / "compile_harness" / "MarriageStaticHarness.csproj"
structure_program = OUT / "compile_harness" / "Program.cs"
assert structure_project.is_file() and structure_program.is_file()
assert "7 scoped read queries" in structure_program.read_text(encoding="utf-8")

matrix_text = (OUT / "route-matrix.md").read_text(encoding="utf-8")
assert "blocked=166，needs-runtime-verify=21，done=0" in matrix_text
for node_id in FIXED_IDS:
    assert f"| `{node_id}` | blocked |" in matrix_text

print(
    "MARRIAGE_V3_FINAL_VERIFY_PASS "
    "route=marriage schema=6 nodes=217 leaves=187 "
    "leaf_blocked=166 leaf_runtime=21 ledger_blocked=196 ledger_runtime=21 "
    "fixed_blocked=4 blocked_runtime_gap_empty=true compiled_views=5"
)
