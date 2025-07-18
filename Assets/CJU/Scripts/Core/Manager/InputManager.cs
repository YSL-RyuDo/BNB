using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance;

    public event Action<Vector2> moveInput;
    public event Action bombInput;
    public event Action<bool> changeModeInput;

    private bool isWeaponMode = false;


    private void Awake()
    {
        if(Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        Vector2 move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        moveInput?.Invoke(move);

        if (Input.GetKeyDown(KeyCode.U))
        {
            isWeaponMode = !isWeaponMode;
            changeModeInput?.Invoke(isWeaponMode);
        }

        if (Input.GetKeyDown(KeyCode.Space))
            bombInput?.Invoke();

    }
}
