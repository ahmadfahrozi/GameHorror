using UnityEngine;
using System.Collections;

public class TuyulPatrol : MonoBehaviour
{
    public Transform[] points;

    public float patrolSpeed = 3f;

    private float currentSpeed;

    private int currentPoint = 0;

    private bool isAttacking = false;

    private Animator anim;

    private Transform playerTarget;

    private bool goingForward = true;

    private Vector2 lastDirection = Vector2.down;

    void Start()
    {
        anim = GetComponent<Animator>();

        currentSpeed = patrolSpeed;
    }

    void Update()
    {
        if (isAttacking)
            return;

        Patrol();

        UpdateAnimator();
    }

    void Patrol()
    {
        if (points.Length == 0)
            return;

        Transform target = points[currentPoint];

        Vector2 dir =
            (target.position - transform.position).normalized;

        transform.position = Vector2.MoveTowards(
            transform.position,
            target.position,
            currentSpeed * Time.deltaTime
        );

        if (Vector2.Distance(transform.position, target.position) < 0.1f)
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
        if (points.Length == 0)
            return;

        Transform target = points[currentPoint];

        Vector2 dir =
            (target.position - transform.position).normalized;

        if (dir.magnitude > 0.01f)
        {
            lastDirection = dir;

            anim.SetFloat("Horizontal", dir.x * 2f);
            anim.SetFloat("Vertical", dir.y * 2f);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isAttacking)
        {
            StartCoroutine(AttackPlayer(other.transform));
        }
    }

    IEnumerator AttackPlayer(Transform player)
    {
        isAttacking = true;

        playerTarget = player;

        anim.SetBool("IsAttacking", true);

        currentSpeed = patrolSpeed * 0.5f;

        GerakPlayer gp = player.GetComponent<GerakPlayer>();

        if (gp != null)
        {
            gp.speedMultiplier = 0.75f;
        }

        PlayerHealth hp = player.GetComponent<PlayerHealth>();

        float timer = 0f;
        float damageTimer = 0f;

        while (timer < 3f)
        {
            Vector2 dir =
                (player.position - transform.position).normalized;

            lastDirection = dir;

            anim.SetFloat("Horizontal", dir.x * 2f);
            anim.SetFloat("Vertical", dir.y * 2f);

            transform.position = Vector2.MoveTowards(
                transform.position,
                player.position,
                currentSpeed * Time.deltaTime
            );

            timer += Time.deltaTime;
            damageTimer += Time.deltaTime;

            if (damageTimer >= 1f)
            {
                if (hp != null)
                {
                    hp.TakeDamage(5);
                }

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

        anim.SetFloat("Horizontal", Mathf.Sign(lastDirection.x));
        anim.SetFloat("Vertical", Mathf.Sign(lastDirection.y));

        isAttacking = false;
    }
    public void StartAttack(Transform player)
    {
        if(!isAttacking)
        {
            StartCoroutine(AttackPlayer(player));
        }
    }
}