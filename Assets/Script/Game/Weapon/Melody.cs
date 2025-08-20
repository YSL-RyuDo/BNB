using System.Text;
using UnityEngine;

public class Melody : MonoBehaviour
{
    public float speed = 5.0f;
    public float lifetime = 1.5f;
    public float collisionCooldown = 0.1f; // 0.3�ʰ� �浹 ����

    private Rigidbody rb;
    private float timer = 0f;
    private float collisionTimer = 0f; // �浹 ��ٿ� Ÿ�̸�

    public string attackerNick;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.velocity = transform.forward * speed;

        timer = lifetime;
        collisionTimer = 0f;
    }
    void FixedUpdate()
    {
        SendPositionToServer();
    }


    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            NotifyDestroyToServer();
            Destroy(gameObject);
        }

        if (collisionTimer > 0f)
        {
            collisionTimer -= Time.deltaTime;
        }
    }

    void SendPositionToServer()
    {
        Vector3 pos = transform.position;
        Quaternion rot = transform.rotation;

        string myNick = NetworkConnector.Instance.UserNickname;
        string msg = $"MELODY_MOVE|{myNick}|{pos.x:F2},{pos.y:F2},{pos.z:F2}|{rot.eulerAngles.y:F2}\n";
        byte[] bytes = Encoding.UTF8.GetBytes(msg);
        NetworkConnector.Instance.Stream.Write(bytes, 0, bytes.Length);
    }

    void NotifyDestroyToServer()
    {
        string myNick = NetworkConnector.Instance.UserNickname;
        string msg = $"MELODY_DESTROY|{myNick}\n";
        byte[] bytes = Encoding.UTF8.GetBytes(msg);
        NetworkConnector.Instance.Stream.Write(bytes, 0, bytes.Length);
    }


    private void OnTriggerEnter(Collider other)
    {
        // �浹 ��ٿ� ���̸� ����
        if (collisionTimer > 0f)
            return;

        if (other.CompareTag("Ground") || other.CompareTag("Block") || other.CompareTag("Wall"))
        {
            Vector3 incoming = rb.velocity.normalized;
            Vector3 collisionPoint = other.ClosestPoint(transform.position);
            Vector3 collisionNormal = (transform.position - collisionPoint).normalized;
            Vector3 reflect = Vector3.Reflect(incoming, collisionNormal);

            rb.velocity = reflect * speed * 0.8f;
            if (reflect != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(reflect);

            timer = lifetime;

            collisionTimer = collisionCooldown; // �浹 ���� ��ٿ� ����

            Debug.Log($"[Melody] {other.tag}�� �浹 �� �ݻ� & Ÿ�̸� ����");
            return;
        }

        if (other.name.StartsWith("Character_"))
        {
            string myNickname = NetworkConnector.Instance.UserNickname;
            string hitPlayerName = other.name.Replace("Character_", "").Trim();

            if (hitPlayerName == attackerNick.Trim())
            {
                Debug.Log($"[Melody] �ڱ� �ڽ�({attackerNick})�� �浹, ������ ����");
                Destroy(gameObject);
                NotifyDestroyToServer();
                return;
            }

            int weaponIndex = 4;
            string attackMsg = $"HIT|{weaponIndex}|{myNickname}|{hitPlayerName}\n";
            byte[] bytes = Encoding.UTF8.GetBytes(attackMsg);
            NetworkConnector.Instance.Stream.Write(bytes, 0, bytes.Length);

            Debug.Log($"[Melody] {attackerNick}�� {hitPlayerName} ���� (����:{weaponIndex})");

            Destroy(gameObject);
            NotifyDestroyToServer();
        }
    }
}
