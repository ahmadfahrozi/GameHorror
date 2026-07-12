using UnityEngine;
using System.Collections;

public class TuyulPatrol : MonoBehaviour
{
    public Transform[] points;
    public float patrolSpeed = 3f;
    public float sightRange = 5f; // Jarak tuyul bisa melihat player

    private float currentSpeed;
    private int currentPoint = 0;
    private bool isAttacking = false;
    private bool ignorePlayer = false; // Cooldown agar tuyul bisa kabur
    private Animator anim;
    private Transform playerTarget;
    public int health = 3; // Nyawa tuyul
    private bool goingForward = true;
    private Vector2 lastDirection = Vector2.down;

    private Rigidbody2D rb;
    private Vector2 moveDir;
    private Transform player; // Referensi ke player
    
    [Header("Audio Settings")]
    [Tooltip("Masukkan komponen Audio Source untuk suara Tuyul berlari (Centang Loop di Unity)")]
    public AudioSource runSource;
    public AudioClip deathSound; // Suara saat mati
    public AudioClip detectSound; // Suara saat mendeteksi player
    public AudioClip attackSound; // Suara saat menyerang

    private float speedMultiplier = 1f; // Efek slow dari senter
    private Coroutine slowCoroutine;
    private bool isChasing = false;

    // --- SISTEM JEJAK REKAM (BREADCRUMBS) ---
    private System.Collections.Generic.List<Vector2> breadcrumbs = new System.Collections.Generic.List<Vector2>();

    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        currentSpeed = patrolSpeed;

        GameObject pObj = GameObject.FindGameObjectWithTag("Player");
        if (pObj != null)
        {
            player = pObj.transform;
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            PhysicsMaterial2D slipMat = new PhysicsMaterial2D("SlipMat");
            slipMat.friction = 0f;
            slipMat.bounciness = 0f;
            col.sharedMaterial = slipMat;
        }

        // Mencegah mendorong Player dan sesama Hantu
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
        if (GameManager.Instance != null && !GameManager.Instance.isGameActive) return;

        if (isAttacking)
        {
            DropBreadcrumb(); // Tetap rekam jejak walau sedang menyerang
            return;
        }

        // Jika jarak sangat dekat, langsung serang!
        if (player != null && !ignorePlayer && Vector2.Distance(transform.position, player.position) <= 1.2f)
        {
            if (!isChasing)
            {
                isChasing = true;
                if (detectSound != null) AudioSource.PlayClipAtPoint(detectSound, transform.position);
                if (GameManager.Instance != null) GameManager.Instance.MunculkanLegacyText("Bunuh Tuyul Itu dengan Senter mu");
            }
            StartCoroutine(AttackPlayer(player));
            return;
        }

        // Cek jarak dengan player, jika masuk jarak pandang, KEJAR!
        if (player != null && !ignorePlayer && Vector2.Distance(transform.position, player.position) <= sightRange)
        {
            if (!isChasing)
            {
                isChasing = true;
                if (detectSound != null) AudioSource.PlayClipAtPoint(detectSound, transform.position);
                if (GameManager.Instance != null) GameManager.Instance.MunculkanLegacyText("Bunuh Tuyul Itu dengan Senter mu");
            }
            Chase();
            DropBreadcrumb(); // Tinggalkan jejak saat mengejar
        }
        else
        {
            if (isChasing)
            {
                isChasing = false;
                if (GameManager.Instance != null) GameManager.Instance.SembunyikanLegacyText();
            }
            
            // Jika kehilangan player, cek apakah ada jejak rekam untuk pulang?
            if (breadcrumbs.Count > 0)
            {
                ReturnToPath();
            }
            else
            {
                Patrol(); // Kalau jejak sudah habis, berarti sudah kembali ke rute
            }
        }

        UpdateAnimator();

