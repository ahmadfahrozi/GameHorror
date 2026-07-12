using UnityEngine;
using System.Collections.Generic;

public class QuestPointerUI : MonoBehaviour
{
    [Header("Pengaturan Panah")]
    [Tooltip("Masukkan RectTransform dari Image Panah (UI Canvas) ke sini")]
    public RectTransform pointerArrow; 
    
    [Tooltip("Masukkan objek Player ke sini (otomatis terisi jika kosong)")]
    public Transform player; 

    [Tooltip("Masukkan titik Ruang Tangga (Pintu Keluar) ke sini")]
    public Transform exitTransform;
    
    [Tooltip("Batas jarak panah dari pinggir layar (agar gambar tidak terpotong)")]
    public float edgeMargin = 50f;

    [Tooltip("Jarak minimum untuk menyembunyikan panah (saat player sudah sangat dekat dengan surat)")]
    public float hideDistance = 0.5f;

    [Header("Pengaturan Jarak Maksimal (Baru)")]
    [Tooltip("Centang agar panah selalu menempel di dekat player (orbit)")]
    public bool orbitPlayer = true;
    [Tooltip("Jarak maksimal panah dari player (dalam pixel layar)")]
    public float orbitRadius = 150f;

    private List<Transform> activeTargets = new List<Transform>();
    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;

        // Mencari Player secara otomatis jika belum diisi
        if (player == null)
        {
            GameObject pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) player = pObj.transform;
        }

        // Cari semua soal/surat di scene saat game dimulai
        FindAllSoal();
    }

    public void FindAllSoal()
    {
        activeTargets.Clear();
        // Mengumpulkan semua soal dan surat yang ada di map
        SoalCollectible[] soals = FindObjectsByType<SoalCollectible>(FindObjectsSortMode.None);
        foreach(var s in soals) activeTargets.Add(s.transform);

        SuratCollectible[] surats = FindObjectsByType<SuratCollectible>(FindObjectsSortMode.None);
        foreach(var s in surats) activeTargets.Add(s.transform);
    }

    void Update()
    {
        // Bersihkan list dari target yang sudah hancur (sudah diambil player)
        activeTargets.RemoveAll(item => item == null);

        if (pointerArrow == null || player == null || mainCam == null)
        {
            return;
        }

        // --- CEK STATUS UI & GAME OVER ---
        // Jika UI Surat, Soal terbuka, atau game sudah Game Over, sembunyikan panah
        if (GameManager.Instance != null)
        {
            bool isGameOver = !GameManager.Instance.isGameActive;
            bool isPanelSuratOpen = (GameManager.Instance.panelKodeSurat != null && GameManager.Instance.panelKodeSurat.activeSelf);
            bool isPanelSoalOpen = (GameManager.Instance.panelSoal != null && GameManager.Instance.panelSoal.activeSelf);
            bool isPlayerAtExit = GameManager.Instance.isPlayerAtExit;
            bool isWaitingForPasscode = (GameManager.Instance.isSuratLengkap && !GameManager.Instance.isPasscodeRead);

            if (isGameOver || isPanelSuratOpen || isPanelSoalOpen || isPlayerAtExit || isWaitingForPasscode)
            {
                pointerArrow.gameObject.SetActive(false);
                return; // Berhenti memproses update panah
            }
        }

        Transform targetSurat = null;

        // Jika masih ada soal/surat, arahkan panah ke soal/surat
        if (activeTargets.Count > 0)
        {
            // Urutkan daftar surat berdasarkan abjad namanya (Surat_1, Surat_2, dst.)
            activeTargets.Sort((a, b) => a.name.CompareTo(b.name));
            targetSurat = activeTargets[0];
        }
        // Jika tugas sudah habis (surat terkumpul semua), arahkan ke Tangga
        else if (exitTransform != null)
        {
            targetSurat = exitTransform;
        }
        // Jika tugas habis dan tidak ada Tangga yang diset, sembunyikan panah
        else
        {
            pointerArrow.gameObject.SetActive(false);
            return;
        }

        if (targetSurat != null)
        {
            // Sembunyikan panah jika sudah sangat dekat dengan surat
            float distanceToTarget = Vector2.Distance(player.position, targetSurat.position);
            if (distanceToTarget <= hideDistance)
            {
                pointerArrow.gameObject.SetActive(false);
                return;
            }
            else
            {
                pointerArrow.gameObject.SetActive(true);
            }

            // --- 1. ROTASI PANAH ---
            Vector3 dirToTarget = (targetSurat.position - player.position).normalized;
            float angle = Mathf.Atan2(dirToTarget.y, dirToTarget.x) * Mathf.Rad2Deg;
            pointerArrow.rotation = Quaternion.Euler(0, 0, angle - 90f);

            // --- 2. POSISI PANAH (ANTI-BUG CANVAS SCALER) ---
            pointerArrow.anchorMin = new Vector2(0.5f, 0.5f);
            pointerArrow.anchorMax = new Vector2(0.5f, 0.5f);

            // Dapatkan ukuran asli Canvas
            Canvas parentCanvas = pointerArrow.GetComponentInParent<Canvas>();
            if (parentCanvas == null) return;
            RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();

            // Ubah posisi dunia ke Viewport (0.0 sampai 1.0)
            Vector3 viewportPos = mainCam.WorldToViewportPoint(targetSurat.position);

            // Jika objek di belakang kamera, balik arahnya
            if (viewportPos.z < 0)
            {
                viewportPos.x = 1f - viewportPos.x;
                viewportPos.y = 1f - viewportPos.y;
            }

            // Jadikan (0,0) berada di tengah Viewport (sekarang -0.5 sampai 0.5)
            Vector2 viewportCenter = new Vector2(viewportPos.x - 0.5f, viewportPos.y - 0.5f);

            // Kalikan dengan ukuran Canvas untuk mendapatkan posisi UI yang sebenarnya
            Vector2 canvasPos = new Vector2(viewportCenter.x * canvasRect.sizeDelta.x, viewportCenter.y * canvasRect.sizeDelta.y);

            if (orbitPlayer)
            {
                // Batasi jarak panah agar tidak pernah melampaui orbitRadius dari titik tengah (player)
                if (canvasPos.magnitude > orbitRadius)
                {
                    canvasPos = canvasPos.normalized * orbitRadius;
                }
            }
            else
            {
                // Tentukan batas maksimal panah bergerak di dalam Canvas (Kotak Pinggir Layar)
                float boundsX = (canvasRect.sizeDelta.x / 2f) - edgeMargin;
                float boundsY = (canvasRect.sizeDelta.y / 2f) - edgeMargin;

                // Jika surat berada di luar margin layar, tempelkan ke pinggir
                if (Mathf.Abs(canvasPos.x) > boundsX || Mathf.Abs(canvasPos.y) > boundsY)
                {
                    if (canvasPos.magnitude > 0.001f)
                    {
                        Vector2 dirUI = canvasPos.normalized;
                        
                        float tX = boundsX / Mathf.Max(Mathf.Abs(dirUI.x), 0.0001f);
                        float tY = boundsY / Mathf.Max(Mathf.Abs(dirUI.y), 0.0001f);
                        
                        float t = Mathf.Min(tX, tY);
                        canvasPos = dirUI * t;
                    }
                }
            }

            // Tambahkan Debug posisi
            // Debug.Log("Posisi Panah UI: " + canvasPos);

            // Terapkan posisi akhir ke panah
            pointerArrow.anchoredPosition = canvasPos;
        }
    }
}
