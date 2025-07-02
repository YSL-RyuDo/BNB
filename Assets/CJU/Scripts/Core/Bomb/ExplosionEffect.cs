using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionEffect : MonoBehaviour
{
    public float lifetime = 0.5f;

    private void OnEnable()
    {
        Invoke(nameof(ReturnToPool), lifetime);
    }

    private void ReturnToPool()
    {
        // ���� ����Ʈ Ǯ�� ��ȯ
        ObjectPoolManager.Instance.Return("BombEffect", gameObject);
    }

    private void OnDisable()
    {
        CancelInvoke();
    }
}
