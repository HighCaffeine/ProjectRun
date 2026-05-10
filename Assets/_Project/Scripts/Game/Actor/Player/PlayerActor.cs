using System;
using UnityEngine;
using System.Collections;
using UnityEngine.Rendering.Universal;

public class PlayerActor : Actor
{
    private Camera cam;

    const float CAMERA_SHAKE = 1.0f;

    [Header("밀치기 힘 배율")][SerializeField] private float pushMulti = 1.0f;
    public float PushMulti => pushMulti;

    public LayerMask targetLayer;

    //동기화
    public uint inputSeq = 0;

    public override bool IsLocal => isLocal;
    public bool isLocal;

    public float ignoreServerPosTimer = 0f;


    [Header("Visual Effects")]

    public ParticleSystem pushParticle;
    public Transform attackEffectPivot;

    public void PushParticle() { attackEffectPivot.localRotation = playerPivot.localRotation; pushParticle.Play(); }

    public TrailRenderer trailRenderer;
    public ParticleSystem[] travelSparkParticle;
    public ParticleSystem[] brakeParticle;


    private Vector3 platformDelta;
    public Vector3 windDir;
    public float windPower;
    public bool isInWind;

    #region 플레이어 통계
    public int fallDeathCount = 0;
    public int pushCount = 0;
    public int pullCount = 0;
    #endregion
    public void SetController(CharacterController cc) => this.controller = cc;
    public void SetControllerActive(bool isActive) { if (this.controller != null) this.controller.enabled = isActive; }
    public void SetPlayerPivot(Transform pivot) => this.playerPivot = pivot;

    public Transform mainCam { get; private set; }
    private P_PlayerMovement cachedMovePacket;

    protected new void Start()
    {
        cam = Camera.main;
        mainCam = cam.transform;
        cachedMovePacket = new P_PlayerMovement
        {
            currentPos = new P_PacketVector3(),
            currentRot = new P_PacketQuaternion()
        };

        base.Start();

        if (GameManager.Instance.currentMode == GameManager.PlayMode.Offline_Test)
        {
            ActorManager.Instance.AddPlayer(this);
        }

        fallDeathCount = 0; pushCount = 0; pullCount = 0;
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

        if (IsLocal)
        {
            h = 0f; v = 0f;
            HandleInput();

            ApplyWind();
        }

        sm.Update();
        ApplyMovement();

        if (IsLocal) HandleNetworkSync();
    }

    public override void ApplyMovement()
    {
        if (controller == null || !controller.enabled) return;

        if (sm.currentState is ActionState || sm.currentState is KnockbackState)
        {
            verticalVelocity = 0f;
        }
        else
        {
            ApplyGravity();
        }

        Vector3 finalMove = horizontalMove + (Vector3.up * verticalVelocity);
        controller.Move(finalMove * Mathf.Min(Time.deltaTime, 0.1f));

        horizontalMove = Vector3.zero;
    }

    public bool CheckActionInput()
    {
        if (Is2p)
        {
            if (Input.GetKeyDown(KeyCode.F)) { sm.ChangeState(new ActionState(this, eState.Push)); return true; }
            if (Input.GetKeyDown(KeyCode.G)) { sm.ChangeState(new ActionState(this, eState.Pull)); return true; }
        }
        else
        {
            if (Input.GetMouseButtonDown(0)) { sm.ChangeState(new ActionState(this, eState.Push)); return true; }
            if (Input.GetMouseButtonDown(1)) { sm.ChangeState(new ActionState(this, eState.Pull)); return true; }
        }
        return false;
    }

    private Vector3 CalculateWallSlide(Vector3 move)
    {
        if (move == Vector3.zero) return move;

        Vector3 origin = transform.position + Vector3.up * 0.5f;
        if (Physics.SphereCast(origin, controller.radius, move.normalized, out RaycastHit hit, 0.5f, targetLayer))
        {
            return Vector3.ProjectOnPlane(move, hit.normal);
        }
        return move;
    }

