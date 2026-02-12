using UnityEngine;

public class Coin : MonoBehaviour
{
    public float turnSpeed = 90f;

    // Kullanýcýnýn boyut ayarýný buradan yapmasýný saðlayacaðýz.
    public Vector3 targetScale = new Vector3(1f, 1f, 1f);

    private Vector3 initialPosition; // Pozisyonu kilitlemek için

    // Iþýk Efekti
    private Light coinLight;

    void Start()
    {
        // ... (Eski kodlar: Animator Destroy vs) ...
        Animator[] anims = GetComponentsInChildren<Animator>();
        foreach (Animator anim in anims) Destroy(anim);

        Animation[] legacyAnims = GetComponentsInChildren<Animation>();
        foreach (Animation anim in legacyAnims) Destroy(anim);

        // ...
        transform.localScale = targetScale;
        initialPosition = transform.position;

        // Iþýðý bul veya ekle
        coinLight = GetComponentInChildren<Light>();
        if (coinLight == null)
        {
            // Eðer ýþýk yoksa kodla ekleyelim (Pratik çözüm)
            GameObject lightObj = new GameObject("CoinLight");
            lightObj.transform.parent = transform;
            lightObj.transform.localPosition = Vector3.zero;

            coinLight = lightObj.AddComponent<Light>();
            coinLight.type = LightType.Point;
            coinLight.color = Color.yellow;
            coinLight.range = 3f;
            coinLight.intensity = 2f;
        }
    }

    private void Update()
    {
        // ... (Eski kodlar: Scale, Pos) ...
        transform.localScale = targetScale;
        transform.position = initialPosition;
        transform.Rotate(0, turnSpeed * Time.deltaTime, 0);

        // Iþýk Animasyonu (Pulse)
        if (coinLight != null)
        {
            // Sinüs dalgasý ile yoðunluðu açýp kapat (1 ile 3 arasý)
            float noise = Mathf.PingPong(Time.time * 2f, 2f); // 0-2 arasý gider gelir
            coinLight.intensity = 1f + noise; // 1-3 arasý
        }
    }

    // Efekt ve Ses
    public GameObject collectVFX; // Toplama Efekti (Particle System prefabý)

    private void OnTriggerEnter(Collider other)
    {
        // Oyuncu çarparsa
        if (other.tag == "Player")
        {
            // Ses Çal
            if (AudioManager.inst != null)
            {
                AudioManager.inst.PlayCoinSound();
            }

            // Efekt Oluþtur
            if (collectVFX != null)
            {
                Instantiate(collectVFX, transform.position, Quaternion.identity);
            }

            // GameManager'a haber ver
            if (GameManager.inst != null)
            {
                GameManager.inst.IncrementCoin();
            }

            // Kendini yok et
            Destroy(gameObject);
        }
    }
}
