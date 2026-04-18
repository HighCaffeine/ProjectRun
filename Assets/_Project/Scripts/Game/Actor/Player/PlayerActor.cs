using System;
using UnityEngine;
using System.Collections;

public class PlayerActor : Actor
{
    const float CAMERA_SHAKE = 1.0f;

    [Header("밀치기 힘 배율")][SerializeField] private float pushMulti = 1.0f;
    public float PushMulti => pushMulti;

    public float moveSpeed = 5.0f;
    private bool wasMoving = false;
    public LayerMask targetLayer;
    [SerializeField] private Transform playerPivot;
    private CharacterController controller;
    public Vector3 horizontalMove;

    public DashCameraEffect dashCameraEffect;

    //동기화
    public uint inputSeq = 0;
    public bool IsLocal = false;
    public float sendTimer = 0f;
    public const float sendInterval = 0.02f;
    private P_PacketVector3 curPos;
    private P_PacketQuaternion curRot;

    [Header("Visual Effects")]
    public TrailRenderer trailRenderer;
    public ParticleSystem travelSparkParticle;
    public ParticleSystem[] brakeParticle;

    [Header("Gravity Settings")]
    public float gravity = -20f;       // 기본 중력 값
    private float verticalVelocity;     // 현재 수직 속도
    private float maxVerticalVelocity = -30f; // 최대 낙하 속도 제한


    public Animator animator;
    private int spawninDex = 0;

    private Vector3 platformDelta;
    public Vector3 windDir;
    public float windPower;
    public bool isInWind;

    [SerializeField]
    public bool is2p = false;
    // 상태머신 값
    public float h { private set; get; }
    public float v { private set; get; }

    public int fallDeathCount=0;
    public int pushCount=0;
    public int pullCount=0;


    public Vector3 GetForward() { return playerPivot.forward; }
    public void SetController(CharacterController cc) => this.controller = cc;
    public void SetControllerActive(bool isActive) { if (this.controller != null) this.controller.enabled = isActive; }
    public void SetPlayerPivot(Transform pivot) => this.playerPivot = pivot;

