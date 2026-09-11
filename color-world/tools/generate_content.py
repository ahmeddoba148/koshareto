"""Deterministic authored kit composition -> explicit 100-area / 1500-group content.
No runtime random targets. Every primitive, camera and reward is in world.json.
"""
from pathlib import Path
import json, random, math, hashlib
ROOT=Path(__file__).resolve().parents[1]
# name | landmark recipe | ambient biome | thematic object recipes (cycled with variants)
CATALOG='''
First Home|house|garden|fence tree bench planter car lamp mailbox gazebo
Secret Garden|gazebo|garden|flower tree fountain arch bench planter hedge birdhouse
Residential Lane|apartments|city|house car tree lamp fence mailbox bike bench
Morning Bakery|bakery|market|cart cafe awning table planter lamp sign bench
Café Crescent|cafe|market|table awning tree bike lamp fountain cart planter
Local Market|market|market|stall cart arch crate tree lamp awning sign
Little School|school|garden|bus playground tree bench fence sign ball planter
Public Park|fountain|garden|tree bridge bench flower lamp pond gazebo bike
Town Square|clocktower|city|fountain cafe tree statue bench lamp arch cart
Shopping Promenade|arcade|market|shop awning lamp tree bench sign bike car
Downtown|tower|city|apartments car bus lamp tree cafe sign fountain
Restaurant Quarter|restaurant|market|cafe table cart awning lamp planter bike arch
Grand Mall|mall|city|shop fountain tree lamp sign car bench planter
Cinema Boulevard|cinema|city|sign shop cafe car lamp bench cart tree
Museum Gardens|museum|garden|statue pillar fountain tree bench arch flower lamp
Library Quarter|library|garden|bench tree bike fountain lamp sign planter arch
Hospital District|hospital|city|ambulance tree bench sign car lamp planter fence
Sports Club|club|garden|court ball tree bench fence lamp pool bike
National Stadium|stadium|city|ball court sign bus lamp tree bench arch
Wildlife Zoo|zoo|forest|animal tree pond fence bridge hut sign bench
Blue Aquarium|aquarium|ocean|fish shell pond arch bench tree sign lamp
Botanical Domes|greenhouse|garden|flower tree planter pond bridge bench sign fountain
Wonder Fair|wheel|market|carousel stall cart sign bench tree lamp arch
Water Paradise|waterslide|water|pool palm parasol bench hut fountain shell bridge
Golden Beach|lifeguard|ocean|palm parasol boat shell hut bench surfboard sign
Sunset Corniche|promenade|ocean|bench lamp palm boat parasol fountain cart bike
Sailing Marina|sailboat|ocean|boat pier lamp hut bench palm buoy sign
Working Harbor|crane|ocean|container boat pier crate truck lamp buoy warehouse
Fishing Village|windmill|ocean|hut boat net pier crate lamp fish tree
Central Station|station|city|train clocktower bench lamp sign tree cart fence
Metro Plaza|metro|city|sign bench lamp kiosk tree bike bus planter
Bus Terminal|terminal|city|bus sign bench lamp tree car kiosk fence
International Airport|airport|airport|plane tower bus lamp sign car hangar truck
Family Farm|barn|forest|tractor windmill hay fence tree silo animal cart
Country Hamlet|cottage|forest|tree bridge fence hay cart flower bench pond
Sunlit Orchard|fruitstand|garden|tree cart crate fence bench birdhouse flower hut
Flower Fields|flowerarch|garden|flower planter windmill cart fence bench tree gazebo
Whispering Forest|treehouse|forest|tree mushroom pond bridge log birdhouse hut fern
Pine Campsite|tent|forest|tent log fire tree canoe sign bench lantern
River Crossing|bridge|water|boat tree bench mill pond reed lamp hut
Crystal Lake|boathouse|water|canoe pier tree bench duck parasol reed lamp
Silver Waterfalls|waterfall|water|rock bridge tree pond fern bench hut sign
Mountain Village|chalet|wind|cottage pine rock bridge fence lamp cart bench
Alpine Resort|cablecar|wind|chalet ski pine bench lamp sign rock fence
Snow Kingdom|igloo|wind|snowman pine ski chalet bench lamp fence sled
Desert Oasis|oasis|wind|palm pond tent camel hut crate lamp arch
Saffron Desert Town|deserthouse|market|arch market pot palm bench lamp cart fountain
Ancient Ruins|temple|wind|pillar statue arch rock pot tree bench sign
Island Hideaway|islandhut|ocean|palm boat shell pier hammock lamp parasol fish
Tropical Retreat|resort|ocean|pool palm parasol table hammock lamp boat flower
Lighthouse Coast|lighthouse|ocean|rock boat pier buoy bench lamp shell hut
Royal Avenue|mansion|garden|fountain statue tree car gate lamp flower bench
Business Quarter|office|city|tower car tree lamp cafe bike sign planter
Financial Center|bank|city|office tower car fountain statue lamp bench tree
University Campus|university|garden|library bike tree bench sign lamp fountain arch
Technology Park|techhub|city|antenna robot tree bench lamp solar sign bike
Science Center|observatory|wind|dish rocket statue tree bench lamp sign sphere
Space Museum|rocket|wind|planet dish rover astronaut bench lamp sign arch
Industrial District|factory|city|tank pipe truck container crane lamp crate fence
Factory Campus|sawtooth|city|warehouse pipe truck tank crate lamp tree sign
Logistics Hub|warehouse|city|truck container crane crate lamp fence sign cart
Grand Prix Circuit|racetrack|city|racecar flag bench lamp tire tower sign tree
Festival Meadow|stage|market|tent stall flag tree bench lamp cart flower
Music Quarter|concert|city|piano speaker stage bench tree lamp sign cafe
Artists' Lane|gallery|market|easel statue fountain tree bench lamp pot arch
Lantern Market|pagoda|market|stall lantern arch cart tree bench pot sign
City After Dark|hotel|city|car lamp sign cafe tree bench fountain bike
Neon District|neontower|city|sign arcade car lamp tree bench shop robot
Skyline Terrace|skytower|wind|tower gardenpod bench lamp tree fountain arch sign
High-Rise City|spiraltower|city|tower office car lamp tree bench bus planter
Eco Village|ecohouse|garden|solar tree bike pond bench flower windmill planter
Solar Valley|solartower|wind|solar panel shed truck lamp fence tree sign
Wind Ridge|turbine|wind|turbine shed tree rock fence lamp bench sign
Smart City|smarttower|city|robot tram solar lamp tree bench sign gardenpod
Future Transit|monorail|city|tram station robot lamp tree bench sign solar
Robot Quarter|robotgiant|city|robot factory antenna lamp tree bench sign truck
Tomorrow City|futuretower|wind|gardenpod drone robot lamp sphere bridge tree solar
Canal Gardens|canalhouse|water|bridge boat flower tree bench lamp pier duck
Floating Bazaar|floatingmarket|water|boat stall pier cart palm lamp pot crate
Tea Terraces|teahouse|forest|terrace tree cart bench lantern bridge pot arch
Bamboo Sanctuary|bamboogate|forest|bamboo pond bridge lantern bench hut pot birdhouse
Cherry Walk|shrine|garden|tree lantern arch bridge bench pond flower sign
Volcanic Island|volcano|wind|rock palm hut boat lamp bridge crystal sign
Coral Research Base|underwaterdome|ocean|coral fish dome dish lamp submarine shell sign
Glacier Research Base|polarbase|wind|dish rover igloo pipe lamp sled tank sign
Northern Lights Camp|auroratower|wind|tent pine lantern bench sled igloo rock sign
Red Canyon|canyonbridge|wind|rock arch cactus hut cart lamp sign tent
Cactus Village|adobechapel|wind|deserthouse cactus pot cart bench lamp arch well
Crystal Caverns|crystalpalace|water|crystal rock pond bridge lamp cart statue sign
Salt Flats|saltobservatory|wind|dish solar rover rock sphere lamp sign tent
Pottery Village|kiln|market|pot cart hut bench tree lamp well arch
Textile Quarter|textilemill|market|loom roll cart awning lamp tree bench shop
Glassworks|glassdome|city|sphere kiln pot cart lamp tree bench sculpture
Clockwork City|geartower|city|gear clocktower robot tram lamp bench sign bridge
Balloon Valley|balloon|wind|balloon tent cart flower tree bench lamp arch
Celestial Garden|planetarium|garden|planet flower sphere tree bench lamp bridge pond
Cloud Terrace|cloudcastle|wind|arch gardenpod fountain bench lamp sphere bridge tree
Orbital Port|spaceport|wind|rocket dish rover drone lamp tank robot sign
Lunar Gardens|moondome|wind|dome crystal rover gardenpod solar lamp robot sphere
Prismatic Heart|prismtower|garden|crystal fountain arch tree sphere lamp flower statue
'''.strip().splitlines()
assert len(CATALOG)==100, len(CATALOG)

