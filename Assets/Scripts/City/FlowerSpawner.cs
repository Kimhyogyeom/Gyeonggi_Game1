using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif
namespace SeongWon
{


public class FlowerSpawner : MonoBehaviour
{
    [Header("Terrain 설정")]
    public Terrain targetTerrain;

    [Header("배치할 프리팹들 (콜라이더 포함 꽃들)")]
    public GameObject[] flowerPrefabs;   // 여러 개 등록

    [Header("개수 및 범위")]
    public int count = 100;
    public float minYOffset = 0.0f;
    public float maxSlope = 30.0f;  

#if UNITY_EDITOR
    [ContextMenu("Spawn Flowers On Terrain")]
    public void SpawnFlowers()
    {
        if (targetTerrain == null)
        {
            Debug.LogError("[FlowerSpawner] targetTerrain이 설정되지 않았습니다. 인스펙터에서 Terrain을 넣어주세요.");
            return;
        }

        if (flowerPrefabs == null || flowerPrefabs.Length == 0)
        {
            Debug.LogError("[FlowerSpawner] flowerPrefabs가 비어있습니다. 프리팹을 하나 이상 넣어주세요.");
            return;
        }

        // 배열 안에 null 있는지 체크
        for (int i = 0; i < flowerPrefabs.Length; i++)
        {
            if (flowerPrefabs[i] == null)
            {
                Debug.LogError($"[FlowerSpawner] flowerPrefabs[{i}] 가 null 입니다. 비어있는 칸을 제거하거나 프리팹을 넣어주세요.");
                return;
            }
        }

        TerrainData data = targetTerrain.terrainData;
        if (data == null)
        {
            Debug.LogError("[FlowerSpawner] targetTerrain.terrainData 가 null 입니다.");
            return;
        }

        Vector3 terrainPos = targetTerrain.transform.position;
        Vector3 size = data.size;

        Undo.RegisterFullObjectHierarchyUndo(targetTerrain.gameObject, "Spawn Flowers");

        int spawned = 0;
        int safety = 0;


        while (spawned < count && safety < count * 10)
        {
            safety++;

            // 0~1 사이 랜덤 좌표
            float rx = Random.value;
            float rz = Random.value;

            // 실제 월드 좌표
            float worldX = terrainPos.x + rx * size.x;
            float worldZ = terrainPos.z + rz * size.z;

            // 지형 높이
            float terrainY = data.GetInterpolatedHeight(rx, rz) + terrainPos.y;

            // 기울기(슬로프) 체크
            Vector3 normal = data.GetInterpolatedNormal(rx, rz);
            float slope = Vector3.Angle(normal, Vector3.up);
            if (slope > maxSlope)
                continue; // 너무 기울어진 곳은 패스

            // 배치 위치
            Vector3 pos = new Vector3(worldX, terrainY + minYOffset, worldZ);

            int prefabIndex = Random.Range(0, flowerPrefabs.Length);
            GameObject prefabToUse = flowerPrefabs[prefabIndex];

            if (prefabToUse == null)
            {
                Debug.LogError($"[FlowerSpawner] 선택된 프리팹 flowerPrefabs[{prefabIndex}] 가 null 입니다.");
                continue;
            }

            // Prefab인지 씬 오브젝트인지 체크 (에디터용)
            if (!PrefabUtility.IsPartOfPrefabAsset(prefabToUse))
            {
                Debug.LogWarning($"[FlowerSpawner] flowerPrefabs[{prefabIndex}] 는 Prefab Asset 이 아니라 씬 오브젝트일 수 있습니다. Prefab 폴더에 있는 프리팹을 넣는 걸 추천합니다.");
            }

            GameObject go = Instantiate(prefabToUse);

            if (go == null)
            {
                Debug.LogError($"[FlowerSpawner] PrefabUtility.InstantiatePrefab 에서 null 이 리턴되었습니다. 프리팹 설정을 확인해주세요. (index: {prefabIndex})");
                continue;
            }

            go.transform.position = pos;
            go.transform.SetParent(this.transform); // 관리하기 편하게 Spawner 아래에 붙임

            // 지면에 수직 정렬하고 싶으면:
            go.transform.up = normal;

            spawned++;
        }

        Debug.Log($"[FlowerSpawner] 꽃(프리팹 {flowerPrefabs.Length}종) {spawned}개 배치 완료 (시도: {safety}번)");
    }
#endif
}
}
