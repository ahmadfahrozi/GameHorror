using UnityEngine;
using System.Collections;

public class ShadeAI : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float chaseRange = 5f; // Jarak The Shade mulai mengejar (Sekarang jadi Detection Range)
    public float attackRange = 2.5f; 
    public int damage = 25;
    public float attackCooldown = 3f;
    public int health = 35;

    private Transform player;
    private Rigidbody2D rb;
    private Animator anim;
    private bool isAttacking;
    private float currentCooldown = 0f;
    private bool hasDetectedPlayer = false; // Kejar terus hingga mati
    
    public AudioClip deathSound; // Suara saat mati
    public AudioClip detectSound; // Suara saat mendeteksi player
    public AudioClip attackSound; // Suara saat menyerang

    private float speedMultiplier = 1f; // Efek slow dari senter
    private Coroutine slowCoroutine;

    void Start()
    {
        // Mencari game object dengan tag "Player"
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        // Tembus tembok: Jadikan Collider-nya Trigger
        Collider2D[] cols = GetComponents<Collider2D>();
        foreach(var c in cols)
        {
            c.isTrigger = true;
        }

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

        if(player == null) return;

        // Kurangi waktu cooldown setiap frame
        if (currentCooldown > 0)
        {
            currentCooldown -= Time.deltaTime;
        }

        // Jika The Shade sedang animasi menyerang (0.5 dtk), dia tidak boleh berjalan
        if (isAttacking) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (!hasDetectedPlayer)
        {
            if (distanceToPlayer <= chaseRange) // chaseRange bertindak sebagai detection range
            {
                hasDetectedPlayer = true;
                if (detectSound != null) AudioSource.PlayClipAtPoint(detectSound, transform.position);
                if (GameManager.Instance != null) GameManager.Instance.MunculkanLegacyText("gawat!, kamu dikejar hantu The Shade1, Bunuh dia atau dia yang akan Bunuh kamu");
            }
            else
            {
                // Diam
                rb.linearVelocity = Vector2.zero;
                return;
            }
        }

        if(distanceToPlayer <= attackRange)
        {
            // Berhenti agar tidak menabrak ke titik tengah player
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
        else
        {
            // Kejar selamanya
            ChasePlayer();
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
        currentCooldown = attackCooldown; // Mulai hitungan cooldown (2 detik)
        
        if (attackSound != null) AudioSource.PlayClipAtPoint(attackSound, transform.position);

        // Memastikan The Shade berhenti saat memukul
        rb.linearVelocity = Vector2.zero;

        // Hadap ke arah player sebelum memukul
        Vector2 dirToPlayer = (player.position - transform.position).normalized;
        anim.SetFloat("Horizontal", dirToPlayer.x);
        anim.SetFloat("Vertical", dirToPlayer.y);

        anim.SetBool("IsAttacking", true);
        Debug.Log("The Shade mulai menyerang! (Jeda animasi 0.5 detik)");

        // Beri waktu tunda sedikit (0.5 detik) agar animasi pukulan dimainkan
        yield return new WaitForSeconds(0.5f);

        // Cek lagi dihilangkan sesuai permintaan: Jika animasi serang sudah jalan, pasti kena!
        Debug.Log("Serangan The Shade KENA Player!");
        PlayerHealth hp = player.GetComponent<PlayerHealth>();
        if(hp != null)
        {
            hp.TakeDamage(damage);
        }

        // Selesai memukul, The Shade langsung bisa bergerak/mengejar lagi!
        anim.SetBool("IsAttacking", false);
        isAttacking = false;
        Debug.Log("Animasi memukul selesai. The Shade bisa bergerak sambil menunggu cooldown serangan.");
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
        Destroy(gameObject);
    }

    // Menggambar garis panduan di Editor (untuk melihat jarak pandang/serang)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
