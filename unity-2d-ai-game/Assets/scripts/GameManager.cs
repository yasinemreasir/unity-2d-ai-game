using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Oyunun bitiş akışı: oyuncu ölünce Game Over, boss ölünce kazanma paneli.
// Paneldeki butonlar Restart / LoadMainMenu / QuitGame metotlarına bağlanır (Button > OnClick).
public class GameManager : MonoBehaviour
{
    [Header("Dinlenen karakterler")]
    [Tooltip("Boşsa 'Player' tag'li obje otomatik bulunur.")]
    [SerializeField] PlayerController player;
    [Tooltip("Boss (kırmızı slime). Ölünce oyun biter ve kazanma paneli açılır.")]
    [SerializeField] Enemy boss;

    [Header("Paneller")]
    [SerializeField] GameObject gameOverPanel;
    [SerializeField] GameObject winPanel;
    [Tooltip("Ölüm animasyonu görünsün diye panel kaç saniye sonra açılsın.")]
    [SerializeField] float endScreenDelay = 1.5f;

    [Header("Sahneler")]
    [Tooltip("Ana menü sahnesinin adı (Build Settings'te ekli olmalı).")]
    [SerializeField] string menuSceneName = "MainMenu";

    bool gameEnded;

    void Awake()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (winPanel != null) winPanel.SetActive(false);
    }

    void OnEnable()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.GetComponent<PlayerController>();
            }
        }

        if (player != null)
        {
            player.Died += OnPlayerDied;
        }
        else
        {
            Debug.LogWarning("GameManager: Player bulunamadı, Game Over çalışmaz.", this);
        }

        if (boss != null)
        {
            boss.Died += OnBossDied;
        }
        else
        {
            Debug.LogWarning("GameManager: Boss atanmamış, kazanma ekranı çalışmaz.", this);
        }
    }

    void OnDisable()
    {
        if (player != null) player.Died -= OnPlayerDied;
        if (boss != null) boss.Died -= OnBossDied;
    }

    void OnPlayerDied()
    {
        EndGame(gameOverPanel, "Game Over");
    }

    void OnBossDied(Enemy defeated)
    {
        if (gameEnded) return;

        // Kazandık: oyuncu artık hareket/saldırı yapmasın
        if (player != null) player.SetInputLocked(true);

        EndGame(winPanel, "Kazandın");
    }

    void EndGame(GameObject panel, string reason)
    {
        // Oyun bir kez biter (boss öldükten sonra oyuncu ölse bile kazanma ekranı kalır)
        if (gameEnded) return;
        gameEnded = true;

        Debug.Log("[GameManager] Oyun bitti: " + reason, this);
        StartCoroutine(ShowPanelAfterDelay(panel));
    }

    IEnumerator ShowPanelAfterDelay(GameObject panel)
    {
        yield return new WaitForSeconds(endScreenDelay);

        if (panel != null)
        {
            panel.SetActive(true);
        }
    }

    // --- Buton olayları ---

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadMainMenu()
    {
        if (!Application.CanStreamedLevelBeLoaded(menuSceneName))
        {
            Debug.LogError("GameManager: '" + menuSceneName + "' sahnesi yüklenemiyor. File > Build Settings'e ekledin mi?", this);
            return;
        }

        SceneManager.LoadScene(menuSceneName);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
