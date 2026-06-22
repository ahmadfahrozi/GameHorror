using UnityEngine;

public class TuyulDamage : MonoBehaviour
{
    public int damage = 5;

    private Animator anim;
    void Start()
    {
        anim = GetComponent<Animator>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.CompareTag("Player"))
        {
            anim.SetBool("IsAttacking", true);
        }
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        if(other.CompareTag("Player"))
        {
            anim.SetBool("IsAttacking", false);
        }
    }
}