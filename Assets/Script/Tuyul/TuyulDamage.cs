using UnityEngine;

public class TuyulDamage : MonoBehaviour
{
    public int damage = 5;

    private Animator anim;
    void Start()
    {
        anim = GetComponent<Animator>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.CompareTag("Player"))
        {
            anim.SetBool("IsAttacking", true);
        }
    }
    private void OnCollisionExit2D(Collision2D collision)
    {
        if(collision.gameObject.CompareTag("Player"))
        {
            anim.SetBool("IsAttacking", false);
        }
    }
}