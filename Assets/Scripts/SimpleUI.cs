using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SimpleUI : MonoBehaviour
{
    // Bu metod sahne yüklendiðinde otomatik çalýþýr.
    // Böylece objeyi sahneye elle koymaya gerek kalmaz.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void OnRuntimeMethodLoad()
    {
        GameObject uiObj = new GameObject("SimpleUI_Auto");
        uiObj.AddComponent<SimpleUI>();
        DontDestroyOnLoad(uiObj); // Sahne deðiþince yok olmasýn
    }

    void Start()
    {
        SetupUI();
    }

    void SetupUI()
    {
        // 1. Canvas Var mý?
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // 1.5 EventSystem Var mý? (Butonlarýn çalýþmasý için þart)
        UnityEngine.EventSystems.EventSystem eventSystem = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // 2. Score Text (Kýrmýzý - Sol Üst)
        GameObject scoreObj = new GameObject("ScoreText");
        scoreObj.transform.SetParent(canvas.transform, false);
        TextMeshProUGUI scoreText = scoreObj.AddComponent<TextMeshProUGUI>();
        scoreText.text = "SCORE: 0";
        scoreText.fontSize = 36;
        scoreText.color = Color.red; // Kýrmýzý yapýldý
        scoreText.fontStyle = FontStyles.Bold;
        scoreText.alignment = TextAlignmentOptions.TopLeft;

        RectTransform scoreRect = scoreText.rectTransform;
        scoreRect.sizeDelta = new Vector2(400, 50); // Geniþlik ver, sýðsýn
        scoreRect.anchorMin = new Vector2(0, 1);
        scoreRect.anchorMax = new Vector2(0, 1);
        scoreRect.pivot = new Vector2(0, 1);
        scoreRect.anchoredPosition = new Vector2(20, -20); // Sol Üstten 20 birim içeride

        // 3. Coin Text (Kýrmýzý - Score'un Altýnda)
        GameObject coinObj = new GameObject("CoinText");
        coinObj.transform.SetParent(canvas.transform, false);
        TextMeshProUGUI coinText = coinObj.AddComponent<TextMeshProUGUI>();
        coinText.text = "COINS: 0";
        coinText.fontSize = 30; // Biraz daha küçük
        coinText.color = Color.red; // Kýrmýzý yapýldý
        coinText.fontStyle = FontStyles.Bold;
        coinText.alignment = TextAlignmentOptions.TopLeft;

        RectTransform coinRect = coinText.rectTransform;
        coinRect.sizeDelta = new Vector2(400, 50); // Geniþlik ver
        coinRect.anchorMin = new Vector2(0, 1);
        coinRect.anchorMax = new Vector2(0, 1);
        coinRect.pivot = new Vector2(0, 1);
        coinRect.anchoredPosition = new Vector2(20, -80); // Score'un altýnda daha fazla boþluk (Eskiden -70)

        // 4. Start Game Button (Baþlangýç Ekraný)
        GameObject startBtnObj = new GameObject("StartButton");
        startBtnObj.transform.SetParent(canvas.transform, false);
        Image startBtnImg = startBtnObj.AddComponent<Image>();
        startBtnImg.color = Color.green;
        Button startBtn = startBtnObj.AddComponent<Button>();

        RectTransform startBtnRect = startBtnObj.GetComponent<RectTransform>();
        startBtnRect.sizeDelta = new Vector2(300, 100);
        startBtnRect.anchoredPosition = Vector2.zero; // Tam Ortada

        GameObject startBtnTextObj = new GameObject("StartBtnText");
        startBtnTextObj.transform.SetParent(startBtnObj.transform, false);
        TextMeshProUGUI startBtnText = startBtnTextObj.AddComponent<TextMeshProUGUI>();
        startBtnText.text = "BAÞLA";
        startBtnText.fontSize = 48;
        startBtnText.color = Color.white;
        startBtnText.alignment = TextAlignmentOptions.Center;

        RectTransform startTextRect = startBtnTextObj.GetComponent<RectTransform>();
        startTextRect.anchorMin = Vector2.zero;
        startTextRect.anchorMax = Vector2.one;
        startTextRect.offsetMin = Vector2.zero;
        startTextRect.offsetMax = Vector2.zero;

        // Baþla Butonu Olayý
        startBtn.onClick.AddListener(() => {
            if (GameManager.inst != null) GameManager.inst.StartGameSignal();
            startBtnObj.SetActive(false); // Butonu gizle
        });

        // 5. Game Over Panel
        GameObject panelObj = new GameObject("GameOverPanel");
        panelObj.transform.SetParent(canvas.transform, false);
        Image panelImg = panelObj.AddComponent<Image>();
        panelImg.color = new Color(0, 0, 0, 0.9f); // Koyu arka plan

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one; // Tam Ekran
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Game Over Baþlýðý
        GameObject goTextObj = new GameObject("GameOverHeader");
        goTextObj.transform.SetParent(panelObj.transform, false);
        TextMeshProUGUI goText = goTextObj.AddComponent<TextMeshProUGUI>();
        goText.text = "OYUN BÝTTÝ"; // Türkçe yapýldý
        goText.fontSize = 72;
        goText.color = Color.red;
        goText.alignment = TextAlignmentOptions.Center;

        RectTransform goTextRect = goText.rectTransform;
        goTextRect.sizeDelta = new Vector2(600, 200); // KUTU GENÝÞLÝÐÝ ARTTIRILDI (Alt alta kaymasýný önler)
        goTextRect.anchoredPosition = new Vector2(0, 150); // Daha yukarý alýndý (Eskiden 100)

        // Final Score (Yeni Ýstek)
        GameObject finalScoreObj = new GameObject("FinalScoreText");
        finalScoreObj.transform.SetParent(panelObj.transform, false);
        TextMeshProUGUI finalScoreText = finalScoreObj.AddComponent<TextMeshProUGUI>();
        finalScoreText.text = "SCORE: 0";
        finalScoreText.fontSize = 48;
        finalScoreText.color = Color.white;
        finalScoreText.alignment = TextAlignmentOptions.Center;

        RectTransform finalScoreRect = finalScoreText.rectTransform;
        finalScoreRect.sizeDelta = new Vector2(600, 200); // Geniþlik ver
        finalScoreRect.anchoredPosition = new Vector2(0, 0); // Tam Ortada

        // Restart Button
        GameObject btnObj = new GameObject("RestartButton");
        btnObj.transform.SetParent(panelObj.transform, false);
        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = Color.white;
        Button btn = btnObj.AddComponent<Button>();

        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.sizeDelta = new Vector2(250, 70); // Buton büyütüldü
        btnRect.anchoredPosition = new Vector2(0, -150); // Daha aþaðý alýndý (Eskiden -120)

        GameObject btnTextObj = new GameObject("BtnText");
        btnTextObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI btnText = btnTextObj.AddComponent<TextMeshProUGUI>();
        btnText.text = "TEKRAR OYNA";
        btnText.fontSize = 28;
        btnText.color = Color.black;
        btnText.alignment = TextAlignmentOptions.Center;

        RectTransform btnTextRect = btnTextObj.GetComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.offsetMin = Vector2.zero;
        btnTextRect.offsetMax = Vector2.zero;

        // Restart Olayý
        btn.onClick.AddListener(() => {
            if (GameManager.inst != null) GameManager.inst.RestartGame();
        });

        // 6. GameManager'a Baðla
        if (GameManager.inst != null)
        {
            GameManager.inst.scoreText = scoreText;
            GameManager.inst.coinText = coinText;
            GameManager.inst.gameOverPanel = panelObj;
            GameManager.inst.finalScoreText = finalScoreText; // Yeni alaný baðla

            // Paneli baþta kapat
            panelObj.SetActive(false);
        }
    }
}
