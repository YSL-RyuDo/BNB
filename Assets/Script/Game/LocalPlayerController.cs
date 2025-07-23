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

        // �� ��ũ��Ʈ�� ���� ���ӿ�����Ʈ �̸��� �� ĳ���� �̸��� �ƴϸ� ������
        if (gameObject.name != characterObjectName)
        {
            this.enabled = false;
            return;
        }

        localCharacter = gameObject;  // �� ĳ���� ������Ʈ

        rb = localCharacter.GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError($"Rigidbody ������Ʈ�� ����: {characterObjectName}");
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
            Debug.Log("[LocalPlayerController] ���� ��� " + (isWeaponMode ? "Ȱ��ȭ" : "��Ȱ��ȭ"));
        }

        // �Է¸� �ް�, �̵��� FixedUpdate���� ó��
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        inputDirection = new Vector3(h, 0, v).normalized;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isWeaponMode)
            {
                // ���� ���� (����� ���)
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
                                    Debug.Log($"[Send] ���� ��Ŷ ����: {msg.Trim()}");
                                }
                                catch (System.Exception ex)
                                {
                                    Debug.LogError($"[Send] ���� ��Ŷ ���� ����: {ex.Message}");
                                }

                                WeaponSystem.Instance.StartCooldown(1.5f);
                                Debug.Log("[Sword Attack] ĳ���� �ε��� 0�� �� �� �ֵθ�");
                            }
                        }
                    }
                }
                else
                {
                    Debug.Log("[LocalPlayerController] ���� ��ٿ� ��...");
                }
            }
            else
            {
                // ���� ��ǳ�� ��ġ ����
                if (!BalloonSystem.Instance.CanPlaceBalloon())
                    return;

                Vector3 pos = localCharacter.transform.position;
                float cellSize = 1.0f;
                float snappedX = Mathf.Round(pos.x / cellSize) * cellSize;
                float snappedZ = Mathf.Round(pos.z / cellSize) * cellSize;
                Vector3 snappedPos = new Vector3(snappedX, 0, snappedZ);

                if ((snappedPos - lastBalloonPos).sqrMagnitude < 0.01f)
                {
                    Debug.Log("[LocalPlayerController] ���� ��ġ�� �ߺ� ��ġ ����");
                    return;
                }

                lastBalloonPos = snappedPos;

                int balloonType = BalloonSystem.Instance.GetCurrentBalloonType();

                string balloonMsg = $"PLACE_BALLOON|{myNick}|{snappedX:F2},{snappedZ:F2}|{balloonType}\n";
                byte[] bytes = Encoding.UTF8.GetBytes(balloonMsg);

                try
                {
                    NetworkConnector.Instance.Stream.WriteAsync(bytes, 0, bytes.Length);
                    Debug.Log($"[PlayerInput] ��ǳ�� ��ġ ��û ����: {balloonMsg.Trim()}");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[PlayerInput] ��ǳ�� ��ġ ��û ����: {ex.Message}");
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
            Debug.LogError($"��ġ ���� ����: {e.Message}");
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
                Debug.Log("[WaterHitDetector] �� ĳ���Ͱ� ���� ����! ��Ŷ ����");
                
                string msg = $"WATER_HIT|{myNick}|{waterDamage}\n";
                byte[] bytes = Encoding.UTF8.GetBytes(msg);
                await NetworkConnector.Instance.Stream.WriteAsync(bytes, 0, bytes.Length);

                Debug.Log("[WaterHitDetector] WATER_HIT ��Ŷ ���� �Ϸ�");

                ResetHitFlagAfterDelay(2f);
            }
        }
    }

    private async void ResetHitFlagAfterDelay(float delaySeconds)
    {
        await Task.Delay((int)(delaySeconds * 1000));
        hasSentWaterHit = false;
        Debug.Log("[WaterHitDetector] hasSentWaterHit �÷��� �ʱ�ȭ��");
    }
}
