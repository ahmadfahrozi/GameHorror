using UnityEngine;

public class AttackRange : MonoBehaviour
{
    private TuyulPatrol tuyul;

    private void Start()
    {
        tuyul = GetComponentInParent<TuyulPatrol>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("KENA : " + other.name);

        if(other.CompareTag("Player"))
        {
            Debug.Log("PLAYER MASUK RANGE");

            tuyul.StartAttack(other.transform);
        }
    }
}