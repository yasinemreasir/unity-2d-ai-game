using UnityEngine;
using UnityEngine.SceneManagement;

// Ana menü sahnesindeki butonların bağlandığı script (Başla / Çık).
public class MainMenu : MonoBehaviour
{
    [Tooltip("Oyun sahnesinin adı (Build Settings'te ekli olmalı).")]
    [SerializeField] string gameSceneName = "SampleScene";

    public void StartGame()
    {
        if (!Application.CanStreamedLevelBeLoaded(gameSceneName))
        {
            Debug.LogError("MainMenu: '" + gameSceneName + "' sahnesi yüklenemiyor. Adı doğru mu ve File > Build Settings'e ekli mi?", this);
            return;
        }

        SceneManager.LoadScene(gameSceneName);
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
