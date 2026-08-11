#!/usr/bin/env python3
"""Atomic phase/event timing ledger for UI refinement routes."""

from __future__ import annotations

import argparse
import contextlib
import copy
import datetime as dt
import hashlib
import json
import os
import pathlib
import tempfile
from typing import Any, Dict, Iterator, Optional


SCHEMA = 1
KINDS = {"active", "wait"}
BUCKETS = {
    "context-load",
    "legacy-inventory",
    "uiaudit-preflight",
    "diagnosis",
    "implementation",
    "component-matrix",
    "editor-cli",
    "build-web",
    "rework",
    "environment-wait",
}
EVENT_CATEGORIES = {
    "saved-work",
    "blocker",
    "failure",
    "repeat-failure",
    "scope-correction",
    "estimate",
}


def now_iso() -> str:
    return dt.datetime.now().astimezone().isoformat(timespec="milliseconds")


def parse_time(value: str) -> dt.datetime:
    normalized = value[:-1] + "+00:00" if value.endswith("Z") else value
    parsed = dt.datetime.fromisoformat(normalized)
    if parsed.tzinfo is None or parsed.utcoffset() is None:
        raise ValueError(f"timestamp must include timezone: {value}")
    return parsed


def elapsed_ms(start: str, end: str) -> int:
    value = int(round((parse_time(end) - parse_time(start)).total_seconds() * 1000))
    if value < 0:
        raise ValueError(f"end precedes start: {start} -> {end}")
    return value


def read_ledger(path: os.PathLike[str] | str) -> Dict[str, Any]:
    with open(path, "r", encoding="utf-8") as stream:
        return json.load(stream)


def atomic_write(path: os.PathLike[str] | str, payload: Dict[str, Any]) -> None:
    target = pathlib.Path(path).resolve()
    target.parent.mkdir(parents=True, exist_ok=True)
    descriptor, temporary = tempfile.mkstemp(prefix=f".{target.name}.", suffix=".tmp", dir=target.parent)
    try:
        with os.fdopen(descriptor, "w", encoding="utf-8", newline="\n") as stream:
            json.dump(payload, stream, ensure_ascii=False, indent=2)
            stream.write("\n")
            stream.flush()
            os.fsync(stream.fileno())
        os.replace(temporary, target)
    finally:
        if os.path.exists(temporary):
            os.unlink(temporary)


def lock_path(path: os.PathLike[str] | str) -> pathlib.Path:
    identity = hashlib.sha256(str(pathlib.Path(path).resolve()).encode("utf-8")).hexdigest()
    return pathlib.Path(tempfile.gettempdir()) / f"ui-route-timing-{identity}.lock"


@contextlib.contextmanager
def write_lock(path: os.PathLike[str] | str) -> Iterator[None]:
    lock = lock_path(path)
    try:
        descriptor = os.open(lock, os.O_CREAT | os.O_EXCL | os.O_WRONLY)
    except FileExistsError as error:
        raise RuntimeError(f"TIMING_LEDGER_LOCKED: {path}") from error
    try:
        os.write(descriptor, f"pid={os.getpid()}\n".encode("ascii"))
        os.close(descriptor)
        yield
    finally:
        with contextlib.suppress(FileNotFoundError):
            lock.unlink()


def compute_summary(ledger: Dict[str, Any]) -> Dict[str, Any]:
    by_kind = {kind: 0 for kind in sorted(KINDS)}
    by_bucket = {bucket: 0 for bucket in sorted(BUCKETS)}
    closed_ms = 0
    for phase in ledger.get("phases", []):
        duration = phase.get("duration_ms")
        if duration is None:
            continue
        duration = int(duration)
        closed_ms += duration
        by_kind[phase["kind"]] += duration
        by_bucket[phase["bucket"]] += duration
    events = ledger.get("events", [])
    return {
        "closed_duration_ms": closed_ms,
        "active_ms": by_kind["active"],
        "wait_ms": by_kind["wait"],
        "by_bucket_ms": by_bucket,
        "estimated_saved_ms": sum(int(event.get("estimated_saved_ms", 0)) for event in events),
        "recorded_failure_ms": sum(int(event.get("duration_ms", 0)) for event in events if event["category"] in {"failure", "repeat-failure"}),
        "repeat_failure_count": sum(1 for event in events if event["category"] == "repeat-failure"),
        "open_phase": next((phase["id"] for phase in ledger.get("phases", []) if phase.get("ended_at") is None), None),
    }


