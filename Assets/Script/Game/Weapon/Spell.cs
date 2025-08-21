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
        rb.velocity = transform.forward * speed; // Rigidbody�� �̵��� ó��

        StartCoroutine(DestroyAfterSeconds(1.5f));
    }

    private IEnumerator DestroyAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        string myNickname = NetworkConnector.Instance.UserNickname;

        if (!this.name.StartsWith(myNickname + "_"))
            return; // ���� ���� ���Ⱑ �ƴϸ� ���� (�ڱ� ����θ� ������)

        // ĳ���Ϳ� �浹�� ���
        if (other.name.StartsWith("Character_"))
        {
            string hitPlayerName = other.name.Replace("Character_", "");

            string myNick = NetworkConnector.Instance.UserNickname;

            if (hitPlayerName != myNick)
            {
                int weaponIndex = 2;
                string attackMsg = $"HIT|{weaponIndex}|{myNickname}|{hitPlayerName}\n";
                byte[] bytes = Encoding.UTF8.GetBytes(attackMsg);
                NetworkConnector.Instance.Stream.Write(bytes, 0, bytes.Length);

                Debug.Log($"[Spell] {hitPlayerName} �Կ��� HIT �޽���(����:{weaponIndex}) ����");

                string destroyMsg = $"DESTROY_SPELL|{this.name}\n";
                byte[] destroyBytes = Encoding.UTF8.GetBytes(destroyMsg);
                NetworkConnector.Instance.Stream.Write(destroyBytes, 0, destroyBytes.Length);

                Debug.Log($"[Spell] {this.name} ���� ��Ŷ ����");
            }

           // Destroy(gameObject);
            return;
        }
    }
}
