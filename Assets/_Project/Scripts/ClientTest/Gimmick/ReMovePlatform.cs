using System.Collections;
using UnityEngine;

public class ReMovePlatform : BaseGimmick
{
    [SerializeField] private float shakeTime = 3f;     // ???? ?ï¿½ï¿½?
    [SerializeField] private float shakePower = 0.05f; // ??? ???? ????
    [SerializeField] private float hangTime = 0.5f;    // ??? + ???? ?? ???
    [SerializeField] private float fallDistance = 15f;  // ???? ???
    [SerializeField] private float fallDuration = 0.5f; // ???? ?ï¿½ï¿½?
    [SerializeField] private float respawnDelay = 2f;  // ?????? ?? ???
    [SerializeField] private float riseDuration = 5f;  // ?????? ?ï¿½ï¿½?


    [SerializeField] private GameObject platform;
    [SerializeField] private Transform visual;

    private Coroutine currentCoroutine;
    private bool isRestoring = false;

    private GameObject startPos;
    private GameObject endPos;
    private Vector3 originPos;

    public void SetStartPos(GameObject start) { startPos = start; }
    public void SetEndPos(GameObject end) { endPos = end; }


    private void Awake()
    {
        platform = GetComponentInChildren<MeshRenderer>().gameObject;
        visual = platform.transform;

        originPos = transform.position;
        //originPos = visual.localPosition;
    }

    public override void Execute(P_GimmickInteractNtf ntf)
    {
        // ï¿½ß¶ï¿½ ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ ï¿½ï¿½ï¿½ï¿½, ï¿½ï¿½ï¿½ï¿½ ï¿½ï¿½ï¿½ï¿½ ï¿½Æ´Ï¶ï¿½ï¿? ï¿½ï¿½ï¿½ï¿½
        if (ntf.state == (byte)eGimmickState.On_Activate && !isRestoring)
        {
            if (currentCoroutine != null) StopCoroutine(currentCoroutine);
            currentCoroutine = StartCoroutine(RemoveSequence());
        }
    }

    public void StartRemove()
    {
        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);

        currentCoroutine = StartCoroutine(RemoveSequence());
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isRestoring) return; // ÀÌ¹Ì ¹«³ÊÁ³°Å³ª º¹±¸ ÁßÀÌ¸é ¹«½Ã

        PlayerActor pActor = other.GetComponent<PlayerActor>();

        // ¿ÀÁ÷ ³» Ä³¸¯ÅÍ°¡ ¹â¾ÒÀ» ¶§¸¸ ¿äÃ»
        if (pActor != null && pActor.IsLocal)
        {
            if (GameManager.Instance.currentMode == GameManager.PlayMode.Server_Online)
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
                // ¿ÀÇÁ¶óÀÎ Å×½ºÆ®¿ë
                Execute(new P_GimmickInteractNtf { state = (byte)eGimmickState.On_Activate });
            }
        }
    }

    IEnumerator RemoveSequence()
    {
        isRestoring = true;
        // 1. ?????? 0.5?? ???
        yield return new WaitForSeconds(0.5f);

        // 2~3. ???? (???? ??????)
        yield return StartCoroutine(ShakePlatform(shakeTime, shakePower));

        // 4. ??? ???? ???? ???? 0.5??
        yield return new WaitForSeconds(0.5f);

        // 5. ???? ?? ???
        yield return new WaitForSeconds(hangTime);

        // ????
        yield return StartCoroutine(DropPlatform());

        // ?ï¿½ï¿½ ????
        platform.GetComponent<Collider>().enabled = false;

        // 6. 2?? ??? ?? ????
        yield return new WaitForSeconds(respawnDelay);

        currentCoroutine = StartCoroutine(RestorePlatform());
    }

    IEnumerator RestorePlatform()
    {

        // ???? ?????? (5??)
        yield return StartCoroutine(MoveUp(riseDuration));

        // ?ï¿½ï¿½ ????
        platform.GetComponent<Collider>().enabled = true;

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
            visual.localPosition = originPos + offset;

            yield return null;
        }

        visual.localPosition = originPos;
    }

    IEnumerator DropPlatform()
    {
        float elapsed = 0f;
        Vector3 target = startPos.transform.position + Vector3.down * fallDistance;

        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / fallDuration;
            t = t * t;

            transform.position = Vector3.Lerp(startPos.transform.position, target, t);

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

            transform.position = Vector3.Lerp(current, startPos.transform.position, t);

            yield return null;
        }

        transform.position = startPos.transform.position;
    }
}