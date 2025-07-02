using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    private Rigidbody rigid;
    private Vector3 moveDirection;

    public GameObject bombPrefab;

    private bool isWeaponMode = false;

    public void Awake()
    {
        rigid = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        InputManager.Instance.moveInput += HandleMoveInput;
        InputManager.Instance.bombInput += HandleBombOrAttack;
        InputManager.Instance.changeModeInput += HandleModeChange;
    }

    private void OnDisable()
    {
        InputManager.Instance.moveInput -= HandleMoveInput;
        InputManager.Instance.bombInput -= HandleBombOrAttack;
        InputManager.Instance.changeModeInput -= HandleModeChange;
    }

    void HandleMoveInput(Vector2 input)
    {
        moveDirection = new Vector3(input.x, 0, input.y).normalized;
    }

    void HandleModeChange(bool newMode)
    {
        isWeaponMode = newMode;
        Debug.Log(isWeaponMode ? "���� ���� ��ȯ" : "��ź ���� ��ȯ");
    }

    void HandleBombOrAttack()
    {
        if (isWeaponMode)
        {
            Debug.Log("�⺻ ���� ����");
        }
        else
        {
            Debug.Log("��ź ��ġ");
            // ��ź ������Ʈ Ǯ���� ����
            ObjectPoolManager.Instance.Get("Bomb", transform.position, Quaternion.identity);
        }
    }


    private void FixedUpdate()
    {
        rigid.velocity = moveDirection * moveSpeed;
    }
}
