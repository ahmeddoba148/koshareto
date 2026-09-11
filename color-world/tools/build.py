"""Fail-fast native tests/export. All command output is retained for review."""
import argparse,json,os,platform,shutil,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
def main():
 p=argparse.ArgumentParser();p.add_argument('--platform',choices=['android','ios','linux'],default='android');p.add_argument('--godot',default=os.environ.get('GODOT','godot'))
 mode=p.add_mutually_exclusive_group();mode.add_argument('--tests-only',action='store_true');mode.add_argument('--export-only',action='store_true');a=p.parse_args()
 engine=shutil.which(a.godot);report={'engine_found':bool(engine),'platform':a.platform,'steps':[],'native_build_verified':False,'native_tests_verified':False}
 out=ROOT/'docs'/('native_export_report.json' if a.export_only else 'native_build_report.json');out.parent.mkdir(exist_ok=True);(ROOT/'build').mkdir(exist_ok=True)
 def save():out.write_text(json.dumps(report,indent=2))
 if not engine:
  report['blocker']='Godot executable is not installed.';save();print(report['blocker']);return 2
 if a.platform=='ios' and platform.system()!='Darwin' and not a.tests_only:
  report['blocker']='iOS export requires macOS/Xcode and the owner signing team.';save();print(report['blocker']);return 2
 commands=[] if a.export_only else [('import',['--headless','--editor','--import','--quit']),('core',['--headless','--script','tests/test_game.gd']),('scene',['--headless','--verbose','--script','tests/smoke_scene.gd'])]
 if not a.tests_only:commands.append(('export',['--headless','--export-debug',{'android':'Android','ios':'iOS','linux':'Linux QA'}[a.platform]]))
 for name,args in commands:
  cmd=[engine,'--path',str(ROOT)]+args
  print('RUN',name,flush=True)
  try:r=subprocess.run(cmd,text=True,capture_output=True,timeout=600)
  except subprocess.TimeoutExpired as e:
   report['blocker']=name+' timed out';report['steps'].append({'step':name,'timeout':True});save();print(e.stdout,e.stderr);return 1
  log=r.stdout+'\n'+r.stderr;(ROOT/'build'/f'{name}.log').write_text(log)
  print(log,flush=True);report['steps'].append({'step':name,'returncode':r.returncode,'stdout':r.stdout,'stderr':r.stderr});save()
  if r.returncode or any(marker in log for marker in ['SCRIPT ERROR','Parse Error','ERROR:','Assertion failed']):return 1
 report['native_tests_verified']=not a.export_only
 report['native_build_verified']=not a.tests_only
 report['device_qa_verified']=False
 save();return 0
if __name__=='__main__':sys.exit(main())
