using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager instance;

    [Header("UI Components")]
    public Canvas transitionCanvas;
    public Image fadeImage;

    [Header("Settings")]
    public float fadeSpeed = 1f;

    private void Awake()
    {
        // Membuat script ini menjadi Singleton dan tidak hancur saat pindah scene (DontDestroyOnLoad)
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Pastikan Canvas juga ikut tidak hancur
            if (transitionCanvas != null)
            {
                DontDestroyOnLoad(transitionCanvas.gameObject);
            }
        }
        else
        {
            Destroy(gameObject);
            if (transitionCanvas != null && transitionCanvas.gameObject != instance.transitionCanvas.gameObject)
            {
                Destroy(transitionCanvas.gameObject);
            }
            return;
        }

        // Mendaftarkan fungsi agar otomatis Fade In saat scene baru selesai dimuat
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Jalankan efek Fade In setiap masuk scene baru
        StartCoroutine(FadeIn());
    }

    // Fungsi ini bisa dipanggil dari script manapun menggunakan: SceneTransitionManager.instance.LoadScene("NamaScene");
    public void LoadScene(string sceneName)
    {
        StartCoroutine(FadeOutAndLoad(sceneName));
    }

    private IEnumerator FadeOutAndLoad(string sceneName)
    {
        // Pastikan gambar aktif
        fadeImage.gameObject.SetActive(true);

        float elapsedTime = 0f;
        Color color = fadeImage.color;
        float startVolume = AudioListener.volume;

        // Proses Fade Out Gambar (ke hitam) dan Fade Out Suara (ke 0)
        while (elapsedTime < fadeSpeed)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeSpeed;
            
            // Fade Out Layar
            color.a = Mathf.Lerp(0f, 1f, t);
            fadeImage.color = color;

            // Fade Out Suara Global
            AudioListener.volume = Mathf.Lerp(startVolume, 0f, t);

            yield return null;
        }

        // Pindah scene
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator FadeIn()
    {
        fadeImage.gameObject.SetActive(true);

        float elapsedTime = 0f;
        Color color = fadeImage.color;
        // Kita anggap suara akan kembali normal ke volume 1
        float targetVolume = 1f; 

        // Proses Fade In Gambar (transparan) dan Fade In Suara
        while (elapsedTime < fadeSpeed)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeSpeed;
            
            // Fade In Layar
            color.a = Mathf.Lerp(1f, 0f, t);
            fadeImage.color = color;

            // Fade In Suara Global
            AudioListener.volume = Mathf.Lerp(0f, targetVolume, t);

            yield return null;
        }

        // Nonaktifkan gambar agar tidak menghalangi klik UI
        fadeImage.gameObject.SetActive(false);
    }
}
