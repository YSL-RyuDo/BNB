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
        rb.velocity = transform.forward * speed; // Rigidbody�� �̵��� ó��
    }

    private void OnTriggerEnter(Collider other)
    {
        // ��, �ٴ�, ���� ������ ����
        if (other.CompareTag("Ground") || other.CompareTag("Block") || other.CompareTag("Wall"))
        {
            Debug.Log($"[Arrow] {other.tag}�� �浹 �� ȭ�� ����");
            Destroy(gameObject);
            return;
        }

        // ĳ���Ϳ� �浹�� ���
        if (other.name.StartsWith("Character_"))
        {
            string hitPlayerName = other.name.Replace("Character_", "");

            string myNick = NetworkConnector.Instance.UserNickname;

            // �ڱ� �ڽ��� �¾Ҵ��� �˻��ϰ�
            if (hitPlayerName != myNick)
            {
                int weaponIndex = 1; // ȭ�� ���� �ε���
                string attackMsg = $"HIT|{weaponIndex}|{myNick}|{hitPlayerName}\n";
                byte[] bytes = Encoding.UTF8.GetBytes(attackMsg);
                NetworkConnector.Instance.Stream.Write(bytes, 0, bytes.Length);

                Debug.Log($"[Arrow] {hitPlayerName} �Կ��� HIT �޽���(����:{weaponIndex}) ����");
            }

            Destroy(gameObject);
            return;
        }
    }


}