        // --- LOGIKA AUDIO LARI TUYUL ---
        if (runSource != null)
        {
            // Jika Tuyul sedang bergerak dan tidak dalam mode serang
            if (moveDir.magnitude > 0.01f && !isAttacking)
            {
                if (!runSource.isPlaying) runSource.Play();
            }
            else
            {
                if (runSource.isPlaying) runSource.Stop();
            }
        }
    }

    void FixedUpdate()
    {
        if (GameManager.Instance != null && !GameManager.Instance.isGameActive)
        {
            if (rb != null) rb.linearVelocity = Vector2.zero;
            return;
        }

        if (rb != null)
        {
            rb.linearVelocity = moveDir * currentSpeed * speedMultiplier;
        }
    }

    // --- LOGIKA JEJAK REKAM ---
    void DropBreadcrumb()
    {
        // Taruh titik jejak pertama saat mulai mengejar
        if (breadcrumbs.Count == 0)
        {
            breadcrumbs.Add(transform.position);
        }
        // Taruh titik jejak tambahan setiap kali bergerak menjauh sejauh 1 meter
        else if (Vector2.Distance(transform.position, breadcrumbs[breadcrumbs.Count - 1]) > 1.0f)
        {
            // Batasi maksimal jejak agar memori tidak bengkak
            if (breadcrumbs.Count < 100)
            {
                breadcrumbs.Add(transform.position);
            }
        }
    }

    void ReturnToPath()
    {
        currentSpeed = patrolSpeed;
        
        // Ambil jejak paling terakhir yang ditinggalkan
        Vector2 target = breadcrumbs[breadcrumbs.Count - 1];
        
        moveDir = (target - (Vector2)transform.position).normalized;

        // Jika sudah sampai di titik jejak tersebut, hapus dan lanjut ke jejak sebelumnya
        if (Vector2.Distance(transform.position, target) < 0.4f)
        {
            breadcrumbs.RemoveAt(breadcrumbs.Count - 1);
        }
    }

    void Chase()
    {
        currentSpeed = patrolSpeed * 1.2f; // Lari sedikit lebih cepat saat mengejar
        moveDir = ((Vector2)player.position - (Vector2)transform.position).normalized;
    }

    void Patrol()
    {
        currentSpeed = patrolSpeed;

        if (points.Length == 0)
        {
            moveDir = Vector2.zero;
            return;
        }

        Transform target = points[currentPoint];
        
        moveDir = ((Vector2)target.position - (Vector2)transform.position).normalized;

        if (Vector2.Distance(transform.position, target.position) < 0.2f)
        {
            if (goingForward)
            {
                currentPoint++;

                if (currentPoint >= points.Length - 1)
                {
                    currentPoint = points.Length - 1;
                    goingForward = false;
                }
            }
            else
            {
                currentPoint--;

                if (currentPoint <= 0)
                {
                    currentPoint = 0;
                    goingForward = true;
                }
            }
        }
    }

    void UpdateAnimator()
    {
        if (moveDir.magnitude > 0.01f)
        {
            lastDirection = moveDir;

            anim.SetFloat("Horizontal", moveDir.x * 2f);
            anim.SetFloat("Vertical", moveDir.y * 2f);
        }
    }

    // OnCollisionEnter2D dihapus karena kita pakai sistem jarak (Distance) di Update
    // agar Player tidak terdorong tembok.

    IEnumerator AttackPlayer(Transform targetPlayer)
    {
        isAttacking = true;
        playerTarget = targetPlayer;
        anim.SetBool("IsAttacking", true);
        currentSpeed = patrolSpeed * 0.5f;

        GerakPlayer gp = targetPlayer.GetComponent<GerakPlayer>();
        if (gp != null)
        {
            gp.speedMultiplier = 0.75f;
        }

        PlayerHealth hp = targetPlayer.GetComponent<PlayerHealth>();

        float timer = 0f;
        float damageTimer = 0f;

        while (timer < 7f) // Nempel selama 7 detik
        {
            float distToPlayer = Vector2.Distance(transform.position, targetPlayer.position);
            
            // Berhenti berjalan jika jaraknya kurang dari 1.5 agar tidak saling tumpang tindih di tengah
            if (distToPlayer > 1.5f)
            {
                moveDir = ((Vector2)targetPlayer.position - (Vector2)transform.position).normalized;
                lastDirection = moveDir;
                anim.SetFloat("Horizontal", moveDir.x * 2f);
                anim.SetFloat("Vertical", moveDir.y * 2f);
            }
            else
            {
                moveDir = Vector2.zero;
                // Tetap menghadap player meski berhenti
                Vector2 faceDir = (targetPlayer.position - transform.position).normalized;
                lastDirection = faceDir;
                anim.SetFloat("Horizontal", faceDir.x * 2f);
                anim.SetFloat("Vertical", faceDir.y * 2f);
            }

            timer += Time.deltaTime;
            damageTimer += Time.deltaTime;

            if (damageTimer >= 1f)
            {
                if (hp != null)
                {
                    hp.TakeDamage(3); // 3 damage tiap detik
                }
                if (attackSound != null) AudioSource.PlayClipAtPoint(attackSound, transform.position);
                damageTimer = 0f;
            }

            yield return null;
        }

        if (gp != null)
        {
            gp.speedMultiplier = 1f; // Kembalikan kecepatan normal
        }

        currentSpeed = patrolSpeed;
        anim.SetBool("IsAttacking", false);
        anim.SetFloat("Horizontal", Mathf.Sign(lastDirection.x));
        anim.SetFloat("Vertical", Mathf.Sign(lastDirection.y));
        
        moveDir = Vector2.zero;
        isAttacking = false;

        // Beri waktu cooldown agar tuyul mengabaikan player dan pergi kembali ke jalurnya
        StartCoroutine(IgnorePlayerCooldown(5f));
    }

    IEnumerator IgnorePlayerCooldown(float time)
    {
        ignorePlayer = true;
        yield return new WaitForSeconds(time);
        ignorePlayer = false;
    }

    // Fungsi jika player menyerang balik Tuyul
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
        // Pastikan mengembalikan kecepatan player jika tuyul mati saat sedang nempel
        if (playerTarget != null)
        {
            GerakPlayer gp = playerTarget.GetComponent<GerakPlayer>();
            if (gp != null) gp.speedMultiplier = 1f;
        }
        
        // Putar suara meringis/mati
        if (deathSound != null)
        {
            AudioSource.PlayClipAtPoint(deathSound, transform.position);
        }

        if (GameManager.Instance != null) GameManager.Instance.SembunyikanLegacyText();

        // Hancurkan Tuyul
        Destroy(gameObject);
    }

    public void StartAttack(Transform targetPlayer)
    {
        if(!isAttacking)
        {
            StartCoroutine(AttackPlayer(targetPlayer));
        }
    }

    // OnCollisionEnter2D dihapus karena kita pakai sistem jarak (Distance) di Update
    // agar Player tidak terdorong tembok.
}