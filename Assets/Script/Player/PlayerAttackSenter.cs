using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerAttack : MonoBehaviour
{
    [Header("Referensi Senter")]
    public Transform flashlightArea; // Masukkan Empty GameObject senter ke sini
    public GameObject flashlightVisual; // Objek visual cahaya/gambar senternya (untuk dinyalakan pas nyerang)

    [Header("Pengaturan Serangan")]
    public float attackRange = 2f; // Jarak sorotan senter
    public float attackRadius = 1f; // Lebar/radius sorotan
    public int damage = 5;
    public float attackCooldown = 0.5f; // Waktu senter harus menyinari hantu sebelum damage masuk

    private GerakPlayer gerakPlayer;
    private Dictionary<Collider2D, float> enemyExposure = new Dictionary<Collider2D, float>();
    private float nextClickAttackTime = 0f;

    void Start()
    {
        gerakPlayer = GetComponent<GerakPlayer>();
        
        // Sembunyikan senter pada awal mulai
        if (flashlightVisual != null)
        {
            flashlightVisual.SetActive(false);
        }
    }

    void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.isGameActive)
        {
            if (flashlightVisual != null) flashlightVisual.SetActive(false);
            gerakPlayer.attackSpeedMultiplier = 1f;
            enemyExposure.Clear();
            return;
        }

        // 1. Dapatkan arah hadap Player
        Vector2 lastDir = gerakPlayer.GetLastDirection();

        // 2. Putar Senter (FlashlightArea) agar menghadap sesuai arah Player
        if (flashlightArea != null && lastDir != Vector2.zero)
        {
            // Menghitung sudut rotasi berdasarkan arah (X, Y)
            float angle = Mathf.Atan2(lastDir.y, lastDir.x) * Mathf.Rad2Deg;
            
            // Dikurang 90 derajat dengan asumsi gambar Senter aslinya menghadap ke Atas (UP)
            flashlightArea.rotation = Quaternion.Euler(0, 0, angle - 90f);
        }

        // 3. Serangan Klik Cepat (Instant Attack)
        // Menambahkan KeyCode.Space sebagai alternatif jika trackpad laptop menolak klik sambil menekan WASD (Palm Rejection)
        if ((Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)) && Time.time >= nextClickAttackTime)
        {
            AttackInstant(lastDir);
            nextClickAttackTime = Time.time + attackCooldown;
        }

        // 4. Kontrol Senter & Logika Damage "Burn" (Hold Attack)
        if (Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space))
        {
            if (flashlightVisual != null) flashlightVisual.SetActive(true);
            
            // Perlambat player 70% saat menyerang
            gerakPlayer.attackSpeedMultiplier = 0.7f;
            
            // Hitung exposure setiap musuh selama senter ditahan
            AttackContinuous(lastDir);
        }
        else
        {
            if (flashlightVisual != null) flashlightVisual.SetActive(false);
            
            // Kembalikan kecepatan normal
            gerakPlayer.attackSpeedMultiplier = 1f;
            
            // Jika senter dilepas, maka waktu "pembakaran" semua musuh direset ke 0!
            // Artinya kalau player asal klik-klik terus (spam click), musuh tidak akan pernah kena damage
            enemyExposure.Clear();
        }
    }

    void AttackContinuous(Vector2 direction)
    {
        // Hitung titik tengah area serangan
        Vector2 attackCenter = (Vector2)transform.position + (direction * (attackRange * 0.5f));
        
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackCenter, attackRadius);
        List<Collider2D> currentHits = new List<Collider2D>();

        foreach (Collider2D col in hitEnemies)
        {
            // Cek apakah itu adalah musuh
            if (col.GetComponent<TuyulPatrol>() != null || 
                col.GetComponent<ShadeAI>() != null || 
                col.GetComponent<WatcherAI>() != null ||
                col.GetComponent<WipolAI>() != null ||
                col.GetComponent<LibrarianAI>() != null ||
                col.GetComponent<CrawlerAI>() != null ||
                col.GetComponent<HoleAI>() != null)
            {
                currentHits.Add(col);

                // Tambahkan waktu paparan sinar
                if (enemyExposure.ContainsKey(col))
                {
                    enemyExposure[col] += Time.deltaTime;
                }
                else
                {
                    enemyExposure[col] = Time.deltaTime; // Mulai dihitung dari 0
                }

                // JEDA DULU SEBELUM DAMAGE! (sesuai request)
                // Jika sudah disinar terus-menerus selama 0.5 detik, BARU berikan damage
                if (enemyExposure[col] >= attackCooldown)
                {
                    DealDamageTo(col);
                    // Reset ke 0 agar 0.5 detik kemudian baru kena damage lagi
                    enemyExposure[col] = 0f; 
                }
            }
        }

        // Hapus musuh dari catatan jika mereka berhasil keluar dari sorotan cahaya
        // (Waktu "pembakaran" kereset)
        List<Collider2D> keys = new List<Collider2D>(enemyExposure.Keys);
        foreach (Collider2D key in keys)
        {
            if (!currentHits.Contains(key))
            {
                enemyExposure.Remove(key);
            }
        }
    }

    void AttackInstant(Vector2 direction)
    {
        Vector2 attackCenter = (Vector2)transform.position + (direction * (attackRange * 0.5f));
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackCenter, attackRadius);

        foreach (Collider2D col in hitEnemies)
        {
            if (col.GetComponent<TuyulPatrol>() != null || 
                col.GetComponent<ShadeAI>() != null || 
                col.GetComponent<WatcherAI>() != null ||
                col.GetComponent<WipolAI>() != null ||
                col.GetComponent<LibrarianAI>() != null ||
                col.GetComponent<CrawlerAI>() != null ||
                col.GetComponent<HoleAI>() != null)
            {
                DealDamageTo(col);
                
                // Set exposure jadi 0 agar damage dari 'Hold' tidak dobel di waktu yang sama
                enemyExposure[col] = 0f;
            }
        }
    }

    void DealDamageTo(Collider2D col)
        {
            // Coba deteksi Tuyul
            TuyulPatrol tuyul = col.GetComponent<TuyulPatrol>();
            if (tuyul != null)
            {
                tuyul.TakeDamage(damage);
                Debug.Log("Senter mengenai Tuyul!");
            }

            // Coba deteksi The Shade
            ShadeAI shade = col.GetComponent<ShadeAI>();
            if (shade != null)
            {
                shade.TakeDamage(damage);
                Debug.Log("Senter mengenai The Shade!");
            }

            // Coba deteksi The Watcher
            WatcherAI watcher = col.GetComponent<WatcherAI>();
            if (watcher != null)
            {
                watcher.TakeDamage(damage);
                Debug.Log("Senter mengenai The Watcher!");
            }

            WipolAI wipol = col.GetComponent<WipolAI>();
            if (wipol != null) wipol.TakeDamage(damage);

            LibrarianAI librarian = col.GetComponent<LibrarianAI>();
            if (librarian != null) librarian.TakeDamage(damage);

            CrawlerAI crawler = col.GetComponent<CrawlerAI>();
            if (crawler != null) crawler.TakeDamage(damage);

            HoleAI hole = col.GetComponent<HoleAI>();
            if (hole != null) hole.TakeDamage(damage);
        }

    // Untuk membantu melihat area serangan Senter di layar Editor Unity (Garis Merah)
    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying && gerakPlayer != null)
        {
            Vector2 dir = gerakPlayer.GetLastDirection();
            Vector2 attackCenter = (Vector2)transform.position + (dir * (attackRange * 0.5f));
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackCenter, attackRadius);
        }
    }
}
