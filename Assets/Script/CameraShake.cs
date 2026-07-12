using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    private Coroutine currentShakeCoroutine;
    
    // Variabel ini akan dibaca oleh CameraFollow untuk ditambahkan ke posisi kamera
    [HideInInspector]
    public Vector3 shakeOffset; 

    public void TriggerShake(float duration, float magnitude)
    {
        if (currentShakeCoroutine != null)
        {
            StopCoroutine(currentShakeCoroutine);
            shakeOffset = Vector3.zero;
        }
        currentShakeCoroutine = StartCoroutine(Shake(duration, magnitude));
    }

    private IEnumerator Shake(float duration, float magnitude)
    {
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            shakeOffset = new Vector3(x, y, 0);
            
            elapsed += Time.deltaTime;
            yield return null; 
        }

        shakeOffset = Vector3.zero;
    }
}