    protected override void Start()
    {
        if (GameManager.Instance.currentMode == GameManager.PlayMode.Offline_Test)
        { ActorManager.Instance.AddPlayer(this); }

        animator = GetComponentInChildren<Animator>();
        controller = GetComponent<CharacterController>();
        sm.ChangeState(new IdleState(this));
        fallDeathCount = 0;
        pushCount = 0;
        pullCount = 0;
    }
    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            sendTimer = 0f;
        }
    }
    void Update()
    {
        if (sm.currentState == null) return;
        horizontalMove = Vector3.zero;
        h = 0f;
        v = 0f;

        if (is2p)
        {
            if (Input.GetKey(KeyCode.W))
            {
                v += 1f;

            }
            if (Input.GetKey(KeyCode.A))
            {
                h -= 1f;
            }
            if (Input.GetKey(KeyCode.S))
            {
                v -= 1f;
            }
            if (Input.GetKey(KeyCode.D))
            {
                h += 1f;
            }
        }
        else
        {
              //  h = Input.GetAxisRaw("Horizontal");
           //  v = Input.GetAxisRaw("Vertical");
            if (Input.GetKey(KeyCode.UpArrow))
            {
                v += 1f;
            }
            if (Input.GetKey(KeyCode.DownArrow))
            {
                v -= 1f;
            }
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                h -= 1f;
            }
            if (Input.GetKey(KeyCode.RightArrow))
            {
                h += 1f;
            }

        }
            sm.Update();
        ApplyWind();
       
        if (controller != null && controller.enabled)
        {
            ApplyGravity();

            // 플랫폼(이동 발판) 델타 이동 우선 적용
            if (platformDelta != Vector3.zero)
            {
                controller.Move(platformDelta);
                platformDelta = Vector3.zero;
            }
            Vector3 move = horizontalMove;

            if (move != Vector3.zero)
            {
                RaycastHit hit;
                float checkDistance = 0.6f;
                Vector3 origin = transform.position + Vector3.up * 0.3f;

                if (Physics.Raycast(origin, move.normalized, out hit, checkDistance))
                {
                    float dot = Vector3.Dot(hit.normal, Vector3.up);

                    if (dot < 0.3f) // 거의 수직면일 때만
                    {
                        move = Vector3.ProjectOnPlane(move, hit.normal);
                    }
                }
            }
            // 최종 이동
            Vector3 finalMove = move + (Vector3.up * verticalVelocity);

            float safeDelta = Mathf.Min(Time.deltaTime, 0.1f);
            controller.Move(finalMove * safeDelta);
        }
       
    }

    public Action<string, int> OnUpdatePoint;

    public void Move(Vector3 dir, float speed)
    {
        horizontalMove += dir * speed;
    }
    public void SetWind(Vector3 dir, float power)
    {
        windDir = dir.normalized;
        windPower = power;
        isInWind = true;
    }
    public void ResetWind()
    {
        windDir = Vector3.zero;
        windPower = 0f;

        isInWind = false;
        moveSpeed = 5f;
    }

    public void ApplyWind()
    {
        if (!isInWind) return;

        Vector3 move = horizontalMove;

        //  가만히 있을 때
        if (move.sqrMagnitude < 0.001f)
        {
            horizontalMove += windDir * windPower;
            Debug.Log("[Wind] Idle Push");
            return;
        }

        Vector3 moveDir = move.normalized;
        float dot = Vector3.Dot(moveDir, windDir);

        moveSpeed = 5f; // 기본 이동 속도로 초기화
        //  역방향 (감속)
        if (dot < 0f)
        {
            //horizontalMove -= moveDir * windPower;
            moveSpeed = moveSpeed * 0.3f;
            Debug.Log("[Wind] Decelerate");
        }
        // 정방향 (가속)
        else
        {
            //  horizontalMove += windDir * windPower;
            moveSpeed = moveSpeed * 1.7f;
            Debug.Log("[Wind] Accelerate");
        }
    }
    public void SetPlatformDelta(Vector3 delta)
    {
        platformDelta = delta;
    }
    private void ApplyGravity()
    {
        if (sm.currentState is KnockbackState) return; // 넉백 상태에서는 중력 적용 안 함
        if (controller.isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f;
        }
        else
        {
            float safeDelta = Mathf.Min(Time.deltaTime, 0.1f);
            verticalVelocity += gravity * safeDelta;

            if (verticalVelocity < maxVerticalVelocity)
            {
                verticalVelocity = maxVerticalVelocity;
            }
        }
    }
    //마우스 방향 보기
    public Vector3 GetMouseDir()
    {
        Camera cam = Camera.main;

        Vector3 playerScreen = cam.WorldToScreenPoint(transform.position);
        Vector3 mouse = Input.mousePosition;

        Vector3 screenDir = mouse - playerScreen;

        Vector3 forward = cam.transform.forward;
        Vector3 right = cam.transform.right;

        forward.y = 0;
        right.y = 0;

        forward.Normalize();
        right.Normalize();

        Vector3 worldDir = right * screenDir.x + forward * screenDir.y;

        return worldDir.normalized;
    }

    public void LookAtDirection(Vector3 dir)
    {
        if (playerPivot != null)
        {
            playerPivot.rotation = Quaternion.LookRotation(dir);
        }
        else
        {
            transform.rotation = Quaternion.LookRotation(dir);
        }
    }
    public void PlayBrakeParticles()
    {
        if (brakeParticle == null) return;
        foreach (ParticleSystem p in brakeParticle)
        {
            if (p != null) p.Play();
        }
    }
    public void SetVerticalVelocity(float velocity) => verticalVelocity = velocity;

    public IEnumerator HitStopRoutine(float duration = 0.05f)
    {
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1.0f;
    }

    public void PlayTravelSpark(eState actionType)
    {
        if (travelSparkParticle == null) return;

        Transform sparkTransform = travelSparkParticle.transform;

        if (actionType == eState.Pull)
        {
            sparkTransform.localRotation = Quaternion.Euler(0, 180, 0);
        }
        else
        {
            sparkTransform.localRotation = Quaternion.identity;
        }

        travelSparkParticle.Play();
    }

    public void StopTravelSpark()
    {
        if (travelSparkParticle != null && travelSparkParticle.isPlaying)
        {
            travelSparkParticle.Stop();
        }
    }

    public void ShakeCamera() { CameraManager.Instance.PlayEffect(new CameraShakeEffect(CAMERA_SHAKE, CAMERA_SHAKE, 0.3f)); }

    // 상태 변경 패킷 전송용 함수
    public void SendStateChange(eState stateCode, Vector3 dir = default, float param = 0f, long targetUUID = 0)
    {
        // 로컬이 쏘는거 아니면 리턴
        if (!Client.IS_SERVER_PLAY || !IsLocal) return;

        long finalUUID = (targetUUID == 0) ? LocalPlayerInfo.ID : targetUUID;

        P_PlayerStateNtf pkt = new P_PlayerStateNtf
        {
            userUUID = finalUUID,
            newState = (byte)stateCode,
            targetDir = new P_PacketVector3 { x = dir.x, y = dir.y, z = dir.z },
            powerOrTime = param
        };

        Client.TCP.SendPacket2(E_PACKET.PLAYER_STATUS_NTF, pkt);
    }
    public void SetLocal(bool value)
    {
        IsLocal = value;
        if (!IsLocal)
        {
            h = 0f;
            v = 0f;
            horizontalMove = Vector3.zero;
            if (sm != null && !(sm.currentState is IdleState))
            {
                sm.ChangeState(new IdleState(this));
            }
        }
    }
    public void PlayerDead(Vector3 pos, float spawnDelay)
    {
        if (!controller)
        {
            return;
        }
        else
        {
            controller.enabled = false;
            transform.position = pos;
            playerPivot.gameObject.SetActive(false);
            StartCoroutine(RespawnAfterDelay(spawnDelay));
            fallDeathCount++;
            Debug.Log(gameObject.name + "Die" +  fallDeathCount);
        }
    }
    IEnumerator RespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        controller.enabled = true;
        playerPivot.gameObject.SetActive(true);
    }

    // 상태 머신이 호출, 확정 좌표 패킷 전송
    // public void SendMovePacket(float axisH, float axisV)
    // {
    //     curPos.Set(transform.position);
    //     curRot.Set(transform.rotation);

    //     P_PlayerMovement pkt = new P_PlayerMovement
    //     {
    //         userUUID = LocalPlayerInfo.ID,
    //         inputSeq = ++inputSeq,
    //         currentPos = curPos,
    //         currentRot = curRot,
    //         axisH = axisH,
    //         axisV = axisV
    //     };
    //     Client.UDP.SendPacket2(E_PACKET.PLAYER_MOVEMENT, pkt);
    // }

    // public void SendMovePacket(float axisH, float axisV)
    // {
    //     P_PacketVector3 sendPos = new P_PacketVector3();
    //     sendPos.Set(transform.position);

    //     P_PacketQuaternion sendRot = new P_PacketQuaternion();
    //     sendRot.Set(transform.rotation);

    //     P_PlayerMovement pkt = new P_PlayerMovement
    //     {
    //         userUUID = LocalPlayerInfo.ID,
    //         inputSeq = ++inputSeq,
    //         currentPos = sendPos,
    //         currentRot = sendRot,
    //         axisH = axisH,
    //         axisV = axisV
    //     };

    //     // 직접 직렬화해서 바이트 값 비교
    //     byte[] dataOld = SerializePlayerMovement(pkt); // 됐던 함수
    //     byte[] dataNew = PacketSerializer.Serialize(pkt); // 새 함수

    //     Debug.Log($"[비교] Old: {dataOld.Length}bytes [{string.Join(",", dataOld)}]");
    //     Debug.Log($"[비교] New: {dataNew.Length}bytes [{string.Join(",", dataNew)}]");
    // }



    // public void SendMovePacket(float axisH, float axisV)
    // {
    //     P_PacketVector3 sendPos = new P_PacketVector3();
    //     sendPos.Set(transform.position);

    //     P_PacketQuaternion sendRot = new P_PacketQuaternion();
    //     sendRot.Set(transform.rotation);

    //     P_PlayerMovement pkt = new P_PlayerMovement
    //     {
    //         userUUID = LocalPlayerInfo.ID,
    //         inputSeq = ++inputSeq,
    //         currentPos = sendPos,
    //         currentRot = sendRot,
    //         axisH = axisH,
    //         axisV = axisV
    //     };

    //     Client.UDP.SendPacket2(E_PACKET.PLAYER_MOVEMENT, pkt); // byte[] 말고 pkt 직접
    // }


    public void SendMovePacket(float axisH, float axisV)
    {
        if (!IsLocal) return;

        P_PlayerMovement pkt = new P_PlayerMovement
        {
            userUUID = LocalPlayerInfo.ID,
            inputSeq = ++inputSeq,
            currentPos = new P_PacketVector3(),
            currentRot = new P_PacketQuaternion(),
            axisH = axisH,
            axisV = axisV
        };
        pkt.currentPos.Set(transform.position);
        pkt.currentRot.Set(playerPivot.rotation);

        //byte[] data = SerializePlayerMovement(pkt);
        //byte[] data = PacketSerializer.Serialize(pkt);
        Client.UDP.SendPacket2(E_PACKET.PLAYER_MOVEMENT, pkt);
    }
    public static byte[] SerializePlayerMovement(P_PlayerMovement pkt)
    {
        byte[] buf = new byte[48]; // 헤더 제외 데이터만
        int offset = 0;

        // long userUUID (8)
        Array.Copy(BitConverter.GetBytes(pkt.userUUID), 0, buf, offset, 8); offset += 8;
        // uint inputSeq (4)
        Array.Copy(BitConverter.GetBytes(pkt.inputSeq), 0, buf, offset, 4); offset += 4;
        // Vector3 currentPos (12)
        Array.Copy(BitConverter.GetBytes(pkt.currentPos.x), 0, buf, offset, 4); offset += 4;
        Array.Copy(BitConverter.GetBytes(pkt.currentPos.y), 0, buf, offset, 4); offset += 4;
        Array.Copy(BitConverter.GetBytes(pkt.currentPos.z), 0, buf, offset, 4); offset += 4;
        // Quaternion currentRot (16)
        Array.Copy(BitConverter.GetBytes(pkt.currentRot.x), 0, buf, offset, 4); offset += 4;
        Array.Copy(BitConverter.GetBytes(pkt.currentRot.y), 0, buf, offset, 4); offset += 4;
        Array.Copy(BitConverter.GetBytes(pkt.currentRot.z), 0, buf, offset, 4); offset += 4;
        Array.Copy(BitConverter.GetBytes(pkt.currentRot.w), 0, buf, offset, 4); offset += 4;
        // float axisH (4)
        Array.Copy(BitConverter.GetBytes(pkt.axisH), 0, buf, offset, 4); offset += 4;
        // float axisV (4)
        Array.Copy(BitConverter.GetBytes(pkt.axisV), 0, buf, offset, 4); offset += 4;

        return buf;
    }
    private void OnDrawGizmos()
    {
        if (!isInWind) return;

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, windDir * 3f);

        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, horizontalMove.normalized * 3f);
    }

}