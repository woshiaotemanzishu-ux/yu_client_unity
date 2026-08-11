import json
import tempfile
import unittest
from pathlib import Path

import numpy as np
from PIL import Image

import sys

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from runtime_candidate_diff import match_geometry, write_pixel_diff  # noqa: E402


class RuntimeCandidateDiffTest(unittest.TestCase):
    def test_name_matching_reports_geometry_and_missing_nodes(self):
        runtime = [
            {"name": "root", "x": 0, "y": 80, "width": 720, "height": 992, "visible": True, "runtimePath": "root", "skin": ""},
            {"name": "label", "x": 10, "y": 20, "width": 30, "height": 40, "visible": True, "runtimePath": "root/label", "skin": ""},
        ]
        candidate = [{"name": "candidate", "x": 0, "y": 80, "width": 720, "height": 992, "active": True, "path": "candidate"}]
        matches, missing = match_geometry(runtime, candidate)
        self.assertEqual(1, len(matches))
        self.assertEqual(0, matches[0]["delta"]["maxAbs"])
        self.assertEqual(["label"], [node["name"] for node in missing])

    def test_pixel_math_does_not_overflow_rgba_product(self):
        with tempfile.TemporaryDirectory() as value:
            root = Path(value)
            old = np.zeros((2, 2, 4), dtype=np.uint8)
            old[:, :, 3] = 255
            candidate = np.full((2, 2, 4), 255, dtype=np.uint8)
            Image.fromarray(old, "RGBA").save(root / "old.png")
            Image.fromarray(candidate, "RGBA").save(root / "candidate.png")
            out = root / "out"
            out.mkdir()
            metrics = write_pixel_diff(root / "old.png", root / "candidate.png", out)
            self.assertEqual(255.0, metrics["meanAbsRgbOnCandidateFootprint"])
            self.assertLessEqual(metrics["p95AbsRgbOnCandidateFootprint"], 255.0)


if __name__ == "__main__":
    unittest.main()
