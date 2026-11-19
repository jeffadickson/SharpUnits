"""Seeded properties for the repaired evidence boundary, using Hypothesis.

Counters measure attempted examples, not a proof or a claim of unique inputs.
The enclosing unittest exit status is the pass/fail authority.
"""
import atexit,json,pathlib,sys,tempfile,unittest
sys.path.insert(0,str(pathlib.Path(__file__).resolve().parent))
from hypothesis import given,seed,settings,strategies as st
from verification_contract import outcome,load_rows
import compare_java_dotnet as comparison
counts={'integerNeighbours':0,'unicodeFraming':0,'contradictoryOutcomes':0}
@atexit.register
def retain_counts():
 root=pathlib.Path(__file__).resolve().parents[1]/'conformance'
 if root.is_dir():
  (root/'verification-repair-properties.json').write_text(json.dumps(dict(framework='Hypothesis6.170.12',seeds=[385001,385002,385003],maxExamplesPerProperty=1000,attemptedExamples=counts,qualification='Bounded generated trials; use the enclosing unittest exit status, not these counters, for success'),indent=2)+'\n',encoding='utf-8')
class VerificationProperties(unittest.TestCase):
 @seed(385001)
 @settings(max_examples=1000,database=None,deadline=None)
 @given(st.integers(min_value=-(1<<512),max_value=1<<512))
 def test_exact_neighbouring_integers_never_compare_equal(self,value):
  counts['integerNeighbours']+=1
  self.assertTrue(comparison.compare({'x':dict(id='x',result=value)},{'x':dict(id='x',result=value+1)}))
 @seed(385002)
 @settings(max_examples=1000,database=None,deadline=None)
 @given(st.text(alphabet=st.characters(exclude_categories=('Cs',)),max_size=256))
 def test_unicode_jsonl_label_framing_preserves_original_value(self,text):
  counts['unicodeFraming']+=1
  with tempfile.TemporaryDirectory() as directory:
   path=pathlib.Path(directory)/'labels.jsonl';path.write_text(json.dumps(dict(id='x',result=text),ensure_ascii=False)+'\n',encoding='utf-8')
   self.assertEqual(text,load_rows(path)['x']['result'])
 @seed(385003)
 @settings(max_examples=1000,database=None,deadline=None)
 @given(st.one_of(st.none(),st.booleans(),st.integers(),st.text(max_size=100)))
 def test_contradictory_outcomes_are_always_rejected(self,value):
  counts['contradictoryOutcomes']+=1
  with self.assertRaises(ValueError):outcome(dict(id='x',result=value,error='ARGUMENT'))
if __name__=='__main__':unittest.main()
