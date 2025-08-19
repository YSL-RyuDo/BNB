using UnityEngine;
using System.Text;

public class Pitchfork : MonoBehaviour
{
    public float swingDuration = 0.3f;
    private float elapsed = 0f;

    private Quaternion initialRot;
    public Quaternion targetRot;
    public string attackerNick;
    void Start()
    {
        Quaternion baseRot = transform.rotation;

        // 기준 회전을 정면 기준으로 -90도 눕힌 상태로 시작
        initialRot = baseRot * Quaternion.Euler(-90f, 0f, 0f);

        // 정면으로 완전히 눕는 상태
        targetRot = baseRot;

        transform.rotation = initialRot;
    }


    void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / swingDuration);
        transform.rotation = Quaternion.Slerp(initialRot, targetRot, t);

        if (t >= 1f)
        {
            Destroy(gameObject);
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        string myNickname = NetworkConnector.Instance.UserNickname;

        if (!this.name.StartsWith(myNickname + "_"))
            return;

        if (other.name.StartsWith("Character_"))
        {
            string hitPlayerName = other.name.Replace("Character_", "").Trim();

            if (hitPlayerName == attackerNick.Trim())
                return;

            int weaponIndex = 5;
            string attackMsg = $"HIT|{weaponIndex}|{hitPlayerName}\n";
            byte[] bytes = Encoding.UTF8.GetBytes(attackMsg);
            NetworkConnector.Instance.Stream.Write(bytes, 0, bytes.Length);

            Debug.Log($"[Pitchfork] {attackerNick}가 {hitPlayerName} 공격 (무기:{weaponIndex})");
        }

        if (other.CompareTag("Wall"))
        {
            string wallName = other.name;
            string msg = $"HITWALL|{wallName}\n";
            byte[] bytes = Encoding.UTF8.GetBytes(msg);
            NetworkConnector.Instance.Stream.Write(bytes, 0, bytes.Length);
            Destroy(other.gameObject);
            Debug.Log($"[Pitchfork] 서버에 벽 파괴 요청: {wallName}");
        }
    }
}
