using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bomb : MonoBehaviour
{
    public int explosionRange = 10; // 폭발 범위

    public LayerMask obstacleLayer; // 장애물 레이어
    public LayerMask damageLayer; // 피해를 받는 대상 레이어
    public float explosionDelay = 3f; // 폭발 터지기까지 대기 시간
    public float explosionRadius = 0.4f; // 폭발 범위

    public GameObject explosionEffectPrefab; // 폭발 이펙트 프리팹


    private void OnEnable()
    {
        Invoke(nameof(Explode), explosionDelay);
    }

    private void OnDisable()
    {
        CancelInvoke(); // 재사용 대비 타이머 초기화
    }

    // 폭발 함수
    void Explode()
    {

        Vector3 bombPos = transform.position;

        // 중심에서 폭발
        ExplodeAt(bombPos);

        // 네 방향으로 확산
        Vector3[] directions = {
        Vector3.forward, Vector3.back,
        Vector3.right, Vector3.left
        };

        Color[] debugColors = {
        Color.red, Color.green,
        Color.blue, Color.yellow
        };

        // 각 방향 순회
        for (int d = 0; d < directions.Length; d++)
        {
            Vector3 dir = directions[d];
            Color color = debugColors[d];

            // 폭발 범위 만큼 확산
            for (int i = 1; i <= explosionRange; i++)
            {
                Vector3 checkPos = bombPos + dir * i;

                // 장애물 충돌 검사
                // 장애물이 있는 경우
                if (Physics.Raycast(bombPos, dir, out RaycastHit hit, i, obstacleLayer))
                {

                    Debug.DrawLine(bombPos, hit.point, color, 1f);

                    // 충돌한 오브젝트 파괴
                    Destroy(hit.collider.gameObject);

                    // 그 위치까지 폭발 처리, 폭탄의 위치와 일치시킴
                    ExplodeAt(transform.position);

                    break;
                }

                // 장애물이 없는 경우
                Debug.DrawLine(bombPos, checkPos, color, 1f);

                ExplodeAt(checkPos);
            }
        }

        // 폭탄 오브젝트 풀에 반환
        ObjectPoolManager.Instance.Return("Bomb", gameObject);
    }

    // 폭발 위치 처리
    void ExplodeAt(Vector3 pos)
    {
        // 오브젝트 풀에서 이펙트 꺼냄
        GameObject effect = ObjectPoolManager.Instance.Get("BombEffect", pos, Quaternion.identity);

        Debug.DrawRay(pos + Vector3.up * 0.5f, Vector3.down, Color.white, 1.0f); 

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
