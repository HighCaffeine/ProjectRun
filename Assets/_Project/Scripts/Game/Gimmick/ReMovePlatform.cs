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
            StopAllCoroutines();
            StartCoroutine(RemoveSequence());
        }
        else if (ntf.state == (byte)eGimmickState.Restore)
        {
            StopAllCoroutines();
            StartCoroutine(RestorePlatform());
        }
    }

    private void OnTriggerEnter(Collider other)
    {
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