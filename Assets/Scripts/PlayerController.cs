using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float forwardSpeed = 10f;
    public float laneDistance = 3f;  // Şerit mesafesi
    public float laneChangeSpeed = 10f;
    public float jumpForce = 10f;
    public float gravity = -20f;

    private CharacterController controller;
    private Animator animator;
    private Vector3 direction; // Hareket yönü
    private int currentLane = 1; // 0:Sol, 1:Orta, 2:Sağ
    private float targetX; // Hedef X pozisyonu (şerit)

    // Oyun başlama kontrolü
    public bool gameStarted = false;
    private bool isDead = false; // Öldü mü kontrolü

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

        if (controller == null) Debug.LogError("CharacterController bulunamadı!");
        if (animator == null) Debug.LogError("Animator (çocuk objelerde) bulunamadı!");

        gameStarted = false;
        isDead = false;
    }

    public void StartRunning()
    {
        gameStarted = true;
        if (animator != null) animator.SetBool("isGameStarted", true);
    }

    void Update()
    {
        // 1. Oyun Başlamadıysa (ve ölmediysek) bekle
        if (!gameStarted && !isDead) return;

        // --- SKOR ARTTIRMA ---
        // Artık GameManager zaman bazlı yapıyor. Burayı siliyoruz.

        // 2. Hareket Vektörünü Hazırla
        Vector3 moveVector = Vector3.zero;

        // İleri Hızını Ayarla (Bu satır çok önemli!)
        direction.z = forwardSpeed;

        // --- Yerçekimi (Her zaman çalışmalı) ---
        if (controller.isGrounded)
        {
            if (direction.y < 0) direction.y = -2f;
        }
        else
        {
            direction.y += gravity * Time.deltaTime;
        }

        // --- Eğer Öldüysek Sadece Düşeriz ---
        if (isDead)
        {
            moveVector.y = direction.y;
            controller.Move(moveVector * Time.deltaTime);
            return;
        }

        // --- Oyun Devam Ediyorsa (Ölmediysek) ---

        // Zıplama
        if (controller.isGrounded)
        {
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space))
            {
                Jump();
            }
        }

        // Şerit Değiştirme (Input'lar Update'te kontrol edilmeli)
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            currentLane++;
            if (currentLane == 3)
                currentLane = 2;
            else
            {
                // Sadece geçerli bir hareketse ses çal
                if (AudioManager.inst != null) AudioManager.inst.PlayMoveSound();
            }
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            currentLane--; // Assuming desiredLane is meant to be currentLane
            if (currentLane == -1) // This implies currentLane was 0, and tried to go to -1, so it should stay at 0.
                currentLane = 0;
            else
            {
                // Sadece geçerli bir hareketse ses çal
                if (AudioManager.inst != null) AudioManager.inst.PlayMoveSound();
            }
        }

        // Hedef X
        targetX = (currentLane - 1) * laneDistance;

        // Hareketi Birleştir
        moveVector = direction * Time.deltaTime;

        // X'i yumuşat
        float newX = Mathf.Lerp(transform.position.x, targetX, laneChangeSpeed * Time.deltaTime);
        moveVector.x = newX - transform.position.x; // Delta X

        controller.Move(moveVector);
    }

    private void Jump()
    {
        direction.y = jumpForce;
        if (animator != null) animator.SetTrigger("jump");

        // Ses Çal
        if (AudioManager.inst != null) AudioManager.inst.PlayJumpSound();
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!gameStarted || isDead) return; // Zaten öldüysek tekrar ölme

        if (hit.gameObject.CompareTag("Obstacle"))
        {
            // Çarpışmanın yönünü kontrol et!
            // hit.normal: Çarptığımız yüzeyin baktığı yön.
            // Eğer yüzey yukarı bakıyorsa (Y > 0.3 gibi), demek ki biz üstüne bastık.
            // Bu durumda ölmeyelim, koşmaya devam edelim.
            if (hit.normal.y > 0.3f)
            {
                return;
            }

            // Değilse (Yandan veya alttan çarptıysak)
            GameOver();
        }
    }

    private void GameOver()
    {
        Debug.Log("💀 OYUN BİTTİ! 💀");
        gameStarted = false;
        isDead = true;

        if (animator != null)
        {
            animator.SetTrigger("die");
            animator.applyRootMotion = true;
        }

        // GameManager'a haber ver
        if (GameManager.inst != null)
        {
            GameManager.inst.TriggerGameOver();
        }
    }

    // Hız limitleri
    public float maxSpeed = 25f;

    public void SetSpeed(float newSpeed)
    {
        forwardSpeed = newSpeed;
        if (forwardSpeed > maxSpeed) forwardSpeed = maxSpeed;
    }
}