def validate_ledger(ledger: Dict[str, Any]) -> Dict[str, Any]:
    if ledger.get("schema") != SCHEMA:
        raise ValueError("TIMING_SCHEMA_INVALID")
    if not isinstance(ledger.get("route"), str) or not ledger["route"].strip():
        raise ValueError("TIMING_ROUTE_INVALID")
    parse_time(ledger["started_at"])
    phases = ledger.get("phases")
    events = ledger.get("events")
    if not isinstance(phases, list) or not isinstance(events, list):
        raise ValueError("TIMING_COLLECTION_INVALID")
    phase_ids = set()
    open_count = 0
    for phase in phases:
        phase_id = phase.get("id")
        if not isinstance(phase_id, str) or not phase_id or phase_id in phase_ids:
            raise ValueError(f"TIMING_PHASE_ID_INVALID: {phase_id}")
        phase_ids.add(phase_id)
        if phase.get("kind") not in KINDS or phase.get("bucket") not in BUCKETS:
            raise ValueError(f"TIMING_PHASE_CLASS_INVALID: {phase_id}")
        parse_time(phase["started_at"])
        if phase.get("ended_at") is None:
            open_count += 1
            if phase.get("duration_ms") is not None:
                raise ValueError(f"TIMING_OPEN_DURATION_INVALID: {phase_id}")
        else:
            expected = elapsed_ms(phase["started_at"], phase["ended_at"])
            if int(phase.get("duration_ms", -1)) != expected:
                raise ValueError(f"TIMING_DURATION_INVALID: {phase_id}")
    if open_count > 1:
        raise ValueError("TIMING_MULTIPLE_OPEN_PHASES")
    for event in events:
        if event.get("category") not in EVENT_CATEGORIES:
            raise ValueError(f"TIMING_EVENT_CATEGORY_INVALID: {event.get('category')}")
        parse_time(event["at"])
        for key in ("duration_ms", "estimated_saved_ms"):
            if int(event.get(key, 0)) < 0:
                raise ValueError(f"TIMING_EVENT_VALUE_INVALID: {key}")
    expected_summary = compute_summary(ledger)
    if ledger.get("summary") != expected_summary:
        raise ValueError("TIMING_SUMMARY_STALE")
    return ledger


def new_ledger(route: str, started_at: Optional[str] = None, note: Optional[str] = None) -> Dict[str, Any]:
    start = started_at or now_iso()
    parse_time(start)
    ledger = {
        "schema": SCHEMA,
        "route": route,
        "started_at": start,
        "updated_at": start,
        "note": note or "",
        "phases": [],
        "events": [],
    }
    ledger["summary"] = compute_summary(ledger)
    return ledger


def start_phase(ledger: Dict[str, Any], phase_id: str, bucket: str, kind: str, at: Optional[str] = None, note: Optional[str] = None) -> Dict[str, Any]:
    validate_ledger(ledger)
    if bucket not in BUCKETS or kind not in KINDS:
        raise ValueError("TIMING_PHASE_CLASS_INVALID")
    if any(phase.get("ended_at") is None for phase in ledger["phases"]):
        raise ValueError("TIMING_PHASE_ALREADY_OPEN")
    if any(phase["id"] == phase_id for phase in ledger["phases"]):
        raise ValueError(f"TIMING_PHASE_DUPLICATE: {phase_id}")
    timestamp = at or now_iso()
    parse_time(timestamp)
    candidate = copy.deepcopy(ledger)
    candidate["phases"].append({
        "id": phase_id,
        "bucket": bucket,
        "kind": kind,
        "started_at": timestamp,
        "ended_at": None,
        "duration_ms": None,
        "note": note or "",
    })
    candidate["updated_at"] = timestamp
    candidate["summary"] = compute_summary(candidate)
    return validate_ledger(candidate)


