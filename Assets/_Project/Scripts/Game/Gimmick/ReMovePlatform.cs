using System.Collections;
using UnityEngine;

public class ReMovePlatform : BaseGimmick
{
    [SerializeField] private float shakeTime = 3f;     // 덜덜 떠는 시간
    [SerializeField] private float shakePower = 0.05f; // 떨림 강도
    [SerializeField] private float hangTime = 0.5f;    // 멈춤 시간
    [SerializeField] private float fallDistance = 15f;  // 떨어질 거리
    [SerializeField] private float fallDuration = 0.5f; // 떨어지는 속도
    [SerializeField] private float respawnDelay = 2f;  // 재생성 대기 시간
    [SerializeField] private float riseDuration = 5f;  // 올라오는 속도

    [SerializeField] private GameObject platform;
    [SerializeField] private Transform visual;

    private Coroutine currentCoroutine;
    [SerializeField] private bool isRestoring = false;

    private Vector3 originWorldPos; 
    private Vector3 visualLocalPos; 

    private void Awake()
    {
        platform = TargetTransform.gameObject;
        visual = platform.transform;

        originWorldPos = transform.position;   
        visualLocalPos = visual.localPosition;  
    }

    public override void Execute(P_GimmickInteractNtf ntf)
    {
        //Debug.Log($"[ReMove Platform] {ntf.state}, {((eGimmickKey)ntf.gimmickKey).ToString()}");
        if (ntf.state == (byte)eGimmickState.On_Activate && !isRestoring)
        {
            //Debug.Log($"[ReMove Platform] Execute");
            if (currentCoroutine != null) StopCoroutine(currentCoroutine);
            currentCoroutine = StartCoroutine(RemoveSequence());
        }
        else if (ntf.state == (byte)eGimmickState.Restore)
        {
            currentCoroutine = StartCoroutine(RestorePlatform());
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        if (isRestoring) return;

        if (other.gameObject.CompareTag("Player"))
        {
            PlayerActor pActor = other.transform.GetComponent<PlayerActor>();
            if (pActor != null && pActor.IsLocal)
            {
                if (Client.IS_SERVER_PLAY || GameManager.Instance.currentMode == GameManager.PlayMode.Server_Online)
                {
                    P_GimmickInteractReq req = new P_GimmickInteractReq
                    {
                        activeUUID = LocalPlayerInfo.ID,
                        gimmickID = this.gimmickUID,
                        gimmickKey = (byte)eGimmickKey.FallingPlatform,
                        state = (byte)eGimmickState.On_Activate,
                        targetPos = new P_PacketVector3(),
                        param = 0f
                    };
                    Client.TCP.SendPacket2(E_PACKET.GIMMICK_INTERACT_REQ, req);
                }
                else
                {
                    Execute(new P_GimmickInteractNtf { state = (byte)eGimmickState.On_Activate });
                }
            }
        }
    }

    IEnumerator RemoveSequence()
    {
        Debug.Log($"[ReMove Platform] RemoveSequence");

        isRestoring = true;
        yield return new WaitForSeconds(0.5f);

        yield return StartCoroutine(ShakePlatform(shakeTime, shakePower));

        yield return new WaitForSeconds(0.5f);
        yield return new WaitForSeconds(hangTime);

        yield return StartCoroutine(DropPlatform());

        yield return new WaitForSeconds(respawnDelay);

        //currentCoroutine = StartCoroutine(RestorePlatform());
    }

    IEnumerator RestorePlatform()
    {
        yield return StartCoroutine(MoveUp(riseDuration));

        isRestoring = false;
        currentCoroutine = null;
    }

    IEnumerator ShakePlatform(float duration, float maxPower)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float power = Mathf.Lerp(0f, maxPower, elapsed / duration);
            Vector3 offset = Random.insideUnitSphere * power;
            
            visual.localPosition = visualLocalPos + offset; 

            yield return null;
        }

        visual.localPosition = visualLocalPos; 
    }

    IEnumerator DropPlatform()
    {
        float elapsed = 0f;
        Vector3 target = originWorldPos + Vector3.down * fallDistance; 

        // 물리 충돌 끄기
        var col = platform.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fallDuration;
            t = t * t; // 점점 빠르게 (가속도)

            // 월드 좌표 기준으로 하강
            transform.position = Vector3.Lerp(originWorldPos, target, t); 

            yield return null;
        }

        transform.position = target;
    }

    IEnumerator MoveUp(float duration)
    {
        float elapsed = 0f;
        Vector3 current = transform.position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = Mathf.SmoothStep(0f, 1f, t); // 부드럽게 감속

            // 다시 월드 좌표 원상 복구
            transform.position = Vector3.Lerp(current, originWorldPos, t); 

            yield return null;
        }

        transform.position = originWorldPos;

        // 물리 충돌 다시 켜기
        var col = platform.GetComponent<Collider>();
        if (col != null) col.enabled = true;
    }

    private Vector3 gizmos_dest;

    private void OnDrawGizmos()
    {
        if (platform == null) return;

        Gizmos.color = Color.red;

        gizmos_dest = TargetTransform != null ? TargetTransform.position - (Vector3.up * fallDistance) : transform.position - (Vector3.up * fallDistance);
        Gizmos.DrawLine(platform.transform.position, gizmos_dest);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(gizmos_dest, TargetTransform.localScale);
    }
}