using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class KosharetoBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        if (UnityEngine.Object.FindAnyObjectByType<KosharetoGame>() != null) return;
        GameObject root = new GameObject("KosharetoGame");
        UnityEngine.Object.DontDestroyOnLoad(root);
        root.AddComponent<KosharetoGame>();
    }
}

public partial class KosharetoGame : MonoBehaviour
{
    readonly string[] ingredients = { "RICE", "PASTA", "LENTILS", "CHICKPEAS", "TOMATO", "GARLIC", "CHILI", "ONION" };
    readonly string[] names = { "MIDO", "NOUR", "SALMA", "HASSAN", "MARIAM", "OMAR", "YOUSSEF", "FARAH", "NADA", "KARIM", "AYA", "ALI" };
    readonly Color[] foodColors = {
        new Color(.94f,.85f,.60f), new Color(.93f,.68f,.31f), new Color(.48f,.25f,.13f), new Color(.84f,.67f,.34f),
        new Color(.72f,.10f,.07f), new Color(.87f,.82f,.57f), new Color(.62f,.05f,.04f), new Color(.45f,.20f,.07f)
    };

    static readonly Color Cream = new Color(.96f,.87f,.70f);
    static readonly Color Dark = new Color(.075f,.05f,.035f);
    static readonly Color Gold = new Color(.95f,.61f,.12f);
    static readonly Color Green = new Color(.16f,.55f,.28f);
    static readonly Color Red = new Color(.62f,.06f,.035f);
    static readonly Color Blue = new Color(.18f,.43f,.62f);

    Font font;
    Shader worldShader;
    Shader uiShader;
    Material uiMaterial;

    Canvas canvas;
    RectTransform safe;
    GameObject world;
    GameObject activeCustomer;
    GameObject bowlLayers;
    GameObject startPanel;
    GameObject hud;
    GameObject endPanel;
    GameObject tutorialPanel;
    GameObject pausePanel;

    Text dayText;
    Text cashText;
    Text ratingText;
    Text timerText;
    Text customerText;
    Text orderText;
    Text bowlText;
    Text feedbackText;
    Text rushText;
    Text startStats;
    Text endTitle;
    Text endStats;
    Text endStars;
    Text tutorialText;

    Slider patienceBar;
    Button nextButton;
    Button patUpgrade;
    Button priceUpgrade;
    Button tipsUpgrade;
    Button stockUpgrade;
    Button soundButton;

    readonly Dictionary<string,int> stock = new Dictionary<string,int>();
    readonly Dictionary<string,Text> stockLabels = new Dictionary<string,Text>();
    readonly Dictionary<string,Button> ingredientButtons = new Dictionary<string,Button>();
    readonly List<string> order = new List<string>();
    readonly List<string> bowl = new List<string>();

    AudioSource audioSource;
    AudioClip clickClip;
    AudioClip okClip;
    AudioClip badClip;
    AudioClip coinClip;

    string fatalError = "";
    int day;
    int cash;
    int served;
    int target;
    int mistakes;
    int combo;
    int bestCombo;
    int patienceLevel;
    int priceLevel;
    int tipsLevel;
    int stockLevel;
    int tutorialStep;
    int dayStartCash;
    int basePrice;
    int dayStars;
    float rating;
    float timeLeft;
    float patience;
    float maxPatience;
    bool playing;
    bool paused;
    bool switching;
    bool soundOn;
    bool tutorialSeen;
    bool passedDay;
    bool rushOrder;
    string sizeName;

    void Awake()
    {
        Screen.orientation = ScreenOrientation.Portrait;
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        QualitySettings.vSyncCount = 0;

        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        worldShader = Resources.Load<Shader>("KosharetoMobile");
        uiShader = Resources.Load<Shader>("KosharetoUI");

        if (font == null) fatalError += "Runtime font missing. ";
        if (worldShader == null || !worldShader.isSupported) fatalError += "World shader missing/unsupported. ";
        if (uiShader == null || !uiShader.isSupported) fatalError += "UI shader missing/unsupported. ";

        LoadProgress();
        BuildAudio();

        try
        {
            BuildUI();
            ShowMenu();
        }
        catch (Exception ex)
        {
            fatalError += "UI failure: " + ex.GetType().Name + " - " + ex.Message + ". ";
            Debug.LogException(ex);
        }

        try
        {
            BuildWorld();
        }
        catch (Exception ex)
        {
            fatalError += "World failure: " + ex.GetType().Name + " - " + ex.Message + ". ";
            Debug.LogException(ex);
        }
    }

