using UnityEngine;
using UnityEngine.UI; // Standart UI için
using TMPro; // TextMeshPro için (Daha kaliteli yazýlar)
using UnityEngine.SceneManagement; // Sahne yüklemek için

public class GameManager : MonoBehaviour
{
    public static GameManager inst; // Heryerden ulaþabilmek için (Singleton)

    [Header("Score Settings")]
    public int score;
    public int coins; // Toplanan altýn sayýsý
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI coinText; // Altýn sayýsý yazýsý
    public TextMeshProUGUI finalScoreText; // Game Over panelindeki skor yazýsý

    [Header("Game Over Settings")]
    public GameObject gameOverPanel;

    private void Awake()
    {
        inst = this;
    }

    public bool isGameActive = false;
    float scoreAccumulator = 0;

    // PlayerController referansý (Hýz artýþý için)
    PlayerController playerController;

    private void Start()
    {
        score = 0;
        coins = 0; // Sýfýrla

        // PlayerController'ý bul
        playerController = FindFirstObjectByType<PlayerController>();

        // High Score'u yükle
        if (PlayerPrefs.HasKey("HighScore"))
        {
            Debug.Log("High Score: " + PlayerPrefs.GetInt("HighScore"));
        }

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    private void FixedUpdate()
    {
        if (!isGameActive) return;

        // Her saniye 1 puan (Eskiden 10'du)
        scoreAccumulator += 1 * Time.fixedDeltaTime;

        if (scoreAccumulator >= 1)
        {
            int gain = Mathf.FloorToInt(scoreAccumulator);
            score += gain;
            scoreAccumulator -= gain;
            UpdateScoreUI();
        }

        // --- AÞAMALI HIZ ARTIRMA (YENÝ) ---
        // Formül: BaseSpeed (10) + (Score * 0.01)
        if (playerController != null)
        {
            float baseSpeed = 10f; // Baþlangýç hýzý
            float newSpeed = baseSpeed + (score * 0.01f);
            playerController.SetSpeed(newSpeed);
        }
    }

    public void StartGameSignal()
    {
        isGameActive = true;
        if (playerController != null) playerController.StartRunning();
    }

    public void IncrementScore()
    {
        // Manuel artýþ (Örn: Bonus)
        score += 10;
        UpdateScoreUI();
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "SCORE: " + score;
        }
    }

    public void IncrementCoin()
    {
        coins++;
        // Altýn baþýna +1 Skor
        score++;

        if (coinText != null)
        {
            coinText.text = "COINS: " + coins;
        }
        UpdateScoreUI(); // Skoru da güncelle
    }

    public void TriggerGameOver()
    {
        isGameActive = false; // Skoru durdur
        Debug.Log("GameManager: Oyun Bitti!");

        // Ses Çal
        if (AudioManager.inst != null) AudioManager.inst.PlayGameOverSound();

        // High Score Kaydet
        int currentHighScore = PlayerPrefs.GetInt("HighScore", 0);
        if (score > currentHighScore)
        {
            PlayerPrefs.SetInt("HighScore", score);
            PlayerPrefs.Save();
            Debug.Log("Yeni High Score: " + score);
        }

        // Paneli aç
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);

            // Final Skorunu Yazdýr
            if (finalScoreText != null)
            {
                // High Score (Zaten yukarýda tanýmlýydý, tekrar tanýmlama!)
                currentHighScore = PlayerPrefs.GetInt("HighScore", 0);
                finalScoreText.text = "SCORE: " + score + "\nBEST: " + currentHighScore;
            }

            // Eðer panelin içinde "ScoreText" vs varsa onlarý da güncellemek þýk olur
            // Ama þimdilik basit tutuyoruz.
        }
    }

    public void RestartGame()
    {
        // Sahneyi baþtan yükle
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}