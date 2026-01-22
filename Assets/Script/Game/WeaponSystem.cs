using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeaponSystem : MonoBehaviour
{
    public static WeaponSystem Instance;

    [Header("UI")]
    public Button weaponButton;
    public Sprite[] weaponImageArray;

    [Header("Weapon Prefabs")]
    public GameObject[] weaponPrefabs;

    public bool isCooldown = false;

    struct PendingAttack
    {
        public string attackerNick;
        public int charIndex;
        public Vector3 position;
        public Quaternion rotation;
        public float extra; // 레이저 길이, 없으면 -1
    }

    private Dictionary<string, PendingAttack> pendingAttacks = new();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        string myNick = NetworkConnector.Instance.UserNickname;

        if (!NetworkConnector.Instance.CurrentUserCharacterIndices
            .TryGetValue(myNick, out int charIndex))
            charIndex = 0;

        if (weaponButton != null && weaponImageArray != null)
        {
            if (charIndex >= 0 && charIndex < weaponImageArray.Length)
            {
                weaponButton.image.sprite = weaponImageArray[charIndex];
            }
            else
            {
                Debug.LogWarning(
                    $"[WeaponSystem] 캐릭터 인덱스 {charIndex}가 weaponImageArray 범위를 벗어남");
            }
        }
    }

    public GameObject GetWeaponPrefab(int index)
    {
        if (index >= 0 && index < weaponPrefabs.Length)
            return weaponPrefabs[index];

        Debug.LogWarning($"[WeaponSystem] 무기 프리팹 없음: {index}");
        return null;
    }

    public void CachePendingAttack(
        string attackerNick,
        int charIndex,
        Vector3 position,
        Quaternion rotation,
        float extra = -1f)
    {
        pendingAttacks[attackerNick] = new PendingAttack
        {
            attackerNick = attackerNick,
            charIndex = charIndex,
            position = position,
            rotation = rotation,
            extra = extra
        };
    }

    public void SpawnCachedWeapon(string characterObjectName)
    {
        string attackerNick = characterObjectName.Replace("Character_", "");

        if (!pendingAttacks.TryGetValue(attackerNick, out var data))
            return;

        if (data.charIndex == 6)
        {
            HandleRemoteLaserAttack(
                data.attackerNick,
                data.charIndex,
                data.position,
                data.rotation,
                data.extra
            );
        }
        else
        {
            HandleRemoteWeaponAttack(
                data.attackerNick,
                data.charIndex,
                data.position,
                data.rotation
            );
        }

        pendingAttacks.Remove(attackerNick);
    }

    public void HandleRemoteWeaponAttack(
        string attackerNick,
        int charIndex,
        Vector3 position,
        Quaternion rotation)
    {
        Debug.Log($"[SPAWN CALL] attacker={attackerNick}, idx={charIndex}\n{System.Environment.StackTrace}");

        GameObject prefab = GetWeaponPrefab(charIndex);
        if (prefab == null)
            return;
        
        GameObject weaponObj = Instantiate(prefab, position, rotation);
        weaponObj.transform.parent = null;
        Debug.Log($"[DEBUG][Spawn] weapon={charIndex}, name={weaponObj.name}, pos={weaponObj.transform.position}, rot={weaponObj.transform.eulerAngles}");
        //Debug.Break();

        GameObject attackerObj = GameObject.Find($"Character_{attackerNick}");

        switch (charIndex)
        {
            case 0:
                weaponObj.name = $"{attackerNick}_Sword";
                break;

            case 1:
                weaponObj.name = $"{attackerNick}_Arrow";
                break;

            case 2:
                weaponObj.name = $"{attackerNick}_Spell";
                break;

            case 3:
                weaponObj.name = $"{attackerNick}_Mace";
                var mace = weaponObj.GetComponent<Mace>();
                if (mace != null && attackerObj != null)
                {
                    mace.attackerNick = attackerNick;
                    mace.targetTransform = attackerObj.transform;
                }
                break;
            case 5:
                weaponObj.name = $"{attackerNick}_Pitchfork";
                if (attackerObj != null)
                {
                    weaponObj.transform.position =
                        attackerObj.transform.position
                        + attackerObj.transform.forward * 1.3f
                        + Vector3.up * 0.4f;
                }
                break;
        }

        if (attackerNick == NetworkConnector.Instance.UserNickname)
            StartCooldown(1.5f);
    }

    public void HandleRemoteLaserAttack(
        string attackerNick,
        int charIndex,
        Vector3 position,
        Quaternion rotation,
        float laserLength)
    {
        GameObject prefab = GetWeaponPrefab(charIndex);
        if (prefab == null)
            return;

        GameObject laserObj = Instantiate(prefab, position, rotation);
        laserObj.transform.parent = null;
        laserObj.name = $"{attackerNick}_Laser";

        Laser laser = laserObj.GetComponent<Laser>();
        if (laser != null)
        {
            laser.attackerNick = attackerNick;
            laser.SetLength(laserLength);
        }

        if (attackerNick == NetworkConnector.Instance.UserNickname)
            StartCooldown(2.0f);
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

        yield return new WaitForSeconds(duration);

        if (weaponButton != null)
            weaponButton.interactable = true;

        isCooldown = false;
    }

    public void SpawnMelodyFromServer(
    string attackerNick,
    Vector3 pos,
    Quaternion rot)
    {
        GameObject prefab = GetWeaponPrefab(4);
        if (prefab == null) return;

        GameObject obj = Instantiate(prefab, pos, rot);
        obj.name = $"{attackerNick}_Melody";

        Melody melody = obj.GetComponent<Melody>();
        if (melody != null)
            melody.attackerNick = attackerNick;
    }

    public void InvokeSpawnFallback(string attackerNick, float delay)
    {
        StartCoroutine(SpawnFallbackRoutine(attackerNick, delay));
    }

    private IEnumerator SpawnFallbackRoutine(string attackerNick, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (pendingAttacks.ContainsKey(attackerNick))
        {
            Debug.Log($"[Fallback] {attackerNick} 무기 강제 생성");
            SpawnCachedWeaponIfExists(attackerNick);
        }
    }

    public void SpawnCachedWeaponIfExists(string attackerNick)
    {
        if (!pendingAttacks.TryGetValue(attackerNick, out var data))
            return;

        if (data.charIndex == 6)
        {
            HandleRemoteLaserAttack(
                data.attackerNick,
                data.charIndex,
                data.position,
                data.rotation,
                data.extra
            );
        }
        else
        {
            HandleRemoteWeaponAttack(
                data.attackerNick,
                data.charIndex,
                data.position,
                data.rotation
            );
        }

        pendingAttacks.Remove(attackerNick);
    }
}
