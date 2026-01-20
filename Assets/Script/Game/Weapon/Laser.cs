using System.Text;
using UnityEngine;

public class Laser : MonoBehaviour
{
    public string attackerNick;
    private float laserLength = 15f;  // 기본 길이
    public float duration = 1.0f;

    private LineRenderer lineRenderer;
    private BoxCollider box;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        box = GetComponent<BoxCollider>();
    }

    // 외부에서 길이 설정 가능하게 함수 추가
    public void SetLength(float length)
    {
        laserLength = length;
    }

    void Start()
    {
        // 콜라이더 크기 조정
        if (box != null)
        {
            box.size = new Vector3(box.size.x, box.size.y, laserLength);
            box.center = new Vector3(0f, 0f, laserLength / 2f);
        }

        // 라인 렌더러 길이 설정
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, transform.position + transform.forward * laserLength);

        // 스케일 조정 (필요하면)
        Vector3 scale = transform.localScale;
        scale.z = laserLength;
        transform.localScale = scale;

        // 일정 시간 후 제거
        Destroy(gameObject, duration);
    }

    private void OnTriggerEnter(Collider other)
    {
        string myNickname = NetworkConnector.Instance.UserNickname;

        if (other.name == $"Character_{myNickname}")
            return;

        if (!this.name.StartsWith(myNickname + "_"))
            return;

        if (other.name.StartsWith("Character_"))
        {
            string hitPlayerName = other.name.Replace("Character_", "");
            if (hitPlayerName == myNickname)
                return;

            int weaponIndex = 6;
            string attackMsg = $"HIT|{weaponIndex}|{myNickname}|{hitPlayerName}\n";
            byte[] bytes = Encoding.UTF8.GetBytes(attackMsg);
            NetworkConnector.Instance.Stream.Write(bytes, 0, bytes.Length);

            Debug.Log($"[Laser] {hitPlayerName} 님에게 HIT 메시지(무기:{weaponIndex}) 전송");
        }
    }
}
