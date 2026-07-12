using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class IntroLevel : MonoBehaviour
{
    [Header("Komponen UI")]
    [Tooltip("Masukkan Object Panel atau Canvas yang menampung image ini (yang kamu nonaktifkan)")]
    public GameObject introPanel;
    public Image introImage;
    
    [Header("Data Intro")]
    [Tooltip("Masukkan gambar-gambar cutscene ke sini")]
    public Sprite[] introSprites; 
    
    [Header("Pengaturan")]
    public float fadeSpeed = 1f; // Kecepatan efek fade
    [Tooltip("Waktu tunggu (detik) sebelum gambar pertama muncul")]
    public float delaySebelumMulai = 1.5f;

    private int currentIndex = 0;
    private bool isWaitingForInput = false;

    void Start()
    {
        // Otomatis nyalakan panel saat game dimulai
        if (introPanel != null)
        {
            introPanel.SetActive(true);
        }

        if (introImage != null)
        {
            // Pastikan gambar transparan penuh di awal agar tidak bocor
            Color c = introImage.color;
            c.a = 0f;
            introImage.color = c;
        }

        StartCoroutine(PlayIntro());
    }

    void Update()
    {
        // Tombol untuk lanjut
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || 
            Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.RightArrow) || 
            Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (isWaitingForInput)
            {
                isWaitingForInput = false; 
            }
        }
    }

    IEnumerator PlayIntro()
    {
        if (delaySebelumMulai > 0f)
        {
            yield return new WaitForSeconds(delaySebelumMulai);
        }

        for (currentIndex = 0; currentIndex < introSprites.Length; currentIndex++)
        {
            // Ganti gambar
            introImage.sprite = introSprites[currentIndex];
            
            // Efek Fade In (Gelap ke Terang)
            yield return StartCoroutine(FadeImage(0f, 1f, fadeSpeed));
            
            // Tunggu player klik untuk lanjut
            isWaitingForInput = true;
            yield return new WaitUntil(() => !isWaitingForInput);

            // Efek Fade Out (Terang ke Gelap)
            yield return StartCoroutine(FadeImage(1f, 0f, fadeSpeed));
        }

        // Intro selesai! Matikan panel agar tidak menutupi layar
        if (introPanel != null)
        {
            introPanel.SetActive(false);
        }
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.MulaiGameSetelahIntro();
        }
    }

    // Fungsi untuk memunculkan gambar dengan gaya Fade Alpha (Transparan ke Nyata)
    IEnumerator FadeImage(float startAlpha, float endAlpha, float duration)
    {
        float elapsedTime = 0f;
        Color imgColor = introImage.color;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
            
            imgColor.a = newAlpha;
            introImage.color = imgColor;
            
            yield return null;
        }
        
        imgColor.a = endAlpha;
        introImage.color = imgColor;
    }
}
