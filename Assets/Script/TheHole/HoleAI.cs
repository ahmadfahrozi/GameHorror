using UnityEngine;
using System.Collections;

public class HoleAI : MonoBehaviour
{
    public float moveSpeed = 2.5f;
    public float detectionRange = 5f;
    public float attackRange = 1.5f;
    public int damage = 10;
    public float attackCooldown = 1.5f;
    public int health = 100; // BOSS HP

    private Transform player;
    private Rigidbody2D rb;
    private Animator anim;
    
    private bool isWaiting = true;
    private bool isSpawning = false;
    private bool isDead = false;
    private bool isAttacking = false;
    private float currentCooldown = 0f;
    
    private Vector2 moveDir;
    private Vector2 lastDirection = Vector2.down;
    private bool hasDetectedPlayer = false;

    [Header("Audio Settings")]
    public AudioSource bossAudioSource;
    public AudioClip spawnSound;
    public AudioClip deathSound; 
    public AudioClip attackSound; 

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        // Default state: Lingkaran doang di BlendTree2 (IsAttacking=true, H=0, V=0)
        anim.SetBool("IsAttacking", true);
        anim.SetFloat("Horizontal", 0);
        anim.SetFloat("Vertical", 0);

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

        if (isDead || player == null) return;

        if (currentCooldown > 0)
        {
            currentCooldown -= Time.deltaTime;
        }

        if (isWaiting)
        {
            float dist = Vector2.Distance(transform.position, player.position);
            if (dist <= detectionRange)
            {
                StartCoroutine(SpawnRoutine());
            }
            return;
        }

        if (isSpawning || isAttacking) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= attackRange)
        {
            moveDir = Vector2.zero;
            
            if (currentCooldown <= 0f)
            {
                StartCoroutine(AttackRoutine());
            }
            else
            {
                Vector2 faceDir = (player.position - transform.position).normalized;
                lastDirection = faceDir;
                anim.SetFloat("Horizontal", faceDir.x * 1f); // Idle
                anim.SetFloat("Vertical", faceDir.y * 1f);
            }
        }
        else
        {
            ChasePlayer();
        }

        UpdateAnimator();
    }

    void FixedUpdate()
    {
        if (GameManager.Instance != null && !GameManager.Instance.isGameActive)
        {
            if (rb != null) rb.linearVelocity = Vector2.zero;
            return;
        }

        if (rb != null && !isWaiting && !isSpawning && !isAttacking && !isDead)
        {
            rb.linearVelocity = moveDir * moveSpeed;
        }
        else if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    void ChasePlayer()
    {
        moveDir = ((Vector2)player.position - (Vector2)transform.position).normalized;
    }

    void UpdateAnimator()
    {
        if (moveDir.magnitude > 0.01f && !isAttacking && !isSpawning && !isWaiting && !isDead)
        {
            lastDirection = moveDir;
            anim.SetFloat("Horizontal", moveDir.x * 2f); // Walk
            anim.SetFloat("Vertical", moveDir.y * 2f);
        }
    }

    IEnumerator SpawnRoutine()
    {
        isWaiting = false;
        isSpawning = true;
        hasDetectedPlayer = true;

        if (spawnSound != null)
        {
            if (bossAudioSource != null) bossAudioSource.PlayOneShot(spawnSound);
            else AudioSource.PlayClipAtPoint(spawnSound, transform.position);
        }

        // Animasi Spawn (IsAttacking=true, H=1, V=1)
        anim.SetBool("IsAttacking", true);
        anim.SetFloat("Horizontal", 1f);
        anim.SetFloat("Vertical", 1f);

        yield return new WaitForSeconds(1.5f); // Waktu animasi spawn selesai

        anim.SetBool("IsAttacking", false);
        isSpawning = false;
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        currentCooldown = attackCooldown;

        if (attackSound != null)
        {
            if (bossAudioSource != null) bossAudioSource.PlayOneShot(attackSound);
            else AudioSource.PlayClipAtPoint(attackSound, transform.position);
        }

        Vector2 dirToPlayer = (player.position - transform.position).normalized;
        lastDirection = dirToPlayer;
        
        anim.SetBool("IsAttacking", true);
        anim.SetFloat("Horizontal", dirToPlayer.x * 1f); // Animasi serang sesuai arah
        anim.SetFloat("Vertical", dirToPlayer.y * 1f);

        yield return new WaitForSeconds(0.5f);

        PlayerHealth hp = player.GetComponent<PlayerHealth>();
        if(hp != null)
        {
            hp.TakeDamage(damage);
        }

        anim.SetBool("IsAttacking", false);
        isAttacking = false;
    }

    public void TakeDamage(int dmg)
    {
        if (isDead) return;

        health -= dmg;

        if (health <= 0)
        {
            StartCoroutine(DieRoutine());
        }
    }

    IEnumerator DieRoutine()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;

        if (deathSound != null)
        {
            if (bossAudioSource != null) bossAudioSource.PlayOneShot(deathSound);
            else AudioSource.PlayClipAtPoint(deathSound, transform.position);
        }

        // Animasi Death (IsAttacking=true, H=-1, V=-1)
        anim.SetBool("IsAttacking", true);
        anim.SetFloat("Horizontal", -1f);
        anim.SetFloat("Vertical", -1f);

        yield return new WaitForSeconds(2f); // Tunggu animasi mati selesai

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