def box(parts,p,s,c=1): parts.append(dict(mesh='box',p=p,s=s,tint=c))
def cyl(parts,p,s,c=1): parts.append(dict(mesh='cylinder',p=p,s=s,tint=c))
def sphere(parts,p,s,c=1): parts.append(dict(mesh='sphere',p=p,s=s,tint=c))
def cone(parts,p,s,c=1): parts.append(dict(mesh='cone',p=p,s=s,tint=c))
def torus(parts,p,s,c=1): parts.append(dict(mesh='torus',p=p,s=s,tint=c))

def recipe(kind,variant=0):
 p=[]; v=variant%5; h=1.8+v*.18
 buildings={'house','apartments','bakery','cafe','school','arcade','restaurant','mall','cinema','museum','library','hospital','club','shop','hut','kiosk','cottage','chalet','deserthouse','islandhut','resort','mansion','office','bank','university','techhub','factory','sawtooth','warehouse','gallery','hotel','ecohouse','shed','canalhouse','teahouse','polarbase','textilemill','boathouse','fruitstand','hangar','terminal','barn','loom','station','floatingmarket','lifeguard','adobechapel'}
 towers={'tower','clocktower','skytower','spiraltower','neontower','smarttower','futuretower','geartower','auroratower','solartower','prismtower','lighthouse','windmill','turbine','crane','antenna'}
 trees={'tree','pine','palm','bamboo','cactus','fern','reed','flower','hedge','planter','mushroom'}
 cars={'car','racecar','ambulance','bus','truck','train','tram','tractor','rover','sled','bike','cart'}
 if kind in buildings:
  w=2.4+v*.18; depth=1.8+((variant*3)%4)*.18
  box(p,[0,h/2,0],[w,h,depth]); box(p,[0,.13,0],[w+.3,.26,depth+.3],.68)
  roofkind=kind in {'house','cottage','chalet','barn','hut','teahouse','islandhut','boathouse','canalhouse','adobechapel'}
  if roofkind: cone(p,[0,h+.48,0],[w*1.25,1.15,depth*1.5],.65)
  else: box(p,[0,h+.12,0],[w+.25,.24,depth+.24],.62)
  box(p,[0,.58,-depth/2-.04],[.43,1.12,.1],.42)
  for x in [-.78,.78]:
   for y in ([.75,1.4] if h>2 else [1.15]):
    box(p,[x,y,-depth/2-.06],[.48,.48,.1],.32)
    box(p,[x,y-.29,-depth/2-.1],[.6,.09,.2],1.3)
  for z in [-.48,.48]:box(p,[w/2+.03,1.18,z],[.1,.52,.42],.4)
  if kind in {'bakery','cafe','shop','restaurant','fruitstand','gallery'}:
   box(p,[0,1.58,-depth/2-.45],[w,.15,.9],1.15)
   for x in range(6):box(p,[-w/2+w*(x+.5)/6,1.45,-depth/2-.85],[w/6,.3,.12],.65 if x%2 else 1.15)
   box(p,[0,h-.2,-depth/2-.1],[1.2,.32,.1],1.25)
  if kind in {'museum','bank','university','library','mansion'}:
   for x in [-1,-.5,.5,1]:cyl(p,[x,h/2,-depth/2-.35],[.17,h,.17],1.3)
   cone(p,[0,h+.4,-.25],[w+1,.8,depth+1],1.15)
  if kind in {'hospital','ambulance'}:
   box(p,[0,h+.5,0],[.25,.85,.18],.4);box(p,[0,h+.5,0],[.75,.23,.18],.4)
  if kind in {'factory','sawtooth','textilemill','warehouse','polarbase'}:
   for x in [-.8,.5]:cyl(p,[x,h+.65,.3],[.33,1.6,.33],.6)
  if kind in {'hotel','apartments','office','mall','techhub'}:
   for i in range(2+v):
    box(p,[0,h+i*.72+.48,0],[w*(1-i*.06),.65,depth],.9)
    for x in [-.65,0,.65]:box(p,[x,h+i*.72+.5,-depth/2-.06],[.4,.4,.08],.35)
  if kind in {'station','terminal','hangar'}:
   box(p,[0,h,-2],[w+2,.18,3],1.2)
   for x in [-2,2]:cyl(p,[x,h/2,-2.7],[.12,h,.12],.5)
 elif kind in {'igloo','metro','mill','pillar','playground','shell','treehouse'}:
  if kind=='igloo':
   sphere(p,[0,.65,0],[2.7,2.2,2.7],1.2);sphere(p,[0,.35,-1.3],[1,1.1,1.4],.8)
  elif kind=='pillar':
   box(p,[0,.13,0],[.9,.26,.9],.8);cyl(p,[0,1.4,0],[.45,2.5,.45]);box(p,[0,2.7,0],[.8,.25,.8],1.2)
  elif kind=='shell':
   for i in range(7):
    sphere(p,[(i-3)*.14,.2,abs(i-3)*.13],[.24,.4,1.2],.8+i*.05);p[-1]['ry']=(i-3)*.2
  elif kind=='playground':
   for x in [-1,1]:box(p,[x,1.1,0],[.12,2.2,.12],.6)
   box(p,[0,2.2,0],[2.5,.12,.2]);
   for x in [-.3,.3]:cyl(p,[x,1.4,0],[.035,1.5,.035],.5)
   box(p,[0,.65,0],[.8,.12,.45],1.2)
  elif kind=='metro':
   box(p,[0,.15,0],[3,.3,2],.6)
   for i in range(5):box(p,[0,.25+i*.14,i*.24-.5],[2,.15,.3],.8)
   for x in [-1.3,1.3]:cyl(p,[x,1,0],[.1,2,.1],.5)
   box(p,[0,2,0],[3,.4,.2],1.2)
  elif kind=='treehouse':
   p+=recipe('tree',variant)
   q=recipe('hut',variant)
   for part in q:
    part['p']=[n*.65 for n in part['p']];part['p'][1]+=1.9;part['s']=[n*.65 for n in part['s']]
   p+=q
  else:
   p+=recipe('cottage',variant);torus(p,[1.4,.9,0],[1.5,.2,1.5],.6);p[-1]['rz']=math.pi/2
 elif kind in towers:
  if kind=='crane':
   box(p,[0,3,0],[.35,6,.35],.7);box(p,[1,5.6,0],[5,.32,.4]);box(p,[3,4.5,0],[.05,2.2,.05],.4);box(p,[-1,5.3,0],[.7,.7,.8],.5)
  elif kind in {'turbine','windmill'}:
   cyl(p,[0,2.4,0],[.45,4.8,.45],1.2);sphere(p,[0,4.2,-.38],[.45,.45,.5],.6)
   for i in range(3):
    a=i*math.tau/3; item=dict(mesh='box',p=[math.sin(a)*.85,4.2+math.cos(a)*.85,-.55],s=[.24,2,.13],tint=1.15,rz=-a);p.append(item)
   if kind=='windmill':cone(p,[0,1.4,.1],[2,2.8,2],.8)
  else:
   floors=6+v; twist=kind in {'spiraltower','futuretower','geartower','prismtower'}
   for i in range(floors):
    width=max(.7,2.1-i*.14)
    box(p,[0,.35+i*.63,0],[width,.53,width],.8+i*.025)
    p[-1]['ry']=i*.17 if twist else 0
    box(p,[0,.35+i*.63,-width/2-.015],[width*.8,.25,.06],.35)
   cone(p,[0,floors*.63+.35,0],[.8,.9,.8],1.2)
   if kind=='clocktower':cyl(p,[0,floors*.63-.45,-.85],[.6,.12,.6],1.4);p[-1]['rx']=math.pi/2
   if kind=='lighthouse':sphere(p,[0,floors*.63,0],[1.25,.75,1.25],1.5)
 elif kind in trees:
  if kind in {'tree','pine','palm'}:
   cyl(p,[0,1.0,0],[.24,2,.24],.45)
   if kind=='pine':
    for i in range(3):cone(p,[0,1.2+i*.55,0],[1.9-i*.35,1.5,1.9-i*.35],.85+i*.1)
   elif kind=='palm':
    for i in range(6):
     a=i*math.tau/6;sphere(p,[math.cos(a)*.65,2.1,math.sin(a)*.65],[1.3,.22,.55],1-i*.025);p[-1]['ry']=-a
   else:
    for x,y,z,s in [(-.5,2,0,1.3),(.5,2.25,.1,1.4),(0,2.65,0,1.4),(0,2,.5,1.25)]:sphere(p,[x,y,z],[s,s,s],.9+y*.04)
  elif kind=='mushroom':cyl(p,[0,.35,0],[.22,.7,.22],1.3);sphere(p,[0,.75,0],[1.3,.5,1.3],.9)
  elif kind in {'bamboo','cactus','reed'}:
   for x in [-.38,0,.38]:
    cyl(p,[x,1+abs(x),0],[.2,2+abs(x),.2],.8)
    if kind=='cactus':sphere(p,[x,2+abs(x),0],[.2,.25,.2])
    else:
     for y in [.5,1,1.5]:sphere(p,[x+.18,y,0],[.6,.13,.25],1.1)
  else:
   box(p,[0,.2,0],[1.35,.4,1.0],.6)
   for i in range(5):
    x=(i%3-.8)*.4;z=(i//3-.5)*.35;cyl(p,[x,.58,z],[.045,.5,.045],.55);sphere(p,[x,.9,z],[.48,.3,.48],.9+(i%3)*.15)
 elif kind in cars:
  length=2.8 if kind in {'bus','truck','train','tram'} else 1.7
  box(p,[0,.48,0],[length,.5,.85]);box(p,[-.12,.96,0],[length*.6,.55,.76],1.15)
  for x in [-length*.32,length*.32]:
   for z in [-.49,.49]:cyl(p,[x,.3,z],[.4,.15,.4],.24);p[-1]['rx']=math.pi/2
  for z in [-.39,.39]:box(p,[-.12,1.0,z],[length*.48,.32,.04],.35)
  for z in [-.28,.28]:sphere(p,[length/2+.02,.5,z],[.09,.15,.15],1.6)
  if kind=='ambulance':box(p,[0,1.3,0],[.45,.15,.2],.6)
  if kind=='tractor':cyl(p,[.5,1,0],[.1,.8,.1],.5)
 elif kind in {'boat','sailboat','canoe','submarine'}:
  sphere(p,[0,.35,0],[2.5,.7,1.05]);box(p,[0,.6,0],[1.6,.15,.75],1.3)
  if kind=='sailboat':cyl(p,[0,1.7,0],[.07,2.8,.07],.5);cone(p,[.48,1.9,0],[1.1,2,.12],1.2)
  elif kind=='submarine':sphere(p,[0,.8,0],[1.2,.7,.8],.7);cyl(p,[0,1.3,0],[.15,.7,.15],.4)
  else:box(p,[-.65,.85,0],[.65,.4,.5],.7)
 elif kind in {'fountain','pond','pool','oasis'}:
  cyl(p,[0,.15,0],[3,.3,2.7],.65);cyl(p,[0,.32,0],[2.65,.1,2.35],1.1)
  if kind=='fountain':
   cyl(p,[0,.7,0],[.35,.85,.35],.8);cyl(p,[0,1.1,0],[1.4,.16,1.4],1.2);sphere(p,[0,1.4,0],[.3,.6,.3],1.2)
  if kind=='oasis':p+=recipe('palm',variant)
 elif kind in {'wheel','carousel','gear'}:
  for x in [-1,1]:box(p,[x,1.5,0],[.22,3,.3],.6)
  torus(p,[0,3,0],[3.9,3.9,.22]);p[-1]['rx']=math.pi/2
  for i in range(10):
   a=i*math.tau/10;x=math.sin(a)*1.9;y=3+math.cos(a)*1.9
   box(p,[x,y,0],[.5,.5,.65],.65+i*.05)
  cyl(p,[0,3,0],[.5,.5,.5],1.4);p[-1]['rx']=math.pi/2
 elif kind in {'arch','flowerarch','bamboogate','gate','shrine','pagoda','temple','arcade','promenade'}:
  for x in [-1,1]:cyl(p,[x,1.2,0],[.3,2.4,.3],.8)
  box(p,[0,2.5,0],[2.9,.3,.65],1.15)
  if kind in {'pagoda','shrine','temple'}:
   for i in range(3):cone(p,[0,2.6+i*.6,0],[3.5-i*.65,.85,2.2-i*.3],.8+i*.1)
  elif kind=='flowerarch':
   for x in [-1,-.5,0,.5,1]:sphere(p,[x,2.7,0],[.6,.55,.65],1.1)
 elif kind in {'dome','glassdome','greenhouse','underwaterdome','moondome','planetarium','aquarium','observatory','saltobservatory'}:
  cyl(p,[0,.2,0],[3.7,.4,3.7],.65);sphere(p,[0,.8,0],[3.3,2.8,3.3],1.1)
  for a in [0,math.pi/2]:torus(p,[0,1,0],[3.4,.1,3.4],.65);p[-1]['rx']=a
  box(p,[0,.55,-1.6],[.7,1.1,.5],.4)
  if kind in {'observatory','saltobservatory'}:cyl(p,[0,2.2,-.5],[.5,2,.5],.6);p[-1]['rx']=.9
 elif kind in {'bridge','canyonbridge','pier','monorail','canalhouse'}:
  box(p,[0,1,0],[4,.2,1.4]);
  for x in [-1.6,1.6]:
   box(p,[x,.45,0],[.3,.9,1.4],.6)
  for z in [-.7,.7]:
   box(p,[0,1.65,z],[4,.12,.1],.8)
   for x in [-1.8,-.9,0,.9,1.8]:box(p,[x,1.35,z],[.07,.6,.07],.8)
  if kind=='monorail':
   q=recipe('tram',variant)
   for part in q:part['p'][1]+=1.1
   p+=q
 elif kind in {'rocket','spaceport','plane','airport','drone','cablecar','balloon'}:
  if kind in {'plane','airport','drone'}:
   sphere(p,[0,1,0],[.7,.7,3.5]);box(p,[0,1,.25],[3.5,.13,.8],.8);box(p,[0,1.2,1.3],[1.3,.1,.5],.6);box(p,[0,1.5,1.2],[.1,.9,.6],.65)
   if kind=='airport':box(p,[0,.2,0],[5,.1,5],.5)
  elif kind=='balloon':
   sphere(p,[0,3,0],[2.5,3,2.5]);box(p,[0,.6,0],[.8,.6,.7],.6)
   for x in [-.35,.35]:cyl(p,[x,1.1,0],[.04,1.1,.04],.5)
  elif kind=='cablecar':
   box(p,[0,2,0],[1.8,1.4,1]);box(p,[0,2.2,-.52],[1.4,.6,.05],.4);cyl(p,[0,3,0],[.1,1,.1],.5);box(p,[0,3.5,0],[4,.05,.05],.4)
  else:
   cyl(p,[0,1.5,0],[1,3,1],1.15);cone(p,[0,3.45,0],[1,1,1],.7)
   for x in [-.6,.6]:cone(p,[x,.5,0],[.55,1,.55],.55)
   sphere(p,[0,2.3,-.5],[.35,.35,.08],.4)
 elif kind in {'statue','robot','robotgiant','astronaut','animal','camel','duck','fish','snowman'}:
  sphere(p,[0,.9,0],[1,1.2,.65]);sphere(p,[0,1.85,0],[.8,.8,.8],1.15)
  for x in [-.32,.32]:
   cyl(p,[x,.28,0],[.25,.55,.25],.65);sphere(p,[x*.6,1.93,-.38],[.11,.11,.08],.25)
  for x in [-.63,.63]:box(p,[x,1,0],[.22,.8,.25],.8)
  if kind=='robotgiant':
   for part in p:part['p']=[n*2.2 for n in part['p']];part['s']=[n*2.2 for n in part['s']]
  if kind in {'camel','animal','duck','fish'}:
   for part in p:part['s'][2]*=1.7
 elif kind in {'mountain','rock','volcano','waterfall','crystal','crystalpalace','prismtower'}:
  for i in range(4):
   x=(i%2-.5)*1.2;z=(i//2-.5)*1.1;hh=1.5+(i%3)*.7
   cone(p,[x,hh/2,z],[1.9,hh,1.7],.75+i*.1)
  if kind=='waterfall':box(p,[0,1.3,-.8],[.8,2.5,.12],1.2);cyl(p,[0,.1,-1],[3,.15,2],1.15)
  if kind=='volcano':cone(p,[0,1.9,0],[3,3.8,3],.65);cyl(p,[0,3.4,0],[.8,.15,.8],1.5)
 elif kind in {'bench','table','piano','easel','loom'}:
  box(p,[0,.65,0],[1.7,.15,.7]);
  for x in [-.65,.65]:
   for z in [-.23,.23]:box(p,[x,.3,z],[.1,.6,.1],.5)
  if kind=='bench':box(p,[0,1.1,.3],[1.7,.6,.12],.85)
  if kind=='piano':box(p,[0,1.15,.18],[1.7,.9,.3],.65)
 elif kind in {'lamp','lantern','sign','flag','buoy','surfboard','mailbox'}:
  cyl(p,[0,1,0],[.12,2,.12],.55);cyl(p,[0,.08,0],[.5,.16,.5],.6)
  if kind in {'lamp','lantern','buoy'}:sphere(p,[0,2.05,0],[.55,.65,.55],1.35);cone(p,[0,2.5,0],[.85,.4,.85],.6)
  else:box(p,[.3,1.8,0],[1.2,.65,.12]);box(p,[.3,1.8,-.08],[.85,.1,.05],1.4)
 elif kind in {'stall','market','awning','tent','gazebo','stage','concert','zoo','parasol','hammock','net'}:
  for x in [-1,1]:
   for z in [-.7,.7]:cyl(p,[x,1,z],[.1,2,.1],.6)
  cone(p,[0,2.2,0],[3,.9,2.4],1.1);box(p,[0,.18,0],[2.5,.36,1.9],.6)
  if kind in {'market','stall'}:box(p,[0,.8,-.6],[2,.2,.5],.8)
 elif kind in {'stadium','court','racetrack','waterslide'}:
  box(p,[0,.1,0],[4.5,.2,3.2],.7)
  for z in [-1.7,1.7]:
   for i in range(3):box(p,[0,.3+i*.25,z+i*.25*(1 if z>0 else -1)],[4,.25,.3],.85+i*.1)
  if kind=='waterslide':
   for i in range(12):cyl(p,[math.sin(i*.35),.3+i*.17,math.cos(i*.35)],[.8,.15,.8],1.1)
 elif kind in {'solar','panel','dish','gardenpod','sphere','planet','sculpture','cloudcastle'}:
  cyl(p,[0,.6,0],[.2,1.2,.2],.5)
  if kind in {'solar','panel'}:
   box(p,[0,1.25,0],[2,.1,1.4],.5);p[-1]['rx']=.3
   for x in [-.6,0,.6]:box(p,[x,1.34,0],[.025,.02,1.35],1.4)
  else:
   sphere(p,[0,1.6,0],[2,1.6,2],1.1);torus(p,[0,1.6,0],[2.8,.08,2.8],.6);p[-1]['rx']=.3
 elif kind in {'fence','roll','log','pipe','tank','silo','pot','well','kiln','container','crate','hay','tire','ball','speaker','fire','birdhouse','coral','terrace','ski'}:
  if kind=='fence':
   for x in [-1,-.5,0,.5,1]:box(p,[x,.55,0],[.15,1.1,.15],.9)
   for y in [.3,.8]:box(p,[0,y,0],[2.3,.12,.13],.7)
  elif kind in {'tank','silo','pot','well','kiln','roll','pipe','log'}:
   cyl(p,[0,.6,0],[1.1,1.2,1.1]);torus(p,[0,1.22,0],[1.15,.1,1.15],.65)
  elif kind in {'ball','hay','fire','coral','tire'}:
   for i in range(3):sphere(p,[(i-1)*.4,.5+i*.2,0],[.7,.9,.7],.8+i*.15)
  else:
   box(p,[0,.5,0],[1.4,1,1.2]);
   for y in [.15,.8]:box(p,[0,y,-.62],[1.5,.1,.08],.6)
 else:raise ValueError('Missing actual mesh recipe: '+kind)
 return p

def main():
 rng=random.Random(20260911);areas=[];levels=[];seen=set()
 for ai,row in enumerate(CATALOG):
  name,landmark,biome,kinds=row.split('|');kinds=kinds.split();gx=ai%10 if (ai//10)%2==0 else 9-ai%10;gz=ai//10
  area={'id':ai+1,'name':name,'biome':biome,'origin':[gx*24,0,gz*24],'landmark':landmark,'composition':ai%5,'groups':[],'ambient':biome,'music':ai%6,'unlock_stars':30}
  for j in range(15):
   lid=ai*15+j+1;kind=landmark if j==0 else kinds[(j-1)%len(kinds)]
   # Landmark at north, props placed on an authored ring/grid, never overlapping paths.
   if j==0:pos=[0,0,-4.7]
   else:
    theta=(j-1)*math.tau/14+(ai%3)*.1;radius=7.3 if j%2 else 9.1
    pos=[round(math.cos(theta)*radius,3),0,round(math.sin(theta)*radius,3)]
   while True:
    if lid<=30:
     vals=[rng.randint(30,220),rng.randint(30,220)];dom=rng.randrange(3);r=[vals[0],vals[1],1000-sum(vals)];r=r[dom:]+r[:dom]
    else:
     a=rng.randint(10,920);b=rng.randint(10,990-a);r=[a,b,1000-a-b];rng.shuffle(r)
     if lid>400:
      strength=.75 if lid<801 else .48 if lid<1201 else .3
      r=[round(333+(n-333)*strength) for n in r];r[2]=1000-r[0]-r[1]
    bright=round(rng.uniform(.35,.95),3);sat=round(rng.uniform(.7,1) if lid<401 else rng.uniform(.32,.8),3)
    key=tuple(r)+(bright,sat)
    if key not in seen:seen.add(key);break
   primitives=recipe(kind,ai+j)
   uid=f'paint_{lid:04d}';tool=['brush','roller','spray','wide_brush','splash','magic'][j%6]
   group={'id':uid,'level_id':lid,'name':kind.replace('_',' ').title(),'kind':kind,'position':pos,'rotation':round((ai%4)*.15+(j%3)*.23,3),'primitives':primitives,'life':kind in {'car','boat','fountain','lamp','wheel','tree','turbine','robot','balloon','drone','fish','windmill'},'paint_tool':tool}
   area['groups'].append(group)
   levels.append({'id':lid,'area_id':ai+1,'target':r,'brightness':bright,'saturation':sat,'difficulty':next(i for i,n in enumerate([30,150,400,800,1200,1500],1) if lid<=n),'world_object_id':uid,'paint_group_id':uid,'camera':{'focus':[pos[0]+gx*24,1.3,pos[2]+gz*24],'distance':11 if j else 15,'yaw':.5+(j%5)*.25,'pitch':.6},'paint_tool':tool,'rewards':[0,10,20,35,50]})
  areas.append(area)
 out={'schema_version':1,'seed':20260911,'area_count':100,'level_count':1500,'max_stars':4500,'areas':areas,'levels':levels}
 (ROOT/'data/world.json').write_text(json.dumps(out,separators=(',',':')))
 manifest={'sha256':hashlib.sha256((ROOT/'data/world.json').read_bytes()).hexdigest(),'areas':len(areas),'levels':len(levels),'groups':sum(len(a['groups']) for a in areas),'primitives':sum(len(g['primitives']) for a in areas for g in a['groups']),'unique_landmarks':len(set(a['landmark'] for a in areas))}
 (ROOT/'data/content_manifest.json').write_text(json.dumps(manifest,indent=2));print(manifest)
if __name__=='__main__':main()
