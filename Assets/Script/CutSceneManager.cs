using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CutSceneManager : MonoBehaviour
{
    [Header("Komponen UI")]
    public Image cutsceneImage;
    public Text dialogueText; // Menggunakan Legacy Text
    
    [Header("Data Cutscene")]
    public Sprite[] cutsceneSprites; // Array untuk menyimpan gambar-gambar cutscene
    [TextArea(3, 5)]
    public string[] cutsceneTexts; // Array untuk menyimpan teks per gambar

    [Header("Audio Settings")]
    public AudioSource bgmSource; // AudioSource untuk background music
    public AudioSource sfxSource; // AudioSource untuk efek suara ketikan
    public AudioClip firstLastAudio; // Audio untuk cutscene pertama dan terakhir
    public AudioClip middleAudio; // Audio untuk cutscene kedua hingga sebelum terakhir
    public AudioClip typingAudio; // Audio untuk efek ketikan
    
    [Header("Pengaturan")]
    public float fadeSpeed = 1f; // Kecepatan fade in / fade out
    public float typingSpeed = 0.05f; // Kecepatan ngetik per huruf
    public string nextSceneName = "home"; // Nama scene selanjutnya

    private int currentIndex = 0;
    private bool isWaitingForInput = false;
    private bool isTyping = false;
    private bool skipTyping = false;


    void Start()
    {
        // Memulai jalannya cutscene
        StartCoroutine(PlayCutscene());
    }

    void Update()
    {
        // Kumpulkan semua tombol skip (termasuk Spasi dan Panah Kanan)
        bool skipPressed = Input.GetKeyDown(KeyCode.Return) || 
                           Input.GetKeyDown(KeyCode.KeypadEnter) || 
                           Input.GetMouseButtonDown(0) || 
                           Input.GetKeyDown(KeyCode.Space) || 
                           Input.GetKeyDown(KeyCode.RightArrow);

        if (skipPressed)
        {
            if (isTyping)
            {
                // Jika sedang mengetik teks, skip efek mengetiknya (langsung tampil semua)
                skipTyping = true;
                
                // Matikan sisa suara ketikan langsung saat teks di-skip
                if (sfxSource != null)
                {
                    sfxSource.Stop();
                }
            }
            else if (isWaitingForInput)
            {
                // Jika teks sudah selesai dan sedang menunggu, lanjut ke gambar berikutnya
                isWaitingForInput = false;
            }
        }
    }

    IEnumerator PlayCutscene()
    {
        for (currentIndex = 0; currentIndex < cutsceneSprites.Length; currentIndex++)
        {
            // Tentukan kecepatan fade (2x lebih lambat untuk gambar pertama dan terakhir)
            float currentFadeSpeed = fadeSpeed;
            if (currentIndex == 0 || currentIndex == cutsceneSprites.Length - 1)
            {
                currentFadeSpeed = fadeSpeed * 2f;
            }

            // --- PENGATURAN AUDIO BGM ---
            if (bgmSource != null)
            {
                AudioClip targetBGM = (currentIndex == 0 || currentIndex == cutsceneSprites.Length - 1) ? firstLastAudio : middleAudio;
                
                // Jika lagu target berbeda dengan lagu yang sedang berputar, ganti dan putar
                if (targetBGM != null && bgmSource.clip != targetBGM)
                {
                    bgmSource.clip = targetBGM;
                    bgmSource.Play();
                }
            }
            // ----------------------------

            // 1. Ganti gambar cutscene & kosongkan teks awal
            cutsceneImage.sprite = cutsceneSprites[currentIndex];
            dialogueText.text = ""; 
            
            // 2. Transisi Fade In (Dari gelap/transparan ke terang)
            yield return StartCoroutine(FadeImage(0f, 1f, currentFadeSpeed));
            
            // 3. Memulai Efek Mengetik Teks (Typewriter)
            if (currentIndex < cutsceneTexts.Length)
            {
                yield return StartCoroutine(TypeText(cutsceneTexts[currentIndex]));
            }

            // 4. Tunggu player klik layar untuk lanjut ke scene berikutnya
            isWaitingForInput = true;
            yield return new WaitUntil(() => !isWaitingForInput);

            // 5. Transisi Fade Out
            if (currentIndex < cutsceneSprites.Length - 1)
            {
                // Fade out normal untuk gambar di tengah-tengah
                yield return StartCoroutine(FadeImage(1f, 0f, currentFadeSpeed));
            }
            else
            {
                // Fade out untuk gambar terakhir sebelum pindah scene
                // (Mengecilkan volume BGM secara perlahan juga bisa dilakukan di sini jika mau)
                yield return StartCoroutine(FadeImage(1f, 0f, currentFadeSpeed));
            }
        }

        // Jika semua gambar dan teks sudah habis, pindah ke scene utama (home)
        if (SceneTransitionManager.instance != null)
        {
            SceneTransitionManager.instance.LoadScene(nextSceneName);
        }
        else
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }

    // Fungsi untuk membuat efek Fade In / Fade Out pada gambar dan teks
    IEnumerator FadeImage(float startAlpha, float endAlpha, float duration)
    {
        float elapsedTime = 0f;
        Color imgColor = cutsceneImage.color;
        Color textColor = dialogueText.color;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
            
            // Terapkan alpha ke gambar
            imgColor.a = newAlpha;
            cutsceneImage.color = imgColor;
            
            // Terapkan alpha ke teks
            if (dialogueText != null)
            {
                textColor.a = newAlpha;
                dialogueText.color = textColor;
            }
            
            yield return null;
        }
        
        // Pastikan alpha tepat di akhir
        imgColor.a = endAlpha;
        cutsceneImage.color = imgColor;
        
        if (dialogueText != null)
        {
            textColor.a = endAlpha;
            dialogueText.color = textColor;
        }
    }

    // Fungsi untuk membuat efek teks diketik satu-satu
    IEnumerator TypeText(string textToType)
    {
        // Bersihkan spasi kosong atau enter ('enter/newline') berlebih di bagian paling akhir teks
        textToType = textToType.TrimEnd();

        dialogueText.text = "";
        isTyping = true;
        skipTyping = false;
        
        Debug.Log("MULAI NGETIK: " + textToType.Length + " huruf.");

        // Cek apakah cutscene ini BUKAN yang pertama dan BUKAN yang terakhir (hanya cutscene tengah)
        bool isMiddleCutscene = (currentIndex > 0 && currentIndex < cutsceneSprites.Length - 1);
        
        // Mulai mainkan suara ketikan SEBELUM mulai ngetik, agar suaranya tidak bertumpuk
        if (isMiddleCutscene && sfxSource != null && typingAudio != null)
        {
            sfxSource.clip = typingAudio;
            sfxSource.loop = true; // Bikin suaranya mengulang terus
            sfxSource.Play(); // Mulai putar sekali
        }

        // Loop untuk memunculkan huruf satu persatu
        foreach (char letter in textToType.ToCharArray())
        {
            // Jika pemain menekan tombol skip saat sedang ngetik
            if (skipTyping)
            {
                dialogueText.text = textToType; // Tampilkan teks penuh secara instan
                break; // Keluar dari loop ngetik
            }

            dialogueText.text += letter;
            
            yield return new WaitForSeconds(typingSpeed); // Jeda tiap huruf
        }

        Debug.Log("SELESAI NGETIK! Mematikan Audio...");

        // Hentikan suara efek ketikan ketika sudah selesai ngetik (atau di-skip)
        if (sfxSource != null)
        {
            sfxSource.Stop();
        }

        isTyping = false;
        skipTyping = false;
    }
}
