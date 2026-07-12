using UnityEngine;

public class GerakPlayer : MonoBehaviour
{
    
    private Rigidbody2D rb;
    private Animator anim;

    private Vector2 moveInput;
    private Vector2 lastDirection = Vector2.down;

    public float moveSpeed = 5f;

    [HideInInspector]
    public float speedMultiplier = 1f; // Untuk slow dari musuh (seperti Tuyul)

    [HideInInspector]
    public float attackSpeedMultiplier = 1f; // Untuk slow saat nyenter (menyerang)

    [Header("Audio Settings")]
    [Tooltip("Masukkan komponen Audio Source untuk suara langkah kaki ke sini")]
    public AudioSource footstepSource;
    
    [Tooltip("Atur kecepatan suara langkah (1 = Normal, 1.5 = Lebih Cepat, dst)")]
    public float baseFootstepPitch = 1f;

    private SpriteRenderer sr;

    public Vector2 GetLastDirection()
    {
        return lastDirection;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // Cegah pergerakan jika game sedang ditahan (misal saat Cutscene Intro)
        if (GameManager.Instance != null && !GameManager.Instance.isGameActive)
        {
            moveInput = Vector2.zero;
            anim.SetFloat("Speed", 0);
            if (footstepSource != null && footstepSource.isPlaying) footstepSource.Stop();
            return;
        }
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        moveInput = new Vector2(h, v).normalized;

        if(moveInput != Vector2.zero)
        {
            // Simpan arah terakhir (tanpa normalisasi agar 1 atau -1 murni)
            lastDirection = new Vector2(h, v);

            // Karena di Animator Walk ada di posisi 2 atau -2, kita kalikan 2
            anim.SetFloat("Horizontal", h * 2);
            anim.SetFloat("Vertical", v * 2);
            anim.SetFloat("Speed", 1);

            // --- AUDIO JALAN ---
            if (footstepSource != null && !footstepSource.isPlaying)
            {
                footstepSource.Play(); // Mainkan suara jika belum main
            }
        }
        else
        {
            // Karena di Animator Idle ada di posisi 1 atau -1, kita gunakan lastDirection langsung
            anim.SetFloat("Horizontal", lastDirection.x);
            anim.SetFloat("Vertical", lastDirection.y);
            anim.SetFloat("Speed", 0);

            // --- HENTIKAN AUDIO JALAN ---
            if (footstepSource != null && footstepSource.isPlaying)
            {
                footstepSource.Stop(); // Berhenti jika player diam
            }
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            GetComponent<PlayerHealth>().TakeDamage(10);
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            GetComponent<PlayerHealth>().Heal(10);
        }

        // --- VISUAL INDICATOR UNTUK EFEK SLOW ---
        float totalSlow = speedMultiplier * attackSpeedMultiplier;
        
        if (sr != null)
        {
            if (speedMultiplier < 1f)
            {
                // Jika terkena slow dari musuh (Tuyul), ubah warna menjadi agak ungu/kebiruan
                sr.color = new Color(0.7f, 0.5f, 1f, 1f);
            }
            else if (attackSpeedMultiplier < 1f)
            {
                // Jika lambat karena nyenter, ubah warna sedikit gelap / kuning
                sr.color = new Color(0.9f, 0.9f, 0.7f, 1f);
            }
            else
            {
                // Kecepatan normal = warna normal
                sr.color = Color.white;
            }
        }

        // Melambatkan animasi jalan dan menyelaraskan suara jalan jika terkena slow
        if (moveInput != Vector2.zero)
        {
            anim.speed = totalSlow; 
            
            if (footstepSource != null)
            {
                // Samakan kecepatan suara (pitch) dengan kecepatan gerak animasi
                footstepSource.pitch = baseFootstepPitch * totalSlow;
            }
        }
        else
        {
            anim.speed = 1f;
        }
    }

    void FixedUpdate()
    {
        rb.linearVelocity = moveInput * moveSpeed * speedMultiplier * attackSpeedMultiplier;
    }
}