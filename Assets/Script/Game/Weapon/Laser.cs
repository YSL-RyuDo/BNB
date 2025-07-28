using System.Text;
using UnityEngine;

public class Laser : MonoBehaviour
{
    public string attackerNick;
    private float laserLength = 15f;  // �⺻ ����
    public float duration = 1.0f;

    private LineRenderer lineRenderer;
    private BoxCollider box;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        box = GetComponent<BoxCollider>();
    }

    // �ܺο��� ���� ���� �����ϰ� �Լ� �߰�
    public void SetLength(float length)
    {
        laserLength = length;
    }

    void Start()
    {
        // �ݶ��̴� ũ�� ����
        if (box != null)
        {
            box.size = new Vector3(box.size.x, box.size.y, laserLength);
            box.center = new Vector3(0f, 0f, laserLength / 2f);
        }

        // ���� ������ ���� ����
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, transform.position + transform.forward * laserLength);

        // ������ ���� (�ʿ��ϸ�)
        Vector3 scale = transform.localScale;
        scale.z = laserLength;
        transform.localScale = scale;

        // ���� �ð� �� ����
        Destroy(gameObject, duration);
    }

    private void OnTriggerEnter(Collider other)
    {
        string myNickname = NetworkConnector.Instance.UserNickname;

        if (!this.name.StartsWith(myNickname + "_"))
            return;

        if (other.name.StartsWith("Character_"))
        {
            string hitPlayerName = other.name.Replace("Character_", "");
            if (hitPlayerName == myNickname)
                return;

            int weaponIndex = 6;
            string attackMsg = $"HIT|{weaponIndex}|{hitPlayerName}\n";
            byte[] bytes = Encoding.UTF8.GetBytes(attackMsg);
            NetworkConnector.Instance.Stream.Write(bytes, 0, bytes.Length);

            Debug.Log($"[Laser] {hitPlayerName} �Կ��� HIT �޽���(����:{weaponIndex}) ����");
        }
    }
}
