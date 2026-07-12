using UnityEngine;

public class SuratCollectible : MonoBehaviour
{
    private bool isPlayerNearby = false;

    private void Update()
    {
        // Mengecek apakah player ada di dekat item dan menekan tombol E
        if (isPlayerNearby && Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("Player ambil surat dengan tombol E");

            GameManager.Instance.AmbilSurat();
            GameManager.Instance.MunculkanLegacyTextSementara($"Kamu telah mengumpulkan {GameManager.Instance.suratTerkumpul} surat", 3f);

            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player mendekat ke surat. Tekan 'E' untuk ambil.");
            isPlayerNearby = true;
            if (GameManager.Instance != null)
            {
                int nextSurat = GameManager.Instance.suratTerkumpul + 1;
                GameManager.Instance.MunculkanLegacyText($"Kamu menemukan surat ke-{nextSurat}, ambil dengan tombol E atau klik kanan");
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player menjauh dari surat.");
            isPlayerNearby = false;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SembunyikanLegacyText();
            }
        }
    }
}