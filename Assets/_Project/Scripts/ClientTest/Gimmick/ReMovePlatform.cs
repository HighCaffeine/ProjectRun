using System.Collections;
using UnityEngine;

public class ReMovePlatform : BaseGimmick
{
    [SerializeField] private float shakeTime = 3f;     // ???? ?ð?
    [SerializeField] private float shakePower = 0.05f; // ??? ???? ????
    [SerializeField] private float hangTime = 0.5f;    // ??? + ???? ?? ???
    [SerializeField] private float fallDistance = 15f;  // ???? ???
    [SerializeField] private float fallDuration = 0.5f; // ???? ?ð?
    [SerializeField] private float respawnDelay = 2f;  // ?????? ?? ???
    [SerializeField] private float riseDuration = 5f;  // ?????? ?ð?

    [SerializeField] private GameObject platform;
    [SerializeField] private Transform visual;

    private Coroutine currentCoroutine;
    private bool isRestoring = false;

    private Vector3 startPos;
    private Vector3 originPos;

    private void Awake()
    {
        platform = GetComponentInChildren<MeshRenderer>().gameObject;
        visual = platform.transform;

        startPos = transform.position;
        originPos = visual.localPosition;
    }

    public override void Execute(P_GimmickInteractNtf ntf)
    {
        // 추락 명령이 오고, 복구 중이 아니라면 실행
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

    // private void OnTriggerEnter(Collider other)
    // {
    //     if (isRestoring) return;

    //     if (other.GetComponent<PlayerActor>() && other.transform.position.y > transform.position.y)
    //     {
    //         StartRemove();
    //     }
    // }

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

        // ?浹 ????
        platform.GetComponent<Collider>().enabled = false;

        // 6. 2?? ??? ?? ????
        yield return new WaitForSeconds(respawnDelay);

        currentCoroutine = StartCoroutine(RestorePlatform());
    }

    IEnumerator RestorePlatform()
    {

        // ???? ?????? (5??)
        yield return StartCoroutine(MoveUp(riseDuration));

        // ?浹 ????
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
        Vector3 target = startPos + Vector3.down * fallDistance;

        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / fallDuration;
            t = t * t;

            transform.position = Vector3.Lerp(startPos, target, t);

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

            transform.position = Vector3.Lerp(current, startPos, t);

            yield return null;
        }

        transform.position = startPos;
    }
}