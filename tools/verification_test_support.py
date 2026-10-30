"""Run real CLI entry points in-process so coverage includes validation branches.

Argument parsing, file IO and SystemExit remain real; only console capture and
argv restoration are test plumbing. Separate process smoke/replay runs remain.
"""
import contextlib,io,runpy,sys
from types import SimpleNamespace
def run_cli(script,arguments):
 previous=sys.argv;output=io.StringIO();error=io.StringIO();code=0
 try:
  sys.argv=[str(script),*map(str,arguments)]
  with contextlib.redirect_stdout(output),contextlib.redirect_stderr(error):
   try:runpy.run_path(str(script),run_name='__main__')
   except SystemExit as result:
    code=result.code if isinstance(result.code,int) else (0 if result.code is None else 1)
    if result.code is not None:error.write(str(result.code))
   except (ValueError,TypeError,KeyError,OSError) as problem:code=1;error.write(str(problem))
 finally:sys.argv=previous
 return SimpleNamespace(returncode=code,stdout=output.getvalue().encode(),stderr=error.getvalue().encode())