    public Action<string, int> OnUpdatePoint;


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
            return;
        }

        Vector3 moveDir = move.normalized;
        float dot = Vector3.Dot(moveDir, windDir);

        moveSpeed = 5f; // 기본 이동 속도로 초기화
        //  역방향 (감속)
        if (dot < 0f)
        {
            //horizontalMove -= moveDir * windPower;
            moveSpeed = moveSpeed * 0.5f;
        }
        // 정방향 (가속)
        else
        {
            //  horizontalMove += windDir * windPower;
            moveSpeed = moveSpeed * 1.4f;
        }
    }
    public void SetPlatformDelta(Vector3 delta)
    {
        platformDelta = delta;
    }
    private void ApplyGravity()
    {
        if (sm.currentState is KnockbackState) return;

        if (sm.currentState is ActionState)
        {
            if (IsLocal)
            {
                verticalVelocity = 0f;
                return;
            }

            if (controller.isGrounded)
            {
                verticalVelocity = -2f;
                return;
            }
        }

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

    public override Vector3 GetActionDir()
    {
        return GetMouseDir();
    }

    private Vector3 GetMouseDir()
    {
        Vector3 playerScreenPos = cam.WorldToScreenPoint(transform.position);

        Vector3 mousePos = Input.mousePosition;
        mousePos.z = playerScreenPos.z;

        Vector3 mouseWorldPos = cam.ScreenToWorldPoint(mousePos);
        Vector3 dir = mouseWorldPos - transform.position;

        dir.y = 0;

        return dir.normalized;
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

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        Time.timeScale = 1.0f;
    }

    public void PlayTravelSpark(eState actionType)
    {
        if (travelSparkParticle == null) return;
        foreach (ParticleSystem p in travelSparkParticle)
        {
            Transform sparkTransform = p.transform;

            if (actionType == eState.Pull)
            {
                sparkTransform.localRotation = Quaternion.Euler(0, 180, 0);
            }
            else
            {
                sparkTransform.localRotation = Quaternion.identity;
            }


            p.Play();
        }
    }

    public void StopTravelSpark()
    {
        foreach (ParticleSystem p in travelSparkParticle)
        {
            if (p != null && p.isPlaying)
            {
                p.Stop();
            }
        }
    }

    public void ShakeCamera() { CameraManager.Instance.PlayEffect(new CameraShakeEffect(CAMERA_SHAKE, CAMERA_SHAKE, 0.3f)); }

    // 상태 변경 패킷 전송용 함수
    public override void SendStateChange(eState stateCode, Vector3 dir = default, float param = 0f, long targetUUID = 0)
    {
        // 로컬이 쏘는거 아니면 리턴
        if (GameManager.Instance.currentMode != GameManager.PlayMode.Server_Online) return;
        if (!Client.IS_SERVER_PLAY || !IsLocal) return;
        if (Client.TCP == null) return;

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
        isLocal = value;
        if (!isLocal)
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
            Debug.Log(gameObject.name + "Die" + fallDeathCount);
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


    public override void SendMovePacket(float axisH, float axisV)
    {
        if (!IsLocal) return;

        cachedMovePacket.userUUID = LocalPlayerInfo.ID;
        cachedMovePacket.inputSeq = ++inputSeq;
        cachedMovePacket.currentPos.Set(transform.position);
        cachedMovePacket.currentRot.Set(playerPivot.rotation);
        cachedMovePacket.axisH = axisH;
        cachedMovePacket.axisV = axisV;

        if (Client.UDP != null) Client.UDP.SendPacket2(E_PACKET.PLAYER_MOVEMENT, cachedMovePacket);
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

    public override bool Is2p => is2p;
    public bool is2p = false;
    private void HandleInput()
    {
        if (!Is2p)
        {
            if (Input.GetKey(KeyCode.W)) v += 1f;
            if (Input.GetKey(KeyCode.A)) h -= 1f;
            if (Input.GetKey(KeyCode.S)) v -= 1f;
            if (Input.GetKey(KeyCode.D)) h += 1f;
        }
        else
        {
            if (Input.GetKey(KeyCode.UpArrow)) v += 1f;
            if (Input.GetKey(KeyCode.DownArrow)) v -= 1f;
            if (Input.GetKey(KeyCode.LeftArrow)) h -= 1f;
            if (Input.GetKey(KeyCode.RightArrow)) h += 1f;
        }
    }

    public override Vector3 GetMovementDirection()
    {
        Vector3 forward = mainCam.forward;
        Vector3 right = mainCam.right;
        forward.y = 0; right.y = 0;

        return (forward.normalized * v + right.normalized * h).normalized;
    }

    public override bool HasMoveIntent()
    {
        return (h != 0 || v != 0);
    }

    private void HandleNetworkSync()
    {
        sendTimer += Time.deltaTime;
        if (sendTimer >= Actor.sendInterval)
        {
            if (HasMoveIntent() || sm.currentState is KnockbackState)
            {
                SendMovePacket(h, v);
                sendTimer = 0f;
            }
        }
    }

    public override bool CheckActionIntent()
    {
        if (!IsLocal) return false;

        if (Input.GetMouseButtonDown(0)) { sm.ChangeState(new ActionState(this, eState.Push)); return true; }
        if (Input.GetMouseButtonDown(1)) { sm.ChangeState(new ActionState(this, eState.Pull)); return true; }
        return false;
    }
}

