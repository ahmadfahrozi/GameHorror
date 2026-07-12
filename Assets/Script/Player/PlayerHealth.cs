using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    public int maxHP = 100;
    public int currentHP;

    [Header("Health UI")]
    public Text healthText;

    [Header("Efek Darah")]
    public GameObject darah1;
    public GameObject darah2;

    [Header("Efek Suara")]
    public AudioSource audioSource;
    public AudioClip damageSound;

    private float healTimer = 0f;
    private Coroutine soundCoroutine;

    void Start()
    {
        currentHP = maxHP;
        UpdateHealthUI();

        // Cari otomatis jika belum diassign di Inspector
        if (darah1 == null)
        {
            Transform d1 = transform.Find("Darah1");
            if (d1 != null) darah1 = d1.gameObject;
        }
        if (darah2 == null)
        {
            Transform d2 = transform.Find("Darah2");
            if (d2 != null) darah2 = d2.gameObject;
        }

        // Pastikan tersembunyi di awal permainan
        if (darah1 != null) darah1.SetActive(false);
        if (darah2 != null) darah2.SetActive(false);

        // Pastikan ada AudioSource
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    void Update()
    {
        // Auto heal 1 HP setiap 2 detik jika nyawa belum penuh dan belum mati
        if (currentHP > 0 && currentHP < maxHP)
        {
            healTimer += Time.deltaTime;
            if (healTimer >= 2f)
            {
                Heal(1);
                healTimer = 0f;
            }
        }
    }

    public void TakeDamage(int damage)
    {
        // Cegah error jika player sudah mati atau object-nya dinonaktifkan
        if (!gameObject.activeInHierarchy || currentHP <= 0) return;

        currentHP -= damage;

        // Logika Camera Shake berdasarkan besaran damage
        if (Camera.main != null)
        {
            CameraShake camShake = Camera.main.GetComponent<CameraShake>();
            if (camShake != null)
            {
                if (damage < 10)
                {
                    camShake.TriggerShake(0.15f, 0.1f);  // Getaran sangat ringan
                }
                else if (damage < 20)
                {
                    camShake.TriggerShake(0.2f, 0.25f);  // Getaran ringan
                }
                else if (damage < 40)
                {
                    camShake.TriggerShake(0.3f, 0.5f);   // Getaran sedang
                }
                else // Damage >= 40
                {
                    camShake.TriggerShake(0.4f, 1.0f);   // Getaran hebat
                }
            }
        }

        // Logika memunculkan efek darah dan suara
        if (damage >= 20)
        {
            if (darah1 != null)
            {
                darah1.SetActive(true);
                StopCoroutine("SembunyikanDarah1");
                StartCoroutine("SembunyikanDarah1");
            }
            if (darah2 != null)
            {
                darah2.SetActive(true);
                StopCoroutine("SembunyikanDarah2");
                StartCoroutine("SembunyikanDarah2");
            }

            if (soundCoroutine != null) StopCoroutine(soundCoroutine);
            soundCoroutine = StartCoroutine(MainkanSuaraLukaTiapDetik(5f));
        }
        else if (damage >= 5)
        {
            if (darah1 != null)
            {
                darah1.SetActive(true);
                StopCoroutine("SembunyikanDarah1");
                StartCoroutine("SembunyikanDarah1");
            }

            if (soundCoroutine != null) StopCoroutine(soundCoroutine);
            soundCoroutine = StartCoroutine(MainkanSuaraLukaTiapDetik(3f));
        }

        if (currentHP < 0)
            currentHP = 0;

        UpdateHealthUI();

        if (currentHP <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        currentHP += amount;

        if (currentHP > maxHP)
            currentHP = maxHP;

        UpdateHealthUI();
    }

    void UpdateHealthUI()
    {
        if (healthText != null)
        {
            healthText.text = string.Format("{0:00}", currentHP);
        }
        else
        {
            Debug.Log(currentHP);
        }
    }

    void Die()
    {
        Debug.Log("PLAYER MATI - GAME OVER");
        
        // Sembunyikan teks HP dan background-nya
        if (healthText != null)
        {
            // Cek apakah teks berada di dalam sebuah background gambar (Image)
            if (healthText.transform.parent != null && healthText.transform.parent.GetComponent<Image>() != null)
            {
                healthText.transform.parent.gameObject.SetActive(false);
            }
            else
            {
                healthText.gameObject.SetActive(false);
            }
        }

        // Memanggil fungsi GameOver di GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }

        // Matikan objek player agar tidak bisa diserang lagi atau bergerak
        gameObject.SetActive(false);
    }

    private IEnumerator SembunyikanDarah1()
    {
        yield return new WaitForSeconds(3f);
        if (darah1 != null) darah1.SetActive(false);
    }

    private IEnumerator SembunyikanDarah2()
    {
        yield return new WaitForSeconds(5f);
        if (darah2 != null) darah2.SetActive(false);
    }

    private IEnumerator MainkanSuaraLukaTiapDetik(float durasi)
    {
        float timer = 0f;
        while (timer < durasi)
        {
            if (audioSource != null && damageSound != null)
            {
                audioSource.PlayOneShot(damageSound);
            }
            yield return new WaitForSeconds(1f);
            timer += 1f;
        }
    }
}