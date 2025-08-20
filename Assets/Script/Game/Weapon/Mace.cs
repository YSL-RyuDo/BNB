using System.Text;
using UnityEngine;

public class Mace : MonoBehaviour
{
    public float swingDuration = 0.5f;
    public float radius = 1.5f;
    public float speed = 720f; // 360도 이상 돌도록 충분히 빠르게 설정
    public Transform targetTransform;      // 중심이 될 캐릭터
    public string attackerNick;

    private float angle = 0f;
    private float elapsed = 0f;

    void Update()
    {
        if (targetTransform == null) return;

        elapsed += Time.deltaTime;
        angle += speed * Time.deltaTime;
        float rad = angle * Mathf.Deg2Rad;

        Vector3 center = targetTransform.position + Vector3.up * 0.5f;
        float x = Mathf.Cos(rad) * radius;
        float z = Mathf.Sin(rad) * radius;
        transform.position = center + new Vector3(x, 0, z);

        transform.LookAt(center);

        if (elapsed >= swingDuration)
            Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        string myNickname = NetworkConnector.Instance.UserNickname;

        if (!name.StartsWith(attackerNick + "_"))
            return;

        if (other.name.StartsWith("Character_"))
        {
            string hitPlayerName = other.name.Replace("Character_", "");
            if (hitPlayerName == attackerNick) return;

            int weaponIndex = 3;
            string attackMsg = $"HIT|{weaponIndex}|{myNickname}|{hitPlayerName}\n";
            byte[] bytes = Encoding.UTF8.GetBytes(attackMsg);
            NetworkConnector.Instance.Stream.Write(bytes, 0, bytes.Length);

            Debug.Log($"[Mace] {hitPlayerName} 님에게 HIT 메시지(무기:{weaponIndex}) 전송");
        }
    }
}
