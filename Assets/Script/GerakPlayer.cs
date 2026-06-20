using UnityEngine;

public class GerakPlayer : MonoBehaviour
{
    public float speed = 5f;

    private Rigidbody2D rb;
    private Animator anim;

    private Vector2 moveInput;
    private Vector2 lastDirection = Vector2.down;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        moveInput = new Vector2(h, v).normalized;

        if(moveInput != Vector2.zero)
        {
            lastDirection = moveInput;

            anim.SetFloat("Horizontal", h * 2);
            anim.SetFloat("Vertical", v * 2);
            anim.SetFloat("Speed", 1);
        }
        else
        {
            anim.SetFloat("Horizontal", Mathf.Sign(lastDirection.x));
            anim.SetFloat("Vertical", Mathf.Sign(lastDirection.y));
            anim.SetFloat("Speed", 0);
        }
    }

    void FixedUpdate()
    {
        rb.linearVelocity = moveInput * speed;
    }
}