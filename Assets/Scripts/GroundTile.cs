using UnityEngine;
using System.Collections.Generic; // Listeler için gerekli

public class GroundTile : MonoBehaviour
{
    GroundSpawner groundSpawner;
    public PlayerController playerCtrl; // Renamed public field

    [Header("Obstacle Settings")]
    public GameObject[] obstaclePrefabs;
    public float obstacleChance = 0.5f;

    [Header("Coin Settings")]
    public GameObject coinPrefab;

    // Oluþturduðumuz objeleri listede tutuyoruz
    List<GameObject> mySpawnedObjects = new List<GameObject>();

    // Engellerin koyulmadýðý boþ þeridi takip etmek için
    int freeLaneIndex = -1;
    // "Yemleme" yapmak için hangi þeridin dolu olduðunu tutacaðýz
    int blockedLaneIndex = -1;

    void Start()
    {
        // Unity 2023+ için yeni metod (Eski uyarýyý kaldýrýr)
        groundSpawner = GameObject.FindFirstObjectByType<GroundSpawner>();
        playerCtrl = GameObject.FindFirstObjectByType<PlayerController>(); // Renamed

        // Önce engelleri oluþtur (Bu sýrada boþ þerit belirlenecek)
        SpawnObstacles();

        // Sonra o boþ þeride altýnlarý döþe
        SpawnCoins();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            groundSpawner.SpawnTile();

            // Yol yok olmadan 2 saniye bekliyor.
            // Bizim oluþturduðumuz objeleri de o zaman sileceðiz.
            Destroy(gameObject, 2);
        }
    }

    private void OnDestroy()
    {
        foreach (GameObject obj in mySpawnedObjects)
        {
            if (obj != null) Destroy(obj);
        }
        mySpawnedObjects.Clear();
    }

    void SpawnCoins()
    {
        if (coinPrefab == null) return;

        // EÐER "Yemleme" yapabileceðimiz bir engel durumu varsa 
        // Ve ZigZag deðilse (blockedLaneIndex != -1)
        if (blockedLaneIndex != -1 && freeLaneIndex != -1 && Random.value > 0.3f)
        {
            SpawnDynamicCoins(); // Yeni Profesyonel Daðýtým (5-3, 6-3, 6-5)
        }
        else
        {
            SpawnLinearCoins(); // Eski Düz Daðýtým (5/7/9)
        }
    }

    // Yeni: Kullanýcýnýn belirlediði 3 senaryodan birini seçer (5-3, 6-3, 6-5)
    void SpawnDynamicCoins()
    {
        // Senaryo Seçimi (0, 1 veya 2)
        int scenario = Random.Range(0, 3);

        // Bu deðerler senaryoya göre deðiþecek
        int blockedCount = 5;
        int freeCount = 3;

        switch (scenario)
        {
            case 0: // 5'e 3 (Klasik)
                blockedCount = 5;
                freeCount = 3;
                break;
            case 1: // 6'ya 3 (Biraz daha riskli)
                blockedCount = 6;
                freeCount = 3;
                break;
            case 2: // 6'ya 5 (Bol kazançlý risk)
                blockedCount = 6;
                freeCount = 5;
                break;
        }

        // --- AYARLAR ---
        float obstacleZ = 7.5f; // Engelin olduðu tahmini yer (Single/Double için)
        float safeMargin = 3.5f; // Engelden ne kadar önce altýnlar bitsin? (Refleks payý)
        float spacing = 1.0f; // Altýnlar arasý mesafe

        // 1. Kýsým: TEHLÝKELÝ ÞERÝT (Blocked Lane - Risk)
        // Hedef: (Obstacle - SafeMargin) noktasýna kadar döþeyelim.

        float blockedZinciriBitis = obstacleZ - safeMargin;
        float blockedStart = blockedZinciriBitis - ((blockedCount - 1) * spacing);

        for (int i = 0; i < blockedCount; i++)
        {
            float zPos = blockedStart + (i * spacing);
            if (zPos < 0.5f) zPos = 0.5f;
            SpawnItem(coinPrefab, blockedLaneIndex, zPos, 1f, true);
        }

        // 2. Kýsým: GÜVENLÝ ÞERÝT - DIAGONAL GEÇÝÞ (MATEMATÝKSEL AÇI)
        // Hedef: Riskli þeritten Güvenli þeride doðru kayan (Lerp) bir yapý.
        // Baþlangýç: Riskli þeridin bittiði yerin biraz ilerisi.

        float freeStart = blockedZinciriBitis + spacing; // Örn: 4.0 + 1.0 = 5.0f

        // Þeritlerin X pozisyonlarýný bulalým
        float blockedX = (blockedLaneIndex - 1) * 3f; // 0->-3, 1->0, 2->3
        float freeX = (freeLaneIndex - 1) * 3f;

        for (int i = 0; i < freeCount; i++)
        {
            float zPos = freeStart + (i * spacing);
            if (zPos > 9.5f) break;

            // Eðer geçiþ aþamasýndaysak (ilk 2-3 altýn), aradaki açýyý (Interpolation) ver
            // i=0 -> %30 geçiþ
            // i=1 -> %60 geçiþ
            // i>=2 -> %100 (Tam güvenli þerit)

            float t = 0;
            if (i == 0) t = 0.3f;
            else if (i == 1) t = 0.7f;
            else t = 1.0f;

            // Lerp ile ara pozisyonu bul
            float currentX = Mathf.Lerp(blockedX, freeX, t);

            SpawnItemCustomPos(coinPrefab, currentX, zPos, 1f, true);
        }
    }

    void SpawnLinearCoins()
    {
        // Lineer olanda da artýk CustomPos kullanabiliriz ama þimdilik düz kalsýn
        // veya ufak bir S çizdirebiliriz. Basitlik adýna düz býrakýyorum.
        int laneToSpawn = freeLaneIndex;
        if (laneToSpawn == -1) laneToSpawn = Random.Range(0, 3);

        int r = Random.Range(0, 3);
        int count = 5;
        if (r == 1) count = 7;
        if (r == 2) count = 9;

        float spacing = 1.2f;
        float startZ = 7.5f - ((count - 1) * spacing * 0.5f);

        for (int i = 0; i < count; i++)
        {
            float zPos = startZ + (i * spacing);
            SpawnItem(coinPrefab, laneToSpawn, zPos, 1f, true);
        }
    }

    void SpawnObstacles()
    {
        blockedLaneIndex = -1; // Sýfýrla

        if (obstaclePrefabs == null || obstaclePrefabs.Length == 0) return;

        // %20 ihtimalle hiç engel olmasýn (Bomboþ yol)
        if (Random.value > 0.8f)
        {
            freeLaneIndex = Random.Range(0, 3); // Hepsi boþ, rastgele seç
            return;
        }

        int pattern = Random.Range(0, 3);

        switch (pattern)
        {
            case 0: // Tekli
                SpawnSingleObstacle(7.5f);
                break;
            case 1: // Ýkili
                SpawnDoubleObstacle(7.5f);
                break;
            case 2: // ZigZag 
                SpawnZigZagObstacle(5f, 10f);
                break;
        }
    }

    void SpawnSingleObstacle(float zPos)
    {
        int obstacleLane = Random.Range(0, 3);
        bool hasArc = SpawnObstacleWithCoin(obstacleLane, zPos);

        freeLaneIndex = (obstacleLane + 1) % 3;

        // Eðer engelde kemer (Arc) varsa, yerde yemleme (Bait) YAPMA!
        if (hasArc)
            blockedLaneIndex = -1;
        else
            blockedLaneIndex = obstacleLane;
    }

    void SpawnDoubleObstacle(float zPos)
    {
        int freeLane = Random.Range(0, 3);
        freeLaneIndex = freeLane;

        blockedLaneIndex = (freeLane + 1) % 3; // Varsayýlan yem
        bool anyArc = false;

        for (int i = 0; i < 3; i++)
        {
            if (i != freeLane)
            {
                if (SpawnObstacleWithCoin(i, zPos)) anyArc = true;
            }
        }

        // Herhangi birinde kemer varsa yemleme iptal
        if (anyArc) blockedLaneIndex = -1;
    }

    void SpawnZigZagObstacle(float zPos1, float zPos2)
    {
        int lane1 = Random.Range(0, 3);
        int lane2 = Random.Range(0, 3);
        while (lane1 == lane2) lane2 = Random.Range(0, 3);

        freeLaneIndex = 3 - (lane1 + lane2);

        bool arc1 = SpawnObstacleWithCoin(lane1, zPos1);
        bool arc2 = SpawnObstacleWithCoin(lane2, zPos2);

        // ZigZag'da genelde yemleme zordur ama yine de kuralý koyalým
        if (arc1 || arc2) blockedLaneIndex = -1;
    }

    // Engel oluþturur, üstüne kemer koyduysa true döner
    bool SpawnObstacleWithCoin(int lane, float zPos)
    {
        // 1. Engeli koy
        GameObject obstacle = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
        SpawnItem(obstacle, lane, zPos, 0f, false);

        // 2. Þans %50: Engelin üstünde Altýn Kemeri olsun mu?
        if (coinPrefab != null && Random.value < 0.5f)
        {
            // Rastgele 9 veya 7 altýn seç (Kullanýcý isteði: 5 yerine 9)
            int coinCount = Random.Range(0, 2) == 0 ? 9 : 7;

            // varsayýlan (hýz bulunamazsa)
            float currentSpeed = 10f;
            if (playerCtrl != null) currentSpeed = playerCtrl.forwardSpeed; // Renamed

            // Geniþlik Faktörü: Hýz arttýkça yayvanlaþýr (Speed * 0.5)
            // Örn: Speed 10 -> Width 5. Speed 20 -> Width 10.
            float arcWidth = currentSpeed * 0.5f;

            // Altýnlar arasý mesafe de hýza göre açýlmalý ki kemeri kaplasýnlar
            // 9 altýn varsa merkezden 4 saða 4 sola gider. Max offset = 4 * spacing.
            // Bunun arcWidth'e yakýn olmasýný isteriz.
            float spacing = arcWidth / 4.0f;

            // Tepe yüksekliði (Zýplama yüksekliðine uygun)
            float jumpHeight = 3f;

            // Ortadan saða ve sola doðru daðýt (Örn: -2, -1, 0, 1, 2)
            int halfCount = coinCount / 2;

            for (int i = -halfCount; i <= halfCount; i++)
            {
                // Z eksenindeki sapma (Merkezden ne kadar uzakta?)
                float zOffset = i * spacing;

                // Parabol Formülü: y = Height * (1 - (x / Width)^2)
                float normalizedX = zOffset / arcWidth;
                float heightY = jumpHeight * (1 - (normalizedX * normalizedX));

                // Yere gömülmesin, en az 1 birim yukarýda olsun
                if (heightY < 1f) heightY = 1f;

                // Hesaplanan pozisyonda altýný oluþtur
                SpawnItem(coinPrefab, lane, zPos + zOffset, heightY, true);
            }
            return true; // Kemer oluþturuldu
        }
        return false; // Sadece engel var
    }

    // Genel oluþturma fonksiyonu (Þerit bazlý)
    void SpawnItem(GameObject prefab, int laneIndex, float zPos, float heightY, bool isCoin)
    {
        float xPos = 0;
        if (laneIndex == 0) xPos = -3f;
        else if (laneIndex == 1) xPos = 0f;
        else if (laneIndex == 2) xPos = 3f;

        SpawnItemCustomPos(prefab, xPos, zPos, heightY, isCoin);
    }

    // Yeni: X koordinatýný elle verebildiðimiz versiyon (Diagonal dizilim için)
    void SpawnItemCustomPos(GameObject prefab, float xPos, float zPos, float heightY, bool isCoin)
    {
        Vector3 spawnPosition = transform.position + new Vector3(xPos, heightY, zPos);

        // Parent kullanmýyoruz, yamulmayý önlüyoruz.
        GameObject obj = Instantiate(prefab, spawnPosition, Quaternion.identity);

        // Listeye ekle ki sonra silebilelim
        mySpawnedObjects.Add(obj);
    }
}

