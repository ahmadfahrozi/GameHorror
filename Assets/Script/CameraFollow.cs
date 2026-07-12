using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 5f;

    private Vector3 currentSmoothedPosition;

    void Start()
    {
        currentSmoothedPosition = transform.position;
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = new Vector3(
            target.position.x,
            target.position.y,
            -10
        );

        // 1. Lakukan pergerakan smooth (Lerp) untuk mengikuti player
        currentSmoothedPosition = Vector3.Lerp(
            currentSmoothedPosition,
            desiredPosition,
            smoothSpeed * Time.deltaTime
        );

        Vector3 finalPosition = currentSmoothedPosition;

        // 2. Tambahkan efek getaran (jika ada) di atas pergerakan yang sudah smooth
        CameraShake shake = GetComponent<CameraShake>();
        if (shake != null)
        {
            finalPosition += shake.shakeOffset;
        }

        // 3. Terapkan posisi akhir ke kamera
        transform.position = finalPosition;
    }
}