using System;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.Threading.Tasks;
using System.Collections;


public class LocalPlayerController : MonoBehaviour
{
    private GameObject localCharacter;
    private Rigidbody rb;

    private Vector3 lastSentPosition;
    private float moveSpeed = 3.0f;

    private string myNick;
    private bool hasSentWaterHit = false;
    private Vector3 lastBalloonPos = Vector3.positiveInfinity;
    public bool isDead = false;
    public bool isWeaponMode = false;
    public int waterDamage = 10;

    public UnityEngine.UI.Button weaponButtonUI;
    public UnityEngine.UI.Button balloonButtonUI;

    Animator anim;

    [SerializeField] private float animDamp = 15f;
    [SerializeField] private float moveThreshold = 0.1f;

    private Dictionary<KeyCode, Action> keyDownActions = new();

    private bool pendingAttack = false;
    private int pendingWeaponIdx = -1;
    private bool spawnInvoked = false;


    void Start()
    {
        anim = GetComponent<Animator>();
        myNick = NetworkConnector.Instance.UserNickname;
        string characterObjectName = $"Character_{myNick}";

        // 이 스크립트가 붙은 게임오브젝트 이름이 내 캐릭터 이름이 아니면 꺼버림
        if (gameObject.name != characterObjectName)
        {
            this.enabled = false;
            return;
        }

        localCharacter = gameObject;  // 내 캐릭터 오브젝트

        rb = localCharacter.GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError($"Rigidbody 컴포넌트가 없음: {characterObjectName}");
            this.enabled = false;
            return;
        }

        lastSentPosition = localCharacter.transform.position;

        weaponButtonUI = GameObject.Find("WeaponButton")?.GetComponent<UnityEngine.UI.Button>();
        balloonButtonUI = GameObject.Find("BalloonButton")?.GetComponent<UnityEngine.UI.Button>();

        UpdateButtonVisuals();

