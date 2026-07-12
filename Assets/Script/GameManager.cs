using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int suratTerkumpul = 0;
    public int targetSurat = 5; // Diubah menjadi 5

    [Header("Timer UI")]
    public Text timerText;
    public float timeElapsed = 0f;
    public bool isGameActive = true;

    [Header("Surat UI")]
    public Text suratCounterText; // UI Teks untuk menampilkan X/5 surat

    [Header("UI Panels")]
    public GameObject panelKodeSurat;
    public GameObject panelSoal;
    public GameObject panelGameOver; // Panel saat player mati

    [Header("UI Pemberitahuan (Baru)")]
    public Text notificationText; // Tarik object NotifSurat ke sini
    public Text legacyText; // Tarik Teks Legacy (ukuran kecil) ke sini
    [TextArea(2, 4)]
    public string pesanBukaSurat = "Tekan 1 untuk melihat petunjuk!"; // Bisa diganti di Inspector

    [Header("Pengaturan Intro Level")]
    [Tooltip("Centang jika ada Cutscene Intro di scene ini agar waktu tidak langsung berjalan")]
    public bool adaIntroCutscene = false;

    // Penanda status item
    public bool isSuratLengkap = false;
    public bool isPasscodeRead = false;
    public bool isPlayerAtExit = false;
    private bool hasSoal = false;

    private void Awake()
    {
        Instance = this;
    }

    private Coroutine notifCoroutine;

    private void Start()
    {
        UpdateSuratUI(); // Update teks 0/5 saat game baru mulai
        SembunyikanNotifikasi(); // Pastikan teks mati di awal
        SembunyikanLegacyText();
        
        if (adaIntroCutscene)
        {
            isGameActive = false; // Tahan jalannya waktu selama cutscene
        }
        else
        {
            MunculkanLegacyTextSementara("Mulai jelajahi ruangan dan cari potongan surat itu!", 3f);
        }
    }

    // Dipanggil oleh CutSceneManager ketika cutscene awal level selesai
    public void MulaiGameSetelahIntro()
    {
        isGameActive = true; // Jalankan kembali waktu game
        MunculkanLegacyTextSementara("Mulai jelajahi ruangan dan cari potongan surat itu!", 3f);
    }

    // --- FUNGSI NOTIFIKASI PINTAR ---
    public void MunculkanNotifikasi(string pesan)
    {
        if(notificationText != null)
        {
            if (notifCoroutine != null)
            {
                StopCoroutine(notifCoroutine);
                notifCoroutine = null;
            }
            notificationText.text = pesan;
            notificationText.gameObject.SetActive(true);
        }
    }

    public void SembunyikanNotifikasi()
    {
        if(notificationText != null)
        {
            notificationText.gameObject.SetActive(false);
        }
    }

    // --- FUNGSI LEGACY TEXT (TEKS KECIL) ---
    private Coroutine legacyCoroutine;

    public void MunculkanLegacyText(string pesan)
    {
        if(legacyText != null)
        {
            if (legacyCoroutine != null)
            {
                StopCoroutine(legacyCoroutine);
                legacyCoroutine = null;
            }
            legacyText.text = pesan;
            legacyText.gameObject.SetActive(true);
        }
    }

    public void MunculkanLegacyTextSementara(string pesan, float durasi)
    {
        if(legacyText != null)
        {
            if (legacyCoroutine != null)
            {
                StopCoroutine(legacyCoroutine);
            }
            legacyText.text = pesan;
            legacyText.gameObject.SetActive(true);
            legacyCoroutine = StartCoroutine(TutupLegacyTextOtomatis(durasi));
        }
    }

    private IEnumerator TutupLegacyTextOtomatis(float durasi)
    {
        yield return new WaitForSeconds(durasi);
        SembunyikanLegacyText();
    }

    public void SembunyikanLegacyText()
    {
        if(legacyText != null)
        {
            legacyText.gameObject.SetActive(false);
        }
    }
    // --------------------------------
    // --------------------------------

    private void Update()
    {
        // Waktu akan terus bertambah selama game aktif
        if (isGameActive)
        {
            timeElapsed += Time.deltaTime;
            UpdateTimerUI();
        }

        // Cari referensi otomatis jika kosong
        if (panelKodeSurat == null)
        {
            GameObject canvas = GameObject.Find("Canvas");
            if (canvas != null) panelKodeSurat = canvas.transform.Find("Surat Kode3")?.gameObject;
        }

        // Cek Input untuk membuka/menutup Kode Surat
        if (isSuratLengkap)
        {
            if (panelKodeSurat != null && panelKodeSurat.activeSelf)
            {
                // Tutup panel
                if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Escape))
                {
                    panelKodeSurat.SetActive(false);
                    isPasscodeRead = true; // Tandai sudah dibaca agar pointer bisa menunjuk ke exit
                    
                    SembunyikanNotifikasi(); // Sembunyikan teks perintah tekan 1
                }
            }
            else
            {
                // Buka panel
                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    if (panelKodeSurat != null) panelKodeSurat.SetActive(true);
                    SembunyikanNotifikasi(); // Sembunyikan teks saat surat terbuka
                }
            }
        }

        // Cek Input untuk membuka Soal (Tombol E)
        if (hasSoal && Input.GetKeyDown(KeyCode.E))
        {
            if (panelSoal != null)
            {
                bool isPanelActive = panelSoal.activeSelf;
                panelSoal.SetActive(!isPanelActive);
            }
        }
    }

    void UpdateTimerUI()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(timeElapsed / 60F);
            int seconds = Mathf.FloorToInt(timeElapsed - minutes * 60);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }

    public void AmbilSurat()
    {
        suratTerkumpul++;

        if(suratTerkumpul >= targetSurat)
        {
            isSuratLengkap = true; // Tandai surat sudah lengkap
            
            // Panggil fungsi memunculkan notifikasi pintar dengan pesan khusus
            MunculkanNotifikasi(pesanBukaSurat);
        }
        
        UpdateSuratUI(); // Update UI setiap kali ambil surat
    }

    public void UpdateSuratUI()
    {
        if (suratCounterText != null)
        {
            suratCounterText.text = suratTerkumpul + " / " + targetSurat;
        }
    }

    public void AmbilSoal()
    {
        hasSoal = true; // Tandai soal sudah didapatkan
    }

    public void GameOver()
    {
        if (!isGameActive) return;

        isGameActive = false; // Hentikan timer

        // Sembunyikan HUD (Waktu dan Surat beserta Background-nya)
        if (timerText != null) timerText.gameObject.SetActive(false);
        if (suratCounterText != null) suratCounterText.gameObject.SetActive(false);

        GameObject papanWaktu = GameObject.Find("PapanWaktu");
        if (papanWaktu != null) papanWaktu.SetActive(false);

        GameObject papanHP = GameObject.Find("PapanHP");
        if (papanHP != null) papanHP.SetActive(false);

        GameObject healthText = GameObject.Find("Health");
        if (healthText != null) healthText.SetActive(false);

        QuestPointerUI questPointerUI = FindFirstObjectByType<QuestPointerUI>();
        if (questPointerUI != null && questPointerUI.pointerArrow != null)
        {
            questPointerUI.pointerArrow.gameObject.SetActive(false);
        }

        StartCoroutine(ShowGameOverDelay());
    }

    private IEnumerator ShowGameOverDelay()
    {
        yield return new WaitForSeconds(0.5f);
        
        if (panelGameOver != null)
        {
            panelGameOver.SetActive(true);

            CanvasGroup canvasGroup = panelGameOver.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = panelGameOver.AddComponent<CanvasGroup>();
            }

            canvasGroup.alpha = 0f;
            float fadeDuration = 1.5f;
            float timer = 0f;

            while (timer < fadeDuration)
            {
                timer += Time.unscaledDeltaTime; 
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
                yield return null;
            }
            
            canvasGroup.alpha = 1f;
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToHome()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Home");
    }
}