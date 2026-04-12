using System.Collections;
using UnityEngine;

public class ReMovePlatform : MonoBehaviour
{
    [SerializeField] private float shakeTime = 3f;     // 진동 시간
    [SerializeField] private float shakePower = 0.05f; // 최대 진동 세기
    [SerializeField] private float hangTime = 0.5f;    // 멈칫 + 낙하 전 대기
    [SerializeField] private float fallDistance = 15f;  // 낙하 거리
    [SerializeField] private float fallDuration = 0.5f; // 낙하 시간
    [SerializeField] private float respawnDelay = 2f;  // 떨어진 후 대기
    [SerializeField] private float riseDuration = 5f;  // 올라오는 시간

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

    public void StartRemove()
    {
        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);

        currentCoroutine = StartCoroutine(RemoveSequence());
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isRestoring) return;

        if (other.GetComponent<PlayerActor>() && other.transform.position.y > transform.position.y)
        {
            StartRemove();
        }
    }

    IEnumerator RemoveSequence()
    {
        isRestoring = true;
        // 1. 올라오고 0.5초 대기
        yield return new WaitForSeconds(0.5f);

        // 2~3. 진동 (점점 강해짐)
        yield return StartCoroutine(ShakePlatform(shakeTime, shakePower));

        // 4. 최대 진동 상태 유지 0.5초
        yield return new WaitForSeconds(0.5f);

        // 5. 낙하 전 멈칫
        yield return new WaitForSeconds(hangTime);

        // 낙하
        yield return StartCoroutine(DropPlatform());

        // 충돌 끄기
        platform.GetComponent<Collider>().enabled = false;

        // 6. 2초 대기 후 복구
        yield return new WaitForSeconds(respawnDelay);

        currentCoroutine = StartCoroutine(RestorePlatform());
    }

    IEnumerator RestorePlatform()
    {

        // 천천히 올라오기 (5초)
        yield return StartCoroutine(MoveUp(riseDuration));

        // 충돌 복구
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