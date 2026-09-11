"""Executable content/math/static audit. Does NOT claim to execute GDScript."""
from pathlib import Path
import json,hashlib,math,random,re,wave
ROOT=Path(__file__).resolve().parents[1]
world=json.loads((ROOT/'data/world.json').read_text())
checks=0;failures=[]
def check(ok,msg):
 global checks
 checks+=1
 if not ok:failures.append(msg)
def srgb(v):return v/12.92 if v<=.04045 else ((v+.055)/1.055)**2.4
def color(r,l):
 peak=max(r);b=l['brightness'];s=l['saturation'];return [b*(1-s)+b*s*x/peak for x in r]
def lab(c):
 r,g,b=map(srgb,c)
 l=(.4122214708*r+.5363325363*g+.0514459929*b)**(1/3)
 m=(.2119034982*r+.6806995451*g+.1073969566*b)**(1/3)
 s=(.0883024619*r+.2817188376*g+.6299787005*b)**(1/3)
 return [.2104542553*l+.793617785*m-.0040720468*s,1.9779984951*l-2.428592205*m+.4505937099*s,.0259040371*l+.7827717662*m-.808675766*s]
def score(r,l):return round(max(0,100-math.dist(lab(color(r,l)),lab(color(l['target'],l)))*400),1)
check(len(world['areas'])==100,'exact area count')
check(len(world['levels'])==1500,'exact level count')
check(len(world['levels'])*3==4500,'maximum stars')
ids=set();mappings={};landmarks=set();kinds=set();components=0
for expected,a in enumerate(world['areas'],1):
 check(a['id']==expected,f'area order {expected}');check(len(a['groups'])==15,f'group count {expected}')
 check(len(a['origin'])==3,f'area origin {expected}');landmarks.add(a['landmark'])
 check((ROOT/f'audio/ambient_{a["ambient"]}.wav').exists(),f'ambient {expected}')
 for g in a['groups']:
  check(g['id'] not in mappings,'duplicate paint mapping');mappings[g['id']]=(a,g);kinds.add(g['kind'])
  check(g['primitives'],f'missing geometry {g["id"]}');check(len(g['position'])==3,f'position {g["id"]}')
  for p in g['primitives']:
   check(p['mesh'] in ['box','sphere','cylinder','cone','torus'],'unresolved primitive')
   check(all(x>0 and math.isfinite(x) for x in p['s']),'invalid dimension')
   check(all(math.isfinite(x) for x in p['p']),'invalid vertex transform');components+=1
for expected,l in enumerate(world['levels'],1):
 check(l['id']==expected,f'level order {expected}');check(l['id'] not in ids,'duplicate level');ids.add(l['id'])
 check(len(l['target'])==3 and sum(l['target'])==1000 and all(isinstance(x,int) and 0<=x<=1000 for x in l['target']),f'target simplex {expected}')
 check(l['area_id']==(expected-1)//15+1,f'area mapping {expected}')
 check(0<l['brightness']<=1 and 0<l['saturation']<=1,f'exposure {expected}')
 check(l['paint_group_id']==l['world_object_id'],f'paint mapping {expected}')
 check(l['paint_group_id'] in mappings,f'missing mesh mapping {expected}')
 a,g=mappings[l['paint_group_id']]
 check(a['id']==l['area_id'] and g['level_id']==expected,f'one-to-one mapping {expected}')
 check(l['paint_tool']==g['paint_tool'] and (ROOT/f'audio/{l["paint_tool"]}.wav').exists(),f'tool {expected}')
 check(len(l['camera']['focus'])==3 and l['camera']['distance']>0,f'camera {expected}')
 check(l['rewards']==[0,10,20,35,50],f'rewards {expected}')
 check(score(l['target'],l)==100,f'exact mathematical reachability {expected}')
 check(all(0<=v<=1 for v in color(l['target'],l)),f'display gamut {expected}')
 # Invert mapping independently: recover ratios from brightness and white floor.
 rgb=color(l['target'],l);floor=l['brightness']*(1-l['saturation']);linear=[v-floor for v in rgb];total=sum(linear)
 recovered=[round(v/total*1000) for v in linear]
 check(recovered==l['target'],f'color mapping invertibility {expected}')
check(len(landmarks)==100,'100 distinct landmark specifications')
check(len(mappings)==1500,'1500 real groups')
# An independent data-driven progression feasibility traversal; not runtime QA.
coins=stars=0;unlocked=1;completed_areas=0
for l in world['levels']:
 check(l['area_id']<=unlocked,'unreachable area in simulated perfect path')
 coins+=l['rewards'][4];stars+=3
 if l['id']%15==0:
  completed_areas+=1;unlocked=min(100,unlocked+1)
check((stars,coins,completed_areas)==(4500,75000,100),'full content perfect path')
strings=json.loads((ROOT/'data/strings.json').read_text())
check(set(strings['en'])==set(strings['ar']),'locale coverage parity')
source='\n'.join(f.read_text() for f in (ROOT/'scripts').glob('*.gd'))
for key in re.findall(r'tr_key\("([a-z_]+)"\)',source):check(key in strings['en'],'undefined locale key '+key)
check('ScrollContainer' not in source,'no scrolling UI controls')
check('HTTPRequest' not in source and 'HTTPClient' not in source,'offline runtime has no HTTP dependency')
for ref in re.findall(r'(?:preload|load)\("res://([^"+]+)"\)',source):check((ROOT/ref).exists(),'missing preload '+ref)
check('window/handheld/orientation=1' in (ROOT/'project.godot').read_text(),'portrait configuration')
for f in (ROOT/'audio').glob('*.wav'):
 with wave.open(str(f)) as w:
  check(w.getnframes()>0 and w.getframerate()==22050 and w.getsampwidth()==2,'invalid audio '+f.name)
# Balanced tokens catches accidental string/bracket editing, not full parsing.
for f in (ROOT/'scripts').glob('*.gd'):
 text=f.read_text();stack=[];quote=None;escape=False
 for line_no,line in enumerate(text.splitlines(),1):
  for c in line:
   if quote:
    if escape:escape=False
    elif c=='\\':escape=True
    elif c==quote:quote=None
   elif c=='#':break
   elif c in ['"',"'"]:quote=c
   elif c in '([{':stack.append((c,line_no))
   elif c in ')]}':
    check(bool(stack) and '([{'.index(stack[-1][0])==')]}'.index(c),f'token mismatch {f.name}:{line_no}')
    if stack:stack.pop()
 check(not stack and quote is None,'unclosed tokens '+f.name)
manifest=json.loads((ROOT/'data/content_manifest.json').read_text())
check(manifest['sha256']==hashlib.sha256((ROOT/'data/world.json').read_bytes()).hexdigest(),'content checksum')
report={'audit':'content, independent color mathematics and source structure only','checks':checks,'failures':failures,'passed':not failures,'areas':100,'levels':1500,'paint_groups':len(mappings),'primitives':components,'object_recipe_types':len(kinds),'landmark_specs':len(landmarks),'audio_assets':len(list((ROOT/'audio').glob('*.wav'))),'native_gdscript_execution':'NOT RUN — Godot unavailable','mobile_build':'NOT RUN','visual_device_qa':'NOT RUN','fps_memory_thermals':'NOT MEASURED','art_quality_approval':'PENDING','score_human_calibration':'PENDING'}
(ROOT/'docs/content_validation_report.json').write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2));raise SystemExit(1 if failures else 0)