    void OnGUI()
    {
        if (string.IsNullOrEmpty(fatalError)) return;
        GUI.color = Color.black;
        GUI.Box(new Rect(0,0,Screen.width,Screen.height), "");
        GUI.color = Color.white;
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = Mathf.Max(18, Screen.width / 24);
        style.wordWrap = true;
        style.alignment = TextAnchor.MiddleCenter;
        GUI.Label(new Rect(Screen.width*.06f, Screen.height*.20f, Screen.width*.88f, Screen.height*.60f),
            "KOSHARETO SAFE MODE\n\n" + fatalError + "\n\nVersion 1.0.0", style);
    }

    void Update()
    {
        ApplySafeArea();
        AnimateWorld();
        if (!playing || paused || switching || timerText == null || patienceBar == null) return;

        float dt = Time.unscaledDeltaTime;
        timeLeft = Mathf.Max(0, timeLeft - dt);
        patience = Mathf.Max(0, patience - dt);

        int seconds = Mathf.CeilToInt(timeLeft);
        timerText.text = string.Format("{0:00}:{1:00}", seconds / 60, seconds % 60);
        timerText.color = timeLeft < 20 ? new Color(1,.32f,.18f) : Cream;
        patienceBar.value = maxPatience <= 0 ? 0 : patience / maxPatience;

        if (rushText != null) rushText.gameObject.SetActive(rushOrder);

        if (patience <= 0) StartCoroutine(CustomerLeaves(false, "CUSTOMER LEFT!"));
        else if (timeLeft <= 0) FinishDay();
    }

    void LoadProgress()
    {
        day = Mathf.Max(1, PlayerPrefs.GetInt("day",1));
        cash = Mathf.Max(0, PlayerPrefs.GetInt("cash",60));
        patienceLevel = Mathf.Max(0, PlayerPrefs.GetInt("pat",0));
        priceLevel = Mathf.Max(0, PlayerPrefs.GetInt("price",0));
        tipsLevel = Mathf.Max(0, PlayerPrefs.GetInt("tips",0));
        stockLevel = Mathf.Max(0, PlayerPrefs.GetInt("stock",0));
        rating = Mathf.Clamp(PlayerPrefs.GetFloat("rating",4.5f),1,5);
        tutorialSeen = PlayerPrefs.GetInt("tutorial",0) == 1;
        soundOn = PlayerPrefs.GetInt("sound",1) == 1;
    }

