using System;
using UnityEngine;
using System.Collections;

public class PlayerActor : Actor
{
    private const float MOVE_ACCEL_VALUE = 10f;

    private Camera cam;

    const float CAMERA_SHAKE = 1.0f;

    public override float PushMulti => pushMulti;

    public float pushMulti = 1.0f;

    public LayerMask targetLayer;

    //동기화
    public uint inputSeq = 0;

    public override bool IsLocal => isLocal;
    public bool isLocal;

    public float ignoreServerPosTimer = 0f;


    [Header("Visual Effects")]

    public ParticleSystem pushParticle;
    public Transform attackEffectPivot;

    [Header("Aiming Indicator")]
    public LineRenderer aimLine;

    public void PushParticle()
    {
        attackEffectPivot.localRotation = playerPivot.localRotation;
        pushParticle.Stop(); pushParticle.Play();
    }

    public TrailRenderer trailRenderer;
    public ParticleSystem[] travelSparkParticle;

    public ParticleSystem[] brakeParticle;


    private Vector3 platformDelta;
    private Vector3 windDir;
    private float windPower;
    private bool isInWind;

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
    [Space(5f)]
    [Header("Action Sprite")]
    public SpriteRenderer Indicator;
    public SpriteRenderer PullIndicator;
    public SpriteRenderer PushIndicator;
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


        ActorManager.Instance.AddPlayer(this);

