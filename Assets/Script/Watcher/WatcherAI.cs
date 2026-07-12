using UnityEngine;
using System.Collections;

public class WatcherAI : MonoBehaviour
{
    public float moveSpeed = 2.5f;
    public float detectionRange = 7f;
    public float attackRange = 2f;
    public int damage = 15;
    public float attackCooldown = 1.5f;
    public int health = 30;

    private Transform player;
    private Rigidbody2D rb;
    private Animator anim;
    
    private Vector2 startPosition; // Untuk kembali ke titik awal
    private bool isAttacking = false;
    private float currentCooldown = 0f;
    
    public AudioClip deathSound; // Suara saat mati
    public AudioClip detectSound; // Suara saat mendeteksi player
    public AudioClip attackSound; // Suara saat menyerang

    private float speedMultiplier = 1f; // Efek slow dari senter
    private Coroutine slowCoroutine;
    private bool isChasing = false;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        startPosition = transform.position;

        Collider2D[] myCols = GetComponents<Collider2D>();
        if (player != null)
        {
            Collider2D[] playerCols = player.GetComponents<Collider2D>();
            foreach (var myCol in myCols)
            {
                foreach (var pCol in playerCols)
                {
                    Physics2D.IgnoreCollision(myCol, pCol);
                }
            }
        }

        Collider2D[] allCols = FindObjectsOfType<Collider2D>();
        foreach (Collider2D otherCol in allCols)
        {
            if (otherCol.gameObject != gameObject && (
                otherCol.GetComponent<TuyulPatrol>() != null ||
                otherCol.GetComponent<ShadeAI>() != null ||
                otherCol.GetComponent<WatcherAI>() != null ||
                otherCol.GetComponent<WipolAI>() != null ||
                otherCol.GetComponent<LibrarianAI>() != null ||
                otherCol.GetComponent<CrawlerAI>() != null ||
                otherCol.GetComponent<HoleAI>() != null))
            {
                foreach (var myCol in myCols)
                {
                    Physics2D.IgnoreCollision(myCol, otherCol);
                }
            }
        }
    }

    void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.isGameActive)
        {
            if (rb != null) rb.linearVelocity = Vector2.zero;
            return;
        }

        if (player == null) return;

        // Kurangi waktu cooldown setiap frame
        if (currentCooldown > 0)
        {
            currentCooldown -= Time.deltaTime;
        }

        // Jika sedang animasi menyerang, diam di tempat
        if (isAttacking) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= attackRange)
        {
            if (!isChasing)
            {
                isChasing = true;
                if (detectSound != null) AudioSource.PlayClipAtPoint(detectSound, transform.position);
                if (GameManager.Instance != null) GameManager.Instance.MunculkanLegacyText("Kamu memasuki WIlayah the Watcher, Pergi atau bunuh dia!");
            }

            rb.linearVelocity = Vector2.zero;

            if (currentCooldown <= 0f)
            {
                StartCoroutine(AttackRoutine());
            }
            else
            {
                // Tetap menghadap player selama masa cooldown
                Vector2 dir = (player.position - transform.position).normalized;
                anim.SetFloat("Horizontal", dir.x);
                anim.SetFloat("Vertical", dir.y);
            }
        }
        else if (distanceToPlayer <= detectionRange)
        {
            if (!isChasing)
            {
                isChasing = true;
                if (detectSound != null) AudioSource.PlayClipAtPoint(detectSound, transform.position);
                if (GameManager.Instance != null) GameManager.Instance.MunculkanLegacyText("Kamu memasuki WIlayah the Watcher, Pergi atau bunuh dia!");
            }
            // Kejar player jika masih di dalam area deteksi
            ChasePlayer();
        }
        else
        {
            if (isChasing)
            {
                isChasing = false;
                if (GameManager.Instance != null) GameManager.Instance.SembunyikanLegacyText();
            }
            // Player di luar area deteksi, kembali ke posisi awal
            ReturnToStart();
        }
    }

    void ReturnToStart()
    {
        float distanceToStart = Vector2.Distance(transform.position, startPosition);
        if (distanceToStart > 0.1f)
        {
            Vector2 dir = (startPosition - (Vector2)transform.position).normalized;
            rb.linearVelocity = dir * moveSpeed * speedMultiplier;

            anim.SetFloat("Horizontal", dir.x);
            anim.SetFloat("Vertical", dir.y);
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    void ChasePlayer()
    {
        Vector2 dir = (player.position - transform.position).normalized;
        rb.linearVelocity = dir * moveSpeed * speedMultiplier;

        anim.SetFloat("Horizontal", dir.x);
        anim.SetFloat("Vertical", dir.y);
        anim.SetBool("IsAttacking", false);
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        currentCooldown = attackCooldown; 
        
        if (attackSound != null) AudioSource.PlayClipAtPoint(attackSound, transform.position);

        rb.linearVelocity = Vector2.zero;

        // Hadap ke arah player sebelum memukul
        Vector2 dirToPlayer = (player.position - transform.position).normalized;
        anim.SetFloat("Horizontal", dirToPlayer.x);
        anim.SetFloat("Vertical", dirToPlayer.y);

        anim.SetBool("IsAttacking", true);

        // Beri waktu tunda 0.5 detik agar animasi pukulan dimainkan
        yield return new WaitForSeconds(0.5f);

        // Karena animasi serang sudah jalan, dipastikan player kena damage
        PlayerHealth hp = player.GetComponent<PlayerHealth>();
        if(hp != null)
        {
            hp.TakeDamage(damage);
        }

        // Selesai memukul, langsung bisa mengejar lagi!
        anim.SetBool("IsAttacking", false);
        isAttacking = false;
    }

    public void TakeDamage(int dmg)
    {
        health -= dmg;

        // Aplikasikan efek slow 50% selama 1 detik
        if (slowCoroutine != null) StopCoroutine(slowCoroutine);
        slowCoroutine = StartCoroutine(SlowEffect());

        if (health <= 0)
        {
            Die();
        }
    }

    IEnumerator SlowEffect()
    {
        speedMultiplier = 0.5f; // Melambat setengah kecepatan
        yield return new WaitForSeconds(1f);
        speedMultiplier = 1f; // Kembali normal
    }

    void Die()
    {
        if (deathSound != null)
        {
            AudioSource.PlayClipAtPoint(deathSound, transform.position);
        }
        if (GameManager.Instance != null) GameManager.Instance.SembunyikanLegacyText();
        // Hancurkan The Watcher dari arena permainan
        Destroy(gameObject);
    }

    // Menggambar batas kuning (deteksi) dan merah (serang) di Unity Editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
