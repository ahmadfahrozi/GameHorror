using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    // Fungsi ini dipanggil ketika tombol "Mulai" / "Play" ditekan
    public void PlayGame()
    {
        // Pastikan scene "Lantai1" sudah ditambahkan di Build Settings
        SceneManager.LoadScene("Lantai1");
    }

    // Fungsi tambahan jika Anda ingin membuat tombol "Keluar" / "Exit"
    public void QuitGame()
    {
        Debug.Log("Keluar dari game!");
        Application.Quit();
    }
}
