using UnityEngine;
using UnityEngine.SceneManagement;

public class NextLevelTrigger : MonoBehaviour
{
    [Tooltip("Nama Scene untuk Lantai selanjutnya, pastikan sama persis dengan yang ada di Build Settings")]
    public string nextSceneName = "Lantai2"; 

    [Header("UI Konfirmasi")]
    [Tooltip("Pesan yang akan muncul di layar saat player berada di tangga")]
    [TextArea(2, 4)]
    public string pesanPindahLantai = "Tekan Enter untuk ke Lantai 2";

    private bool isPlayerAtExit = false;

    private void Update()
    {
        // Cek apakah player sedang berada di area tangga dan syarat surat sudah lengkap
        if (isPlayerAtExit && GameManager.Instance != null && GameManager.Instance.suratTerkumpul >= GameManager.Instance.targetSurat)
        {
            // Jika player menekan tombol Enter
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                // Tekan Enter: Sembunyikan notif lalu pindah scene
                GameManager.Instance.SembunyikanNotifikasi();
                SceneManager.LoadScene(nextSceneName);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Mengecek apakah yang menyentuh tangga adalah Player
        if (collision.CompareTag("Player"))
        {
            isPlayerAtExit = true;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.isPlayerAtExit = true; // Update status di GameManager

                if (GameManager.Instance.suratTerkumpul >= GameManager.Instance.targetSurat)
                {
                    // Surat sudah lengkap dan player sampai di ExitPoint
                    // Munculkan notifikasi pakai sistem baru
                    GameManager.Instance.MunculkanNotifikasi(pesanPindahLantai);

                    // Sembunyikan panel surat jika terbuka
                    if (GameManager.Instance.panelKodeSurat != null && GameManager.Instance.panelKodeSurat.activeSelf)
                    {
                        GameManager.Instance.panelKodeSurat.SetActive(false);
                    }
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        // Jika player menjauh dari tangga, reset status interaksinya
        if (collision.CompareTag("Player"))
        {
            isPlayerAtExit = false;
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.isPlayerAtExit = false;
                
                // Sembunyikan notifikasi saat player menjauh dari pintu/tangga
                GameManager.Instance.SembunyikanNotifikasi();
            }
        }
    }
}
