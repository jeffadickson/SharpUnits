#!/usr/bin/env python3
"""Check linear adapter conversion and addition against Python's exact Fraction rounding.

This validates the CLR round-trip-decimal model, not arbitrary Java Number parity.
Build the conformance runner first. Pass --dotnet if .NET 10 is not on PATH.
"""
import argparse
from fractions import Fraction
import json
import math
from pathlib import Path
import random
import subprocess
import tempfile


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--dotnet',default='dotnet')
    args=parser.parse_args()
    root=Path(__file__).resolve().parents[1]
    runner=root/'src/SharpUnits.ConformanceRunner/bin/Release/net10.0/SharpUnits.ConformanceRunner.dll'
    scales={'A':1.,'mA':.001,'quarterA':.25,'eighthA':.125,'largeA':1e200,'tinyA':1e-200}
    randomizer=random.Random(385)
    cases=[]
    while len(cases)<200:
        source=randomizer.choice(list(scales));target=randomizer.choice(list(scales))
        exponent=randomizer.choice([-323,-320,-300,-200,-100,-1,0,1,100,200,300])
        value=randomizer.choice([-1,1])*(1+randomizer.randrange(1000)/1000)*10.**exponent
        addition=len(cases)>=100
        rhs=-value/2 if addition else 0
        if not math.isfinite(rhs*scales[target]):
            continue
        try:
            coherent=Fraction(str(value))*Fraction(str(scales[source]))
            exact=coherent+Fraction(str(rhs))*Fraction(str(scales[target])) if addition else coherent
            expected=float(exact/Fraction(str(scales[source] if addition else scales[target])))
        except OverflowError:
            continue
        if not all(math.isfinite(v) for v in [expected,value*scales[source],expected*scales[source if addition else target]]):
            continue
        cases.append(dict(id=f'rounding-{len(cases)+1}',operation='quantityAdd' if addition else 'quantityTo',value=value,
            **{'from':source},to=target,rhsValue=rhs,observe='magnitude',expected=expected))
    with tempfile.TemporaryDirectory() as folder:
        path=Path(folder)/'cases.json'
        path.write_text(json.dumps(dict(cases=cases)),encoding='utf-8')
        result=subprocess.run([args.dotnet,str(runner),str(path)],check=True,capture_output=True,text=True,encoding='utf-8')
    actual=[json.loads(line) for line in result.stdout.splitlines()]
    expected={case['id']:case['expected'] for case in cases}
    if len(actual)!=len(cases) or {row['id'] for row in actual}!=set(expected):
        raise SystemExit('Missing, unexpected or duplicate rounding result')
    failures=[row['id'] for row in actual if 'error' in row or row.get('result')!=expected[row['id']]]
    print(json.dumps(dict(model='exact Fraction of round-trip decimal inputs; final binary64 rounding',
        seed=385,cases=len(cases),mismatches=failures)))
    if failures:
        raise SystemExit(1)


if __name__=='__main__':
    main()