    void SaveProgress()
    {
        PlayerPrefs.SetInt("day",day);
        PlayerPrefs.SetInt("cash",cash);
        PlayerPrefs.SetInt("pat",patienceLevel);
        PlayerPrefs.SetInt("price",priceLevel);
        PlayerPrefs.SetInt("tips",tipsLevel);
        PlayerPrefs.SetInt("stock",stockLevel);
        PlayerPrefs.SetFloat("rating",rating);
        PlayerPrefs.SetInt("tutorial",tutorialSeen ? 1 : 0);
        PlayerPrefs.SetInt("sound",soundOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    void StartPressed()
    {
        Sfx(clickClip);
        if (!tutorialSeen)
        {
            tutorialStep = 0;
            ShowTutorialStep();
            return;
        }
        StartDay();
    }

    void StartDay()
    {
        if (startPanel != null) startPanel.SetActive(false);
        if (endPanel != null) endPanel.SetActive(false);
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (hud != null) hud.SetActive(true);

        playing = true;
        paused = false;
        switching = false;
        passedDay = false;
        served = 0;
        mistakes = 0;
        combo = 0;
        bestCombo = 0;
        dayStars = 0;
        dayStartCash = cash;
        target = Mathf.Clamp(7 + day, 8, 20);
        timeLeft = 105 + Mathf.Min(day,6) * 5;
        rating = Mathf.Clamp(rating,2.5f,5);

        int capacity = 10 + stockLevel * 3 + Mathf.Min(day,4);
        stock.Clear();
        for (int i=0;i<ingredients.Length;i++) stock[ingredients[i]] = capacity;

        UpdateStock();
        ClearBowl(false);
        UpdateHud();
        NewOrder(true);
        Flash("SHOP OPEN!", Gold);
    }

    void NewOrder(bool instant)
    {
        order.Clear();
        order.Add("RICE");
        order.Add("PASTA");
        order.Add("LENTILS");
        order.Add("TOMATO");

        float difficulty = Mathf.Min(.30f, day * .018f);
        if (UnityEngine.Random.value < .72f + difficulty) order.Add("CHICKPEAS");
        if (UnityEngine.Random.value < .52f + difficulty) order.Add("GARLIC");
        if (UnityEngine.Random.value < .28f + difficulty) order.Add("CHILI");
        if (UnityEngine.Random.value < .58f + difficulty) order.Add("ONION");

        int size = UnityEngine.Random.Range(0,3);
        sizeName = size == 0 ? "SMALL" : size == 1 ? "MEDIUM" : "LARGE";
        basePrice = (size == 0 ? 18 : size == 1 ? 25 : 33) + priceLevel * 3 + Mathf.Min(day,10);

        rushOrder = day >= 2 && served > 0 && served % 4 == 0;
        float rushFactor = rushOrder ? .68f : 1f;
        maxPatience = Mathf.Max(8, (23 + patienceLevel * 2.8f - Mathf.Min(day * .5f,6)) * rushFactor);
        patience = maxPatience;

        if (customerText != null) customerText.text = names[UnityEngine.Random.Range(0,names.Length)] + "  •  " + sizeName;
        if (orderText != null) orderText.text = string.Join("  +  ", order.ToArray());
        SpawnCustomer(instant);
        UpdateHud();
        if (rushOrder) Flash("RUSH ORDER! +25% BONUS", new Color(1,.46f,.12f));
    }

    void AddIngredient(string item)
    {
        if (!playing || paused || switching) return;
        if (!stock.ContainsKey(item) || stock[item] <= 0)
        {
            Flash("OUT OF " + item + "!", new Color(1,.28f,.18f));
            Sfx(badClip);
            return;
        }
        if (bowl.Contains(item))
        {
            Flash("ALREADY ADDED", Gold);
            return;
        }

        stock[item]--;
        bowl.Add(item);
        int idx = Array.IndexOf(ingredients,item);
        AddBowlLayer(item, idx);
        Sfx(clickClip);
        UpdateStock();
        UpdateBowl();
    }

    void ClearBowl(bool message)
    {
        bowl.Clear();
        ClearBowlVisual();
        UpdateBowl();
        if (message && playing) Flash("BOWL CLEARED", Cream);
    }

    bool IsOrderCorrect()
    {
        if (bowl.Count != order.Count) return false;
        for (int i=0;i<order.Count;i++) if (!bowl.Contains(order[i])) return false;
        return true;
    }

    void Serve()
    {
        if (!playing || paused || switching) return;
        if (bowl.Count == 0)
        {
            Flash("BUILD THE BOWL FIRST!", Gold);
            Sfx(badClip);
            return;
        }

        if (!IsOrderCorrect())
        {
            mistakes++;
            combo = 0;
            rating = Mathf.Clamp(rating - .16f,1,5);
            Sfx(badClip);
            Flash("WRONG ORDER!", new Color(1,.24f,.16f));
            UpdateHud();
            return;
        }

        float patienceRatio = maxPatience <= 0 ? 0 : patience / maxPatience;
        int comboBonus = Mathf.Min(combo,6);
        int rushBonus = rushOrder ? Mathf.CeilToInt(basePrice * .25f) : 0;
        float tipChance = .08f + tipsLevel * .075f + patienceRatio * .12f;
        int tip = UnityEngine.Random.value < tipChance ? UnityEngine.Random.Range(2,7) + tipsLevel * 2 : 0;
        int earned = basePrice + comboBonus + rushBonus + tip;

        cash += earned;
        served++;
        combo++;
        bestCombo = Mathf.Max(bestCombo,combo);
        rating = Mathf.Clamp(rating + .03f + patienceRatio * .025f,1,5);
        Sfx(okClip);
        if (tip > 0) Sfx(coinClip);

        Flash("PERFECT!  +$" + earned + (tip > 0 ? "  TIP!" : ""), Green);
        ClearBowl(false);
        UpdateHud();

        if (served >= target)
        {
            FinishDay();
            return;
        }
        StartCoroutine(CustomerLeaves(true,""));
    }

    IEnumerator CustomerLeaves(bool happy, string message)
    {
        if (switching) yield break;
        switching = true;

        if (!happy)
        {
            mistakes++;
            combo = 0;
            rating = Mathf.Clamp(rating - .22f,1,5);
            Sfx(badClip);
            Flash(message,new Color(1,.24f,.16f));
            UpdateHud();
        }

        yield return MoveCustomerOut(happy);
        yield return new WaitForSecondsRealtime(.15f);
        ClearBowl(false);
        NewOrder(false);
        switching = false;
    }

    void Restock()
    {
        if (!playing || paused || switching) return;
        int cost = 14 + Mathf.Min(day,8) * 2;
        if (cash < cost)
        {
            Flash("NEED $" + cost + " TO RESTOCK", Gold);
            Sfx(badClip);
            return;
        }

        cash -= cost;
        int amount = 5 + stockLevel;
        for (int i=0;i<ingredients.Length;i++) stock[ingredients[i]] += amount;
        Sfx(coinClip);
        Flash("RESTOCKED +" + amount + " EACH", new Color(.34f,.73f,.95f));
        UpdateStock();
        UpdateHud();
    }

    void FinishDay()
    {
        if (!playing) return;
        playing = false;
        paused = false;
        switching = false;
        if (hud != null) hud.SetActive(false);
        if (endPanel != null) endPanel.SetActive(true);

        passedDay = served >= Mathf.Max(5, Mathf.CeilToInt(target * .65f)) && rating >= 2.5f;
        float accuracy = served <= 0 ? 0 : Mathf.Clamp01(1f - mistakes / (float)Mathf.Max(1,served + mistakes));
        dayStars = !passedDay ? 0 : accuracy > .90f && rating >= 4.4f ? 3 : accuracy > .72f && rating >= 3.7f ? 2 : 1;
        int bonus = passedDay ? 18 + day * 5 + dayStars * 12 + bestCombo * 2 : 0;
        cash += bonus;

        if (endTitle != null)
        {
            endTitle.text = passedDay ? "DAY COMPLETE" : "ROUGH DAY";
            endTitle.color = passedDay ? Gold : new Color(1,.34f,.24f);
        }
        if (endStars != null) endStars.text = dayStars == 0 ? "☆  ☆  ☆" : dayStars == 1 ? "★  ☆  ☆" : dayStars == 2 ? "★  ★  ☆" : "★  ★  ★";
        if (endStats != null)
        {
            int profit = cash - dayStartCash;
            endStats.text = "SERVED " + served + "/" + target + "\nMISTAKES " + mistakes + "   •   BEST COMBO x" + bestCombo +
                "\nRATING ★ " + rating.ToString("0.0") + "   •   BONUS $" + bonus + "\nDAY PROFIT $" + profit + "   •   CASH $" + cash;
        }

        if (nextButton != null)
        {
            SetButtonLabel(nextButton, passedDay ? "NEXT DAY" : "RETRY DAY");
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(AdvanceFromEnd);
        }

        UpdateUpgrades();
        SaveProgress();
    }

    void AdvanceFromEnd()
    {
        Sfx(clickClip);
        if (passedDay) day++;
        SaveProgress();
        ShowMenu();
    }

    int UpgradeCost(int type, int level)
    {
        int baseCost = type == 0 ? 80 : type == 1 ? 100 : type == 2 ? 115 : 90;
        return baseCost + level * 55;
    }

    void BuyUpgrade(int type)
    {
        int level = type == 0 ? patienceLevel : type == 1 ? priceLevel : type == 2 ? tipsLevel : stockLevel;
        int cost = UpgradeCost(type,level);
        if (cash < cost)
        {
            Sfx(badClip);
            Flash("NOT ENOUGH CASH", Red);
            return;
        }

        cash -= cost;
        if (type == 0) patienceLevel++;
        else if (type == 1) priceLevel++;
        else if (type == 2) tipsLevel++;
        else stockLevel++;
        Sfx(coinClip);
        SaveProgress();
        UpdateUpgrades();
        if (endStats != null) endStats.text += "\nUPGRADE PURCHASED!";
    }

    void PauseGame()
    {
        if (!playing || switching) return;
        paused = true;
        if (pausePanel != null) pausePanel.SetActive(true);
        Sfx(clickClip);
    }

    void ResumeGame()
    {
        paused = false;
        if (pausePanel != null) pausePanel.SetActive(false);
        Sfx(clickClip);
    }

    void ReturnToMenu()
    {
        playing = false;
        paused = false;
        switching = false;
        SaveProgress();
        ShowMenu();
    }

    void ToggleSound()
    {
        soundOn = !soundOn;
        SaveProgress();
        RefreshMenu();
        if (soundOn) Sfx(clickClip);
    }

    void ShowMenu()
    {
        playing = false;
        paused = false;
        switching = false;
        if (hud != null) hud.SetActive(false);
        if (endPanel != null) endPanel.SetActive(false);
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (startPanel != null) startPanel.SetActive(true);
        RefreshMenu();
    }

    void CompleteTutorialStep()
    {
        tutorialStep++;
        if (tutorialStep >= 3)
        {
            tutorialSeen = true;
            SaveProgress();
            if (tutorialPanel != null) tutorialPanel.SetActive(false);
            StartDay();
            return;
        }
        ShowTutorialStep();
    }

    void BuildAudio()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.volume = .58f;
        clickClip = Tone("click",700,.05f,.14f);
        okClip = Tone("ok",980,.11f,.23f);
        badClip = Tone("bad",175,.16f,.20f);
        coinClip = Tone("coin",1320,.08f,.18f);
    }

    AudioClip Tone(string clipName, float hz, float length, float volume)
    {
        int rate = 22050;
        int count = Mathf.Max(1,Mathf.RoundToInt(rate * length));
        float[] data = new float[count];
        for (int i=0;i<count;i++)
        {
            float t = i / (float)rate;
            float env = 1f - i / (float)count;
            data[i] = Mathf.Sin(2 * Mathf.PI * hz * t) * volume * env;
        }
        AudioClip clip = AudioClip.Create(clipName,count,1,rate,false);
        clip.SetData(data,0);
        return clip;
    }

    void Sfx(AudioClip clip)
    {
        if (soundOn && clip != null && audioSource != null) audioSource.PlayOneShot(clip);
    }

    void Flash(string message, Color color)
    {
        if (feedbackText == null) return;
        feedbackText.text = message;
        feedbackText.color = color;
        StopCoroutine("FadeFeedback");
        StartCoroutine("FadeFeedback");
    }

    IEnumerator FadeFeedback()
    {
        if (feedbackText == null) yield break;
        Color c = feedbackText.color;
        c.a = 1;
        feedbackText.color = c;
        yield return new WaitForSecondsRealtime(.85f);
        float elapsed = 0;
        while (elapsed < .45f && feedbackText != null)
        {
            elapsed += Time.unscaledDeltaTime;
            c.a = 1 - Mathf.Clamp01(elapsed / .45f);
            feedbackText.color = c;
            yield return null;
        }
    }

    void UpdateHud()
    {
        if (dayText != null) dayText.text = "DAY " + day + "  " + served + "/" + target;
        if (cashText != null) cashText.text = "$ " + cash;
        if (ratingText != null) ratingText.text = "★ " + rating.ToString("0.0");
    }

    void UpdateStock()
    {
        for (int i=0;i<ingredients.Length;i++)
        {
            string item = ingredients[i];
            int value = stock.ContainsKey(item) ? stock[item] : 0;
            if (stockLabels.ContainsKey(item)) stockLabels[item].text = "x" + value;
            if (ingredientButtons.ContainsKey(item)) ingredientButtons[item].interactable = value > 0;
        }
    }

    void UpdateBowl()
    {
        if (bowlText != null) bowlText.text = bowl.Count == 0 ? "BOWL: EMPTY" : "BOWL: " + string.Join(" • ",bowl.ToArray());
    }

    void UpdateUpgrades()
    {
        UpdateUpgradeButton(patUpgrade,"PATIENCE +12%",0,patienceLevel);
        UpdateUpgradeButton(priceUpgrade,"PRICE +$3",1,priceLevel);
        UpdateUpgradeButton(tipsUpgrade,"TIP CHANCE +7%",2,tipsLevel);
        UpdateUpgradeButton(stockUpgrade,"STOCK CAPACITY",3,stockLevel);
    }

    void UpdateUpgradeButton(Button button, string title, int type, int level)
    {
        if (button == null) return;
        int cost = UpgradeCost(type,level);
        SetButtonLabel(button,title + "\n$" + cost + "  •  Lv." + level);
        button.interactable = cash >= cost;
    }
}