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
        // Æø¹ß ÀÌÆåÆ® Ç®¿¡ ¹ÝÈ¯
        ObjectPoolManager.Instance.Return("BombEffect", gameObject);
    }

    private void OnDisable()
    {
        CancelInvoke();
    }
}
