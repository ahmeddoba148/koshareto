"""Install the APK, exercise a fresh session and capture Android evidence."""
from pathlib import Path
import json,subprocess,time
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'build/android';OUT.mkdir(parents=True,exist_ok=True)
PACKAGE='com.ahmednabil.colorworld'
def adb(*args,check=True):return subprocess.run(['adb',*args],check=check,stdout=subprocess.PIPE,stderr=subprocess.PIPE)
def screenshot(name):
 (OUT/(name+'.png')).write_bytes(adb('exec-out','screencap','-p').stdout)
def tap(x,y):adb('shell','input','tap',str(x),str(y));time.sleep(1)
apk=next((ROOT/'build').glob('*.apk'))
adb('install','-r',str(apk));adb('shell','pm','clear',PACKAGE);adb('logcat','-c')
adb('shell','wm','size','432x864');adb('shell','wm','density','160')
adb('shell','monkey','-p',PACKAGE,'-c','android.intent.category.LAUNCHER','1')
time.sleep(12)
pid=adb('shell','pidof',PACKAGE).stdout.decode().strip();assert pid,'App failed to start'
screenshot('01-lobby')
tap(216,702);screenshot('02-gameplay')
adb('shell','input','swipe','110','493','300','493','700');time.sleep(1);screenshot('03-slider')
tap(216,790);time.sleep(2);screenshot('04-match-result')
adb('shell','am','force-stop',PACKAGE);adb('shell','monkey','-p',PACKAGE,'-c','android.intent.category.LAUNCHER','1');time.sleep(8)
screenshot('05-resume')
log=adb('logcat','-d').stdout.decode(errors='replace');(OUT/'logcat.log').write_text(log)
assert 'FATAL EXCEPTION' not in log,'Android Java crash'
assert 'Fatal signal' not in log,'Android native crash'
assert 'SCRIPT ERROR' not in log,'GDScript runtime error on Android'
assert adb('shell','pidof',PACKAGE).stdout.strip(),'Process missing after resume'
(OUT/'android_smoke_report.json').write_text(json.dumps({'installed':True,'launched':True,'input_sent':True,'resumed':True,'crash_checks_passed':True,'visual_review_required':True,'physical_device_test':False},indent=2))
print('Android installation, launch, interaction and resume smoke completed')