def stop_phase(ledger: Dict[str, Any], at: Optional[str] = None) -> Dict[str, Any]:
    validate_ledger(ledger)
    open_indexes = [index for index, phase in enumerate(ledger["phases"]) if phase.get("ended_at") is None]
    if len(open_indexes) != 1:
        raise ValueError("TIMING_NO_OPEN_PHASE")
    timestamp = at or now_iso()
    candidate = copy.deepcopy(ledger)
    phase = candidate["phases"][open_indexes[0]]
    phase["ended_at"] = timestamp
    phase["duration_ms"] = elapsed_ms(phase["started_at"], timestamp)
    candidate["updated_at"] = timestamp
    candidate["summary"] = compute_summary(candidate)
    return validate_ledger(candidate)


def add_event(ledger: Dict[str, Any], category: str, detail: str, at: Optional[str] = None,
              duration_ms: int = 0, estimated_saved_ms: int = 0,
              fingerprint: Optional[str] = None) -> Dict[str, Any]:
    validate_ledger(ledger)
    if category not in EVENT_CATEGORIES:
        raise ValueError(f"TIMING_EVENT_CATEGORY_INVALID: {category}")
    if duration_ms < 0 or estimated_saved_ms < 0:
        raise ValueError("TIMING_EVENT_VALUE_INVALID")
    timestamp = at or now_iso()
    parse_time(timestamp)
    candidate = copy.deepcopy(ledger)
    candidate["events"].append({
        "at": timestamp,
        "category": category,
        "detail": detail,
        "duration_ms": int(duration_ms),
        "estimated_saved_ms": int(estimated_saved_ms),
        "fingerprint": fingerprint,
    })
    candidate["updated_at"] = timestamp
    candidate["summary"] = compute_summary(candidate)
    return validate_ledger(candidate)


def mutate(path: str, operation) -> Dict[str, Any]:
    with write_lock(path):
        original = read_ledger(path)
        candidate = operation(original)
        atomic_write(path, candidate)
        return candidate


def parser() -> argparse.ArgumentParser:
    root = argparse.ArgumentParser(description=__doc__)
    commands = root.add_subparsers(dest="command", required=True)
    init = commands.add_parser("init")
    init.add_argument("route")
    init.add_argument("ledger")
    init.add_argument("--started-at")
    init.add_argument("--note")
    start = commands.add_parser("start")
    start.add_argument("ledger")
    start.add_argument("phase")
    start.add_argument("--bucket", required=True, choices=sorted(BUCKETS))
    start.add_argument("--kind", required=True, choices=sorted(KINDS))
    start.add_argument("--at")
    start.add_argument("--note")
    stop = commands.add_parser("stop")
    stop.add_argument("ledger")
    stop.add_argument("--at")
    event = commands.add_parser("event")
    event.add_argument("ledger")
    event.add_argument("--category", required=True, choices=sorted(EVENT_CATEGORIES))
    event.add_argument("--detail", required=True)
    event.add_argument("--at")
    event.add_argument("--duration-ms", type=int, default=0)
    event.add_argument("--estimated-saved-ms", type=int, default=0)
    event.add_argument("--fingerprint")
    validate = commands.add_parser("validate")
    validate.add_argument("ledger")
    summary = commands.add_parser("summary")
    summary.add_argument("ledger")
    return root


def main(argv: Optional[list[str]] = None) -> int:
    args = parser().parse_args(argv)
    if args.command == "init":
        with write_lock(args.ledger):
            if pathlib.Path(args.ledger).exists():
                raise FileExistsError(f"TIMING_LEDGER_EXISTS: {args.ledger}")
            ledger = new_ledger(args.route, args.started_at, args.note)
            atomic_write(args.ledger, ledger)
    elif args.command == "start":
        ledger = mutate(args.ledger, lambda value: start_phase(value, args.phase, args.bucket, args.kind, args.at, args.note))
    elif args.command == "stop":
        ledger = mutate(args.ledger, lambda value: stop_phase(value, args.at))
    elif args.command == "event":
        ledger = mutate(args.ledger, lambda value: add_event(
            value, args.category, args.detail, args.at, args.duration_ms,
            args.estimated_saved_ms, args.fingerprint))
    else:
        ledger = validate_ledger(read_ledger(args.ledger))
    output = ledger["summary"] if args.command == "summary" else {
        "route": ledger["route"],
        "updated_at": ledger["updated_at"],
        "summary": ledger["summary"],
    }
    print(json.dumps(output, ensure_ascii=False, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
