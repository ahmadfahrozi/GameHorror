using UnityEngine;
using System.Collections;

public class PlayerAttackPisau : MonoBehaviour
{
    [Header("Pengaturan Serangan")]
    public float attackRange = 1.5f; // Jarak jangkauan pisau (lebih pendek dari senter)
    public float attackRadius = 0.8f; // Lebar tebasan pisau
    public int damage = 20; // Damage pisau (bisa disesuaikan)
    public float attackCooldown = 0.8f; // Waktu jeda antar tebasan
    
    [Header("Audio (Opsional)")]
    public AudioSource attackAudioSource;
    public AudioClip swingSound;

    private GerakPlayer gerakPlayer;
    private Animator anim;
    private float nextAttackTime = 0f;

    void Start()
    {
        gerakPlayer = GetComponent<GerakPlayer>();
        anim = GetComponent<Animator>();
    }

    private bool isAttackingFlag = false;

    void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.isGameActive)
        {
            if (isAttackingFlag)
            {
                isAttackingFlag = false;
                anim.SetBool("IsAttacking", false);
            }
            return;
        }

        bool isHoldingAttack = Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space);

        if (isHoldingAttack)
        {
            if (Time.time >= nextAttackTime)
            {
                // Eksekusi damage
                PerformDamage();
                nextAttackTime = Time.time + attackCooldown;
            }
            
            // Selama ditahan, pastikan animator selalu di state Attack
            if (!isAttackingFlag)
            {
                isAttackingFlag = true;
                anim.SetBool("IsAttacking", true);
            }
        }
        else
        {
            // Jika dilepas, matikan animasi HANYA jika waktu serang terakhir (cooldown) sudah selesai
            if (isAttackingFlag && Time.time >= nextAttackTime)
            {
                isAttackingFlag = false;
                anim.SetBool("IsAttacking", false);
            }
        }
    }

    private void PerformDamage()
    {
        // Mainkan suara tebasan
        if (attackAudioSource != null && swingSound != null)
        {
            attackAudioSource.PlayOneShot(swingSound);
        }

        // Deteksi Musuh & Beri Damage
        Vector2 lastDir = gerakPlayer.GetLastDirection();
        Vector2 attackCenter = (Vector2)transform.position + (lastDir * (attackRange * 0.5f));
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
            Debug.Log("Pisau mengenai Tuyul!");
        }

        // Coba deteksi The Shade
        ShadeAI shade = col.GetComponent<ShadeAI>();
        if (shade != null)
        {
            shade.TakeDamage(damage);
            Debug.Log("Pisau mengenai The Shade!");
        }

        // Coba deteksi The Watcher
        WatcherAI watcher = col.GetComponent<WatcherAI>();
        if (watcher != null)
        {
            watcher.TakeDamage(damage);
            Debug.Log("Pisau mengenai The Watcher!");
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

    // Untuk membantu melihat area serangan Pisau di layar Editor Unity (Garis Kuning)
    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying && gerakPlayer != null)
        {
            Vector2 dir = gerakPlayer.GetLastDirection();
            Vector2 attackCenter = (Vector2)transform.position + (dir * (attackRange * 0.5f));
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(attackCenter, attackRadius);
        }
    }
}
