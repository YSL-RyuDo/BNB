using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class Sword : MonoBehaviour
{
    public float swingAngle = 120f; // �� ȸ�� ���� (��: 10��~2�� �� 120��)
    public float swingDuration = 0.3f; // ȸ�� �ð�
    private float elapsed = 0f;
    private Quaternion initialRot;
    private Quaternion targetRot;

    void Start()
    {
        // ���� ��ǥ ���� �ʱ� ȸ�� ���
        Quaternion baseRot = transform.rotation;

        // baseRot�� �߽����� -swingAngle/2 ��ŭ Y�� ȸ���Ѱ� ���� ����
        initialRot = Quaternion.Euler(0f, -swingAngle / 2f, 0f) * baseRot;
        // baseRot�� �߽����� +swingAngle/2 ��ŭ Y�� ȸ���Ѱ� �� ����
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
            Destroy(gameObject); // ���� ������ ����
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        string myNickname = NetworkConnector.Instance.UserNickname;

        // ���� ������ �̸��� "{������}_Sword" ������ ��
        if (!this.name.StartsWith(myNickname + "_"))
            return; // ���� ���� ���Ⱑ �ƴϸ� ���� (�ڱ� ����θ� ������)

        if (other.name.StartsWith("Character_"))
        {
            string hitPlayerName = other.name.Replace("Character_", "");

            if (hitPlayerName == myNickname)
                return; // �ڱ� �ڽ� ���� �Ÿ� ����

            int weaponIndex = 0; // ���̶�� 0
            string attackMsg = $"HIT|{weaponIndex}|{hitPlayerName}\n";
            byte[] bytes = Encoding.UTF8.GetBytes(attackMsg);
            NetworkConnector.Instance.Stream.Write(bytes, 0, bytes.Length);

            Debug.Log($"[Sword] {hitPlayerName} �Կ��� HIT �޽���(����:{weaponIndex}) ����");
        }
    }

}
