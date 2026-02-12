using UnityEngine;

public class GroundSpawner : MonoBehaviour
{
    public GameObject[] groundTilePrefabs; // Artýk dizi (Array) oldu
    Vector3 nextSpawnPoint;

    // Prefab'in atanýp atanmadýðýný kontrol et
    void Awake()
    {
        // Dizi boþsa veya ilk eleman yoksa hata ver
        if (groundTilePrefabs == null || groundTilePrefabs.Length == 0)
        {
            Debug.LogError("HATA: GroundSpawner objesinde 'Ground Tile Prefabs' listesi BOÞ!");
        }
    }

    public void SpawnTile()
    {
        if (groundTilePrefabs == null || groundTilePrefabs.Length == 0) return;

        // Rastgele bir yol parçasý seç
        int randomIndex = Random.Range(0, groundTilePrefabs.Length);
        GameObject selectedPrefab = groundTilePrefabs[randomIndex];

        // Yeni bir yol parçasý oluþtur
        GameObject temp = Instantiate(selectedPrefab, nextSpawnPoint, Quaternion.identity);

        // NextSpawnPoint ismini arayarak bulsun
        Transform point = temp.transform.Find("NextSpawnPoint");
        if (point != null)
        {
            nextSpawnPoint = point.position;
        }
        else
        {
            Debug.LogWarning("UYARI: Oluþan yol parçasýnda 'NextSpawnPoint' isimli obje bulunamadý! Yolun sonuna eklendiði varsayýlýyor (30 birim).");
            // Eðer bulamazsa, yol parçasýnýn kendisinin bittiði yeri tahmin et (yedek plan)
            nextSpawnPoint = temp.transform.position + new Vector3(0, 0, 30);
        }
    }

    void Start()
    {
        Debug.Log("GroundSpawner çalýþtý. Yol üretimi baþlýyor...");
        for (int i = 0; i < 15; i++)
        {
            SpawnTile();
        }
        Debug.Log("Baþlangýç yollarý üretildi.");
    }
}
