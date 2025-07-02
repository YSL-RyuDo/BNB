using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bomb : MonoBehaviour
{
    public int explosionRange = 10; // ���� ����

    public LayerMask obstacleLayer; // ��ֹ� ���̾�
    public LayerMask damageLayer; // ���ظ� �޴� ��� ���̾�
    public float explosionDelay = 3f; // ���� ��������� ��� �ð�
    public float explosionRadius = 0.4f; // ���� ����

    public GameObject explosionEffectPrefab; // ���� ����Ʈ ������


    private void OnEnable()
    {
        Invoke(nameof(Explode), explosionDelay);
    }

    private void OnDisable()
    {
        CancelInvoke(); // ���� ��� Ÿ�̸� �ʱ�ȭ
    }

    // ���� �Լ�
    void Explode()
    {

        Vector3 bombPos = transform.position;

        // �߽ɿ��� ����
        ExplodeAt(bombPos);

        // �� �������� Ȯ��
        Vector3[] directions = {
        Vector3.forward, Vector3.back,
        Vector3.right, Vector3.left
        };

        Color[] debugColors = {
        Color.red, Color.green,
        Color.blue, Color.yellow
        };

        // �� ���� ��ȸ
        for (int d = 0; d < directions.Length; d++)
        {
            Vector3 dir = directions[d];
            Color color = debugColors[d];

            // ���� ���� ��ŭ Ȯ��
            for (int i = 1; i <= explosionRange; i++)
            {
                Vector3 checkPos = bombPos + dir * i;

                // ��ֹ� �浹 �˻�
                // ��ֹ��� �ִ� ���
                if (Physics.Raycast(bombPos, dir, out RaycastHit hit, i, obstacleLayer))
                {

                    Debug.DrawLine(bombPos, hit.point, color, 1f);

                    // �浹�� ������Ʈ �ı�
                    Destroy(hit.collider.gameObject);

                    // �� ��ġ���� ���� ó��, ��ź�� ��ġ�� ��ġ��Ŵ
                    ExplodeAt(transform.position);

                    break;
                }

                // ��ֹ��� ���� ���
                Debug.DrawLine(bombPos, checkPos, color, 1f);

                ExplodeAt(checkPos);
            }
        }

        // ��ź ������Ʈ Ǯ�� ��ȯ
        ObjectPoolManager.Instance.Return("Bomb", gameObject);
    }

    // ���� ��ġ ó��
    void ExplodeAt(Vector3 pos)
    {
        // ������Ʈ Ǯ���� ����Ʈ ����
        GameObject effect = ObjectPoolManager.Instance.Get("BombEffect", pos, Quaternion.identity);

        Debug.DrawRay(pos + Vector3.up * 0.5f, Vector3.down, Color.white, 1.0f); 

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
