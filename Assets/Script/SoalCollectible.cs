using UnityEngine;

public class SoalCollectible : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Mengecek apakah yang menyentuh item ini adalah Player
        if(other.CompareTag("Player"))
        {
            Debug.Log("Player mengambil Item Soal");

            // Melapor ke GameManager bahwa Soal sudah diambil
            GameManager.Instance.AmbilSoal();

            // Menghancurkan item ini dari map agar tidak bisa diambil dua kali
            Destroy(gameObject);
        }
    }
}
