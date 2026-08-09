#!/usr/bin/env python3
"""Self-tests for the read-only UI route master summary."""

from __future__ import annotations

import hashlib
import json
import tempfile
from pathlib import Path

from ui_route_master import build_master_report, main, render_markdown


def _write_json(path: Path, value: object) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2), encoding="utf-8")


def _digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main_test() -> int:
    with tempfile.TemporaryDirectory() as temporary:
        repo = Path(temporary) / "repo"
        audit = repo / "output" / "ui_route_audit"

        current_dir = audit / "2026-08-09_current"
        current_manifest = current_dir / "route-manifest-v2.json"
        _write_json(
            current_manifest,
            {
                "route": "mainui.role.current.v2",
                "nodes": [
                    {"id": "mainui.role.current", "type": "page"},
                    {"id": "mainui.role.current.leaf", "parent": "mainui.role.current", "type": "read"},
                ],
            },
        )
        current_ledger = current_dir / "route-ledger-v2.json"
        _write_json(
            current_ledger,
            {
                "schema": 6,
                "route": "mainui.role.current.v2",
                "manifest_source": {
                    "path": current_manifest.relative_to(repo).as_posix(),
                    "sha256": _digest(current_manifest),
                },
                "verification_runs": {
                    "static-1": {"environment": "static"},
                    "editor-1": {"environment": "unity-editor"},
                    "web-1": {"environment": "real-web"},
                },
                "nodes": [
                    {
                        "id": "mainui.role.current",
                        "parent": None,
                        "type": "page",
                        "status": "blocked",
                    },
                    {
                        "id": "mainui.role.current.leaf",
                        "parent": "mainui.role.current",
                        "type": "read",
                        "status": "needs-runtime-verify",
                    },
                ],
            },
        )

        historical_dir = audit / "2026-08-04_history"
        historical_manifest = historical_dir / "manifest.json"
        _write_json(
            historical_manifest,
            {"schema": 4, "route": "mainui.role.history", "nodes": [{"id": "history"}]},
        )
        historical_ledger = historical_dir / "role-route-ledger.json"
        _write_json(
            historical_ledger,
            {
                "schema": 4,
                "route": "mainui.role.history",
                "nodes": [{"id": "history", "parent": None, "status": "done"}],
            },
        )

        metadata_manifest = audit / "metadata" / "manifest.json"
        _write_json(metadata_manifest, {"schema": 4, "route": "metadata.only", "logs": []})
        capture_manifest = audit / "capture" / "capture-manifest.json"
        _write_json(capture_manifest, {"schema": 1, "captures": []})
        broken_ledger = audit / "broken" / "route-ledger.json"
        broken_ledger.parent.mkdir(parents=True, exist_ok=True)
        broken_ledger.write_text("{broken", encoding="utf-8")

        source_digests = {
            path: _digest(path)
            for path in (
                current_manifest,
                current_ledger,
                historical_manifest,
                historical_ledger,
                metadata_manifest,
                capture_manifest,
                broken_ledger,
            )
        }
        report = build_master_report(audit, repo)
        assert report["summary"]["ledgers_discovered"] == 3
        assert report["summary"]["ledgers_parsed"] == 2
        assert report["summary"]["ledger_errors"] == 1
        assert report["summary"]["routes_by_boundary"] == {
            "current-schema": 1,
            "historical-read-only": 1,
        }
        assert report["summary"]["nodes_total"] == 3
        assert report["summary"]["verification_environment_run_counts"] == {
            "static": 1,
            "unity-editor": 1,
            "real-web": 1,
        }
        assert report["summary"]["ignored_non_route_manifests"] == 1

        current = next(item for item in report["routes"] if item["schema"] == 6)
        assert current["root"] == {"count": 1, "id": "mainui.role.current", "status": "blocked"}
        assert current["status_counts"] == {"blocked": 1, "needs-runtime-verify": 1}
        assert current["verification"]["environments"] == ["static", "unity-editor", "real-web"]
        assert current["manifest"]["declared"]["exists"] is True
        assert current["manifest"]["declared"]["sha256_matches"] is True
        assert current["issues"] == []

        historical = next(item for item in report["routes"] if item["schema"] == 4)
        assert historical["boundary"]["class"] == "historical-read-only"
        assert historical["boundary"]["completion_claim"] == "historical-snapshot-not-schema6"
        assert historical["root"]["status"] == "done"
        assert historical_manifest.relative_to(repo).as_posix() in historical["manifest"]["linked_paths"]

        markdown = render_markdown(report)
        assert "schema 6 是当前证据合同" in markdown
        assert "historical-read-only" in markdown
        assert "mainui.role.current.v2" in markdown

        json_out = Path(temporary) / "reports" / "ui-route-master.json"
        markdown_out = Path(temporary) / "reports" / "ui-route-master.md"
        assert (
            main(
                [
                    str(audit),
                    "--repo-root",
                    str(repo),
                    "--json-out",
                    str(json_out),
                    "--markdown-out",
                    str(markdown_out),
                ]
            )
            == 0
        )
        written = json.loads(json_out.read_text(encoding="utf-8"))
        assert written["summary"] == report["summary"]
        assert "UI 路由项目总控汇总" in markdown_out.read_text(encoding="utf-8")

        assert (
            main(
                [
                    str(audit),
                    "--repo-root",
                    str(repo),
                    "--json-out",
                    str(current_ledger),
                    "--markdown-out",
                    str(markdown_out),
                ]
            )
            == 2
        )
        for path, digest in source_digests.items():
            assert _digest(path) == digest, f"source artifact changed: {path}"

    print("ui_route_master self-test: PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main_test())
