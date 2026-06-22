using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHP = 100;
    public int currentHP;

    void Start()
    {
        currentHP = maxHP;
        Debug.Log("HP Player: " + currentHP);
    }

    public void TakeDamage(int damage)
    {
        currentHP -= damage;

        if (currentHP < 0)
            currentHP = 0;

        Debug.Log("HP Player: " + currentHP);

        if (currentHP <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        currentHP += amount;

        if (currentHP > maxHP)
            currentHP = maxHP;

        Debug.Log("HP Player: " + currentHP);
    }

    void Die()
    {
        Debug.Log("PLAYER MATI");
    }
}