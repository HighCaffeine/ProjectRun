using System.Collections;
using UnityEngine;

public class ReMovePlatform : BaseGimmick
{
    [SerializeField] private float shakeTime = 3f;
    [SerializeField] private float shakePower = 0.05f;
    [SerializeField] private float hangTime = 0.5f;
    [SerializeField] private float fallDistance = 15f;
    [SerializeField] private float fallDuration = 0.5f;
    [SerializeField] private float riseDuration = 2f;

    [SerializeField] private GameObject platform;
    [SerializeField] private Transform visual;

    private Vector3 originWorldPos;
    private Vector3 visualLocalPos;

    private bool isTriggered = false;
    private bool isRestoring = false;

    private void Awake()
    {
        platform = TargetTransform.gameObject;
        visual = platform.transform;

        originWorldPos = transform.position;
        visualLocalPos = visual.localPosition;
    }

    public override void Execute(P_GimmickInteractNtf ntf)
    {
        if (ntf.state == (byte)eGimmickState.On_Activate)
        {
            if (isRestoring) return;
            StopAllCoroutines();
            long triggerServerTime = ntf.timestamp;
            StartCoroutine(RemoveSequenceWithSync(triggerServerTime));
        }
        else if (ntf.state == (byte)eGimmickState.Restore)
        {
            StopAllCoroutines();
            isRestoring = true;
            StartCoroutine(RestorePlatform());
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isTriggered) return;

        if (other.gameObject.CompareTag("Player"))
        {
            PlayerActor pActor = other.transform.GetComponent<PlayerActor>();
            if (pActor != null && pActor.IsLocal)
            {
                if (Client.IS_SERVER_PLAY || GameManager.Instance.currentMode == GameManager.PlayMode.Server_Online)
                {
                    if (!GameManager.Instance.isHost) return;

                    isTriggered = true;

                    P_GimmickInteractReq req = new P_GimmickInteractReq
                    {
                        activeUUID = LocalPlayerInfo.ID,
                        gimmickID = this.gimmickUID,
                        gimmickKey = (byte)eGimmickKey.FallingPlatform,
                        state = (byte)eGimmickState.On_Activate,
                        targetPos = new P_PacketVector3(),
                        param = 0f,
                        timestamp = NetworkTimeManager.Instance.GetServerTime()
                    };
                    Client.TCP.SendPacket2(E_PACKET.GIMMICK_INTERACT_REQ, req);
                }
            }
        }
    }

    IEnumerator RemoveSequence()
    {
        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(ShakePlatform(shakeTime, shakePower));
        yield return new WaitForSeconds(hangTime);
        yield return StartCoroutine(DropPlatform());
        //올라오는 로직 삭제 (서버 처리)
    }

    IEnumerator RemoveSequenceWithSync(long triggerTime)
    {
        long currentTime = NetworkTimeManager.Instance.GetServerTime();
        // 패킷이 도달할 때까지 지연된 시간 계산
        float elapsedTime = Mathf.Max(0f, (currentTime - triggerTime) / 1000f);

        // 초기 대기 시간
        float initialDelay = 0.5f;
        if (elapsedTime < initialDelay)
        {
            yield return new WaitForSeconds(initialDelay - elapsedTime);
            elapsedTime = 0f;
        }
        else
        {
            elapsedTime -= initialDelay;
        }

        // 흔들림 단계 동기화
        if (elapsedTime < shakeTime)
        {
            // 남은 시간만큼만 흔듦
            yield return StartCoroutine(ShakePlatform(shakeTime - elapsedTime, shakePower));
            elapsedTime = 0f;
        }
        else
        {
            elapsedTime -= shakeTime;
            visual.localPosition = visualLocalPos;
        }

        // 매달려 있는 단계 동기화
        if (elapsedTime < hangTime)
        {
            yield return new WaitForSeconds(hangTime - elapsedTime);
            elapsedTime = 0f;
        }
        else
        {
            elapsedTime -= hangTime;
        }

        // 추락이 시작되었어야 하는 경우, 추락 내부에서 elapsedTime만큼 진행된 상태로 시작
        yield return StartCoroutine(DropPlatformWithOffset(elapsedTime));
    }

    IEnumerator RestorePlatform()
    {
        yield return StartCoroutine(MoveUp(riseDuration));
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

        var col = platform.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fallDuration;
            t = t * t;
            transform.position = Vector3.Lerp(originWorldPos, target, t);
            yield return null;
        }
        transform.position = target;
    }

    IEnumerator DropPlatformWithOffset(float offsetTime)
    {
        float elapsed = offsetTime; // 이미 흘러간 시간부터 시작
        Vector3 target = originWorldPos + Vector3.down * fallDistance;

        var col = platform.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fallDuration;
            t = t * t;
            transform.position = Vector3.Lerp(originWorldPos, target, t);
            yield return null;
        }
        transform.position = target;
    }

    public override void ResetGimmick()
    {
        StopAllCoroutines();
        isTriggered = false;
        isRestoring = false;

        // 위치 및 비주얼 초기화
        TargetTransform.position = originWorldPos;
        visual.localPosition = visualLocalPos;

        // 콜라이더 다시 켜기
        var col = platform.GetComponent<Collider>();
        if (col != null) col.enabled = true;
    }

    IEnumerator MoveUp(float duration)
    {
        float elapsed = 0f;
        Vector3 current = transform.position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = Mathf.SmoothStep(0f, 1f, t);
            transform.position = Vector3.Lerp(current, originWorldPos, t);
            yield return null;
        }
        transform.position = originWorldPos;

        var col = platform.GetComponent<Collider>();
        if (col != null) col.enabled = true;

        isTriggered = false;
        isRestoring = false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Vector3 targetPos = transform.position - (Vector3.up * fallDistance);
        Gizmos.DrawLine(transform.position, targetPos);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(targetPos, Vector3.one * TargetTransform.localScale.x);
    }
}