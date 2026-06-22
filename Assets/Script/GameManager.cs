using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int suratTerkumpul = 0;
    public int targetSurat = 3;

    private void Awake()
    {
        Instance = this;
    }

    public void AmbilSurat()
    {
        suratTerkumpul++;

        Debug.Log("Surat : " + suratTerkumpul + "/" + targetSurat);

        if(suratTerkumpul >= targetSurat)
        {
            Debug.Log("SEMUA SURAT TERKUMPUL");
        }
    }
}