        PullIndicator.gameObject.SetActive(false);
        PushIndicator.gameObject.SetActive(false);
        fallDeathCount = 0; pushCount = 0; pullCount = 0;
    }
    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            sendTimer = 0f;
        }
    }

    public void TEST_ResetToStage1()
    {
        DungeonPointManager.Instance.currentMapID = 0;
        DungeonPointManager.Instance.currentSectorIndex = 0;

        Vector3 destPos = DungeonPointManager.Instance.GetSpawnPosition(0, 0);

        OnUpdatePoint?.Invoke(gameObject.name, 0);

        float encodedValue = (0 * 100) + 0;

        P_GimmickInteractReq req = new P_GimmickInteractReq
        {
            activeUUID = LocalPlayerInfo.ID,
            gimmickID = 999,
            gimmickKey = (byte)eGimmickKey.NextZone,
            state = 2,
            targetPos = new P_PacketVector3 { x = destPos.x, y = destPos.y, z = destPos.z },
            param = encodedValue, // 예: 1_2 -> 102
            timestamp = NetworkTimeManager.Instance.GetServerTime()
        };

        Client.TCP.SendPacket2(E_PACKET.GIMMICK_INTERACT_REQ, req);
        Debug.Log($"[System] Map{0}_Sector{0}번 구역으로 이동 요청 (encoded: {encodedValue})");
    }

    void Update()
    {
        if (isDead || controller == null || !controller.enabled) return;
        if (sm.currentState == null) return;

        if (IsLocal)
        {
            if (Input.GetKeyDown(KeyCode.R)) TEST_ResetToStage1();

            if (sm.currentState is IdleState || sm.currentState is MoveState)
            {
                CheckActionIntent();
                HandleInput();
            }

            ApplyWind();
        }

        sm.Update();

        if (IsLocal)
        {
            ApplyMovement();
            HandleNetworkSync();
        }
    }


    public override void ApplyMovement()
    {
        if (controller == null || !controller.enabled) return;

        bool isOnPlatform = (platformDelta.sqrMagnitude > 0.00001f);

        if (sm.currentState is KnockbackState)
        {
            verticalVelocity = 0f;
        }
        else if (isOnPlatform)
        {
            verticalVelocity = -0.1f;
        }
        else
        {
            ApplyGravity();
        }

        Vector3 finalMove = horizontalMove + (Vector3.up * verticalVelocity);
        controller.Move((finalMove * Mathf.Min(Time.deltaTime, 0.1f)) + platformDelta);

        horizontalMove = Vector3.zero;

        platformDelta = Vector3.zero;
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

        // if (sm.currentState is ActionState)
        // {
        //     if (IsLocal)
        //     {
        //         verticalVelocity = 0f;
        //         return;
        //     }

        //     if (controller.isGrounded)
        //     {
        //         verticalVelocity = -2f;
        //         return;
        //     }
        // }

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
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        Plane groundPlane = new Plane(Vector3.up, new Vector3(0, transform.position.y, 0));

        if (groundPlane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);

            Vector3 dir = hitPoint - transform.position;
            dir.y = 0;

            return dir.normalized;
        }

        Vector3 fallbackDir = transform.forward;
        fallbackDir.y = 0;
        return fallbackDir.normalized;
    }

    public void DrawAimLine(Vector3 targetPos)
    {
        if (aimLine == null) return;
        aimLine.enabled = true;
        // 가슴 높이(y + 1f)에서부터 목표물까지 선을 그림
        aimLine.SetPosition(0, transform.position + Vector3.up * 1f);
        aimLine.SetPosition(1, targetPos + Vector3.up * 1f);
    }

    public void HideAimLine()
    {
        if (aimLine != null) aimLine.enabled = false;
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

        int index = (actionType == eState.Pull) ? 1 : 0;

        ParticleSystem particle = travelSparkParticle[index];

        if (particle == null) return;

        Transform sparkTransform = particle.transform;

        // 기본 방향
        sparkTransform.localRotation = Quaternion.identity;

        // Pull이면 뒤집기
        if (actionType == eState.Pull)
        {
            sparkTransform.Rotate(0f, 180f, 0f, Space.Self);
        }

        particle.Stop();
        particle.Play();
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
    public override void SendStateChange(eState stateCode, Vector3 dir = default, float param = 0f, long targetUUID = 0, bool isPull = false, Vector3 casterPos = default)
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.currentMode != GameManager.PlayMode.Server_Online) return;
        if (!Client.IS_SERVER_PLAY || !IsLocal) return;
        if (Client.TCP == null) return;

        long finalUUID = (targetUUID == 0) ? LocalPlayerInfo.ID : targetUUID;

        P_PlayerStatusNtf pkt = new P_PlayerStatusNtf
        {
            userUUID = finalUUID,
            newState = (byte)stateCode,
            targetDir = new P_PacketVector3 { x = dir.x, y = dir.y, z = dir.z },
            param = param,
            isPull = isPull ? (byte)1 : (byte)0,
            casterPos = new P_PacketVector3 { x = casterPos.x, y = casterPos.y, z = casterPos.z },

            timestamp = NetworkTimeManager.Instance.GetServerTime()
        };

        Debug.Log($"[SendStateChange] 현재 모드: {GameManager.Instance.currentMode}, 상태: {stateCode}");

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

        if (isDead || !controller) return;

        isDead = true;

        verticalVelocity = 0f;
        controller.enabled = false;

        pos.y += 1.5f;
        transform.position = pos;

        if (isLocal) SendMovePacket(0f, 0f);

        playerPivot.gameObject.SetActive(false);
        StartCoroutine(RespawnAfterDelay(spawnDelay));
        fallDeathCount++;
        Debug.Log(gameObject.name + "Die" + fallDeathCount);

    }
    IEnumerator RespawnAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        controller.enabled = true;
        verticalVelocity = 0f;
        isDead = false;
        playerPivot.gameObject.SetActive(true);

        if (IsLocal)
        {
            SendMovePacket(0f, 0f);
        }
    }


    public void ForceSendMovePacket()
    {
        SendMovePacket(h, v);
    }


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
        float targetH = 0f;
        float targetV = 0f;

        if (!Is2p)
        {
            if (Input.GetKey(KeyCode.W)) targetV += 1f;
            if (Input.GetKey(KeyCode.S)) targetV -= 1f;
            if (Input.GetKey(KeyCode.A)) targetH -= 1f;
            if (Input.GetKey(KeyCode.D)) targetH += 1f;
        }
        else
        {
            if (Input.GetKey(KeyCode.UpArrow)) targetV += 1f;
            if (Input.GetKey(KeyCode.DownArrow)) targetV -= 1f;
            if (Input.GetKey(KeyCode.LeftArrow)) targetH -= 1f;
            if (Input.GetKey(KeyCode.RightArrow)) targetH += 1f;
        }

        h = Mathf.MoveTowards(h, targetH, Time.deltaTime * MOVE_ACCEL_VALUE);
        v = Mathf.MoveTowards(v, targetV, Time.deltaTime * MOVE_ACCEL_VALUE);
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

    private Vector3 lastSentPos;

    private void HandleNetworkSync()
    {
        if (isDead) return;

        sendTimer += Time.deltaTime;
        if (sendTimer >= Actor.sendInterval)
        {
            bool isPositionChanged = Vector3.Distance(transform.position, lastSentPos) > 0.001f;

            if (HasMoveIntent() || sm.currentState is KnockbackState || (controller != null && !controller.isGrounded) || isPositionChanged)
            {
                SendMovePacket(h, v);

                lastSentPos = transform.position;
                sendTimer = 0f;
            }
        }
    }

    public override bool CheckActionIntent()
    {
        if (!IsLocal) return false;

        if (Input.GetMouseButtonDown(0)) { sm.ChangeState(new AimState(this, eState.Push)); return true; }
        if (Input.GetMouseButtonDown(1)) { sm.ChangeState(new AimState(this, eState.Pull)); return true; }

        return false;
    }

    public void InvokeSSaGay()
    {
        Invoke(nameof(AniTimer), 0.5f);
    }

    public void AniTimer()
    {
        PushIndicator.gameObject.SetActive(false);
        PullIndicator.gameObject.SetActive(false);
    }
}

