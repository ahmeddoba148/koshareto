using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class KosharetoBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        if (UnityEngine.Object.FindFirstObjectByType<KosharetoGame>() != null) return;
        var go = new GameObject("KosharetoGame");
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<KosharetoGame>();
    }
}

public class KosharetoGame : MonoBehaviour
{
    readonly string[] ingredients = { "RICE", "PASTA", "LENTILS", "CHICKPEAS", "TOMATO", "GARLIC", "CHILI", "ONION" };
    readonly string[] names = { "MIDO", "NOUR", "SALMA", "HASSAN", "MARIAM", "OMAR", "YOUSSEF", "FARAH", "NADA", "KARIM" };
    readonly Color[] foodColors = {
        new Color(.94f,.85f,.60f), new Color(.93f,.68f,.31f), new Color(.48f,.25f,.13f), new Color(.84f,.67f,.34f),
        new Color(.72f,.10f,.07f), new Color(.87f,.82f,.57f), new Color(.62f,.05f,.04f), new Color(.45f,.20f,.07f)
    };

    static readonly Color Cream = new Color(.96f,.87f,.70f);
    static readonly Color Dark = new Color(.075f,.05f,.035f);
    static readonly Color Gold = new Color(.95f,.61f,.12f);
    static readonly Color Green = new Color(.16f,.55f,.28f);
    static readonly Color Red = new Color(.62f,.06f,.035f);

    Font font;
    Canvas canvas;
    RectTransform safe;
    GameObject world, activeCustomer, bowlLayers, startPanel, hud, endPanel;
    Text dayText, cashText, ratingText, timerText, customerText, orderText, bowlText, feedbackText, startStats, endTitle, endStats;
    Slider patienceBar;
    Button nextButton, patUpgrade, priceUpgrade, tipsUpgrade;
    readonly Dictionary<string,int> stock = new Dictionary<string,int>();
    readonly Dictionary<string,Text> stockLabels = new Dictionary<string,Text>();
    readonly Dictionary<string,Button> ingredientButtons = new Dictionary<string,Button>();
    readonly List<string> order = new List<string>();
    readonly List<string> bowl = new List<string>();

    AudioSource audioSource;
    AudioClip clickClip, okClip, badClip, coinClip;

    int day, cash, served, target, mistakes, combo, bestCombo, patienceLevel, priceLevel, tipsLevel;
    float rating, timeLeft, patience, maxPatience;
    bool playing, switching;
    string sizeName;
    int basePrice;

    void Awake()
    {
        Screen.orientation = ScreenOrientation.Portrait;
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        QualitySettings.vSyncCount = 0;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Load();
        BuildWorld();
        BuildAudio();
        BuildUI();
        ShowStart();
    }

    void Update()
    {
        ApplySafeArea();
        if (!playing || switching) return;
        timeLeft = Mathf.Max(0, timeLeft - Time.unscaledDeltaTime);
        patience = Mathf.Max(0, patience - Time.unscaledDeltaTime);
        int s = Mathf.CeilToInt(timeLeft);
        timerText.text = string.Format("{0:00}:{1:00}", s/60, s%60);
        timerText.color = timeLeft < 20 ? new Color(1,.35f,.2f) : Cream;
        patienceBar.value = maxPatience <= 0 ? 0 : patience/maxPatience;
        if (patience <= 0) StartCoroutine(CustomerLeaves(false, "TOO SLOW!"));
        else if (timeLeft <= 0) FinishDay();
    }

    void Load()
    {
        day = PlayerPrefs.GetInt("day",1);
        cash = PlayerPrefs.GetInt("cash",40);
        patienceLevel = PlayerPrefs.GetInt("pat",0);
        priceLevel = PlayerPrefs.GetInt("price",0);
        tipsLevel = PlayerPrefs.GetInt("tips",0);
        rating = PlayerPrefs.GetFloat("rating",4.5f);
    }

    void Save()
    {
        PlayerPrefs.SetInt("day",day); PlayerPrefs.SetInt("cash",cash);
        PlayerPrefs.SetInt("pat",patienceLevel); PlayerPrefs.SetInt("price",priceLevel); PlayerPrefs.SetInt("tips",tipsLevel);
        PlayerPrefs.SetFloat("rating",rating); PlayerPrefs.Save();
    }

