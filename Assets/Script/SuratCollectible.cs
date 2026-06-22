using UnityEngine;

public class SuratCollectible : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Kena Trigger: " + other.name);

        if(other.CompareTag("Player"))
        {
            Debug.Log("Player ambil surat");

            GameManager.Instance.AmbilSurat();

            Destroy(gameObject);
        }
}
}