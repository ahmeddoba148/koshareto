using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class KosharetoGame
{
    readonly Dictionary<int,Material> worldMaterials = new Dictionary<int,Material>();
    float customerBaseY;
    float customerSpawnTime;

    void BuildWorld()
    {
        if (worldShader == null) throw new System.InvalidOperationException("Koshareto world shader was not loaded");

        world = new GameObject("Koshareto 3D Shop");
        world.transform.SetParent(transform,false);
        RenderSettings.ambientLight = new Color(.42f,.30f,.20f);
        RenderSettings.fog = false;

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.transform.SetParent(world.transform,false);
        Camera cam = cameraObject.AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(.045f,.025f,.016f);
        cam.fieldOfView = 47;
        cam.nearClipPlane = .1f;
        cam.farClipPlane = 60;
        cam.transform.position = new Vector3(0,5.05f,-11.4f);
        cam.transform.LookAt(new Vector3(0,1.65f,1.25f));

        GameObject sunObject = new GameObject("Warm Key Light");
        sunObject.transform.SetParent(world.transform,false);
        Light sun = sunObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1,.78f,.56f);
        sun.intensity = 1.15f;
        sun.shadows = LightShadows.None;
        sun.transform.rotation = Quaternion.Euler(48,-28,0);
        AddPointLight(new Vector3(-3.6f,4.0f,.5f),new Color(1,.46f,.18f),2.4f,8);
        AddPointLight(new Vector3(3.6f,4.0f,.5f),new Color(1,.72f,.34f),2.0f,8);

        BuildRoom();
        BuildCounter();
        BuildPrepStation();
        BuildDecor();
        BuildQueue();
        SpawnCustomer(true);
    }

    void BuildRoom()
    {
        Color tileA = new Color(.20f,.135f,.085f);
        Color tileB = new Color(.26f,.17f,.10f);
        for (int x=-5;x<=5;x++)
        {
            for (int z=-3;z<=5;z++)
            {
                Color c = ((x+z)&1) == 0 ? tileA : tileB;
                Box("Floor Tile",new Vector3(x*.9f,-.13f,z*.9f+.8f),new Vector3(.88f,.10f,.88f),c);
            }
        }

        Box("Back Wall",new Vector3(0,2.55f,5.05f),new Vector3(10.8f,5.1f,.25f),new Color(.49f,.18f,.085f));
        Box("Left Wall",new Vector3(-5.4f,2.55f,2.1f),new Vector3(.24f,5.1f,6.1f),new Color(.60f,.45f,.30f));
        Box("Right Wall",new Vector3(5.4f,2.55f,2.1f),new Vector3(.24f,5.1f,6.1f),new Color(.60f,.45f,.30f));

        Box("Sign Board",new Vector3(0,3.70f,4.86f),new Vector3(6.5f,1.0f,.12f),new Color(.075f,.04f,.025f));
        Box("Sign Gold",new Vector3(0,3.70f,4.77f),new Vector3(5.7f,.13f,.06f),Gold);
        Box("Sign Lower",new Vector3(0,3.34f,4.76f),new Vector3(3.9f,.08f,.05f),new Color(.92f,.64f,.26f));

        for (int i=0;i<9;i++)
        {
            Color stripe = i%2==0 ? new Color(.76f,.16f,.065f) : new Color(.94f,.80f,.60f);
            Box("Awning Stripe",new Vector3(-4.4f+i*1.1f,4.72f,2.65f),new Vector3(1.05f,.18f,1.30f),stripe);
        }
    }

    void BuildCounter()
    {
        Box("Counter Body",new Vector3(0,.73f,2.02f),new Vector3(9.0f,1.46f,1.16f),new Color(.25f,.095f,.043f));
        Box("Counter Front Accent",new Vector3(0,.77f,1.40f),new Vector3(8.4f,.18f,.05f),Gold);
        Box("Counter Top",new Vector3(0,1.52f,2.02f),new Vector3(9.25f,.13f,1.28f),new Color(.80f,.56f,.31f));

        Box("Register Base",new Vector3(3.75f,1.72f,1.72f),new Vector3(.65f,.28f,.60f),new Color(.11f,.10f,.09f));
        Box("Register Screen",new Vector3(3.75f,2.02f,1.92f),new Vector3(.56f,.48f,.10f),new Color(.10f,.28f,.26f));

        for (int i=0;i<3;i++)
        {
            float x = -4.25f + i*.32f;
            Cylinder("Sauce Bottle",new Vector3(x,1.82f,1.78f),.10f,.48f,i==0?new Color(.75f,.08f,.04f):i==1?new Color(.88f,.74f,.30f):new Color(.58f,.06f,.03f));
        }
    }

    void BuildPrepStation()
    {
        float[] xs = { -3.45f,-2.48f,-1.51f,-.54f,.54f,1.51f,2.48f,3.45f };
        for (int i=0;i<ingredients.Length;i++)
        {
            GameObject pot = Cylinder("Pot " + ingredients[i],new Vector3(xs[i],1.78f,2.06f),.40f,.28f,new Color(.15f,.15f,.145f));
            CylinderChild(pot.transform,"Food",new Vector3(0,.19f,0),.32f,.065f,foodColors[i]);
            CylinderChild(pot.transform,"Rim",new Vector3(0,.27f,0),.41f,.025f,new Color(.48f,.46f,.42f));
        }

        GameObject bowlRoot = new GameObject("Serving Bowl");
        bowlRoot.transform.SetParent(world.transform,false);
        bowlRoot.transform.position = new Vector3(0,1.74f,1.05f);
        CylinderChild(bowlRoot.transform,"Bowl",Vector3.zero,.74f,.23f,new Color(.93f,.85f,.72f));
        CylinderChild(bowlRoot.transform,"Inside",new Vector3(0,.13f,0),.59f,.05f,new Color(.20f,.12f,.075f));
        bowlLayers = new GameObject("Food Layers");
        bowlLayers.transform.SetParent(bowlRoot.transform,false);
    }

    void BuildDecor()
    {
        Box("Shelf",new Vector3(-3.65f,2.77f,4.68f),new Vector3(2.55f,.11f,.45f),new Color(.27f,.12f,.055f));
        Box("Shelf 2",new Vector3(3.65f,2.77f,4.68f),new Vector3(2.55f,.11f,.45f),new Color(.27f,.12f,.055f));
        for (int i=0;i<5;i++)
        {
            Cylinder("Jar",new Vector3(-4.55f+i*.44f,3.02f,4.54f),.12f,.38f,Color.Lerp(foodColors[i],Cream,.20f));
            Cylinder("Jar R",new Vector3(2.75f+i*.44f,3.02f,4.54f),.12f,.38f,Color.Lerp(foodColors[(i+3)%foodColors.Length],Cream,.20f));
        }

        for (int i=0;i<3;i++)
        {
            float x = -2.4f + i*2.4f;
            Cylinder("Pendant",new Vector3(x,4.15f,1.15f),.26f,.20f,new Color(.10f,.075f,.055f));
            Cylinder("Pendant Glow",new Vector3(x,4.00f,1.15f),.18f,.05f,new Color(1,.65f,.22f));
        }
    }

    void BuildQueue()
    {
        Person(new Vector3(2.75f,0,-.28f),new Color(.20f,.42f,.62f),.55f,false);
        Person(new Vector3(3.32f,0,-.82f),new Color(.54f,.24f,.44f),.52f,false);
        Person(new Vector3(3.85f,0,-1.34f),new Color(.24f,.51f,.32f),.49f,false);
    }

    void AddPointLight(Vector3 position, Color color, float intensity, float range)
    {
        GameObject g = new GameObject("Shop Point Light");
        g.transform.SetParent(world.transform,false);
        g.transform.position = position;
        Light light = g.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.None;
    }

    Material WorldMaterial(Color color)
    {
        Color32 c = color;
        int key = c.r | (c.g<<8) | (c.b<<16) | (c.a<<24);
        Material material;
        if (worldMaterials.TryGetValue(key,out material)) return material;
        material = new Material(worldShader);
        material.name = "Koshareto Color " + key;
        material.SetColor("_Color",color);
        worldMaterials[key] = material;
        return material;
    }

    GameObject Box(string name, Vector3 position, Vector3 scale, Color color)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = name;
        g.transform.SetParent(world.transform,false);
        g.transform.position = position;
        g.transform.localScale = scale;
        Renderer r = g.GetComponent<Renderer>();
        r.sharedMaterial = WorldMaterial(color);
        Collider col = g.GetComponent<Collider>();
        if (col != null) Destroy(col);
        return g;
    }

    GameObject Cylinder(string name, Vector3 position, float radius, float height, Color color)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        g.name = name;
        g.transform.SetParent(world.transform,false);
        g.transform.position = position;
        g.transform.localScale = new Vector3(radius*2,height*.5f,radius*2);
        g.GetComponent<Renderer>().sharedMaterial = WorldMaterial(color);
        Collider col = g.GetComponent<Collider>();
        if (col != null) Destroy(col);
        return g;
    }

    GameObject CylinderChild(Transform parent, string name, Vector3 position, float radius, float height, Color color)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        g.name = name;
        g.transform.SetParent(parent,false);
        g.transform.localPosition = position;
        g.transform.localScale = new Vector3(radius*2,height*.5f,radius*2);
        g.GetComponent<Renderer>().sharedMaterial = WorldMaterial(color);
        Collider col = g.GetComponent<Collider>();
        if (col != null) Destroy(col);
        return g;
    }

    GameObject SphereChild(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        g.name = name;
        g.transform.SetParent(parent,false);
        g.transform.localPosition = position;
        g.transform.localScale = scale;
        g.GetComponent<Renderer>().sharedMaterial = WorldMaterial(color);
        Collider col = g.GetComponent<Collider>();
        if (col != null) Destroy(col);
        return g;
    }

    GameObject Person(Vector3 position, Color shirt, float scale, bool hero)
    {
        GameObject root = new GameObject(hero ? "Active Customer" : "Queue Customer");
        root.transform.SetParent(world.transform,false);
        root.transform.position = position;

        Color skin = new Color(.70f + UnityEngine.Random.Range(-.08f,.08f),.46f + UnityEngine.Random.Range(-.06f,.06f),.31f + UnityEngine.Random.Range(-.04f,.04f));
        Color pants = new Color(.08f,.09f,.11f);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.transform.SetParent(root.transform,false);
        body.transform.localPosition = new Vector3(0,.92f,0);
        body.transform.localScale = new Vector3(scale,.74f,scale*.88f);
        body.GetComponent<Renderer>().sharedMaterial = WorldMaterial(shirt);
        Collider bodyCol = body.GetComponent<Collider>(); if (bodyCol != null) Destroy(bodyCol);

        SphereChild(root.transform,"Head",new Vector3(0,1.80f,0),Vector3.one*scale*.76f,skin);
        SphereChild(root.transform,"Hair",new Vector3(0,2.02f,.02f),new Vector3(scale*.82f,scale*.34f,scale*.78f),new Color(.055f,.038f,.028f));
        SphereChild(root.transform,"Left Eye",new Vector3(-.13f*scale,1.84f,-.34f*scale),Vector3.one*scale*.075f,new Color(.03f,.025f,.02f));
        SphereChild(root.transform,"Right Eye",new Vector3(.13f*scale,1.84f,-.34f*scale),Vector3.one*scale*.075f,new Color(.03f,.025f,.02f));

        GameObject legL = GameObject.CreatePrimitive(PrimitiveType.Cube);
        legL.transform.SetParent(root.transform,false);
        legL.transform.localPosition = new Vector3(-.16f*scale,.27f,0);
        legL.transform.localScale = new Vector3(.20f*scale,.55f,.22f*scale);
        legL.GetComponent<Renderer>().sharedMaterial = WorldMaterial(pants);
        Collider legLC = legL.GetComponent<Collider>(); if (legLC != null) Destroy(legLC);
        GameObject legR = GameObject.CreatePrimitive(PrimitiveType.Cube);
        legR.transform.SetParent(root.transform,false);
        legR.transform.localPosition = new Vector3(.16f*scale,.27f,0);
        legR.transform.localScale = new Vector3(.20f*scale,.55f,.22f*scale);
        legR.GetComponent<Renderer>().sharedMaterial = WorldMaterial(pants);
        Collider legRC = legR.GetComponent<Collider>(); if (legRC != null) Destroy(legRC);

        return root;
    }

    void SpawnCustomer(bool instant)
    {
        if (world == null) return;
        if (activeCustomer != null) Destroy(activeCustomer);
        Color shirt = Color.HSVToRGB(UnityEngine.Random.value,.48f,.74f);
        Vector3 start = instant ? new Vector3(0,0,.02f) : new Vector3(-3.9f,0,-.55f);
        activeCustomer = Person(start,shirt,.82f,true);
        customerBaseY = 0;
        customerSpawnTime = Time.unscaledTime;
        if (!instant) StartCoroutine(MoveTransform(activeCustomer.transform,new Vector3(0,0,.02f),.42f));
    }

    IEnumerator MoveTransform(Transform targetTransform, Vector3 end, float duration)
    {
        if (targetTransform == null) yield break;
        Vector3 start = targetTransform.position;
        float elapsed = 0;
        while (elapsed < duration && targetTransform != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/duration));
            targetTransform.position = Vector3.Lerp(start,end,t);
            yield return null;
        }
        if (targetTransform != null) targetTransform.position = end;
    }

    IEnumerator MoveCustomerOut(bool happy)
    {
        if (activeCustomer == null) yield break;
        Transform t = activeCustomer.transform;
        Vector3 start = t.position;
        Vector3 end = happy ? new Vector3(3.9f,0,-.48f) : new Vector3(-3.9f,0,-.48f);
        float elapsed = 0;
        while (elapsed < .38f && t != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float p = Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/.38f));
            t.position = Vector3.Lerp(start,end,p);
            yield return null;
        }
        if (activeCustomer != null) Destroy(activeCustomer);
        activeCustomer = null;
    }

    void AnimateWorld()
    {
        if (activeCustomer != null && playing && !switching)
        {
            Vector3 p = activeCustomer.transform.position;
            p.y = customerBaseY + Mathf.Sin((Time.unscaledTime-customerSpawnTime)*3.2f)*.025f;
            activeCustomer.transform.position = p;
        }
    }

    void AddBowlLayer(string item, int index)
    {
        if (bowlLayers == null || index < 0 || index >= foodColors.Length) return;
        GameObject layer = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        layer.name = item;
        layer.transform.SetParent(bowlLayers.transform,false);
        float targetY = .18f + bowl.Count*.042f;
        layer.transform.localPosition = new Vector3(0,targetY+.52f,0);
        layer.transform.localScale = new Vector3(.12f,.016f,.12f);
        layer.GetComponent<Renderer>().sharedMaterial = WorldMaterial(foodColors[index]);
        Collider col = layer.GetComponent<Collider>(); if (col != null) Destroy(col);
        StartCoroutine(DropLayer(layer.transform,targetY));
    }

    IEnumerator DropLayer(Transform layer, float targetY)
    {
        if (layer == null) yield break;
        Vector3 startPos = layer.localPosition;
        Vector3 endPos = new Vector3(0,targetY,0);
        Vector3 startScale = layer.localScale;
        Vector3 endScale = new Vector3(.52f,.016f,.52f);
        float elapsed = 0;
        while (elapsed < .18f && layer != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/.18f));
            layer.localPosition = Vector3.Lerp(startPos,endPos,t);
            layer.localScale = Vector3.Lerp(startScale,endScale,t);
            yield return null;
        }
        if (layer != null)
        {
            layer.localPosition = endPos;
            layer.localScale = endScale;
        }
    }

    void ClearBowlVisual()
    {
        if (bowlLayers == null) return;
        for (int i=bowlLayers.transform.childCount-1;i>=0;i--) Destroy(bowlLayers.transform.GetChild(i).gameObject);
    }
}