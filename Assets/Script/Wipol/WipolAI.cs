using UnityEngine;
using System.Collections;

public class WipolAI : MonoBehaviour
{
    public Transform[] points;
    public float patrolSpeed = 2.5f;
    public float sightRange = 5f; 
    public float attackRange = 1.5f;
    public int damage = 15;
    public float attackCooldown = 1.5f;
    public int health = 30;

    private float currentSpeed;
    private int currentPoint = 0;
    private bool isAttacking = false;
    private float currentCooldown = 0f;
    
    private Animator anim;
    private Rigidbody2D rb;
    private Transform player;
    private Vector2 moveDir;
    private Vector2 lastDirection = Vector2.down;
    private bool goingForward = true;

    [Header("Audio Settings")]
    public AudioSource runSource;
    public AudioClip deathSound; 
    public AudioClip detectSound; 
    public AudioClip attackSound; 

    private bool isChasing = false;

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

        if (player == null) return;

        if (currentCooldown > 0)
        {
            currentCooldown -= Time.deltaTime;
        }

        if (isAttacking) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= attackRange)
        {
            if (!isChasing)
            {
                isChasing = true;
                if (detectSound != null) AudioSource.PlayClipAtPoint(detectSound, transform.position);
            }

            moveDir = Vector2.zero;

            if (currentCooldown <= 0f)
            {
                StartCoroutine(AttackRoutine());
            }
            else
            {
                Vector2 dir = (player.position - transform.position).normalized;
                lastDirection = dir;
                anim.SetFloat("Horizontal", dir.x * 1f); // Idle
                anim.SetFloat("Vertical", dir.y * 1f);
            }
        }
        else if (distanceToPlayer <= sightRange)
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
            Patrol();
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

        if (rb != null && !isAttacking)
        {
            rb.linearVelocity = moveDir * currentSpeed;
        }
    }

    void Chase()
    {
        currentSpeed = patrolSpeed * 1.2f; 
        moveDir = ((Vector2)player.position - (Vector2)transform.position).normalized;
    }

    void Patrol()
    {
        currentSpeed = patrolSpeed;

        if (points == null || points.Length == 0)
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
            anim.SetFloat("Horizontal", moveDir.x * 2f); // Walk
            anim.SetFloat("Vertical", moveDir.y * 2f);
        }
        else if (!isAttacking && currentCooldown <= 0) // Jika diam tapi tidak menyerang (cooldown)
        {
            anim.SetFloat("Horizontal", lastDirection.x * 1f); // Idle
            anim.SetFloat("Vertical", lastDirection.y * 1f);
        }
    }

    void HandleAudio()
    {
        if (runSource != null)
        {
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

    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        currentCooldown = attackCooldown;
        
        if (attackSound != null) AudioSource.PlayClipAtPoint(attackSound, transform.position);

        moveDir = Vector2.zero;
        if(rb != null) rb.linearVelocity = Vector2.zero;

        Vector2 dirToPlayer = (player.position - transform.position).normalized;
        lastDirection = dirToPlayer;
        anim.SetFloat("Horizontal", dirToPlayer.x * 1f);
        anim.SetFloat("Vertical", dirToPlayer.y * 1f);

        anim.SetBool("IsAttacking", true);

        yield return new WaitForSeconds(0.5f); // Jeda animasi memukul

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
        health -= dmg;
        if (health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (deathSound != null)
        {
            AudioSource.PlayClipAtPoint(deathSound, transform.position);
        }
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
