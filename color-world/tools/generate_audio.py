"""Original synthesized score, UI cues and quiet biome beds. No external assets."""
from pathlib import Path
import numpy as np, wave
ROOT=Path(__file__).resolve().parents[1]/'audio';ROOT.mkdir(exist_ok=True)
SR=22050
rng=np.random.default_rng(91)
def save(name,a):
 a=np.clip(a,-.95,.95)
 with wave.open(str(ROOT/(name+'.wav')),'wb') as f:
  f.setnchannels(1);f.setsampwidth(2);f.setframerate(SR);f.writeframes((a*32767).astype('<i2').tobytes())
def tone(freq,dur,decay=4):
 t=np.arange(int(SR*dur))/SR
 return (np.sin(2*np.pi*freq*t)+.24*np.sin(4*np.pi*freq*t)+.08*np.sin(6*np.pi*freq*t))*np.minimum(1,t/.012)*np.exp(-decay*t)
cues={'tap':[660],'tick':[1100],'match':[330,495], 'score':[880], 'fail':[330,277], 'star1':[440,554], 'star2':[440,554,660], 'star3':[440,554,660,880], 'perfect':[523,659,784,1047,1319], 'exact':[523,784,1047,1568], 'coins':[1047,1397], 'spend':[784,523], 'helper':[392,587,784], 'paint_complete':[523,659,784], 'area_complete':[392,494,587,784,988], 'world_restored':[262,330,392,523,659,784,1047], 'perfect_world':[330,440,554,659,880,1109,1319]}
for name,notes in cues.items():
 dur=.18 if name in ['tap','tick','score'] else .42
 a=np.zeros(int(SR*(dur+.1*len(notes))))
 for i,f in enumerate(notes):
  n=tone(f,dur,9 if dur<.2 else 5)*.17;start=int(i*.1*SR);a[start:start+len(n)]+=n
 save(name,a)
for idx,name in enumerate(['brush','roller','spray','wide_brush','splash','magic']):
 t=np.arange(SR*2)/SR
 noise=rng.normal(0,1,len(t));noise=np.convolve(noise,np.ones(18+idx*6)/(18+idx*6),mode='same')
 a=noise*.18*np.sin(np.pi*t/2)**2
 if name=='magic':a+=np.sin(2*np.pi*(660*t+90*t*t))*.045*np.sin(np.pi*t/2)**2
 save(name,a)
# All layers have equal lengths and phrase boundaries. The score uses six modes.
for theme,root in enumerate([48,50,53,55,57,60]):
 duration=19.2;t=np.arange(int(SR*duration))/SR
 pad=np.zeros(len(t));melody=np.zeros(len(t));bass=np.zeros(len(t))
 for bar,chord in enumerate([[0,4,7],[5,9,12],[2,5,9],[7,11,14]]):
  start=int(bar*4.8*SR);seg=np.arange(int(4.8*SR))/SR;env=np.sin(np.pi*seg/4.8)**1.2
  for interval in chord:
   f=440*2**((root+interval-69)/12);pad[start:start+len(seg)]+=np.sin(2*np.pi*f*seg)*env*.028
  for n in range(8):
   note=chord[(n+theme)%3]+12+(12 if n==7 else 0);f=440*2**((root+note-69)/12)
   clip=tone(f,.58,5)*.055;at=start+int(n*.6*SR);melody[at:at+len(clip)]+=clip
  clip=tone(440*2**((root+chord[0]-12-69)/12),4.7,1)*.07;bass[start:start+len(clip)]+=clip
 for layer,a in enumerate([pad,melody,bass]):save(f'music_{theme}_{layer}',a)
# Low volume ambient textures; original synthesis, not recordings.
for biome in ['garden','forest','ocean','water','wind','market','airport','city']:
 n=SR*12;t=np.arange(n)/SR;noise=rng.normal(0,1,n)
 smooth=np.convolve(noise,np.ones(160)/160,mode='same');a=smooth*.14*(.5+.5*np.sin(t*.8)**2)
 if biome in ['garden','forest']:
  for k in range(8):
   at=int((.4+k*1.35)*SR);tt=np.arange(int(.18*SR))/SR;chirp=np.sin(2*np.pi*(1700*tt+2100*tt*tt))*np.sin(np.pi*tt/.18)**2*.018;a[at:at+len(chirp)]+=chirp
 if biome in ['market','city','airport']:a+=np.sin(2*np.pi*67*t)*.004
 save('ambient_'+biome,a)
print('Generated',len(list(ROOT.glob('*.wav'))),'original WAV assets')
