using UnityEngine;
using System.Text;

public class Arrow : MonoBehaviour
{
    public float speed = 15f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.velocity = transform.forward * speed; // Rigidbody로 이동만 처리
    }

    private void OnTriggerEnter(Collider other)
    {
        // 벽, 바닥, 블럭에 닿으면 제거
        if (other.CompareTag("Ground") || other.CompareTag("Block") || other.CompareTag("Wall"))
        {
            Debug.Log($"[Arrow] {other.tag}와 충돌 → 화살 제거");
            Destroy(gameObject);
            return;
        }

        // 캐릭터와 충돌한 경우
        if (other.name.StartsWith("Character_"))
        {
            string hitPlayerName = other.name.Replace("Character_", "");

            string myNick = NetworkConnector.Instance.UserNickname;

            // 자기 자신이 맞았는지 검사하고
            if (hitPlayerName != myNick)
            {
                int weaponIndex = 1; // 화살 무기 인덱스
                string attackMsg = $"HIT|{weaponIndex}|{myNick}|{hitPlayerName}\n";
                byte[] bytes = Encoding.UTF8.GetBytes(attackMsg);
                NetworkConnector.Instance.Stream.Write(bytes, 0, bytes.Length);

                Debug.Log($"[Arrow] {hitPlayerName} 님에게 HIT 메시지(무기:{weaponIndex}) 전송");
            }

            Destroy(gameObject);
            return;
        }
    }


}
