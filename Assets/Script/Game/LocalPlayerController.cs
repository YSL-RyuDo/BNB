using UnityEngine;
using System.Text;
using System.Threading.Tasks;

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
    void Start()
    {
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
    }


    Vector3 inputDirection = Vector3.zero;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            isWeaponMode = !isWeaponMode;
            Debug.Log("[LocalPlayerController] 무기 모드 " + (isWeaponMode ? "활성화" : "비활성화"));
        }

        // 입력만 받고, 이동은 FixedUpdate에서 처리
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        inputDirection = new Vector3(h, 0, v).normalized;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isWeaponMode)
            {
                // 무기 공격 (디버그 출력)
                if (!WeaponSystem.Instance.isCooldown)
                {
                    if (NetworkConnector.Instance.CurrentUserCharacterIndices.TryGetValue(myNick, out int idx))
                    {
                        if (idx == 0)
                        {
                            GameObject prefab = WeaponSystem.Instance.GetWeaponPrefab(idx);
                            if (prefab != null)
                            {
                                Vector3 attackPosition = transform.position + transform.forward * 0.8f + Vector3.up * 0.5f;
                                Quaternion attackRotation = transform.rotation;

                                GameObject sword = Instantiate(prefab, attackPosition, attackRotation);
                                sword.transform.parent = null;
                                sword.name = $"{myNick}_Sword";

                                string msg = $"WEAPON_ATTACK|{myNick}|{idx}|{attackPosition.x:F2},{attackPosition.y:F2},{attackPosition.z:F2}|{attackRotation.eulerAngles.y:F2}\n";
                                byte[] bytes = Encoding.UTF8.GetBytes(msg);
                                try
                                {
                                    NetworkConnector.Instance.Stream.WriteAsync(bytes, 0, bytes.Length);
                                    Debug.Log($"[Send] 공격 패킷 전송: {msg.Trim()}");
                                }
                                catch (System.Exception ex)
                                {
                                    Debug.LogError($"[Send] 공격 패킷 전송 실패: {ex.Message}");
                                }

                                WeaponSystem.Instance.StartCooldown(1.5f);
                                Debug.Log("[Sword Attack] 캐릭터 인덱스 0번 → 검 휘두름");
                            }
                        }
                        else if(idx == 1)
                        {
                            GameObject prefab = WeaponSystem.Instance.GetWeaponPrefab(idx);
                            if (prefab != null)
                            {
                                Vector3 attackPosition = transform.position + transform.forward * 1.5f + Vector3.up * 0.5f;
                                Quaternion attackRotation = transform.rotation;

                                GameObject arrow = Instantiate(prefab, attackPosition, attackRotation);

                                arrow.transform.parent = null;
                                arrow.name = $"{myNick}_Arrow";

                                string msg = $"WEAPON_ATTACK|{myNick}|{idx}|{attackPosition.x:F2},{attackPosition.y:F2},{attackPosition.z:F2}|{attackRotation.eulerAngles.y:F2}\n";
                                byte[] bytes = Encoding.UTF8.GetBytes(msg);
                                try
                                {
                                    NetworkConnector.Instance.Stream.WriteAsync(bytes, 0, bytes.Length);
                                    Debug.Log($"[Send] 공격 패킷 전송: {msg.Trim()}");
                                }
                                catch (System.Exception ex)
                                {
                                    Debug.LogError($"[Send] 공격 패킷 전송 실패: {ex.Message}");
                                }

                                WeaponSystem.Instance.StartCooldown(1.5f);
                            }
                        }
                        else if(idx ==2)
                        {
                            GameObject prefab = WeaponSystem.Instance.GetWeaponPrefab(idx);
                            if (prefab != null)
                            {
                                Vector3 attackPosition = transform.position + transform.forward * 1.5f + Vector3.up * 0.5f;
                                Quaternion attackRotation = transform.rotation;

                                GameObject spell = Instantiate(prefab, attackPosition, attackRotation);

                                spell.transform.parent = null;
                                spell.name = $"{myNick}_Spell";

                                string msg = $"WEAPON_ATTACK|{myNick}|{idx}|{attackPosition.x:F2},{attackPosition.y:F2},{attackPosition.z:F2}|{attackRotation.eulerAngles.y:F2}\n";
                                byte[] bytes = Encoding.UTF8.GetBytes(msg);
                                try
                                {
                                    NetworkConnector.Instance.Stream.WriteAsync(bytes, 0, bytes.Length);
                                    Debug.Log($"[Send] 공격 패킷 전송: {msg.Trim()}");
                                }
                                catch (System.Exception ex)
                                {
                                    Debug.LogError($"[Send] 공격 패킷 전송 실패: {ex.Message}");
                                }

                                WeaponSystem.Instance.StartCooldown(1.5f);
                            }
                        }
                        else if (idx == 3)
                        {
                            GameObject prefab = WeaponSystem.Instance.GetWeaponPrefab(idx);
                            if (prefab != null)
                            {
                                Vector3 startOffset = -transform.right * 1.5f + Vector3.up * 0.5f;
                                GameObject mace = Instantiate(prefab, transform.position + startOffset, Quaternion.identity);
                                mace.transform.parent = null;
                                mace.name = $"{myNick}_Mace";

                                Mace maceScript = mace.GetComponent<Mace>();
                                if (maceScript != null)
                                {
                                    maceScript.swingDuration = 0.5f;
                                    maceScript.targetTransform = transform; // 캐릭터 Transform 넘김
                                    maceScript.attackerNick = myNick;
                                }

                                string msg = $"WEAPON_ATTACK|{myNick}|{idx}|{transform.position.x:F2},{transform.position.y:F2},{transform.position.z:F2}|{transform.eulerAngles.y:F2}\n";
                                byte[] bytes = Encoding.UTF8.GetBytes(msg);
                                NetworkConnector.Instance.Stream.WriteAsync(bytes, 0, bytes.Length);

                                WeaponSystem.Instance.StartCooldown(1.5f);
                                Debug.Log("[Mace Attack] 캐릭터 인덱스 3번 → 원형 공격");
                            }
                        }
                        else if(idx==4)
                        {
                            GameObject prefab = WeaponSystem.Instance.GetWeaponPrefab(idx);
                            if (prefab != null)
                            {
                                Vector3 attackPosition = transform.position + transform.forward * 1.5f + Vector3.up * 0.5f;
                                Quaternion attackRotation = transform.rotation;

                                GameObject melody = Instantiate(prefab, attackPosition, attackRotation);

                                melody.transform.parent = null;
                                melody.name = $"{myNick}_Melody";

                                Melody melodyScript = melody.GetComponent<Melody>();
                                if (melodyScript != null)
                                {
                                    melodyScript.attackerNick = myNick;
                                }

                                string msg = $"WEAPON_ATTACK|{myNick}|{idx}|{attackPosition.x:F2},{attackPosition.y:F2},{attackPosition.z:F2}|{attackRotation.eulerAngles.y:F2}\n";
                                byte[] bytes = Encoding.UTF8.GetBytes(msg);
                                try
                                {
                                    NetworkConnector.Instance.Stream.WriteAsync(bytes, 0, bytes.Length);
                                    Debug.Log($"[Send] 공격 패킷 전송: {msg.Trim()}");
                                }
                                catch (System.Exception ex)
                                {
                                    Debug.LogError($"[Send] 공격 패킷 전송 실패: {ex.Message}");
                                }

                                WeaponSystem.Instance.StartCooldown(1.5f);
                            }
                        }
                        else if (idx == 5)
                        {
                            GameObject prefab = WeaponSystem.Instance.GetWeaponPrefab(idx);
                            if (prefab != null)
                            {
                                Vector3 spawnPosition = transform.position + transform.forward * 0.8f + Vector3.up * 0.5f;

                                Quaternion attackRotation = transform.rotation;
                                GameObject pitchfork = Instantiate(prefab, spawnPosition, attackRotation);
                                pitchfork.transform.parent = null;
                                pitchfork.name = $"{myNick}_Pitchfork";

                                Pitchfork pfScript = pitchfork.GetComponent<Pitchfork>();
                                if (pfScript != null)
                                {
                                    pfScript.attackerNick = myNick;

                                    Vector3 forward = transform.forward;
                                    forward.y = 0f;
                                    if (forward == Vector3.zero)
                                        forward = Vector3.forward;
                                    forward.Normalize();

                                    pfScript.targetRot = Quaternion.LookRotation(forward) * Quaternion.Euler(0f, -90f, 0f);
                                }

                                string msg = $"WEAPON_ATTACK|{myNick}|{idx}|{spawnPosition.x:F2},{spawnPosition.y:F2},{spawnPosition.z:F2}|{attackRotation.eulerAngles.y:F2}\n";
                                byte[] bytes = Encoding.UTF8.GetBytes(msg);
                                NetworkConnector.Instance.Stream.WriteAsync(bytes, 0, bytes.Length);

                                WeaponSystem.Instance.StartCooldown(2f);
                                Debug.Log("[Pitchfork Attack] 캐릭터 인덱스 5번 → 앞에서 생성 후 휘두름");
                            }
                        }
                        else if (idx == 6)
                        {
                            GameObject prefab = WeaponSystem.Instance.GetWeaponPrefab(idx);
                            if (prefab != null)
                            {
                                Vector3 spawnPosition = transform.position + transform.forward * 0.8f + Vector3.up * 0.5f;
                                Quaternion attackRotation = transform.rotation;

                                // 1. RaycastAll 해서 충돌 모두 검사, 캐릭터 충돌은 무시하고 벽, 블록, 바닥 중 최소 거리 찾기
                                float maxLength = 15f;
                                Vector3 direction = transform.forward;
                                RaycastHit[] hits = Physics.RaycastAll(spawnPosition, direction, maxLength);

                                float laserLength = maxLength;
                                foreach (var hit in hits)
                                {
                                    // 캐릭터 태그 무시
                                    if (hit.collider.CompareTag("Player"))
                                        continue;

                                    // 벽, 블록, 바닥 중 가장 가까운 충돌 거리 갱신
                                    if (hit.collider.CompareTag("Wall") || hit.collider.CompareTag("Block") || hit.collider.CompareTag("Ground"))
                                    {
                                        if (hit.distance < laserLength)
                                        {
                                            laserLength = hit.distance;
                                        }
                                    }
                                }

                                // 2. 레이저 프리팹 생성
                                GameObject scepterLaser = Instantiate(prefab, spawnPosition, attackRotation);
                                scepterLaser.transform.parent = null;
                                scepterLaser.name = $"{myNick}_Laser";

                                // 3. 레이저 길이와 공격자 닉네임 세팅
                                var laserScript = scepterLaser.GetComponent<Laser>();
                                if (laserScript != null)
                                {
                                    laserScript.attackerNick = myNick;
                                    laserScript.SetLength(laserLength);
                                }

                                // 4. 네트워크 공격 메시지 전송
                                string msg = $"WEAPON_ATTACK|{myNick}|{idx}|{spawnPosition.x:F2},{spawnPosition.y:F2},{spawnPosition.z:F2}|{attackRotation.eulerAngles.y:F2}|{laserLength:F2}\n";
                                byte[] bytes = Encoding.UTF8.GetBytes(msg);
                                try
                                {
                                    NetworkConnector.Instance.Stream.WriteAsync(bytes, 0, bytes.Length);
                                    Debug.Log($"[Send] 공격 패킷 전송: {msg.Trim()}");
                                }
                                catch (System.Exception ex)
                                {
                                    Debug.LogError($"[Send] 공격 패킷 전송 실패: {ex.Message}");
                                }

                                WeaponSystem.Instance.StartCooldown(2.0f);
                                Debug.Log("[Scepter Laser Attack] 캐릭터 인덱스 6번 → 레이저 발사");
                            }
                        }

                    }
                }
                else
                {
                    Debug.Log("[LocalPlayerController] 공격 쿨다운 중...");
                }
            }
            else
            {
                // 기존 물풍선 설치 로직
                if (!BalloonSystem.Instance.CanPlaceBalloon())
                    return;

                Vector3 pos = localCharacter.transform.position;
                float cellSize = 1.0f;
                float snappedX = Mathf.Round(pos.x / cellSize) * cellSize;
                float snappedZ = Mathf.Round(pos.z / cellSize) * cellSize;
                Vector3 snappedPos = new Vector3(snappedX, 0, snappedZ);

                if ((snappedPos - lastBalloonPos).sqrMagnitude < 0.01f)
                {
                    Debug.Log("[LocalPlayerController] 같은 위치에 중복 설치 방지");
                    return;
                }

                lastBalloonPos = snappedPos;

                int balloonType = BalloonSystem.Instance.GetCurrentBalloonType();

                string balloonMsg = $"PLACE_BALLOON|{myNick}|{snappedX:F2},{snappedZ:F2}|{balloonType}\n";
                byte[] bytes = Encoding.UTF8.GetBytes(balloonMsg);

                try
                {
                    NetworkConnector.Instance.Stream.WriteAsync(bytes, 0, bytes.Length);
                    Debug.Log($"[PlayerInput] 물풍선 설치 요청 전송: {balloonMsg.Trim()}");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[PlayerInput] 물풍선 설치 요청 실패: {ex.Message}");
                }
            }
        }
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
}
