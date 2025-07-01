using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bomb : MonoBehaviour
{
    public int explosionRange = 10;
    public GameObject explosionPrefab;
    public LayerMask obstacleLayer;
    public LayerMask damageLayer;
    public float explosionDelay = 3f;
    public float explosionRadius = 0.4f;
    // Start is called before the first frame update
    void Start()
    {
        Invoke(nameof(Explode), explosionDelay);
    }

    void Explode()
    {
        Vector3 bombPos = transform.position;
        ExplodeAt(bombPos); // 중심

        Vector3[] directions = {
        Vector3.forward, Vector3.back,
        Vector3.right, Vector3.left
        };

        Color[] debugColors = {
        Color.red, Color.green,
        Color.blue, Color.yellow
        };

        for (int d = 0; d < directions.Length; d++)
        {
            Vector3 dir = directions[d];
            Color color = debugColors[d];

            for (int i = 1; i <= explosionRange; i++)
            {
                Vector3 checkPos = bombPos + dir * i;

                if (Physics.Raycast(bombPos, dir, out RaycastHit hit, i, obstacleLayer))
                {
                    // 시각화: 충돌 위치까지 선 표시
                    Debug.DrawLine(bombPos, hit.point, color, 1f);

                    // 충돌한 오브젝트 파괴
                    Destroy(hit.collider.gameObject);

                    // 그 위치까지 폭발 처리
                    ExplodeAt(hit.collider.transform.position);

                    break; // 더 이상 진행 X
                }

                // 시각화: 해당 위치까지 선
                Debug.DrawLine(bombPos, checkPos, color, 1f);

                ExplodeAt(checkPos);
            }
        }

        Destroy(gameObject); // 폭탄 제거
    }
    void ExplodeAt(Vector3 pos)
    {
        // 디버깅용 구 표시 (씬 뷰에서 원처럼 보임)
        Debug.DrawRay(pos + Vector3.up * 0.5f, Vector3.down, Color.white, 1.0f); // 위에서 아래로 선

        // 피해 범위 체크
        Collider[] targets = Physics.OverlapSphere(pos, explosionRadius, damageLayer);
        foreach (var col in targets)
        {
            var player = col.GetComponent<PlayerController>();
            if (player != null)
            {
                Debug.Log("플레이어 데미지");
            }
        }

    }
}
