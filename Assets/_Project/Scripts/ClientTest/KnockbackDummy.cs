using UnityEngine;

public class KnockbackDummy : MonoBehaviour
{
    [SerializeField]
    private float duration = 0.25f;

    private float timer;
    private bool isActive;

    private Vector3 knockbackDir;
    private float initialPower;
    private float currentPower;

    private bool isPull;
    private Vector3 casterPos;

    [SerializeField]
    private const float STOP_DISTANCE = 0.5f;
    [SerializeField]
    private const float K = 3f; 

    private CharacterController controller;
    [SerializeField]
    private float yVelocity;

    [SerializeField]
    private float liftPower = 2f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    public void StartKnockback(Vector3 dir, float power, bool isPull, Vector3 casterPos)
    {
        this.knockbackDir = dir.normalized;
        this.knockbackDir.y = 0f;
        this.initialPower = power;
        this.currentPower = power;
        this.isPull = isPull;
        this.casterPos = casterPos;

        yVelocity = liftPower;
        controller.Move(Vector3.up * 0.1f);

        timer = 0f;
        isActive = true;
    }

    void Update()
    {
        ApplyGravity();

        if (!isActive) return;

        timer += Time.deltaTime;

        float t = Mathf.Clamp01(timer / duration);

        float logValue = Mathf.Log(1 + K * t) / Mathf.Log(1 + K);

        if (isPull)
        {

            float smooth = logValue * logValue; 

            currentPower = initialPower * smooth;

            Vector3 dir = casterPos - transform.position;
            dir.y = 0f;
            knockbackDir = dir.normalized;
        }
        else
        {
  
            currentPower = initialPower * (1f - logValue);
        }

        // 거리 제한
        if (isPull)
        {
            Vector3 myPos = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 targetPos = new Vector3(casterPos.x, 0, casterPos.z);
            float dist = Vector3.Distance(myPos, targetPos);

            if (dist <= STOP_DISTANCE)
            {
                currentPower = 0f;
                timer = duration;
            }
        }

        Vector3 move = knockbackDir * currentPower;
        controller.Move(move * Time.deltaTime);

        if (timer >= duration)
        {
            isActive = false;
        }
    }

    void ApplyGravity()
    {
        if (controller.isGrounded && yVelocity < 0)
        {
            yVelocity = -2f;
        }

        yVelocity += -9.81f * Time.deltaTime;

        Vector3 move = new Vector3(0, yVelocity, 0);
        controller.Move(move * Time.deltaTime);
    }
}