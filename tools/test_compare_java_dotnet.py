import importlib.util
import pathlib
import tempfile
import unittest
import json
import subprocess
import sys

path=pathlib.Path(__file__).with_name("compare_java_dotnet.py")
spec=importlib.util.spec_from_file_location("comparison",path)
comparison=importlib.util.module_from_spec(spec)
spec.loader.exec_module(comparison)

class ComparatorTests(unittest.TestCase):
 def test_exact_match(self):
  self.assertEqual([],comparison.compare({"a":{"id":"a","result":5}},{"a":{"id":"a","result":5}}))
 def test_numeric_mismatch(self):
  self.assertEqual(1,len(comparison.compare({"a":{"id":"a","result":5}},{"a":{"id":"a","result":6}})))
 def test_explicit_tolerance(self):
  self.assertEqual([],comparison.compare({"a":{"id":"a","result":5,"absoluteTolerance":.1}},{"a":{"id":"a","result":5.05}}))
 def test_missing_case(self):
  self.assertEqual(1,len(comparison.compare({"a":{"id":"a","result":5}},{})))
 def test_error_categories(self):
  self.assertEqual([],comparison.compare({"a":{"id":"a","error":"INCOMPATIBLE_DIMENSION"}},{"a":{"id":"a","error":"INCOMPATIBLE_DIMENSION"}}))
 def test_duplicate_rejected(self):
  with tempfile.TemporaryDirectory() as tmp:
   p=pathlib.Path(tmp)/"cases.jsonl"
   p.write_text('{"id":"a","result":1}\n{"id":"a","result":2}\n')
   with self.assertRaises(ValueError):comparison.load(p)
 def test_boolean_is_not_a_numeric_result(self):
  for expected,actual in [(True,1),(False,0)]:
   with self.subTest(expected=expected):
    self.assertEqual(1,len(comparison.compare({"a":{"id":"a","result":expected}},{"a":{"id":"a","result":actual}})))
 def test_missing_result_and_error_rejected(self):
  self.assertEqual(1,len(comparison.compare({"a":{"id":"a"}},{"a":{"id":"a"}})))
 def validate_strict_fixture(self,expected,actual):
  with tempfile.TemporaryDirectory() as tmp:
   cases=pathlib.Path(tmp)/"cases.json";rows=pathlib.Path(tmp)/"actual.jsonl"
   cases.write_text(json.dumps({"cases":[{"id":"a","expected":expected,"absoluteTolerance":0}]}),encoding="utf-8")
   rows.write_text(json.dumps({"id":"a","result":actual})+"\n",encoding="utf-8")
   return subprocess.run([sys.executable,str(path.with_name("check_fixture_expectations.py")),str(cases),str(rows)],capture_output=True).returncode
 def test_zero_tolerance_rejects_a_small_difference(self):
  self.assertNotEqual(0,self.validate_strict_fixture(1,1+5e-13))
 def test_zero_tolerance_does_not_hide_a_lost_tiny_value(self):
  self.assertNotEqual(0,self.validate_strict_fixture(1e-20,0))

if __name__=="__main__":
 unittest.main()
