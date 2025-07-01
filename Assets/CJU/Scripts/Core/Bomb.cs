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
        ExplodeAt(bombPos); // �߽�

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
                    // �ð�ȭ: �浹 ��ġ���� �� ǥ��
                    Debug.DrawLine(bombPos, hit.point, color, 1f);

                    // �浹�� ������Ʈ �ı�
                    Destroy(hit.collider.gameObject);

                    // �� ��ġ���� ���� ó��
                    ExplodeAt(hit.collider.transform.position);

                    break; // �� �̻� ���� X
                }

                // �ð�ȭ: �ش� ��ġ���� ��
                Debug.DrawLine(bombPos, checkPos, color, 1f);

                ExplodeAt(checkPos);
            }
        }

        Destroy(gameObject); // ��ź ����
    }
    void ExplodeAt(Vector3 pos)
    {
        // ������ �� ǥ�� (�� �信�� ��ó�� ����)
        Debug.DrawRay(pos + Vector3.up * 0.5f, Vector3.down, Color.white, 1.0f); // ������ �Ʒ��� ��

        // ���� ���� üũ
        Collider[] targets = Physics.OverlapSphere(pos, explosionRadius, damageLayer);
        foreach (var col in targets)
        {
            var player = col.GetComponent<PlayerController>();
            if (player != null)
            {
                Debug.Log("�÷��̾� ������");
            }
        }

    }
}
