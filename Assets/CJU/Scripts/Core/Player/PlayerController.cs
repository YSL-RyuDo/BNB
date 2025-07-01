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

    public void Awake()
    {
        rigid = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        InputManager.Instance.moveInput += HandleMoveInput;
        InputManager.Instance.bombInput += HandleBombInput;
    }

    private void OnDisable()
    {
        InputManager.Instance.moveInput -= HandleMoveInput;
        InputManager.Instance.bombInput -= HandleBombInput;
    }

    void HandleMoveInput(Vector2 input)
    {
        moveDirection = new Vector3(input.x, 0, input.y).normalized;
    }

    void HandleBombInput()
    {
        Debug.Log("ÆøÅº ¼³Ä¡");
        Instantiate(bombPrefab, this.transform.position, Quaternion.identity);
    }

    private void FixedUpdate()
    {
        rigid.velocity = moveDirection * moveSpeed;
    }
}
