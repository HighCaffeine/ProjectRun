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
    private Vector3 horizontalMove;

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

    private int spawninDex = 0; 

    private Vector3 platformDelta;
    private Vector3 windDir;
    private float windPower;
    private bool isInWind;
    // 상태머신 값
    public float h { private set; get; }
    public float v { private set; get; }


    public enum ActionType : byte { PUSH = 0, PULL = 1, }

    public Vector3 GetForward() { return playerPivot.forward; }
    public void SetController(CharacterController cc) => this.controller = cc;
    public void SetControllerActive(bool isActive) { if (this.controller != null) this.controller.enabled = isActive; }
    public void SetPlayerPivot(Transform pivot) => this.playerPivot = pivot;

    protected override void Start()
    {
        if (GameManager.Instance.currentMode == GameManager.PlayMode.Offline_Test)
        { ActorManager.Instance.AddPlayer(this); }

        controller = GetComponent<CharacterController>();
        sm.ChangeState(new IdleState(this));
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
        sm.Update();

        if (GameManager.Instance.currentMode == GameManager.PlayMode.Offline_Test)
        {
            h = Input.GetAxisRaw("Horizontal");
            v = Input.GetAxisRaw("Vertical");

            if (controller != null && controller.enabled)
            {
                if (isInWind)
                {
                    Vector3 inputDir = new Vector3(h, 0f, v).normalized;

                    if (inputDir.magnitude > 0.01f)
                    {
                        float dot = Vector3.Dot(inputDir, windDir);

    
                        if (dot > 0)
                        {
                            horizontalMove += windDir * windPower * dot;
                        }
    
                        else
                        {
                            horizontalMove += windDir * windPower * dot;
                            // dot이 음수라 자동으로 반대 힘 됨
                        }
                    }
                    else
                    {
                        horizontalMove += windDir * windPower;
                    }
                }
                ApplyGravity();
                Vector3 finalMove = horizontalMove +(windDir * windPower)+ platformDelta + (Vector3.up * verticalVelocity);
                controller.Move(platformDelta);

                float safeDelta = Mathf.Min(Time.deltaTime, 0.1f);
                controller.Move(finalMove * safeDelta);

                platformDelta = Vector3.zero;
            }

            return;
        }

        if (!IsLocal) return;

        h = Input.GetAxisRaw("Horizontal");
        v = Input.GetAxisRaw("Vertical");

        if (IsLocal && controller != null && controller.enabled)
        {
            ApplyGravity();
            Vector3 finalMove = horizontalMove + platformDelta + (Vector3.up * verticalVelocity);
            controller.Move(platformDelta);

            float safeDelta = Mathf.Min(Time.deltaTime, 0.1f);
            controller.Move(finalMove * safeDelta);

            platformDelta = Vector3.zero;
        }
    }

    public Action<string, int> OnUpdatePoint;

    public void Move(Vector3 dir, float speed)
    {
        horizontalMove += dir * speed;
    }
    public void SetPlatformDelta(Vector3 delta)
    {
        platformDelta = delta;
    }
    private void ApplyGravity()
    {
        if(sm.currentState is KnockbackState) return; // 넉백 상태에서는 중력 적용 안 함
        if (controller.isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f;
        }
        else
        {
            // [해결 1] 백그라운드 전환 시 deltaTime이 10초, 20초로 튀는 것을 최대 0.1초로 제한
            float safeDelta = Mathf.Min(Time.deltaTime, 0.1f);
            verticalVelocity += gravity * safeDelta;

            // [해결 2] 바닥을 뚫고 무한 낙하하는 속도 제한 (CharacterController 고장 방지)
            if (verticalVelocity < maxVerticalVelocity)
            {
                verticalVelocity = maxVerticalVelocity;
            }
        }
    }
    //마우스 방향 보기
    public void LookAtMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane ground = new Plane(Vector3.up, transform.position);

        if (ground.Raycast(ray, out float enter))
        {
            Vector3 hit = ray.GetPoint(enter);
            Vector3 dir = hit - transform.position;
            dir.y = 0; // 수평 회전만

            if (playerPivot != null)
            {
                playerPivot.rotation = Quaternion.LookRotation(dir);
            }
            else
            {
                transform.rotation = Quaternion.LookRotation(dir);
            }
        }
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

    public void PlayTravelSpark(ActionType actionType)
    {
        if (travelSparkParticle == null) return;

        Transform sparkTransform = travelSparkParticle.transform;

        if (actionType == ActionType.PULL)
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
    public void SendStateChange(eState stateCode, Vector3 dir = default, float param = 0f)
    {
        if (!Client.IS_SERVER_PLAY || !IsLocal) return;

        P_PlayerStateNtf pkt = new P_PlayerStateNtf
        {
            userUUID = LocalPlayerInfo.ID,
            newState = (byte)stateCode,
            targetDir = new P_PacketVector3 { x = dir.x, y = dir.y, z = dir.z },
            powerOrTime = param
        };

        Client.TCP.SendPacket2(E_PACKET.PLAYER_STATE_NTF, pkt);
    }

    public void SetLocal(bool value)
    {
        IsLocal = value;
    }
    public void PlayerDead(Vector3 pos, float spawnDelay)
    {
        if(!controller)
        {
            return;
        }
        else
        {
            controller.enabled = false;
            transform.position = pos;
            playerPivot.gameObject.SetActive(false);
            StartCoroutine(RespawnAfterDelay(spawnDelay));
        }
    }
    IEnumerator RespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        controller.enabled = true;
        playerPivot.gameObject.SetActive(true);
    }

    public void SetWind(Vector3 dir, float power)
    {
        windDir = dir.normalized;
        windPower = power;
        isInWind = true;
    }

    public void ClearWind()
    {
        isInWind = false;
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
        pkt.currentRot.Set(transform.rotation);

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
}