#!/usr/bin/env python3

import json
import pathlib
import tempfile
import unittest

import route_timing


class RouteTimingTests(unittest.TestCase):
    def test_phase_event_summary_and_atomic_failure(self):
        with tempfile.TemporaryDirectory() as temporary:
            target = pathlib.Path(temporary) / "timing.json"
            ledger = route_timing.new_ledger("mainui.role.outward.wing", "2026-08-11T22:20:00+08:00")
            route_timing.atomic_write(target, ledger)
            ledger = route_timing.start_phase(
                ledger, "load-1", "context-load", "active", "2026-08-11T22:20:00+08:00")
            ledger = route_timing.stop_phase(ledger, "2026-08-11T22:30:00+08:00")
            ledger = route_timing.start_phase(
                ledger, "server-wait-1", "environment-wait", "wait", "2026-08-11T22:30:00+08:00")
            ledger = route_timing.stop_phase(ledger, "2026-08-11T22:32:00+08:00")
            ledger = route_timing.add_event(
                ledger, "saved-work", "reused 57 historical candidate nodes",
                "2026-08-11T22:32:00+08:00", estimated_saved_ms=15 * 60 * 1000)
            ledger = route_timing.add_event(
                ledger, "repeat-failure", "same preflight blocker repeated",
                "2026-08-11T22:33:00+08:00", duration_ms=30_000, fingerprint="server-stale")
            route_timing.atomic_write(target, ledger)
            loaded = route_timing.validate_ledger(route_timing.read_ledger(target))
            self.assertEqual(loaded["summary"]["active_ms"], 600_000)
            self.assertEqual(loaded["summary"]["wait_ms"], 120_000)
            self.assertEqual(loaded["summary"]["estimated_saved_ms"], 900_000)
            self.assertEqual(loaded["summary"]["recorded_failure_ms"], 30_000)
            self.assertEqual(loaded["summary"]["repeat_failure_count"], 1)
            before = target.read_bytes()
            open_ledger = route_timing.start_phase(
                loaded, "bad", "context-load", "active", "2026-08-11T22:34:00+08:00")
            with self.assertRaises(ValueError):
                route_timing.start_phase(open_ledger, "overlap", "diagnosis", "active", "2026-08-11T22:34:00+08:00")
            self.assertEqual(target.read_bytes(), before)

    def test_validate_rejects_naive_timestamp_and_stale_summary(self):
        with self.assertRaises(ValueError):
            route_timing.new_ledger("route", "2026-08-11T22:20:00")
        ledger = route_timing.new_ledger("route", "2026-08-11T22:20:00+08:00")
        ledger["summary"]["active_ms"] = 1
        with self.assertRaisesRegex(ValueError, "SUMMARY_STALE"):
            route_timing.validate_ledger(ledger)

    def test_init_refuses_to_replace_an_existing_ledger(self):
        with tempfile.TemporaryDirectory() as temporary:
            target = pathlib.Path(temporary) / "timing.json"
            route_timing.main(["init", "first-route", str(target), "--started-at", "2026-08-11T22:20:00+08:00"])
            before = target.read_bytes()
            with self.assertRaises(FileExistsError):
                route_timing.main(["init", "second-route", str(target), "--started-at", "2026-08-11T22:21:00+08:00"])
            self.assertEqual(target.read_bytes(), before)


if __name__ == "__main__":
    unittest.main()
