using System.Text;
using UnityEngine;

public class Melody : MonoBehaviour
{
    public float speed = 5.0f;
    public float lifetime = 1.5f;
    public float collisionCooldown = 0.1f;

    private Rigidbody rb;
    private float timer;
    private float collisionTimer;

    public string attackerNick;   // 반드시 생성 시 세팅되어야 함

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
        if (NetworkConnector.Instance.UserNickname == attackerNick)
        {
            SendPositionToServer();
        }
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            if (NetworkConnector.Instance.UserNickname == attackerNick)
                NotifyDestroyToServer();

            Destroy(gameObject);
        }

        if (collisionTimer > 0f)
            collisionTimer -= Time.deltaTime;
    }

    void SendPositionToServer()
    {
        Vector3 pos = transform.position;
        float rotY = transform.eulerAngles.y;

        string msg = $"MELODY_MOVE|{attackerNick}|{pos.x:F2},{pos.y:F2},{pos.z:F2}|{rotY:F2}\n";
        byte[] bytes = Encoding.UTF8.GetBytes(msg);
        NetworkConnector.Instance.Stream.Write(bytes, 0, bytes.Length);
    }

    void NotifyDestroyToServer()
    {
        string msg = $"MELODY_DESTROY|{attackerNick}\n";
        byte[] bytes = Encoding.UTF8.GetBytes(msg);
        NetworkConnector.Instance.Stream.Write(bytes, 0, bytes.Length);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collisionTimer > 0f)
            return;

        // 벽 반사
        if (other.CompareTag("Ground") || other.CompareTag("Block") || other.CompareTag("Wall"))
        {
            Vector3 reflect = Vector3.Reflect(rb.velocity.normalized,
                (transform.position - other.ClosestPoint(transform.position)).normalized);

            rb.velocity = reflect * speed * 0.8f;

            if (reflect != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(reflect);

            timer = lifetime;
            collisionTimer = collisionCooldown;
            return;
        }

        if (!other.name.StartsWith("Character_"))
            return;

        string hitPlayerName = other.name.Replace("Character_", "").Trim();

        if (hitPlayerName == attackerNick)
            return;

        if (NetworkConnector.Instance.UserNickname != attackerNick)
            return;

        int weaponIndex = 4;
        string attackMsg = $"HIT|{weaponIndex}|{attackerNick}|{hitPlayerName}\n";
        byte[] bytes = Encoding.UTF8.GetBytes(attackMsg);
        NetworkConnector.Instance.Stream.Write(bytes, 0, bytes.Length);

        NotifyDestroyToServer();
        Destroy(gameObject);
    }
}
