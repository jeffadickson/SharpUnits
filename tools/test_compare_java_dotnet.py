import importlib.util
import pathlib
import tempfile
import unittest
import json

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

if __name__=="__main__":
 unittest.main()
