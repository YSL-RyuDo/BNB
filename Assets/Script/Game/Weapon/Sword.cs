using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class Sword : MonoBehaviour
{
    public float swingAngle = 120f; // 총 회전 각도 (예: 10시~2시 → 120도)
    public float swingDuration = 0.3f; // 회전 시간
    private float elapsed = 0f;
    private Quaternion initialRot;
    private Quaternion targetRot;

    void Start()
    {
        // 월드 좌표 기준 초기 회전 기억
        Quaternion baseRot = transform.rotation;

        // baseRot를 중심으로 -swingAngle/2 만큼 Y축 회전한게 시작 각도
        initialRot = Quaternion.Euler(0f, -swingAngle / 2f, 0f) * baseRot;
        // baseRot를 중심으로 +swingAngle/2 만큼 Y축 회전한게 끝 각도
        targetRot = Quaternion.Euler(0f, swingAngle / 2f, 0f) * baseRot;

        transform.rotation = initialRot;
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / swingDuration);
        transform.rotation = Quaternion.Slerp(initialRot, targetRot, t);

        if (t >= 1f)
        {
            Destroy(gameObject); // 공격 끝나면 삭제
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        string myNickname = NetworkConnector.Instance.UserNickname;

        // 공격 무기라면 이름이 "{공격자}_Sword" 형식일 것
        if (!this.name.StartsWith(myNickname + "_"))
            return; // 내가 만든 무기가 아니면 무시 (자기 무기로만 판정함)

        if (other.name.StartsWith("Character_"))
        {
            string hitPlayerName = other.name.Replace("Character_", "");

            if (hitPlayerName == myNickname)
                return; // 자기 자신 맞은 거면 무시

            int weaponIndex = 0; // 검이라면 0
            string attackMsg = $"HIT|{weaponIndex}|{hitPlayerName}\n";
            byte[] bytes = Encoding.UTF8.GetBytes(attackMsg);
            NetworkConnector.Instance.Stream.Write(bytes, 0, bytes.Length);

            Debug.Log($"[Sword] {hitPlayerName} 님에게 HIT 메시지(무기:{weaponIndex}) 전송");
        }
    }

}
