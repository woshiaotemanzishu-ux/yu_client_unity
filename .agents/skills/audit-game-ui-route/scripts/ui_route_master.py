#!/usr/bin/env python3
"""Build a read-only project-level summary of UI route ledgers.

The route ledgers remain the source of truth.  This script never imports the
ledger writer and never changes a ledger or manifest; it only writes the two
explicit report destinations supplied by the caller.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import tempfile
from collections import Counter, defaultdict
from pathlib import Path
from typing import Any, Iterable


CURRENT_ROUTE_SCHEMA = 6
HISTORICAL_ROUTE_SCHEMAS = {2, 3, 4, 5}
STATUS_ORDER = (
    "done",
    "blocked",
    "needs-runtime-verify",
    "fixing",
    "defect",
    "baseline-only",
    "not-run",
)
ENVIRONMENT_ORDER = ("static", "unity-editor", "real-web", "user-runtime")


def _is_int(value: object) -> bool:
    return isinstance(value, int) and not isinstance(value, bool)


def _display_path(path: Path, repo_root: Path) -> str:
    resolved = path.resolve()
    try:
        return resolved.relative_to(repo_root.resolve()).as_posix()
    except ValueError:
        return resolved.as_posix()


def _ordered_values(values: Iterable[str], preferred: tuple[str, ...]) -> list[str]:
    value_set = set(values)
    return [item for item in preferred if item in value_set] + sorted(value_set - set(preferred))


def _ordered_counts(counter: Counter[str], preferred: tuple[str, ...]) -> dict[str, int]:
    keys = _ordered_values(counter.keys(), preferred)
    return {key: counter[key] for key in keys}


def _sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def _load_json(path: Path) -> tuple[Any | None, str | None]:
    try:
        return json.loads(path.read_text(encoding="utf-8-sig")), None
    except (OSError, UnicodeError, json.JSONDecodeError) as error:
        return None, str(error)


def _boundary_for_schema(schema: object) -> dict[str, object]:
    if schema == CURRENT_ROUTE_SCHEMA:
        return {
            "class": "current-schema",
            "write_policy": "route_ledger_init_apply_only",
            "completion_claim": "current-evidence-contract",
        }
    if _is_int(schema) and schema in HISTORICAL_ROUTE_SCHEMAS:
        return {
            "class": "historical-read-only",
            "write_policy": "do-not-update-in-place",
            "completion_claim": "historical-snapshot-not-schema6",
        }
    return {
        "class": "unsupported-or-invalid",
        "write_policy": "do-not-update",
        "completion_claim": "unclassified",
    }


def _manifest_path_from_reference(reference: object, repo_root: Path) -> Path | None:
    if not isinstance(reference, dict):
        return None
    raw_path = reference.get("path")
    if not isinstance(raw_path, str) or not raw_path.strip():
        return None
    path = Path(raw_path)
    return path if path.is_absolute() else repo_root / path


def _discover_json_files(audit_root: Path) -> tuple[list[Path], list[Path]]:
    ledgers: list[Path] = []
    manifests: list[Path] = []
    for path in audit_root.rglob("*.json"):
        name = path.name.lower()
        if "route-ledger" in name:
            ledgers.append(path)
        elif "manifest" in name:
            manifests.append(path)
    return sorted(ledgers), sorted(manifests)


def _collect_manifests(
    paths: list[Path], repo_root: Path
) -> tuple[list[dict[str, object]], dict[str, list[str]], list[dict[str, str]], list[str]]:
    records: list[dict[str, object]] = []
    by_route: dict[str, list[str]] = defaultdict(list)
    errors: list[dict[str, str]] = []
    ignored: list[str] = []
    for path in paths:
        display = _display_path(path, repo_root)
        data, error = _load_json(path)
        if error is not None:
            errors.append({"path": display, "error": error})
            continue
        if not isinstance(data, dict) or not isinstance(data.get("route"), str):
            ignored.append(display)
            continue
        nodes = data.get("nodes")
        record = {
            "path": display,
            "route": data["route"],
            "schema": data.get("schema"),
            "kind": "topology" if isinstance(nodes, list) else "route-metadata",
            "node_count": len(nodes) if isinstance(nodes, list) else None,
        }
        records.append(record)
        by_route[data["route"]].append(display)
    for route in by_route:
        by_route[route] = sorted(set(by_route[route]))
    return records, by_route, errors, ignored


def _verification_summary(ledger: dict[str, object]) -> tuple[dict[str, object], Counter[str]]:
    runs = ledger.get("verification_runs")
    if not isinstance(runs, dict):
        runs = {}
    environments: list[str] = []
    environment_counts: Counter[str] = Counter()
    for run in runs.values():
        if not isinstance(run, dict):
            continue
        environment = run.get("environment")
        if isinstance(environment, str) and environment:
            environments.append(environment)
            environment_counts[environment] += 1
    route_run_id = ledger.get("route_run_id")
    route_run_environment = None
    if isinstance(route_run_id, str) and isinstance(runs.get(route_run_id), dict):
        candidate = runs[route_run_id].get("environment")
        if isinstance(candidate, str):
            route_run_environment = candidate
    return (
        {
            "run_count": len(runs),
            "environments": _ordered_values(environments, ENVIRONMENT_ORDER),
            "route_run_id": route_run_id if isinstance(route_run_id, str) else None,
            "route_run_environment": route_run_environment,
        },
        environment_counts,
    )


def _ledger_record(
    path: Path,
    ledger: dict[str, object],
    repo_root: Path,
    manifests_by_route: dict[str, list[str]],
) -> tuple[dict[str, object], Counter[str], Counter[str], list[str]]:
    issues: list[str] = []
    schema = ledger.get("schema")
    route = ledger.get("route")
    route_name = route if isinstance(route, str) and route else None
    nodes = ledger.get("nodes")
    if not isinstance(nodes, list):
        issues.append("nodes is not an array")
        nodes = []

    status_counts: Counter[str] = Counter()
    roots: list[dict[str, object]] = []
    for index, node in enumerate(nodes):
        if not isinstance(node, dict):
            status_counts["<invalid-node>"] += 1
            issues.append(f"nodes[{index}] is not an object")
            continue
        status = node.get("status")
        status_counts[status if isinstance(status, str) and status else "<missing>"] += 1
        if node.get("parent") in (None, ""):
            roots.append(node)
    if len(roots) != 1:
        issues.append(f"expected exactly one root, found {len(roots)}")
    root = roots[0] if len(roots) == 1 else None
    root_status = root.get("status") if isinstance(root, dict) else None
    root_status_counter: Counter[str] = Counter()
    if isinstance(root_status, str):
        root_status_counter[root_status] += 1

    verification, environment_counts = _verification_summary(ledger)
    linked_manifests = list(manifests_by_route.get(route_name, [])) if route_name else []
    declared = None
    reference = ledger.get("manifest_source")
    declared_path = _manifest_path_from_reference(reference, repo_root)
    if isinstance(reference, dict):
        raw_sha = reference.get("sha256")
        declared_display = (
            _display_path(declared_path, repo_root) if declared_path is not None else reference.get("path")
        )
        exists = bool(declared_path is not None and declared_path.is_file())
        actual_sha = _sha256(declared_path) if exists and declared_path is not None else None
        declared = {
            "path": declared_display,
            "sha256": raw_sha if isinstance(raw_sha, str) else None,
            "exists": exists,
            "sha256_matches": (
                actual_sha.lower() == raw_sha.lower()
                if actual_sha is not None and isinstance(raw_sha, str)
                else None
            ),
        }
        if isinstance(declared_display, str) and declared_display not in linked_manifests:
            linked_manifests.append(declared_display)
    if schema == CURRENT_ROUTE_SCHEMA:
        if declared is None:
            issues.append("schema 6 ledger has no manifest_source")
        elif not declared["exists"]:
            issues.append("declared manifest_source does not exist")
        elif declared["sha256_matches"] is False:
            issues.append("declared manifest_source SHA-256 mismatch")

    record = {
        "ledger_path": _display_path(path, repo_root),
        "route": route_name,
        "schema": schema,
        "boundary": _boundary_for_schema(schema),
        "updated_at": ledger.get("updated_at") if isinstance(ledger.get("updated_at"), str) else None,
        "nodes_total": len(nodes),
        "status_counts": _ordered_counts(status_counts, STATUS_ORDER),
        "root": {
            "count": len(roots),
            "id": root.get("id") if isinstance(root, dict) else None,
            "status": root_status if isinstance(root_status, str) else None,
        },
        "verification": verification,
        "manifest": {
            "declared": declared,
            "linked_paths": sorted(set(linked_manifests)),
        },
        "issues": issues,
    }
    return record, environment_counts, root_status_counter, issues


def build_master_report(audit_root: Path, repo_root: Path | None = None) -> dict[str, object]:
    """Read all route artifacts under *audit_root* and return a deterministic report."""

    audit_root = audit_root.resolve()
    if repo_root is None:
        if audit_root.name == "ui_route_audit" and audit_root.parent.name == "output":
            repo_root = audit_root.parent.parent
        else:
            repo_root = Path.cwd()
    repo_root = repo_root.resolve()
    if not audit_root.is_dir():
        raise ValueError(f"audit root does not exist or is not a directory: {audit_root}")

    ledger_paths, manifest_paths = _discover_json_files(audit_root)
    manifests, manifests_by_route, manifest_errors, ignored_manifests = _collect_manifests(
        manifest_paths, repo_root
    )

    routes: list[dict[str, object]] = []
    ledger_errors: list[dict[str, str]] = []
    all_status_counts: Counter[str] = Counter()
    all_root_status_counts: Counter[str] = Counter()
    all_environment_counts: Counter[str] = Counter()
    boundary_counts: Counter[str] = Counter()
    source_files = {_display_path(path, repo_root) for path in ledger_paths + manifest_paths}

    for path in ledger_paths:
        display = _display_path(path, repo_root)
        data, error = _load_json(path)
        if error is not None:
            ledger_errors.append({"path": display, "error": error})
            continue
        if not isinstance(data, dict):
            ledger_errors.append({"path": display, "error": "ledger root is not an object"})
            continue
        record, environments, root_statuses, _ = _ledger_record(
            path, data, repo_root, manifests_by_route
        )
        routes.append(record)
        all_status_counts.update(record["status_counts"])
        all_root_status_counts.update(root_statuses)
        all_environment_counts.update(environments)
        boundary_counts[record["boundary"]["class"]] += 1

    boundary_rank = {"current-schema": 0, "historical-read-only": 1, "unsupported-or-invalid": 2}
    routes.sort(
        key=lambda item: (
            boundary_rank.get(item["boundary"]["class"], 99),
            item["route"] or "",
            item["ledger_path"],
        )
    )
    route_names: Counter[str] = Counter(
        record["route"] for record in routes if isinstance(record["route"], str)
    )
    duplicate_route_names = sorted(name for name, count in route_names.items() if count > 1)

    return {
        "master_schema": 1,
        "source_root": _display_path(audit_root, repo_root),
        "policy": {
            "read_only_sources": True,
            "ledger_validation": "not-performed; this report is inventory, not completion proof",
            "current_route_schema": CURRENT_ROUTE_SCHEMA,
            "historical_route_schemas": sorted(HISTORICAL_ROUTE_SCHEMAS),
        },
        "summary": {
            "ledgers_discovered": len(ledger_paths),
            "ledgers_parsed": len(routes),
            "ledger_errors": len(ledger_errors),
            "manifests_discovered": len(manifest_paths),
            "route_manifests_parsed": len(manifests),
            "ignored_non_route_manifests": len(ignored_manifests),
            "routes_by_boundary": _ordered_counts(
                boundary_counts,
                ("current-schema", "historical-read-only", "unsupported-or-invalid"),
            ),
            "nodes_total": sum(record["nodes_total"] for record in routes),
            "node_status_counts": _ordered_counts(all_status_counts, STATUS_ORDER),
            "root_status_counts": _ordered_counts(all_root_status_counts, STATUS_ORDER),
            "verification_environment_run_counts": _ordered_counts(
                all_environment_counts, ENVIRONMENT_ORDER
            ),
            "duplicate_route_names": duplicate_route_names,
        },
        "routes": routes,
        "manifests": sorted(manifests, key=lambda item: (item["route"], item["path"])),
        "errors": {
            "ledgers": ledger_errors,
            "manifests": manifest_errors,
        },
        "ignored_manifests": sorted(ignored_manifests),
        "source_files": sorted(source_files),
    }


def _escape_markdown(value: object) -> str:
    if value is None:
        return "-"
    return str(value).replace("|", "\\|").replace("\n", " ")


def _compact_counts(counts: dict[str, int]) -> str:
    return ", ".join(f"{key}={value}" for key, value in counts.items()) or "-"


def render_markdown(report: dict[str, object]) -> str:
    summary = report["summary"]
    lines = [
        "# UI 路由项目总控汇总",
        "",
        f"- 扫描根目录：`{report['source_root']}`",
        f"- 台账：发现 {summary['ledgers_discovered']}，成功读取 {summary['ledgers_parsed']}，错误 {summary['ledger_errors']}",
        f"- 节点：{summary['nodes_total']}（{_compact_counts(summary['node_status_counts'])}）",
        f"- 边界：{_compact_counts(summary['routes_by_boundary'])}",
        "- 口径：schema 6 是当前证据合同；schema 2～5 仅为历史只读快照。根状态为 done 也不能跨越该边界升级为当前完成。",
        "- 注意：本报告只做清单汇总，不执行正式台账校验，也不会写回任何 route-ledger/manifest。",
        "",
        "| 边界 | 路线 | Schema | 根状态 | 节点状态 | 验证环境 | 台账 | Manifest |",
        "|---|---|---:|---|---|---|---|---|",
    ]
    for route in report["routes"]:
        manifest_paths = route["manifest"]["linked_paths"]
        lines.append(
            "| "
            + " | ".join(
                _escape_markdown(value)
                for value in (
                    route["boundary"]["class"],
                    route["route"],
                    route["schema"],
                    route["root"]["status"],
                    _compact_counts(route["status_counts"]),
                    ", ".join(route["verification"]["environments"]) or "-",
                    f"`{route['ledger_path']}`",
                    "<br>".join(f"`{path}`" for path in manifest_paths) or "-",
                )
            )
            + " |"
        )

    issue_rows = [route for route in report["routes"] if route["issues"]]
    ledger_errors = report["errors"]["ledgers"]
    manifest_errors = report["errors"]["manifests"]
    if issue_rows or ledger_errors or manifest_errors:
        lines.extend(["", "## 读取问题", ""])
        for route in issue_rows:
            lines.append(f"- `{route['ledger_path']}`：{'；'.join(route['issues'])}")
        for error in ledger_errors:
            lines.append(f"- `{error['path']}`：{error['error']}")
        for error in manifest_errors:
            lines.append(f"- `{error['path']}`：{error['error']}")
    return "\n".join(lines) + "\n"


def _atomic_write(path: Path, content: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    descriptor, temporary_name = tempfile.mkstemp(prefix=f".{path.name}.", dir=str(path.parent))
    temporary = Path(temporary_name)
    try:
        with os.fdopen(descriptor, "w", encoding="utf-8", newline="\n") as handle:
            handle.write(content)
            handle.flush()
            os.fsync(handle.fileno())
        os.replace(temporary, path)
    finally:
        if temporary.exists():
            temporary.unlink()


def write_reports(report: dict[str, object], json_out: Path, markdown_out: Path, repo_root: Path) -> None:
    json_out = json_out.resolve()
    markdown_out = markdown_out.resolve()
    if json_out == markdown_out:
        raise ValueError("JSON and Markdown output paths must be different")
    protected = {
        (repo_root / path).resolve() if not Path(path).is_absolute() else Path(path).resolve()
        for path in report["source_files"]
    }
    for output in (json_out, markdown_out):
        if output in protected:
            raise ValueError(f"refusing to overwrite source route artifact: {output}")
    json_text = json.dumps(report, ensure_ascii=False, indent=2) + "\n"
    markdown_text = render_markdown(report)
    _atomic_write(json_out, json_text)
    _atomic_write(markdown_out, markdown_text)


def _parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "audit_root",
        nargs="?",
        default="output/ui_route_audit",
        help="directory recursively containing route ledgers and manifests",
    )
    parser.add_argument("--repo-root", default=None, help="base used to resolve and display artifact paths")
    parser.add_argument("--json-out", required=True, help="machine-readable report destination")
    parser.add_argument("--markdown-out", required=True, help="concise Markdown report destination")
    return parser


def main(argv: list[str] | None = None) -> int:
    args = _parser().parse_args(argv)
    audit_root = Path(args.audit_root)
    repo_root = Path(args.repo_root).resolve() if args.repo_root else None
    try:
        report = build_master_report(audit_root, repo_root)
        effective_repo_root = repo_root
        if effective_repo_root is None:
            resolved_audit = audit_root.resolve()
            effective_repo_root = (
                resolved_audit.parent.parent
                if resolved_audit.name == "ui_route_audit" and resolved_audit.parent.name == "output"
                else Path.cwd().resolve()
            )
        write_reports(report, Path(args.json_out), Path(args.markdown_out), effective_repo_root)
    except (OSError, ValueError) as error:
        print(f"ui_route_master: ERROR: {error}")
        return 2
    print(
        "ui_route_master: "
        f"routes={report['summary']['ledgers_parsed']} "
        f"nodes={report['summary']['nodes_total']} "
        f"json={Path(args.json_out)} markdown={Path(args.markdown_out)}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
