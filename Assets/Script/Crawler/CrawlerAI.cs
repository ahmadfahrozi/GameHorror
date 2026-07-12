using UnityEngine;
using System.Collections;

public class CrawlerAI : MonoBehaviour
{
    public float patrolSpeed = 3f;
    public float sightRange = 6f; 
    public int health = 25; 

    private float currentSpeed;
    private bool isAttacking = false;
    private bool ignorePlayer = false; 
    private Animator anim;
    private Transform playerTarget;
    
    private Rigidbody2D rb;
    private Vector2 moveDir;
    private Transform player; 
    
    private Vector2 startPosition;
    private Vector2 lastDirection = Vector2.down; // Idle down
    private bool isChasing = false;

    [Header("Audio Settings")]
    public AudioSource crawlSource;
    public AudioClip deathSound; 
    public AudioClip detectSound; 
    public AudioClip attackSound; 

    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        currentSpeed = patrolSpeed;
        startPosition = transform.position;

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
            return;
        }

        if (player != null && !ignorePlayer && Vector2.Distance(transform.position, player.position) <= 1.2f)
        {
            if (!isChasing)
            {
                isChasing = true;
                if (detectSound != null) AudioSource.PlayClipAtPoint(detectSound, transform.position);
            }
            StartCoroutine(AttackPlayer(player));
            return;
        }

        if (player != null && !ignorePlayer && Vector2.Distance(transform.position, player.position) <= sightRange)
        {
            if (!isChasing)
            {
                isChasing = true;
                if (detectSound != null) AudioSource.PlayClipAtPoint(detectSound, transform.position);
            }
            Chase();
        }
        else
        {
            if (isChasing)
            {
                isChasing = false;
            }
            ReturnToStart();
        }

        UpdateAnimator();
        HandleAudio();
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
            rb.linearVelocity = moveDir * currentSpeed;
        }
    }

    void Chase()
    {
        currentSpeed = patrolSpeed * 1.2f; // Lari sedikit lebih cepat saat mengejar
        moveDir = ((Vector2)player.position - (Vector2)transform.position).normalized;
    }

    void ReturnToStart()
    {
        currentSpeed = patrolSpeed;
        float distToStart = Vector2.Distance(transform.position, startPosition);
        
        if (distToStart > 0.1f)
        {
            moveDir = (startPosition - (Vector2)transform.position).normalized;
        }
        else
        {
            moveDir = Vector2.zero;
            // Kembali ke idle down default
            lastDirection = Vector2.down;
        }
    }

    void UpdateAnimator()
    {
        if (moveDir.magnitude > 0.01f)
        {
            lastDirection = moveDir;
            anim.SetFloat("Horizontal", moveDir.x * 2f); // Walk
            anim.SetFloat("Vertical", moveDir.y * 2f);
        }
        else if (!isAttacking)
        {
            anim.SetFloat("Horizontal", lastDirection.x * 1f); // Idle
            anim.SetFloat("Vertical", lastDirection.y * 1f);
        }
    }

    void HandleAudio()
    {
        if (crawlSource != null)
        {
            if (moveDir.magnitude > 0.01f && !isAttacking)
            {
                if (!crawlSource.isPlaying) crawlSource.Play();
            }
            else
            {
                if (crawlSource.isPlaying) crawlSource.Stop();
            }
        }
    }

    IEnumerator AttackPlayer(Transform targetPlayer)
    {
        isAttacking = true;
        playerTarget = targetPlayer;
        anim.SetBool("IsAttacking", true);
        currentSpeed = patrolSpeed * 0.5f;

        GerakPlayer gp = targetPlayer.GetComponent<GerakPlayer>();
        if (gp != null)
        {
            gp.speedMultiplier = 0.65f; // Slow 35% (Tinggal 65%)
        }

        PlayerHealth hp = targetPlayer.GetComponent<PlayerHealth>();

        float timer = 0f;
        float damageTimer = 0f;

        while (timer < 7f) // Nempel selama 7 detik
        {
            float distToPlayer = Vector2.Distance(transform.position, targetPlayer.position);
            
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
                Vector2 faceDir = (targetPlayer.position - transform.position).normalized;
                lastDirection = faceDir;
                anim.SetFloat("Horizontal", faceDir.x * 2f); // Tetap walk saat nempel
                anim.SetFloat("Vertical", faceDir.y * 2f);
            }

            timer += Time.deltaTime;
            damageTimer += Time.deltaTime;

            if (damageTimer >= 0.5f) // Damage tiap 0.5 detik
            {
                if (hp != null)
                {
                    hp.TakeDamage(5); // 5 damage
                }
                if (attackSound != null) AudioSource.PlayClipAtPoint(attackSound, transform.position);
                damageTimer = 0f;
            }

            yield return null;
        }

        if (gp != null)
        {
            gp.speedMultiplier = 1f; 
        }

        currentSpeed = patrolSpeed;
        anim.SetBool("IsAttacking", false);
        anim.SetFloat("Horizontal", lastDirection.x * 1f);
        anim.SetFloat("Vertical", lastDirection.y * 1f);
        
        moveDir = Vector2.zero;
        isAttacking = false;

        StartCoroutine(IgnorePlayerCooldown(5f));
    }

    IEnumerator IgnorePlayerCooldown(float time)
    {
        ignorePlayer = true;
        yield return new WaitForSeconds(time);
        ignorePlayer = false;
    }

    public void TakeDamage(int dmg)
    {
        health -= dmg;

        if (health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (playerTarget != null)
        {
            GerakPlayer gp = playerTarget.GetComponent<GerakPlayer>();
            if (gp != null) gp.speedMultiplier = 1f;
        }
        
        if (deathSound != null)
        {
            AudioSource.PlayClipAtPoint(deathSound, transform.position);
        }

        Destroy(gameObject);
    }

}
