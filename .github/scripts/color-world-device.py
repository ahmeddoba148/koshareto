"""Capture launch diagnostics even when the native application exits early."""
from pathlib import Path
import json,subprocess,time,traceback,sys
OUT=Path('color-world/build/android');OUT.mkdir(parents=True,exist_ok=True)
PACKAGE='com.ahmednabil.colorworld'
def adb(*args,check=True):
 r=subprocess.run(['adb',*args],stdout=subprocess.PIPE,stderr=subprocess.PIPE)
 if check and r.returncode:raise RuntimeError(f'adb {args}: {r.stdout.decode(errors="replace")} {r.stderr.decode(errors="replace")}')
 return r
def screenshot(name):
 r=adb('exec-out','screencap','-p',check=False)
 if r.returncode==0:(OUT/(name+'.png')).write_bytes(r.stdout)
def tap(x,y):adb('shell','input','tap',str(x),str(y));time.sleep(1)
try:
 apk=next(Path('color-world/build').glob('*.apk'))
 print(adb('install','-r',str(apk)).stdout.decode(),flush=True)
 adb('shell','pm','clear',PACKAGE);adb('logcat','-c')
 adb('shell','wm','size','432x864');adb('shell','wm','density','160')
 component=adb('shell','cmd','package','resolve-activity','--brief',PACKAGE).stdout.decode().strip().splitlines()[-1]
 launch=adb('shell','am','start','-W','-n',component)
 print(launch.stdout.decode(),flush=True)
 time.sleep(15);screenshot('01-lobby')
 assert adb('shell','pidof',PACKAGE,check=False).stdout.strip(),'App process missing after launch'
 tap(216,702);screenshot('02-gameplay')
 adb('shell','input','swipe','110','493','300','493','700');time.sleep(1);screenshot('03-slider')
 tap(216,790);time.sleep(2);screenshot('04-match-result')
 adb('shell','am','force-stop',PACKAGE);adb('shell','am','start','-W','-n',component);time.sleep(10);screenshot('05-resume')
 assert adb('shell','pidof',PACKAGE,check=False).stdout.strip(),'Process missing after resume'
 log=adb('logcat','-d').stdout.decode(errors='replace')
 for marker in ['FATAL EXCEPTION','Fatal signal','SCRIPT ERROR']:assert marker not in log,marker
 (OUT/'report.json').write_text(json.dumps({'installed':True,'launched':True,'input_sent':True,'resumed':True,'physical_device_test':False},indent=2))
 print('Android native smoke passed',flush=True)
finally:
 log=adb('logcat','-d',check=False).stdout.decode(errors='replace');(OUT/'logcat.log').write_text(log)
 (OUT/'package.txt').write_bytes(adb('shell','dumpsys','package',PACKAGE,check=False).stdout)
 screenshot('last-state')
 print('\n'.join(line for line in log.splitlines() if any(k in line for k in ['godot','Godot','FATAL EXCEPTION','Fatal signal','AndroidRuntime','colorworld','tombstoned']))[-30000:],flush=True)
