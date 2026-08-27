using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public partial class KosharetoGame
{
    Button tutorialNextButton;

    void BuildUI()
    {
        if (uiShader == null) throw new InvalidOperationException("Koshareto UI shader was not loaded");
        if (font == null) throw new InvalidOperationException("Runtime font was not loaded");

        uiMaterial = new Material(uiShader);
        uiMaterial.name = "Koshareto UI Runtime Material";

        GameObject uiRoot = new GameObject("Koshareto Portrait UI");
        uiRoot.transform.SetParent(transform,false);
        canvas = uiRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = uiRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080,1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = .5f;
        uiRoot.AddComponent<GraphicRaycaster>();

        if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
            DontDestroyOnLoad(es);
        }

        safe = new GameObject("SafeArea",typeof(RectTransform)).GetComponent<RectTransform>();
        safe.SetParent(canvas.transform,false);
        safe.anchorMin = Vector2.zero;
        safe.anchorMax = Vector2.one;
        safe.offsetMin = Vector2.zero;
        safe.offsetMax = Vector2.zero;

        BuildHud();
        BuildStart();
        BuildEnd();
        BuildTutorial();
        BuildPause();
    }

    void ApplySafeArea()
    {
        if (safe == null || Screen.width <= 0 || Screen.height <= 0) return;
        Rect r = Screen.safeArea;
        Vector2 min = r.position;
        Vector2 max = r.position + r.size;
        min.x /= Screen.width;
        min.y /= Screen.height;
        max.x /= Screen.width;
        max.y /= Screen.height;
        if (safe.anchorMin != min || safe.anchorMax != max)
        {
            safe.anchorMin = min;
            safe.anchorMax = max;
            safe.offsetMin = Vector2.zero;
            safe.offsetMax = Vector2.zero;
        }
    }

    void BuildHud()
    {
        hud = Panel("HUD",safe,new Color(0,0,0,0),Vector2.zero,Vector2.one);

        GameObject top = Panel("Top Bar",hud.transform,new Color(.05f,.03f,.02f,.97f),new Vector2(.018f,.916f),new Vector2(.982f,.988f));
        Outline(top,new Color(.72f,.41f,.12f,.70f),2);
        dayText = Label("DAY 1",top.transform,29,FontStyle.Bold,Gold,TextAnchor.MiddleLeft,new Vector2(.025f,0),new Vector2(.31f,1));
        cashText = Label("$ 60",top.transform,31,FontStyle.Bold,Cream,TextAnchor.MiddleCenter,new Vector2(.31f,0),new Vector2(.53f,1));
        ratingText = Label("★ 4.5",top.transform,28,FontStyle.Bold,new Color(1,.79f,.22f),TextAnchor.MiddleCenter,new Vector2(.53f,0),new Vector2(.71f,1));
        timerText = Label("02:00",top.transform,29,FontStyle.Bold,Cream,TextAnchor.MiddleCenter,new Vector2(.71f,0),new Vector2(.90f,1));
        Button pause = UIButton("II",top.transform,new Color(.20f,.14f,.10f),Cream,new Vector2(.905f,.12f),new Vector2(.982f,.88f),22);
        pause.onClick.AddListener(PauseGame);

        GameObject ticket = Panel("Order Ticket",hud.transform,new Color(.065f,.042f,.028f,.97f),new Vector2(.03f,.704f),new Vector2(.97f,.902f));
        Outline(ticket,new Color(.93f,.53f,.14f,.88f),3);
        customerText = Label("CUSTOMER",ticket.transform,23,FontStyle.Bold,Gold,TextAnchor.UpperLeft,new Vector2(.04f,.72f),new Vector2(.96f,.96f));
        orderText = Label("ORDER",ticket.transform,27,FontStyle.Bold,Color.white,TextAnchor.MiddleLeft,new Vector2(.04f,.22f),new Vector2(.96f,.72f));
        orderText.horizontalOverflow = HorizontalWrapMode.Wrap;
        orderText.verticalOverflow = VerticalWrapMode.Truncate;
        patienceBar = SliderBar(ticket.transform,new Vector2(.04f,.06f),new Vector2(.96f,.18f));

        rushText = Label("RUSH ORDER  •  +25%",hud.transform,20,FontStyle.Bold,new Color(1,.48f,.12f),TextAnchor.MiddleCenter,new Vector2(.10f,.655f),new Vector2(.90f,.698f));
        rushText.gameObject.SetActive(false);

        feedbackText = Label("",hud.transform,34,FontStyle.Bold,Color.white,TextAnchor.MiddleCenter,new Vector2(.06f,.594f),new Vector2(.94f,.654f));
        feedbackText.resizeTextForBestFit = true;
        feedbackText.resizeTextMinSize = 20;
        feedbackText.resizeTextMaxSize = 38;
        TextOutline(feedbackText,Dark,2);

        GameObject deck = Panel("Prep Deck",hud.transform,new Color(.043f,.027f,.018f,.985f),new Vector2(.018f,.012f),new Vector2(.982f,.478f));
        Outline(deck,new Color(.66f,.38f,.12f,.82f),3);
        bowlText = Label("BOWL: EMPTY",deck.transform,19,FontStyle.Bold,new Color(.84f,.75f,.64f),TextAnchor.MiddleLeft,new Vector2(.04f,.88f),new Vector2(.96f,.985f));

        float left=.032f, right=.968f, topY=.855f, bottom=.315f, gx=.018f, gy=.022f;
        float cw = (right-left-gx)/2f;
        float ch = (topY-bottom-gy*3f)/4f;
        for (int i=0;i<ingredients.Length;i++)
        {
            int row=i/2;
            int col=i%2;
            float x0 = left + col*(cw+gx);
            float y1 = topY - row*(ch+gy);
            float y0 = y1 - ch;
            string item = ingredients[i];
            Color buttonColor = Color.Lerp(foodColors[i],Dark,.18f);
            Button b = UIButton(item,deck.transform,buttonColor,Color.white,new Vector2(x0,y0),new Vector2(x0+cw,y1),24);
            b.onClick.AddListener(delegate { AddIngredient(item); });
            ingredientButtons[item] = b;
            stockLabels[item] = Label("x12",b.transform,16,FontStyle.Bold,new Color(1,1,1,.82f),TextAnchor.LowerRight,new Vector2(.70f,.03f),new Vector2(.96f,.40f));
        }

        Button serve = UIButton("SERVE",deck.transform,Green,Color.white,new Vector2(.032f,.055f),new Vector2(.50f,.258f),31);
        serve.onClick.AddListener(Serve);
        Button clear = UIButton("CLEAR",deck.transform,Red,Color.white,new Vector2(.515f,.055f),new Vector2(.72f,.258f),22);
        clear.onClick.AddListener(delegate { ClearBowl(true); });
        Button restock = UIButton("RESTOCK",deck.transform,Blue,Color.white,new Vector2(.735f,.055f),new Vector2(.968f,.258f),20);
        restock.onClick.AddListener(Restock);
    }

    void BuildStart()
    {
        startPanel = Panel("Main Menu",safe,new Color(.035f,.020f,.013f,.995f),Vector2.zero,Vector2.one);

        GameObject brand = Panel("Brand Plate",startPanel.transform,new Color(.18f,.058f,.018f,.98f),new Vector2(.075f,.61f),new Vector2(.925f,.855f));
        Outline(brand,Gold,4);
        Label("KOSHARETO",brand.transform,68,FontStyle.Bold,new Color(1,.72f,.18f),TextAnchor.MiddleCenter,new Vector2(.03f,.38f),new Vector2(.97f,.83f));
        Label("EGYPTIAN KOSHARY TYCOON",brand.transform,21,FontStyle.Bold,Cream,TextAnchor.MiddleCenter,new Vector2(.04f,.14f),new Vector2(.96f,.40f));

        Label("FAST HANDS. HAPPY CUSTOMERS. BIGGER SHOP.",startPanel.transform,21,FontStyle.Bold,new Color(.80f,.68f,.56f),TextAnchor.MiddleCenter,new Vector2(.07f,.52f),new Vector2(.93f,.59f));
        startStats = Label("",startPanel.transform,23,FontStyle.Bold,Cream,TextAnchor.UpperCenter,new Vector2(.08f,.38f),new Vector2(.92f,.51f));

        Button play = UIButton("OPEN SHOP",startPanel.transform,Green,Color.white,new Vector2(.12f,.265f),new Vector2(.88f,.355f),34);
        play.onClick.AddListener(StartPressed);

        soundButton = UIButton("SOUND: ON",startPanel.transform,new Color(.20f,.28f,.34f),Color.white,new Vector2(.20f,.185f),new Vector2(.80f,.245f),21);
        soundButton.onClick.AddListener(ToggleSound);

        Label("PORTRAIT • OFFLINE • AUTO SAVE",startPanel.transform,17,FontStyle.Normal,new Color(.56f,.50f,.46f),TextAnchor.MiddleCenter,new Vector2(.05f,.07f),new Vector2(.95f,.125f));
        Label("v1.0",startPanel.transform,15,FontStyle.Normal,new Color(.40f,.36f,.33f),TextAnchor.MiddleCenter,new Vector2(.05f,.025f),new Vector2(.95f,.065f));
    }

    void BuildEnd()
    {
        endPanel = Panel("Day Result",safe,new Color(.03f,.018f,.012f,.996f),Vector2.zero,Vector2.one);
        endTitle = Label("DAY COMPLETE",endPanel.transform,48,FontStyle.Bold,Gold,TextAnchor.MiddleCenter,new Vector2(.06f,.835f),new Vector2(.94f,.92f));
        endStars = Label("★  ★  ★",endPanel.transform,43,FontStyle.Bold,new Color(1,.76f,.18f),TextAnchor.MiddleCenter,new Vector2(.06f,.755f),new Vector2(.94f,.83f));
        endStats = Label("",endPanel.transform,24,FontStyle.Bold,Cream,TextAnchor.UpperCenter,new Vector2(.08f,.58f),new Vector2(.92f,.75f));
        Label("SHOP UPGRADES",endPanel.transform,21,FontStyle.Bold,new Color(.76f,.64f,.52f),TextAnchor.MiddleCenter,new Vector2(.08f,.515f),new Vector2(.92f,.565f));

        patUpgrade = UIButton("PATIENCE",endPanel.transform,new Color(.16f,.34f,.49f),Color.white,new Vector2(.07f,.38f),new Vector2(.49f,.50f),20);
        priceUpgrade = UIButton("PRICE",endPanel.transform,new Color(.44f,.27f,.10f),Color.white,new Vector2(.51f,.38f),new Vector2(.93f,.50f),20);
        tipsUpgrade = UIButton("TIPS",endPanel.transform,new Color(.31f,.18f,.40f),Color.white,new Vector2(.07f,.245f),new Vector2(.49f,.365f),20);
        stockUpgrade = UIButton("STOCK",endPanel.transform,new Color(.18f,.38f,.29f),Color.white,new Vector2(.51f,.245f),new Vector2(.93f,.365f),20);
        patUpgrade.onClick.AddListener(delegate { BuyUpgrade(0); });
        priceUpgrade.onClick.AddListener(delegate { BuyUpgrade(1); });
        tipsUpgrade.onClick.AddListener(delegate { BuyUpgrade(2); });
        stockUpgrade.onClick.AddListener(delegate { BuyUpgrade(3); });

        nextButton = UIButton("NEXT DAY",endPanel.transform,Green,Color.white,new Vector2(.12f,.075f),new Vector2(.88f,.165f),31);
    }

    void BuildTutorial()
    {
        tutorialPanel = Panel("Tutorial",safe,new Color(.025f,.016f,.012f,.997f),Vector2.zero,Vector2.one);
        Label("HOW TO PLAY",tutorialPanel.transform,43,FontStyle.Bold,Gold,TextAnchor.MiddleCenter,new Vector2(.08f,.76f),new Vector2(.92f,.86f));
        tutorialText = Label("",tutorialPanel.transform,27,FontStyle.Bold,Cream,TextAnchor.MiddleCenter,new Vector2(.10f,.40f),new Vector2(.90f,.72f));
        tutorialText.horizontalOverflow = HorizontalWrapMode.Wrap;
        tutorialNextButton = UIButton("NEXT",tutorialPanel.transform,Green,Color.white,new Vector2(.18f,.23f),new Vector2(.82f,.32f),30);
        tutorialNextButton.onClick.AddListener(CompleteTutorialStep);
        Label("You only see this once.",tutorialPanel.transform,16,FontStyle.Normal,new Color(.50f,.45f,.41f),TextAnchor.MiddleCenter,new Vector2(.08f,.12f),new Vector2(.92f,.18f));
        tutorialPanel.SetActive(false);
    }

    void BuildPause()
    {
        pausePanel = Panel("Pause",safe,new Color(.02f,.013f,.010f,.985f),Vector2.zero,Vector2.one);
        Label("SHOP PAUSED",pausePanel.transform,46,FontStyle.Bold,Gold,TextAnchor.MiddleCenter,new Vector2(.08f,.68f),new Vector2(.92f,.80f));
        Button resume = UIButton("RESUME",pausePanel.transform,Green,Color.white,new Vector2(.16f,.49f),new Vector2(.84f,.59f),31);
        resume.onClick.AddListener(ResumeGame);
        Button menu = UIButton("SAVE & MAIN MENU",pausePanel.transform,new Color(.34f,.24f,.17f),Color.white,new Vector2(.16f,.36f),new Vector2(.84f,.46f),24);
        menu.onClick.AddListener(ReturnToMenu);
        pausePanel.SetActive(false);
    }

    void RefreshMenu()
    {
        if (startStats != null)
        {
            startStats.text = "DAY " + day + "   •   CASH $" + cash + "   •   RATING ★ " + rating.ToString("0.0") +
                "\nPATIENCE Lv." + patienceLevel + "   PRICE Lv." + priceLevel + "   TIPS Lv." + tipsLevel + "   STOCK Lv." + stockLevel;
        }
        if (soundButton != null) SetButtonLabel(soundButton,soundOn ? "SOUND: ON" : "SOUND: OFF");
    }

    void ShowTutorialStep()
    {
        if (tutorialPanel == null || tutorialText == null || tutorialNextButton == null) return;
        tutorialPanel.SetActive(true);
        if (startPanel != null) startPanel.SetActive(false);

        if (tutorialStep == 0)
        {
            tutorialText.text = "1 / 3\n\nREAD THE ORDER\n\nEach customer wants a different koshary bowl. Match every ingredient on the ticket.";
            SetButtonLabel(tutorialNextButton,"NEXT");
        }
        else if (tutorialStep == 1)
        {
            tutorialText.text = "2 / 3\n\nBUILD THE BOWL\n\nTap the ingredient buttons. You can CLEAR a mistake before serving.";
            SetButtonLabel(tutorialNextButton,"NEXT");
        }
        else
        {
            tutorialText.text = "3 / 3\n\nBE FAST\n\nServe before patience runs out. Combos, tips and rush orders make more money.";
            SetButtonLabel(tutorialNextButton,"OPEN SHOP");
        }
    }

    GameObject Panel(string name, Transform parent, Color color, Vector2 min, Vector2 max)
    {
        GameObject g = new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
        g.transform.SetParent(parent,false);
        RectTransform rt = g.GetComponent<RectTransform>();
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        Image image = g.GetComponent<Image>();
        image.color = color;
        image.material = uiMaterial;
        return g;
    }

    Text Label(string value, Transform parent, int size, FontStyle style, Color color, TextAnchor anchor, Vector2 min, Vector2 max)
    {
        GameObject g = new GameObject("Text",typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));
        g.transform.SetParent(parent,false);
        RectTransform rt = g.GetComponent<RectTransform>();
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        Text t = g.GetComponent<Text>();
        t.font = font;
        t.text = value;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = anchor;
        t.supportRichText = true;
        t.raycastTarget = false;
        return t;
    }

    Button UIButton(string text, Transform parent, Color background, Color foreground, Vector2 min, Vector2 max, int size)
    {
        GameObject g = new GameObject(text + " Button",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(Button));
        g.transform.SetParent(parent,false);
        RectTransform rt = g.GetComponent<RectTransform>();
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        Image image = g.GetComponent<Image>();
        image.color = background;
        image.material = uiMaterial;

        Button b = g.GetComponent<Button>();
        ColorBlock colors = b.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.08f,1.08f,1.08f);
        colors.pressedColor = new Color(.78f,.78f,.78f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(.36f,.36f,.36f,.65f);
        colors.fadeDuration = .08f;
        b.colors = colors;

        Text label = Label(text,g.transform,size,FontStyle.Bold,foreground,TextAnchor.MiddleCenter,new Vector2(.03f,.03f),new Vector2(.97f,.97f));
        label.raycastTarget = false;
        Outline(g,new Color(1,1,1,.10f),1.5f);
        return b;
    }

    Slider SliderBar(Transform parent, Vector2 min, Vector2 max)
    {
        GameObject g = new GameObject("Patience",typeof(RectTransform),typeof(Slider));
        g.transform.SetParent(parent,false);
        RectTransform rt = g.GetComponent<RectTransform>();
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Panel("BG",g.transform,new Color(.13f,.085f,.06f),Vector2.zero,Vector2.one);
        GameObject area = new GameObject("Fill Area",typeof(RectTransform));
        area.transform.SetParent(g.transform,false);
        RectTransform ar = area.GetComponent<RectTransform>();
        ar.anchorMin = new Vector2(.01f,.16f);
        ar.anchorMax = new Vector2(.99f,.84f);
        ar.offsetMin = Vector2.zero;
        ar.offsetMax = Vector2.zero;
        GameObject fill = Panel("Fill",area.transform,Green,Vector2.zero,Vector2.one);

        Slider s = g.GetComponent<Slider>();
        s.minValue = 0;
        s.maxValue = 1;
        s.value = 1;
        s.fillRect = fill.GetComponent<RectTransform>();
        s.interactable = false;
        return s;
    }

    void Outline(GameObject g, Color color, float distance)
    {
        Outline o = g.AddComponent<Outline>();
        o.effectColor = color;
        o.effectDistance = new Vector2(distance,-distance);
        o.useGraphicAlpha = true;
    }

    void TextOutline(Text text, Color color, int distance)
    {
        if (text == null) return;
        Outline o = text.gameObject.AddComponent<Outline>();
        o.effectColor = color;
        o.effectDistance = new Vector2(distance,-distance);
    }

    void SetButtonLabel(Button button, string value)
    {
        if (button == null) return;
        Text t = button.GetComponentInChildren<Text>();
        if (t != null) t.text = value;
    }
}