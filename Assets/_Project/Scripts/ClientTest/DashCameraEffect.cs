using System.Collections;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.AdaptivePerformance;
public class DashCameraEffect : MonoBehaviour
{
    [SerializeField]
    private CinemachineCamera cam;
    float camfov; //기존 카메라 FOV
    float dashFov = 60; // 대시 시 카메라 FOV    
    float transitionDuration = 0.2f; // FOV 전환 시간

    [SerializeField]
    Transform playerTransform; // 플레이어 위치 참조
    [SerializeField]
    Transform dashAnchor; // 카메라 위치 참조
    [SerializeField]
    float holdTime = 0.3f;
    void Awake()
    {
        cam = GetComponent<CinemachineCamera>();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        camfov = cam.Lens.FieldOfView;
    }

    // Update is called once per frame
    void Update()
    {


    }

    public void OnDash()
    {
        Debug.Log("DashCameraEffect OnDash called");
        StartCoroutine(DashCameraRoutine());

    }

    IEnumerator DashCameraRoutine()
    {
        dashAnchor.position = playerTransform.position; // 대시 시작 시 플레이어 위치로 앵커 이동
        cam.Follow = dashAnchor; // 카메라가 앵커를 따라가도록 설정
        yield return new WaitForSeconds(holdTime);
        cam.Follow = playerTransform; // 대시 종료 후 카메라가 플레이어를 따라가도록 설정
        StartCoroutine(ChangeFOV(camfov, dashFov, transitionDuration)); // 확대
        yield return StartCoroutine(ChangeFOV(dashFov, camfov, transitionDuration)); // 복구
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
}
