using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeaponSystem : MonoBehaviour
{
    public static WeaponSystem Instance;

    public Button weaponButton;
    public Sprite[] weaponImageArray;
    public GameObject[] weaponPrefabs;
    public bool isCooldown = false;
    HashSet<string> hitPlayers = new HashSet<string>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void Start()
    {
        string myNickname = NetworkConnector.Instance.UserNickname;

        int charIndex = 0;
        NetworkConnector.Instance.CurrentUserCharacterIndices.TryGetValue(myNickname, out charIndex);

        // ��ư �̹��� ����
        if (weaponButton != null && weaponImageArray != null)
        {
            if (charIndex >= 0 && charIndex < weaponImageArray.Length)
            {
                weaponButton.image.sprite = weaponImageArray[charIndex];
            }
            else
            {
                Debug.LogWarning($"[WeaponSystem] ĳ���� �ε��� {charIndex}�� weaponImageArray ������ �ʰ���");
            }
        }
    }

    public GameObject GetWeaponPrefab(int index)
    {
        if (index >= 0 && index < weaponPrefabs.Length)
        {
            return weaponPrefabs[index];
        }

        Debug.LogWarning($"[WeaponSystem] �ε��� {index}�� �ش��ϴ� ���� �������� ����");
        return null;
    }

    public void StartCooldown(float duration)
    {
        if (!isCooldown)
            StartCoroutine(CooldownRoutine(duration));
    }

    private IEnumerator CooldownRoutine(float duration)
    {
        isCooldown = true;
        if (weaponButton != null)
            weaponButton.interactable = false;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float fillAmount = 1f - (elapsed / duration);  // ���� �ð� ����

            if (weaponButton != null)
            {
                Transform mask = weaponButton.transform.Find("CooldownMask");
                if (mask != null)
                {
                    Image maskImg = mask.GetComponent<Image>();
                    if (maskImg != null)
                    {
                        maskImg.fillAmount = fillAmount;
                        maskImg.enabled = true;
                    }
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (weaponButton != null)
        {
            weaponButton.interactable = true;

            Transform mask = weaponButton.transform.Find("CooldownMask");
            if (mask != null)
            {
                Image maskImg = mask.GetComponent<Image>();
                if (maskImg != null)
                {
                    maskImg.fillAmount = 0f;
                    maskImg.enabled = false;
                }
            }
        }

        isCooldown = false;
    }

    public void HandleRemoteWeaponAttack(string attackerNick, int charIndex, Vector3 position, Quaternion rotation)
    {
        GameObject prefab = GetWeaponPrefab(charIndex);
        if (prefab == null)
        {
            Debug.LogWarning("[WeaponSystem] ���� ������ ����, charIndex: " + charIndex);
            return;
        }

        GameObject weaponObj = Instantiate(prefab, position, rotation);
        weaponObj.transform.parent = null;

        string weaponName = "";

        if (charIndex == 0) // ���̸�
            weaponName = $"{attackerNick}_Sword";

        weaponObj.name = weaponName;
        Debug.Log($"[WeaponSystem] ���� ����: {attackerNick}, ĳ���� {charIndex} ���� ����");
    }
}
