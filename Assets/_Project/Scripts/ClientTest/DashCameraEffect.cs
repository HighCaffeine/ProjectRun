using System.Collections;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.AdaptivePerformance;
public class DashCameraEffect : MonoBehaviour
{
    [SerializeField]
    private CinemachineCamera cam;
    [SerializeField]
    float camfov; //���� ī�޶� FOV
    [SerializeField]
    float dashFov = 20; // ��� �� ī�޶� FOV    
    float transitionDuration = 0.2f; // FOV ��ȯ �ð�

    [SerializeField]
    Transform playerTransform; // �÷��̾� ��ġ ����
    [SerializeField]
    Transform dashAnchor; // ī�޶� ��ġ ����
    [SerializeField]
    float holdTime = 0.3f;

    Vector3 originalCamPos;

    [SerializeField]
    GameObject camPivot;
    bool isDashing = false;


    float camDistance;
    float beforeDistance;
    float curDistance;
    void Awake()
    {
        cam = GetComponent<CinemachineCamera>();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        camfov = cam.Lens.FieldOfView;
        dashAnchor = GameManager.Instance.playerDashAnchor;
    }

    // Update is called once per frame
    void Update()
    {
        if (isDashing)
        {
            camPivot.transform.position = originalCamPos;
        }
    }

    public void OnDash()
    {
        originalCamPos = camPivot.transform.position; // ī�޶��� ���� ��ġ ����
        isDashing = true;
        StartCoroutine(DashCameraRoutine());

    }

    IEnumerator DashCameraRoutine()
    {
        dashAnchor.position = playerTransform.position; // ��� ���� �� �÷��̾� ��ġ�� ��Ŀ �̵�

        yield return StartCoroutine(ChangeFOV(camfov, dashFov, transitionDuration)); // Ȯ��

        yield return new WaitForSeconds(holdTime);

        StartCoroutine(CamReset());

        yield return StartCoroutine(ChangeFOV(dashFov, camfov, transitionDuration)); // ����


    }
    IEnumerator ChangeFOV(float from, float to, float duration)
    {
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            cam.Lens.FieldOfView = Mathf.Lerp(from, to, t);

            yield return null;
        }

        cam.Lens.FieldOfView = to;
    }
    [SerializeField]
    float fff = 40f;
    IEnumerator CamReset()
    {
        isDashing = false;
        Vector3 startPos = camPivot.transform.localPosition;
        Vector3 targetPos = Vector3.zero;

        float time = 0f;
        float duration = 0.3f;
        float k = 3f;
        beforeDistance = float.MaxValue;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            float logValue = Mathf.Log(1 + k * t) / Mathf.Log(1 + k);
            float smooth = logValue * logValue; // �α� ������   
            float power = fff * smooth;


            camDistance = Vector3.Distance(camPivot.transform.localPosition, targetPos);
            camPivot.transform.localPosition = Vector3.Lerp(startPos, targetPos, power);

            if (beforeDistance <= camDistance)
            {
                break;
            }
            beforeDistance = camDistance;
            yield return null;
        }
        camPivot.transform.localPosition = Vector3.zero;
    }

}
