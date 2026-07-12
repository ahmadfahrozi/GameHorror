using UnityEngine;
using System.Collections;

public class LibrarianAI : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float detectionRange = 7f;
    public float attackRange = 1.5f;
    public int damage = 20;
    public float attackCooldown = 1.5f;
    public int health = 35;

    private Transform player;
    private Rigidbody2D rb;
    private Animator anim;
    
    private Vector2 startPosition; 
    private bool isAttacking = false;
    private float currentCooldown = 0f;
    
    public AudioClip deathSound; 
    public AudioClip detectSound; 
    public AudioClip attackSound; 

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

            rb.linearVelocity = Vector2.zero;

            if (currentCooldown <= 0f)
            {
                StartCoroutine(AttackRoutine());
            }
            else
            {
                Vector2 dir = (player.position - transform.position).normalized;
                anim.SetFloat("Horizontal", dir.x * 1f);
                anim.SetFloat("Vertical", dir.y * 1f);
            }
        }
        else if (distanceToPlayer <= detectionRange)
        {
            if (!isChasing)
            {
                isChasing = true;
                if (detectSound != null) AudioSource.PlayClipAtPoint(detectSound, transform.position);
            }
            ChasePlayer();
        }
        else
        {
            if (isChasing)
            {
                isChasing = false;
            }
            ReturnToStart();
        }
    }

    void ReturnToStart()
    {
        float distanceToStart = Vector2.Distance(transform.position, startPosition);
        if (distanceToStart > 0.1f)
        {
            Vector2 dir = (startPosition - (Vector2)transform.position).normalized;
            rb.linearVelocity = dir * moveSpeed;

            anim.SetFloat("Horizontal", dir.x * 2f);
            anim.SetFloat("Vertical", dir.y * 2f);
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
            // Default idle down (seperti spesifikasi: Librarian diam idle down)
            anim.SetFloat("Horizontal", 0);
            anim.SetFloat("Vertical", -1);
        }
    }

    void ChasePlayer()
    {
        Vector2 dir = (player.position - transform.position).normalized;
        rb.linearVelocity = dir * moveSpeed;

        anim.SetFloat("Horizontal", dir.x * 2f); // Walk
        anim.SetFloat("Vertical", dir.y * 2f);
        anim.SetBool("IsAttacking", false);
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        currentCooldown = attackCooldown; 
        
        if (attackSound != null) AudioSource.PlayClipAtPoint(attackSound, transform.position);

        rb.linearVelocity = Vector2.zero;

        Vector2 dirToPlayer = (player.position - transform.position).normalized;
        anim.SetFloat("Horizontal", dirToPlayer.x * 1f);
        anim.SetFloat("Vertical", dirToPlayer.y * 1f);

        anim.SetBool("IsAttacking", true);

        yield return new WaitForSeconds(0.5f); // Animasi serang

        PlayerHealth hp = player.GetComponent<PlayerHealth>();
        if(hp != null)
        {
            hp.TakeDamage(damage);
        }

        GerakPlayer gp = player.GetComponent<GerakPlayer>();
        if(gp != null)
        {
            StartCoroutine(ApplyStun(gp));
        }

        anim.SetBool("IsAttacking", false);
        isAttacking = false;
    }

    IEnumerator ApplyStun(GerakPlayer gp)
    {
        float originalSpeed = gp.speedMultiplier;
        gp.speedMultiplier = 0f; // Stun
        yield return new WaitForSeconds(1f);
        gp.speedMultiplier = originalSpeed; // Kembalikan ke normal (atau status slow sebelumnya)
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
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