        keyDownActions[KeyCode.LeftShift] = HandleToggleInput;
        keyDownActions[KeyCode.Space] = HandleAttackOrBalloonInput;
    }


    Vector3 inputDirection = Vector3.zero;

    void Update()
    {
        foreach (var pair in keyDownActions)
        {
            if (Input.GetKeyDown(pair.Key))
            {
                pair.Value?.Invoke();
            }
        }

        // 입력만 받고, 이동은 FixedUpdate에서 처리
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");


        inputDirection = new Vector3(h, 0, v).normalized;
    }

    void FixedUpdate()
    {
        if (localCharacter == null || rb == null)
            return;

        if (inputDirection.magnitude > 0.1f)
        {
            Vector3 targetPos = rb.position + inputDirection * moveSpeed * Time.fixedDeltaTime;

            rb.MovePosition(targetPos);

            Quaternion targetRot = Quaternion.LookRotation(inputDirection);
            rb.MoveRotation(targetRot);

            TrySendPosition();
        }

        if(anim != null)
        {
            bool isMoving = inputDirection.magnitude > moveThreshold;

            anim.SetBool("isWalk", isMoving);
        }
    }

    private void HandleToggleInput()
    {
        isWeaponMode = !isWeaponMode;
        Debug.Log("[LocalPlayerController] 무기 모드 " + (isWeaponMode ? "활성화" : "비활성화"));

        UpdateButtonVisuals();
    }

    //private void HandleAttackOrBalloonInput()
    //{
    //    if (isWeaponMode)
    //    {
    //        // 무기 공격 (디버그 출력)
    //        if (!WeaponSystem.Instance.isCooldown)
    //        {
    //            if (NetworkConnector.Instance.CurrentUserCharacterIndices.TryGetValue(myNick, out int idx))
    //            {
    //                pendingAttack = true;
    //                pendingWeaponIdx = idx;

    //                if (anim != null)
    //                {
    //                    anim.SetTrigger("isAttack");
    //                }            
    //            }
    //        }
    //        else
    //        {
    //            Debug.Log("[LocalPlayerController] 공격 쿨다운 중...");
    //        }

    //    }
    //    else
    //    {
    //        // 기존 물풍선 설치 로직
    //        if (!BalloonSystem.Instance.CanPlaceBalloon())
    //            return;

    //        Vector3 pos = localCharacter.transform.position;
    //        float cellSize = 1.0f;
    //        float snappedX = Mathf.Round(pos.x / cellSize) * cellSize;
    //        float snappedZ = Mathf.Round(pos.z / cellSize) * cellSize;
    //        Vector3 snappedPos = new Vector3(snappedX, 0, snappedZ);

    //        //if ((snappedPos - lastBalloonPos).sqrMagnitude < 0.01f)
    //        //{
    //        //    Debug.Log("[LocalPlayerController] 같은 위치에 중복 설치 방지");
    //        //    return;
    //        //}

    //        lastBalloonPos = snappedPos;

    //        int balloonType = BalloonSystem.Instance.GetCurrentBalloonType();

    //        string balloonMsg = $"PLACE_BALLOON|{myNick}|{snappedX:F2},{snappedZ:F2}|{balloonType}\n";
    //        byte[] bytes = Encoding.UTF8.GetBytes(balloonMsg);

    //        try
    //        {
    //            NetworkConnector.Instance.Stream.WriteAsync(bytes, 0, bytes.Length);
    //            Debug.Log($"[PlayerInput] 물풍선 설치 요청 전송: {balloonMsg.Trim()}");
    //        }
    //        catch (System.Exception ex)
    //        {
    //            Debug.LogError($"[PlayerInput] 물풍선 설치 요청 실패: {ex.Message}");
    //        }
    //    }

    //}

    private void HandleAttackOrBalloonInput()
    {
        if (isWeaponMode)
        {
            if (WeaponSystem.Instance.isCooldown)
                return;

            if (!NetworkConnector.Instance.CurrentUserCharacterIndices
                .TryGetValue(myNick, out int idx))
                return;

            // WeaponSystem.Instance.StartCooldown(2.0f);           

            //anim?.SetTrigger("isAttack");
            //spawnInvoked = false;
            //Invoke(nameof(FallbackSpawnWeapon), 0.15f);

            Vector3 attackPos =
                transform.position + transform.forward * 0.8f + Vector3.up * 0.5f;
            float rotY = transform.eulerAngles.y;

            // ===== 멜로디 전용 처리 =====
            //if (idx == 4)
            //{
            //    string msg =
            //        $"MELODY_SPAWN|{myNick}|{attackPos.x:F2},{attackPos.y:F2},{attackPos.z:F2}|{rotY:F2}\n";

            //    byte[] bytes = Encoding.UTF8.GetBytes(msg);
            //    NetworkConnector.Instance.Stream.WriteAsync(bytes, 0, bytes.Length);
            //    return;
            //}

            // ===== 레이저 =====
            if (idx == 6)
            {
                float maxLength = 15f;
                float laserLength = maxLength;

                RaycastHit[] hits = Physics.RaycastAll(
                    attackPos,
                    transform.forward,
                    maxLength
                );

                foreach (var hit in hits)
                {
                    if (hit.collider.CompareTag("Player"))
                        continue;

                    if (hit.collider.CompareTag("Wall") ||
                        hit.collider.CompareTag("Block") ||
                        hit.collider.CompareTag("Ground"))
                    {
                        laserLength = Mathf.Min(laserLength, hit.distance);
                    }
                }

                SendWeaponAttackPacket(idx, attackPos, rotY, laserLength);
            }
            else
            {
                // ===== 일반 무기 =====
                SendWeaponAttackPacket(idx, attackPos, rotY);
            }
        }
        else
        {
            // ===== 물풍선 =====
            if (!BalloonSystem.Instance.CanPlaceBalloon())
                return;

            Vector3 pos = localCharacter.transform.position;
            float cellSize = 1.0f;

            float snappedX = Mathf.Round(pos.x / cellSize) * cellSize;
            float snappedZ = Mathf.Round(pos.z / cellSize) * cellSize;

            int balloonType = BalloonSystem.Instance.GetCurrentBalloonType();

            string balloonMsg =
                $"PLACE_BALLOON|{myNick}|{snappedX:F2},{snappedZ:F2}|{balloonType}\n";

            byte[] bytes = Encoding.UTF8.GetBytes(balloonMsg);
            NetworkConnector.Instance.Stream.WriteAsync(bytes, 0, bytes.Length);
        }
    }

    public void AnimEvent_SpawnWeapon()
    {
        spawnInvoked = true;
        WeaponSystem.Instance.SpawnCachedWeapon(gameObject.name);
    }

    private void FallbackSpawnWeapon()
    {
        if (spawnInvoked)
            return; // 애니메이션 이벤트로 이미 처리됨

        Debug.Log("[FallbackSpawnWeapon] 애니메이션 없음 → 강제 무기 생성");
        WeaponSystem.Instance.SpawnCachedWeapon(gameObject.name);
    }

    //public void AnimEvent_SpawnWeapon()
    //{
    //    if (!pendingAttack) return;
    //    if (WeaponSystem.Instance.isCooldown) return; 

    //    int idx = pendingWeaponIdx;
    //    pendingAttack = false;
    //    pendingWeaponIdx = -1;

    //    if (idx < 0) return;

    //    if (idx == 0)
    //    {
    //        GameObject prefab = WeaponSystem.Instance.GetWeaponPrefab(idx);
    //        if (prefab != null)
    //        {
    //            Vector3 attackPosition = transform.position + transform.forward * 0.8f + Vector3.up * 0.5f;
    //            Quaternion attackRotation = transform.rotation;

    //            GameObject sword = Instantiate(prefab, attackPosition, attackRotation);
    //            sword.transform.parent = null;
    //            sword.name = $"{myNick}_Sword";

    //            SendWeaponAttackPacket(idx, attackPosition, attackRotation.eulerAngles.y);

    //            WeaponSystem.Instance.StartCooldown(1.5f);
    //            Debug.Log("[Sword Attack] 캐릭터 인덱스 0번 → 검 휘두름");
    //        }
    //    }
    //    else if (idx == 1)
    //    {
    //        GameObject prefab = WeaponSystem.Instance.GetWeaponPrefab(idx);
    //        if (prefab != null)
    //        {
    //            Vector3 attackPosition = transform.position + transform.forward * 1.5f + Vector3.up * 0.5f;
    //            Quaternion attackRotation = transform.rotation;

    //            GameObject arrow = Instantiate(prefab, attackPosition, attackRotation);

    //            arrow.transform.parent = null;
    //            arrow.name = $"{myNick}_Arrow";

    //            SendWeaponAttackPacket(idx, attackPosition, attackRotation.eulerAngles.y);

    //            WeaponSystem.Instance.StartCooldown(1.5f);
    //        }
    //    }
    //    else if (idx == 2)
    //    {
    //        GameObject prefab = WeaponSystem.Instance.GetWeaponPrefab(idx);
    //        if (prefab != null)
    //        {
    //            Vector3 attackPosition = transform.position + transform.forward * 1.5f + Vector3.up * 0.5f;
    //            Quaternion attackRotation = transform.rotation;

    //            GameObject spell = Instantiate(prefab, attackPosition, attackRotation);

    //            spell.transform.parent = null;
    //            spell.name = $"{myNick}_Spell";

    //            SendWeaponAttackPacket(idx, attackPosition, attackRotation.eulerAngles.y);

    //            WeaponSystem.Instance.StartCooldown(1.5f);
    //        }
    //    }
    //    else if (idx == 3)
    //    {
    //        GameObject prefab = WeaponSystem.Instance.GetWeaponPrefab(idx);
    //        if (prefab != null)
    //        {
    //            Vector3 startOffset = -transform.right * 1.5f + Vector3.up * 0.5f;
    //            GameObject mace = Instantiate(prefab, transform.position + startOffset, Quaternion.identity);
    //            mace.transform.parent = null;
    //            mace.name = $"{myNick}_Mace";

    //            Mace maceScript = mace.GetComponent<Mace>();
    //            if (maceScript != null)
    //            {
    //                maceScript.swingDuration = 0.5f;
    //                maceScript.targetTransform = transform; // 캐릭터 Transform 넘김
    //                maceScript.attackerNick = myNick;
    //            }

    //            SendWeaponAttackPacket(idx, transform.position, transform.eulerAngles.y);

    //            WeaponSystem.Instance.StartCooldown(1.5f);
    //            Debug.Log("[Mace Attack] 캐릭터 인덱스 3번 → 원형 공격");
    //        }
    //    }
    //    else if (idx == 4)
    //    {
    //        GameObject prefab = WeaponSystem.Instance.GetWeaponPrefab(idx);
    //        if (prefab != null)
    //        {
    //            Vector3 attackPosition = transform.position + transform.forward * 1.5f + Vector3.up * 0.5f;
    //            Quaternion attackRotation = transform.rotation;

    //            GameObject melody = Instantiate(prefab, attackPosition, attackRotation);

    //            melody.transform.parent = null;
    //            melody.name = $"{myNick}_Melody";

    //            Melody melodyScript = melody.GetComponent<Melody>();
    //            if (melodyScript != null)
    //            {
    //                melodyScript.attackerNick = myNick;
    //            }

    //            SendWeaponAttackPacket(idx, attackPosition, attackRotation.eulerAngles.y);

    //            WeaponSystem.Instance.StartCooldown(1.5f);
    //        }
    //    }
    //    else if (idx == 5)
    //    {
    //        GameObject prefab = WeaponSystem.Instance.GetWeaponPrefab(idx);
    //        if (prefab != null)
    //        {
    //            Vector3 spawnPosition = transform.position + transform.forward * 0.8f + Vector3.up * 0.5f;

    //            Quaternion attackRotation = transform.rotation;
    //            GameObject pitchfork = Instantiate(prefab, spawnPosition, attackRotation);
    //            pitchfork.transform.parent = null;
    //            pitchfork.name = $"{myNick}_Pitchfork";

    //            Pitchfork pfScript = pitchfork.GetComponent<Pitchfork>();
    //            if (pfScript != null)
    //            {
    //                pfScript.attackerNick = myNick;

    //                Vector3 forward = transform.forward;
    //                forward.y = 0f;
    //                if (forward == Vector3.zero)
    //                    forward = Vector3.forward;
    //                forward.Normalize();

    //                pfScript.targetRot = Quaternion.LookRotation(forward) * Quaternion.Euler(0f, -90f, 0f);
    //            }

    //            SendWeaponAttackPacket(idx, spawnPosition, transform.eulerAngles.y);

    //            WeaponSystem.Instance.StartCooldown(2f);
    //            Debug.Log("[Pitchfork Attack] 캐릭터 인덱스 5번 → 앞에서 생성 후 휘두름");
    //        }
    //    }
    //    else if (idx == 6)
    //    {
    //        GameObject prefab = WeaponSystem.Instance.GetWeaponPrefab(idx);
    //        if (prefab != null)
    //        {
    //            Vector3 spawnPosition = transform.position + transform.forward * 0.8f + Vector3.up * 0.5f;
    //            Quaternion attackRotation = transform.rotation;

    //            // 1. RaycastAll 해서 충돌 모두 검사, 캐릭터 충돌은 무시하고 벽, 블록, 바닥 중 최소 거리 찾기
    //            float maxLength = 15f;
    //            Vector3 direction = transform.forward;
    //            RaycastHit[] hits = Physics.RaycastAll(spawnPosition, direction, maxLength);

    //            float laserLength = maxLength;
    //            foreach (var hit in hits)
    //            {
    //                // 캐릭터 태그 무시
    //                if (hit.collider.CompareTag("Player"))
    //                    continue;

    //                // 벽, 블록, 바닥 중 가장 가까운 충돌 거리 갱신
    //                if (hit.collider.CompareTag("Wall") || hit.collider.CompareTag("Block") || hit.collider.CompareTag("Ground"))
    //                {
    //                    if (hit.distance < laserLength)
    //                    {
    //                        laserLength = hit.distance;
    //                    }
    //                }
    //            }

    //            // 2. 레이저 프리팹 생성
    //            GameObject scepterLaser = Instantiate(prefab, spawnPosition, attackRotation);
    //            scepterLaser.transform.parent = null;
    //            scepterLaser.name = $"{myNick}_Laser";

    //            // 3. 레이저 길이와 공격자 닉네임 세팅
    //            var laserScript = scepterLaser.GetComponent<Laser>();
    //            if (laserScript != null)
    //            {
    //                laserScript.attackerNick = myNick;
    //                laserScript.SetLength(laserLength);
    //            }

    //            // 4. 네트워크 공격 메시지 전송
    //            SendWeaponAttackPacket(idx, spawnPosition, attackRotation.eulerAngles.y, laserLength);

    //            WeaponSystem.Instance.StartCooldown(2.0f);
    //            Debug.Log("[Scepter Laser Attack] 캐릭터 인덱스 6번 → 레이저 발사");
    //        }
    //    }
    //}


    private async void SendWeaponAttackPacket(int idx, Vector3 pos, float rotationY, float? extraValue = null)
    {
        string msg;

        if (extraValue.HasValue)
        {
            // 레이저용 등 추가 데이터 포함
            msg = $"WEAPON_ATTACK|{myNick}|{idx}|{pos.x:F2},{pos.y:F2},{pos.z:F2}|{rotationY:F2}|{extraValue.Value:F2}\n";
        }
        else
        {
            // 일반 무기 공격
            msg = $"WEAPON_ATTACK|{myNick}|{idx}|{pos.x:F2},{pos.y:F2},{pos.z:F2}|{rotationY:F2}\n";
        }

        byte[] bytes = Encoding.UTF8.GetBytes(msg);

        try
        {
            await NetworkConnector.Instance.Stream.WriteAsync(bytes, 0, bytes.Length);
            Debug.Log($"[Send] 공격 패킷 전송: {msg.Trim()}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Send] 공격 패킷 전송 실패: {ex.Message}");
        }
    }

    private async void TrySendPosition()
    {
        Vector3 currentPos = localCharacter.transform.position;

        lastSentPosition = currentPos;

        string posStr = $"{currentPos.x:F2},{currentPos.z:F2}";
        string msg = $"MOVE|{myNick}|{posStr}\n";

        byte[] bytes = Encoding.UTF8.GetBytes(msg);

        try
        {
            var stream = NetworkConnector.Instance.Stream;
            if (stream != null && stream.CanWrite)
            {
                await stream.WriteAsync(bytes, 0, bytes.Length);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"위치 전송 실패: {e.Message}");
        }
    }

    private async void OnTriggerEnter(Collider other)
    {
        if (localCharacter == null || hasSentWaterHit)
            return;

        if (other.gameObject.CompareTag("Water"))
        {
            if (this.gameObject == localCharacter)
            {
                hasSentWaterHit = true;
                Debug.Log("[WaterHitDetector] 내 캐릭터가 물에 맞음! 패킷 전송");
                
                string msg = $"WATER_HIT|{myNick}|{waterDamage}\n";
                byte[] bytes = Encoding.UTF8.GetBytes(msg);
                await NetworkConnector.Instance.Stream.WriteAsync(bytes, 0, bytes.Length);

                Debug.Log("[WaterHitDetector] WATER_HIT 패킷 전송 완료");

                ResetHitFlagAfterDelay(2f);
            }
        }
    }

    private async void ResetHitFlagAfterDelay(float delaySeconds)
    {
        await Task.Delay((int)(delaySeconds * 1000));
        hasSentWaterHit = false;
        Debug.Log("[WaterHitDetector] hasSentWaterHit 플래그 초기화됨");
    }

    private void UpdateButtonVisuals()
    {
        if (weaponButtonUI != null)
        {
            var color = weaponButtonUI.image.color;
            color.a = isWeaponMode ? 1f : 0.4f; // 무기 모드면 선명
            weaponButtonUI.image.color = color;
        }

        if (balloonButtonUI != null)
        {
            var color = balloonButtonUI.image.color;
            color.a = isWeaponMode ? 0.4f : 1f; // 풍선 모드면 선명
            balloonButtonUI.image.color = color;
        }
    }

    public void InvokeSpawnFallbackFromNetwork(string attackerNick, float delay)
    {
        StartCoroutine(SpawnFallbackRoutine(attackerNick, delay));
    }

    private IEnumerator SpawnFallbackRoutine(string attackerNick, float delay)
    {
        if (attackerNick == NetworkConnector.Instance.UserNickname)
            yield break;

        yield return new WaitForSeconds(delay);

        // 애니메이션 이벤트가 호출되지 않았을 때만 실행
        if (!spawnInvoked)
        {
            Debug.Log($"[Fallback] AnimEvent 없음 → {attackerNick} 무기 생성");
            WeaponSystem.Instance.SpawnCachedWeaponIfExists(attackerNick);
        }
    }

}