    void BuildAudio()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.volume = .65f;
        clickClip = Tone("click",700,.05f,.16f); okClip = Tone("ok",970,.11f,.24f);
        badClip = Tone("bad",175,.17f,.22f); coinClip = Tone("coin",1320,.08f,.18f);
    }

    AudioClip Tone(string n, float hz, float length, float volume)
    {
        int rate=22050, count=Mathf.Max(1,Mathf.RoundToInt(rate*length)); float[] data=new float[count];
        for(int i=0;i<count;i++){ float t=i/(float)rate, env=1f-i/(float)count; data[i]=Mathf.Sin(2*Mathf.PI*hz*t)*volume*env; }
        var c=AudioClip.Create(n,count,1,rate,false); c.SetData(data,0); return c;
    }

    void Sfx(AudioClip c){ if(c!=null) audioSource.PlayOneShot(c); }

    void BuildWorld()
    {
        world = new GameObject("Koshareto World");
        RenderSettings.ambientLight = new Color(.40f,.27f,.16f);
        RenderSettings.fog = true; RenderSettings.fogColor = new Color(.12f,.07f,.04f); RenderSettings.fogDensity=.012f;

        var cam = new GameObject("Main Camera").AddComponent<Camera>(); cam.tag="MainCamera";
        cam.clearFlags=CameraClearFlags.SolidColor; cam.backgroundColor=new Color(.05f,.03f,.02f); cam.fieldOfView=46;
        cam.transform.position=new Vector3(0,4.7f,-11); cam.transform.LookAt(new Vector3(0,1.6f,.8f));

        var sun=new GameObject("Sun").AddComponent<Light>(); sun.type=LightType.Directional; sun.color=new Color(1,.78f,.55f); sun.intensity=1.2f;
        sun.shadows=LightShadows.Soft; sun.transform.rotation=Quaternion.Euler(48,-30,0);
        PointLight(new Vector3(-3,3.8f,1),new Color(1,.42f,.16f),3.2f,9);
        PointLight(new Vector3(3,3.8f,1),new Color(1,.7f,.3f),2.6f,9);

        Box("Floor",new Vector3(0,-.15f,1),new Vector3(10,.3f,10),new Color(.18f,.12f,.08f));
        Box("Back wall",new Vector3(0,2.5f,4.6f),new Vector3(10,5,.3f),new Color(.44f,.14f,.075f));
        Box("Left wall",new Vector3(-5,2.5f,1.8f),new Vector3(.25f,5,5.7f),new Color(.54f,.4f,.26f));
        Box("Right wall",new Vector3(5,2.5f,1.8f),new Vector3(.25f,5,5.7f),new Color(.54f,.4f,.26f));
        Box("Sign",new Vector3(0,3.55f,4.36f),new Vector3(5.8f,1.05f,.15f),Dark);
        Box("Sign stripe",new Vector3(0,3.55f,4.25f),new Vector3(5.2f,.14f,.08f),Gold);
        Box("Counter",new Vector3(0,.75f,1.95f),new Vector3(8.6f,1.5f,1.1f),new Color(.27f,.11f,.055f));
        Box("Counter top",new Vector3(0,1.55f,1.95f),new Vector3(8.9f,.12f,1.25f),new Color(.82f,.58f,.32f));

        float[] xs={-3.35f,-2.4f,-1.45f,-.5f,.5f,1.45f,2.4f,3.35f};
        for(int i=0;i<ingredients.Length;i++) Pot(xs[i],ingredients[i],foodColors[i],i);

        var bowlRoot=new GameObject("Serving bowl"); bowlRoot.transform.SetParent(world.transform,false); bowlRoot.transform.position=new Vector3(0,1.72f,1.12f);
        CylinderChild(bowlRoot.transform,"Bowl",Vector3.zero,.72f,.22f,new Color(.92f,.84f,.71f));
        CylinderChild(bowlRoot.transform,"Inside",new Vector3(0,.13f,0),.58f,.05f,new Color(.22f,.13f,.08f));
        bowlLayers=new GameObject("Food layers"); bowlLayers.transform.SetParent(bowlRoot.transform,false);

        for(int i=0;i<3;i++) Person(new Vector3(2.8f+i*.55f,0,-.15f-i*.5f),Color.HSVToRGB(.55f+i*.1f,.45f,.7f),.55f);
        SpawnCustomer(true);
    }

    void PointLight(Vector3 p,Color c,float intensity,float range)
    { var l=new GameObject("Shop light").AddComponent<Light>(); l.transform.position=p; l.type=LightType.Point;l.color=c;l.intensity=intensity;l.range=range;l.shadows=LightShadows.None; }

    Material Material(Color c,float metallic=0,float smooth=.25f)
    {
        var shader=Shader.Find("Standard"); if(shader==null) shader=Shader.Find("Universal Render Pipeline/Lit");
        var m=new Material(shader); m.color=c; if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",metallic); if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",smooth); return m;
    }

    GameObject Box(string n,Vector3 p,Vector3 s,Color c)
    { var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(world.transform,false);g.transform.position=p;g.transform.localScale=s;g.GetComponent<Renderer>().material=Material(c);Destroy(g.GetComponent<Collider>());return g; }

    GameObject Cylinder(string n,Vector3 p,float r,float h,Color c)
    { var g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name=n;g.transform.SetParent(world.transform,false);g.transform.position=p;g.transform.localScale=new Vector3(r*2,h*.5f,r*2);g.GetComponent<Renderer>().material=Material(c,.05f,.35f);Destroy(g.GetComponent<Collider>());return g; }

    GameObject CylinderChild(Transform parent,string n,Vector3 p,float r,float h,Color c)
    { var g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name=n;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=new Vector3(r*2,h*.5f,r*2);g.GetComponent<Renderer>().material=Material(c);Destroy(g.GetComponent<Collider>());return g; }

    void Pot(float x,string label,Color c,int i)
    { var p=Cylinder("Pot "+label,new Vector3(x,1.79f,2.02f),.39f,.28f,new Color(.18f,.18f,.17f)); CylinderChild(p.transform,"Food",new Vector3(0,.2f,0),.32f,.06f,c); if(i==4||i==5) Steam(p.transform); }

    void Steam(Transform parent)
    {
        var ps=new GameObject("Steam").AddComponent<ParticleSystem>(); ps.transform.SetParent(parent,false);ps.transform.localPosition=new Vector3(0,.42f,0);
        var main=ps.main;main.startLifetime=1.2f;main.startSpeed=.42f;main.startSize=.1f;main.startColor=new Color(1,.92f,.8f,.18f);main.maxParticles=24;
        var em=ps.emission;em.rateOverTime=8;var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=10;shape.radius=.15f;
    }

    GameObject Person(Vector3 p,Color shirt,float scale)
    {
        var root=new GameObject("Customer");root.transform.SetParent(world.transform,false);root.transform.position=p;
        var body=GameObject.CreatePrimitive(PrimitiveType.Capsule);body.transform.SetParent(root.transform,false);body.transform.localPosition=new Vector3(0,.95f,0);body.transform.localScale=new Vector3(scale,.78f,scale);body.GetComponent<Renderer>().material=Material(shirt);Destroy(body.GetComponent<Collider>());
        var head=GameObject.CreatePrimitive(PrimitiveType.Sphere);head.transform.SetParent(root.transform,false);head.transform.localPosition=new Vector3(0,1.82f,0);head.transform.localScale=Vector3.one*scale*.78f;head.GetComponent<Renderer>().material=Material(new Color(.72f,.48f,.33f));Destroy(head.GetComponent<Collider>());
        var hair=GameObject.CreatePrimitive(PrimitiveType.Sphere);hair.transform.SetParent(root.transform,false);hair.transform.localPosition=new Vector3(0,2.02f,0);hair.transform.localScale=new Vector3(scale*.8f,scale*.36f,scale*.8f);hair.GetComponent<Renderer>().material=Material(new Color(.08f,.055f,.04f));Destroy(hair.GetComponent<Collider>());
        return root;
    }

    void SpawnCustomer(bool instant)
    {
        if(activeCustomer!=null)Destroy(activeCustomer); Color shirt=Color.HSVToRGB(UnityEngine.Random.value,.48f,.72f);
        activeCustomer=Person(instant?new Vector3(0,0,.05f):new Vector3(-3.8f,0,-.6f),shirt,.82f);activeCustomer.name="Active customer";
        if(!instant)StartCoroutine(Move(activeCustomer.transform,new Vector3(0,0,.05f),.42f));
    }

    IEnumerator Move(Transform t,Vector3 end,float duration)
    { Vector3 start=t.position;float x=0;while(x<duration&&t!=null){x+=Time.unscaledDeltaTime;t.position=Vector3.Lerp(start,end,Mathf.SmoothStep(0,1,Mathf.Clamp01(x/duration)));yield return null;}if(t!=null)t.position=end; }

    void BuildUI()
    {
        var cg=new GameObject("Portrait UI");cg.transform.SetParent(transform,false);canvas=cg.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=50;
        var scaler=cg.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1080,1920);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;scaler.matchWidthOrHeight=.5f;
        cg.AddComponent<GraphicRaycaster>();
        if(FindFirstObjectByType<EventSystem>()==null){var es=new GameObject("EventSystem");es.AddComponent<EventSystem>();es.AddComponent<StandaloneInputModule>();DontDestroyOnLoad(es);}
        safe=new GameObject("SafeArea",typeof(RectTransform)).GetComponent<RectTransform>();safe.SetParent(canvas.transform,false);safe.anchorMin=Vector2.zero;safe.anchorMax=Vector2.one;safe.offsetMin=safe.offsetMax=Vector2.zero;
        BuildHud();BuildStart();BuildEnd();
    }

    void ApplySafeArea()
    {
        if(safe==null||Screen.width<=0||Screen.height<=0)return;Rect r=Screen.safeArea;Vector2 min=r.position,max=r.position+r.size;min.x/=Screen.width;min.y/=Screen.height;max.x/=Screen.width;max.y/=Screen.height;
        if(safe.anchorMin!=min||safe.anchorMax!=max){safe.anchorMin=min;safe.anchorMax=max;safe.offsetMin=safe.offsetMax=Vector2.zero;}
    }

    void BuildHud()
    {
        hud=Panel("HUD",safe,new Color(0,0,0,0),Vector2.zero,Vector2.one);
        var top=Panel("Top bar",hud.transform,new Color(.055f,.035f,.025f,.95f),new Vector2(.025f,.91f),new Vector2(.975f,.985f));Outline(top,new Color(.72f,.42f,.12f,.7f),2);
        dayText=Label("DAY",top.transform,32,FontStyle.Bold,Gold,TextAnchor.MiddleLeft,new Vector2(.03f,0),new Vector2(.32f,1));
        cashText=Label("$40",top.transform,33,FontStyle.Bold,Cream,TextAnchor.MiddleCenter,new Vector2(.32f,0),new Vector2(.57f,1));
        ratingText=Label("★ 4.5",top.transform,30,FontStyle.Bold,new Color(1,.78f,.22f),TextAnchor.MiddleCenter,new Vector2(.57f,0),new Vector2(.78f,1));
        timerText=Label("02:00",top.transform,32,FontStyle.Bold,Cream,TextAnchor.MiddleRight,new Vector2(.78f,0),new Vector2(.97f,1));

        var ticket=Panel("Order",hud.transform,new Color(.07f,.045f,.03f,.95f),new Vector2(.035f,.70f),new Vector2(.965f,.90f));Outline(ticket,new Color(.9f,.51f,.12f,.85f),3);
        customerText=Label("CUSTOMER",ticket.transform,23,FontStyle.Bold,Gold,TextAnchor.UpperLeft,new Vector2(.04f,.72f),new Vector2(.96f,.96f));
        orderText=Label("ORDER",ticket.transform,28,FontStyle.Bold,Color.white,TextAnchor.MiddleLeft,new Vector2(.04f,.22f),new Vector2(.96f,.72f));orderText.horizontalOverflow=HorizontalWrapMode.Wrap;orderText.verticalOverflow=VerticalWrapMode.Truncate;
        patienceBar=SliderBar(ticket.transform,new Vector2(.04f,.06f),new Vector2(.96f,.18f));
        feedbackText=Label("",hud.transform,34,FontStyle.Bold,Color.white,TextAnchor.MiddleCenter,new Vector2(.06f,.60f),new Vector2(.94f,.68f));feedbackText.resizeTextForBestFit=true;feedbackText.resizeTextMinSize=20;feedbackText.resizeTextMaxSize=38;TextOutline(feedbackText,Dark,2);

        var deck=Panel("Controls",hud.transform,new Color(.045f,.03f,.02f,.98f),new Vector2(.02f,.015f),new Vector2(.98f,.46f));Outline(deck,new Color(.64f,.37f,.12f,.8f),3);
        bowlText=Label("BOWL: EMPTY",deck.transform,20,FontStyle.Bold,new Color(.82f,.73f,.62f),TextAnchor.MiddleLeft,new Vector2(.04f,.87f),new Vector2(.96f,.98f));
        float left=.035f,right=.965f,topY=.84f,bottom=.31f,gx=.018f,gy=.025f,cw=(right-left-gx)/2,ch=(topY-bottom-gy*3)/4;
        for(int i=0;i<ingredients.Length;i++){
            int row=i/2,col=i%2;float x0=left+col*(cw+gx),y1=topY-row*(ch+gy),y0=y1-ch;string item=ingredients[i];
            var b=UIButton(item,deck.transform,foodColors[i],Color.white,new Vector2(x0,y0),new Vector2(x0+cw,y1),25);b.onClick.AddListener(()=>AddIngredient(item));ingredientButtons[item]=b;
            stockLabels[item]=Label("x12",b.transform,17,FontStyle.Bold,new Color(1,1,1,.82f),TextAnchor.LowerRight,new Vector2(.68f,.02f),new Vector2(.96f,.42f));
        }
        var serve=UIButton("SERVE",deck.transform,Green,Color.white,new Vector2(.035f,.055f),new Vector2(.50f,.255f),32);serve.onClick.AddListener(Serve);
        var clear=UIButton("CLEAR",deck.transform,Red,Color.white,new Vector2(.515f,.055f),new Vector2(.72f,.255f),24);clear.onClick.AddListener(()=>ClearBowl(true));
        var restock=UIButton("RESTOCK",deck.transform,new Color(.20f,.30f,.43f),Color.white,new Vector2(.735f,.055f),new Vector2(.965f,.255f),21);restock.onClick.AddListener(Restock);
    }

    void BuildStart()
    {
        startPanel=Panel("Start",safe,new Color(.04f,.025f,.018f,.98f),Vector2.zero,Vector2.one);
        var plate=Panel("Logo",startPanel.transform,new Color(.20f,.075f,.025f,.97f),new Vector2(.08f,.57f),new Vector2(.92f,.82f));Outline(plate,Gold,4);
        var logo=Label("KOSHARETO",plate.transform,66,FontStyle.Bold,new Color(1,.72f,.2f),TextAnchor.MiddleCenter,new Vector2(.03f,.35f),new Vector2(.97f,.82f));TextOutline(logo,new Color(.35f,.08f,.02f),3);
        Label("EGYPTIAN KOSHARY TYCOON",plate.transform,21,FontStyle.Bold,Cream,TextAnchor.MiddleCenter,new Vector2(.04f,.12f),new Vector2(.96f,.38f));
        Label("Build the bowl. Beat the rush. Grow the shop.",startPanel.transform,25,FontStyle.Normal,Cream,TextAnchor.MiddleCenter,new Vector2(.08f,.45f),new Vector2(.92f,.55f));
        var play=UIButton("OPEN SHOP",startPanel.transform,Green,Color.white,new Vector2(.12f,.30f),new Vector2(.88f,.40f),35);play.onClick.AddListener(StartDay);
        startStats=Label("",startPanel.transform,21,FontStyle.Bold,new Color(.77f,.68f,.58f),TextAnchor.UpperCenter,new Vector2(.08f,.12f),new Vector2(.92f,.27f));
        Label("PORTRAIT MOBILE • OFFLINE • NO ADS",startPanel.transform,17,FontStyle.Normal,new Color(.58f,.52f,.48f),TextAnchor.MiddleCenter,new Vector2(.05f,.03f),new Vector2(.95f,.08f));
    }

    void BuildEnd()
    {
        endPanel=Panel("End",safe,new Color(.03f,.02f,.015f,.985f),Vector2.zero,Vector2.one);
        endTitle=Label("DAY COMPLETE",endPanel.transform,50,FontStyle.Bold,Gold,TextAnchor.MiddleCenter,new Vector2(.06f,.78f),new Vector2(.94f,.90f));
        endStats=Label("",endPanel.transform,26,FontStyle.Bold,Cream,TextAnchor.UpperCenter,new Vector2(.08f,.59f),new Vector2(.92f,.76f));
        Label("SHOP UPGRADES",endPanel.transform,23,FontStyle.Bold,new Color(.75f,.64f,.52f),TextAnchor.MiddleCenter,new Vector2(.08f,.51f),new Vector2(.92f,.57f));
        patUpgrade=UIButton("PATIENCE",endPanel.transform,new Color(.18f,.34f,.48f),Color.white,new Vector2(.10f,.40f),new Vector2(.90f,.49f),24);
        priceUpgrade=UIButton("PRICE",endPanel.transform,new Color(.44f,.27f,.10f),Color.white,new Vector2(.10f,.29f),new Vector2(.90f,.38f),24);
        tipsUpgrade=UIButton("TIPS",endPanel.transform,new Color(.30f,.18f,.39f),Color.white,new Vector2(.10f,.18f),new Vector2(.90f,.27f),24);
        patUpgrade.onClick.AddListener(()=>BuyUpgrade(0));priceUpgrade.onClick.AddListener(()=>BuyUpgrade(1));tipsUpgrade.onClick.AddListener(()=>BuyUpgrade(2));
        nextButton=UIButton("NEXT DAY",endPanel.transform,Green,Color.white,new Vector2(.12f,.055f),new Vector2(.88f,.145f),33);
    }

    GameObject Panel(string n,Transform parent,Color c,Vector2 min,Vector2 max)
    { var g=new GameObject(n,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));g.transform.SetParent(parent,false);var rt=g.GetComponent<RectTransform>();rt.anchorMin=min;rt.anchorMax=max;rt.offsetMin=rt.offsetMax=Vector2.zero;g.GetComponent<Image>().color=c;return g; }

    Text Label(string value,Transform parent,int size,FontStyle style,Color c,TextAnchor anchor,Vector2 min,Vector2 max)
    { var g=new GameObject("Text",typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));g.transform.SetParent(parent,false);var rt=g.GetComponent<RectTransform>();rt.anchorMin=min;rt.anchorMax=max;rt.offsetMin=rt.offsetMax=Vector2.zero;var t=g.GetComponent<Text>();t.font=font;t.text=value;t.fontSize=size;t.fontStyle=style;t.color=c;t.alignment=anchor;t.supportRichText=true;return t; }

    Button UIButton(string text,Transform parent,Color bg,Color fg,Vector2 min,Vector2 max,int size)
    {
        var g=new GameObject(text+" button",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(Button));g.transform.SetParent(parent,false);var rt=g.GetComponent<RectTransform>();rt.anchorMin=min;rt.anchorMax=max;rt.offsetMin=rt.offsetMax=Vector2.zero;g.GetComponent<Image>().color=bg;
        var b=g.GetComponent<Button>();var cs=b.colors;cs.normalColor=Color.white;cs.highlightedColor=new Color(1.05f,1.05f,1.05f);cs.pressedColor=new Color(.78f,.78f,.78f);cs.disabledColor=new Color(.35f,.35f,.35f,.5f);b.colors=cs;
        var l=Label(text,g.transform,size,FontStyle.Bold,fg,TextAnchor.MiddleCenter,new Vector2(.03f,.03f),new Vector2(.97f,.97f));l.raycastTarget=false;Outline(g,new Color(1,1,1,.1f),1.5f);return b;
    }

    Slider SliderBar(Transform parent,Vector2 min,Vector2 max)
    {
        var g=new GameObject("Patience",typeof(RectTransform),typeof(Slider));g.transform.SetParent(parent,false);var rt=g.GetComponent<RectTransform>();rt.anchorMin=min;rt.anchorMax=max;rt.offsetMin=rt.offsetMax=Vector2.zero;
        Panel("BG",g.transform,new Color(.14f,.10f,.08f),Vector2.zero,Vector2.one);var area=new GameObject("Fill area",typeof(RectTransform));area.transform.SetParent(g.transform,false);var ar=area.GetComponent<RectTransform>();ar.anchorMin=new Vector2(.01f,.16f);ar.anchorMax=new Vector2(.99f,.84f);ar.offsetMin=ar.offsetMax=Vector2.zero;var fill=Panel("Fill",area.transform,Green,Vector2.zero,Vector2.one);
        var s=g.GetComponent<Slider>();s.minValue=0;s.maxValue=1;s.value=1;s.fillRect=fill.GetComponent<RectTransform>();s.interactable=false;return s;
    }

    void Outline(GameObject g,Color c,float d){var o=g.AddComponent<Outline>();o.effectColor=c;o.effectDistance=new Vector2(d,-d);o.useGraphicAlpha=true;}
    void TextOutline(Text t,Color c,int d){var o=t.gameObject.AddComponent<Outline>();o.effectColor=c;o.effectDistance=new Vector2(d,-d);}

    void ShowStart()
    {
        playing=false;hud.SetActive(false);endPanel.SetActive(false);startPanel.SetActive(true);
        startStats.text=string.Format("DAY {0}   •   CASH ${1}\nPATIENCE Lv.{2}   PRICE Lv.{3}   TIPS Lv.{4}",day,cash,patienceLevel,priceLevel,tipsLevel);
    }

    void StartDay()
    {
        Sfx(clickClip);startPanel.SetActive(false);endPanel.SetActive(false);hud.SetActive(true);playing=true;switching=false;served=0;mistakes=0;combo=0;bestCombo=0;
        target=Mathf.Clamp(7+day,8,18);timeLeft=105+Mathf.Min(day,5)*5;rating=Mathf.Clamp(rating,2.5f,5);
        stock.Clear();foreach(string i in ingredients)stock[i]=10+Mathf.Min(day,5);UpdateStock();ClearBowl(false);UpdateHud();NewOrder(true);Flash("SHOP OPEN!",Gold);
    }

    void NewOrder(bool instant=false)
    {
        order.Clear();order.Add("RICE");order.Add("PASTA");order.Add("LENTILS");order.Add("TOMATO");
        if(UnityEngine.Random.value<.78f)order.Add("CHICKPEAS");if(UnityEngine.Random.value<.58f+Mathf.Min(day*.02f,.18f))order.Add("GARLIC");if(UnityEngine.Random.value<.34f+Mathf.Min(day*.025f,.25f))order.Add("CHILI");if(UnityEngine.Random.value<.62f)order.Add("ONION");
        int p=UnityEngine.Random.Range(0,3);sizeName=p==0?"SMALL":p==1?"MEDIUM":"LARGE";basePrice=(p==0?18:p==1?24:31)+priceLevel*3;
        maxPatience=Mathf.Max(9,22+patienceLevel*2.6f-Mathf.Min(day*.55f,6));patience=maxPatience;
        customerText.text=names[UnityEngine.Random.Range(0,names.Length)]+"  •  "+sizeName;orderText.text=string.Join("  +  ",order.ToArray());SpawnCustomer(instant);UpdateHud();
    }

    void AddIngredient(string item)
    {
        if(!playing||switching)return;if(!stock.ContainsKey(item)||stock[item]<=0){Flash("OUT OF "+item+"!",new Color(1,.3f,.2f));Sfx(badClip);return;}if(bowl.Contains(item)){Flash("ALREADY ADDED",Gold);return;}
        stock[item]--;bowl.Add(item);Sfx(clickClip);int idx=Array.IndexOf(ingredients,item);var layer=GameObject.CreatePrimitive(PrimitiveType.Cylinder);layer.name=item;layer.transform.SetParent(bowlLayers.transform,false);layer.transform.localPosition=new Vector3(0,.18f+bowl.Count*.045f,0);layer.transform.localScale=new Vector3(.5f,.018f,.5f);layer.GetComponent<Renderer>().material=Material(foodColors[idx]);Destroy(layer.GetComponent<Collider>());UpdateStock();UpdateBowl();
    }

    void ClearBowl(bool message)
    { bowl.Clear();if(bowlLayers!=null)for(int i=bowlLayers.transform.childCount-1;i>=0;i--)Destroy(bowlLayers.transform.GetChild(i).gameObject);UpdateBowl();if(message&&playing)Flash("BOWL CLEARED",Cream); }

    void Serve()
    {
        if(!playing||switching)return;if(bowl.Count==0){Flash("BUILD THE BOWL FIRST!",Gold);Sfx(badClip);return;}
        bool correct=bowl.Count==order.Count;if(correct)foreach(string i in order)if(!bowl.Contains(i)){correct=false;break;}
        if(!correct){mistakes++;combo=0;rating=Mathf.Clamp(rating-.16f,1,5);Sfx(badClip);Flash("WRONG ORDER!",new Color(1,.25f,.18f));UpdateHud();return;}
        float pr=maxPatience<=0?0:patience/maxPatience;int tip=UnityEngine.Random.value<.08f+tipsLevel*.08f+pr*.12f?UnityEngine.Random.Range(2,7)+tipsLevel*2:0;int earn=basePrice+tip+Mathf.Min(combo,5);
        cash+=earn;served++;combo++;bestCombo=Mathf.Max(bestCombo,combo);rating=Mathf.Clamp(rating+.035f+pr*.025f,1,5);Sfx(okClip);if(tip>0)Sfx(coinClip);Flash("PERFECT!  +$"+earn+(tip>0?"  TIP!":""),Green);ClearBowl(false);UpdateHud();
        if(served>=target){FinishDay();return;}StartCoroutine(CustomerLeaves(true,""));
    }

    IEnumerator CustomerLeaves(bool happy,string message)
    {
        if(switching)yield break;switching=true;if(!happy){mistakes++;combo=0;rating=Mathf.Clamp(rating-.22f,1,5);Sfx(badClip);Flash(message,new Color(1,.25f,.18f));UpdateHud();}
        if(activeCustomer!=null){Transform t=activeCustomer.transform;Vector3 start=t.position,end=happy?new Vector3(3.8f,0,-.5f):new Vector3(-3.8f,0,-.5f);float x=0;while(x<.35f&&t!=null){x+=Time.unscaledDeltaTime;t.position=Vector3.Lerp(start,end,Mathf.SmoothStep(0,1,x/.35f));yield return null;}}
        yield return new WaitForSecondsRealtime(.18f);ClearBowl(false);NewOrder(false);switching=false;
    }

    void Restock()
    {
        if(!playing||switching)return;if(cash<18){Flash("NEED $18 TO RESTOCK",Gold);Sfx(badClip);return;}cash-=18;foreach(string i in ingredients)stock[i]+=5;Sfx(coinClip);Flash("RESTOCKED +5 EACH",new Color(.34f,.73f,.95f));UpdateStock();UpdateHud();
    }

    void FinishDay()
    {
        if(!playing)return;playing=false;switching=false;hud.SetActive(false);endPanel.SetActive(true);bool pass=served>=Mathf.Max(4,target/2)&&rating>=2.4f;int bonus=pass?20+day*5+bestCombo*2:0;cash+=bonus;
        endTitle.text=pass?"DAY COMPLETE":"ROUGH DAY";endTitle.color=pass?Gold:new Color(1,.35f,.25f);endStats.text=string.Format("SERVED {0}/{1}\nMISTAKES {2}   •   BEST COMBO x{3}\nRATING ★ {4:0.0}   •   BONUS ${5}\nCASH ${6}",served,target,mistakes,bestCombo,rating,bonus,cash);
        nextButton.GetComponentInChildren<Text>().text=pass?"NEXT DAY":"RETRY DAY";nextButton.onClick.RemoveAllListeners();nextButton.onClick.AddListener(()=>{if(pass)day++;Save();ShowStart();});UpdateUpgrades();Save();
    }

    void BuyUpgrade(int type)
    {
        int level=type==0?patienceLevel:type==1?priceLevel:tipsLevel,baseCost=type==0?80:type==1?100:120,cost=baseCost+level*55;if(cash<cost){Sfx(badClip);return;}cash-=cost;if(type==0)patienceLevel++;else if(type==1)priceLevel++;else tipsLevel++;Sfx(coinClip);Save();UpdateUpgrades();endStats.text+="\nUPGRADE PURCHASED!";
    }

    void UpdateUpgrades(){SetUpgrade(patUpgrade,"PATIENCE +12%",80,patienceLevel);SetUpgrade(priceUpgrade,"BETTER PRICE +$3",100,priceLevel);SetUpgrade(tipsUpgrade,"TIP CHANCE +8%",120,tipsLevel);}
    void SetUpgrade(Button b,string title,int baseCost,int level){int cost=baseCost+level*55;b.GetComponentInChildren<Text>().text=title+"  •  $"+cost+"  •  Lv."+level;b.interactable=cash>=cost;}
    void UpdateHud(){dayText.text="DAY "+day+"  "+served+"/"+target;cashText.text="$ "+cash;ratingText.text="★ "+rating.ToString("0.0");}
    void UpdateStock(){foreach(string i in ingredients){int v=stock.ContainsKey(i)?stock[i]:0;if(stockLabels.ContainsKey(i))stockLabels[i].text="x"+v;if(ingredientButtons.ContainsKey(i))ingredientButtons[i].interactable=v>0;}}
    void UpdateBowl(){if(bowlText!=null)bowlText.text=bowl.Count==0?"BOWL: EMPTY":"BOWL: "+string.Join(" • ",bowl.ToArray());}

    void Flash(string msg,Color c){if(feedbackText==null)return;feedbackText.text=msg;feedbackText.color=c;StopCoroutine("Fade");StartCoroutine("Fade");}
    IEnumerator Fade(){Color c=feedbackText.color;c.a=1;feedbackText.color=c;yield return new WaitForSecondsRealtime(.9f);float x=0;while(x<.5f&&feedbackText!=null){x+=Time.unscaledDeltaTime;c.a=1-Mathf.Clamp01(x/.5f);feedbackText.color=c;yield return null;}}
}
