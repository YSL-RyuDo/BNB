using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class Spell : MonoBehaviour
{
    public float speed = 7f;
    private Rigidbody rb;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.velocity = transform.forward * speed; // Rigidbody로 이동만 처리

        StartCoroutine(DestroyAfterSeconds(1.5f));
    }

    private IEnumerator DestroyAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        // 캐릭터와 충돌한 경우
        if (other.name.StartsWith("Character_"))
        {
            string hitPlayerName = other.name.Replace("Character_", "");

            string myNick = NetworkConnector.Instance.UserNickname;

            if (hitPlayerName != myNick)
            {
                int weaponIndex = 2;
                string attackMsg = $"HIT|{weaponIndex}|{hitPlayerName}\n";
                byte[] bytes = Encoding.UTF8.GetBytes(attackMsg);
                NetworkConnector.Instance.Stream.Write(bytes, 0, bytes.Length);

                Debug.Log($"[Spell] {hitPlayerName} 님에게 HIT 메시지(무기:{weaponIndex}) 전송");
            }

            Destroy(gameObject);
            return;
        }
    }
}
