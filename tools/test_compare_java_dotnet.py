import importlib.util
import pathlib
import tempfile
import unittest
import json
import subprocess
import sys
sys.path.insert(0,str(pathlib.Path(__file__).resolve().parent))
from verification_test_support import run_cli

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
 def test_raw_utf8_labels_match_json_unicode_escapes(self):
  with tempfile.TemporaryDirectory() as tmp:
   raw=pathlib.Path(tmp)/"java.jsonl";escaped=pathlib.Path(tmp)/"dotnet.jsonl"
   record={"id":"unicode","result":"20 \u2103; \u03bcA; \u03a9"}
   raw.write_text(json.dumps(record,ensure_ascii=False)+"\n",encoding="utf-8")
   escaped.write_text(json.dumps(record,ensure_ascii=True)+"\n",encoding="utf-8")
   self.assertEqual(record["result"],comparison.load(raw)["unicode"]["result"])
   self.assertEqual([],comparison.compare(comparison.load(raw),comparison.load(escaped)))
 def test_jsonl_does_not_split_unicode_label_separators(self):
  with tempfile.TemporaryDirectory() as tmp:
   raw=pathlib.Path(tmp)/"java.jsonl";escaped=pathlib.Path(tmp)/"dotnet.jsonl"
   record={"id":"unicode-lines","result":"\u0085label\u2028part\u2029end"}
   raw.write_text(json.dumps(record,ensure_ascii=False)+"\n",encoding="utf-8")
   escaped.write_text(json.dumps(record,ensure_ascii=True)+"\n",encoding="utf-8")
   self.assertEqual(record["result"],comparison.load(raw)["unicode-lines"]["result"])
   self.assertEqual([],comparison.compare(comparison.load(raw),comparison.load(escaped)))
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
   return run_cli(path.with_name("check_fixture_expectations.py"),[cases,rows]).returncode
 def test_zero_tolerance_rejects_a_small_difference(self):
  self.assertNotEqual(0,self.validate_strict_fixture(1,1+5e-13))
 def test_zero_tolerance_does_not_hide_a_lost_tiny_value(self):
  self.assertNotEqual(0,self.validate_strict_fixture(1e-20,0))

class VerificationRepairTests(unittest.TestCase):
 def compare(self,expected,actual,**metadata):
  return comparison.compare({'x':dict(id='x',result=expected,**metadata)},{'x':dict(id='x',result=actual)})
 def test_large_integer_difference_is_exact(self):
  self.assertTrue(self.compare(9007199254740992,9007199254740993))
 def test_empty_campaign_rejected(self):
  self.assertTrue(comparison.compare({},{}))
 def test_null_empty_and_unknown_error_categories_rejected(self):
  for error in (None,'','INVENTED'):
   with self.subTest(error=error):
    self.assertTrue(comparison.compare({'x':dict(id='x',error=error)},{'x':dict(id='x',error=error)}))
 def test_nonfinite_results_and_tolerances_rejected(self):
  for value in (float('inf'),float('-inf'),float('nan')):
   with self.subTest(value=value):
    self.assertTrue(self.compare(value,value))
    self.assertTrue(self.compare(1,999,absoluteTolerance=value))
 def test_nested_boolean_and_integer_are_distinct(self):
  self.assertTrue(self.compare({'scale':True},{'scale':1}))
 def test_duplicate_json_keys_rejected(self):
  with tempfile.TemporaryDirectory() as tmp:
   p=pathlib.Path(tmp)/'duplicate.jsonl';p.write_text('{"id":"x","result":1,"result":2}\n',encoding='utf-8')
   with self.assertRaises(ValueError):comparison.load(p)
 def fixture(self,cases,rows):
  with tempfile.TemporaryDirectory() as tmp:
   a=pathlib.Path(tmp)/'cases.json';b=pathlib.Path(tmp)/'rows.jsonl'
   a.write_text(json.dumps({'cases':cases},ensure_ascii=False),encoding='utf-8')
   b.write_text(''.join(json.dumps(r,ensure_ascii=False)+'\n' for r in rows),encoding='utf-8')
   return run_cli(path.with_name('check_fixture_expectations.py'),[a,b]).returncode
 def test_omitted_tolerance_does_not_lose_tiny_value(self):
  self.assertNotEqual(0,self.fixture([dict(id='x',expected=1e-20)],[dict(id='x',result=0)]))
 def test_contradictory_error_rejected(self):
  self.assertNotEqual(0,self.fixture([dict(id='x',expectedError='DIVIDE_BY_ZERO')],[dict(id='x',error='DIVIDE_BY_ZERO',result=99)]))
 def test_duplicate_expected_ids_rejected(self):
  self.assertNotEqual(0,self.fixture([dict(id='x',expected=1)]*2,[dict(id='x',result=1)]))
 def test_empty_fixture_rejected(self):
  self.assertNotEqual(0,self.fixture([],[]))
 def test_unicode_label_framing(self):
  value='a\u0085b\u2028c\u2029d'
  self.assertEqual(0,self.fixture([dict(id='x',expected=value)],[dict(id='x',result=value)]))
class EvidenceAuthorityTests(unittest.TestCase):
 def test_core_check_requires_explicit_observer_role(self):
  from verification_contract import check
  cases={'x':dict(id='x',operation='core',expected=1,javaRepresentations=[])}
  rows={'x':dict(id='x',result=1,javaRepresentations=[])}
  self.assertTrue(check(rows,cases))
  self.assertFalse(check(rows,cases,'java'))
  self.assertFalse(check(rows,cases,'clr'))
 def test_raw_java_metadata_must_match_retained_fixture(self):
  cases={'x':dict(id='x',expected=1,javaRepresentations=[{'javaClass':'java.math.BigDecimal','text':'1.00','scale':2,'clrProfile':'Integer|1/1|0'}])}
  java={'x':dict(id='x',result=1)};clr={'x':dict(id='x',result=1)}
  self.assertTrue(comparison.compare(java,clr,cases))
 def test_clr_exception_mapping_required_on_replay(self):
  cases={'x':dict(id='x',expected={'error':'ARGUMENT','stage':'input'},javaRepresentations=[],javaException='java.lang.NumberFormatException',allowedClrExceptions=['System.FormatException'])}
  java={'x':dict(id='x',result=cases['x']['expected'],javaRepresentations=[],javaException='java.lang.NumberFormatException')}
  clr={'x':dict(id='x',result=cases['x']['expected'],clrException='System.InvalidOperationException')}
  self.assertTrue(comparison.compare(java,clr,cases))
 def test_changed_tolerance_cannot_authorize_difference(self):
  cases={'x':dict(id='x',expected=1,absoluteTolerance=0)}
  self.assertTrue(comparison.compare({'x':dict(id='x',result=1,absoluteTolerance=100)},{'x':dict(id='x',result=2)},cases))
if __name__=="__main__":
 unittest.main()
