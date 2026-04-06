using UnityEngine;

public class KnockbackTest : MonoBehaviour
{
    [SerializeField]
    private float power = 10f;
    [SerializeField]
    private float range = 5f;
    [SerializeField]
    private float moveSpeed = 5f;
    private CharacterController controller;
    [SerializeField]
    private float yVelocity;

    [SerializeField] private Transform pivot;
    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        Move(); 

        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("밀치기");
            TryKnockback(false);
        }

        if (Input.GetMouseButtonDown(1))
        {
            TryKnockback(true);
        }

        //  샌드백 생성
        if (Input.GetKeyDown(KeyCode.Q))
        {
            SpawnDummy();
        }
    }

    void Move()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 isoForward = new Vector3(1f, 0f, 1f).normalized;
        Vector3 isoRight = new Vector3(1f, 0f, -1f).normalized;

        Vector3 dir = (isoForward * v + isoRight * h).normalized;
        if (dir.magnitude > 0.1f)
        {
            pivot.rotation = Quaternion.LookRotation(dir);
        }

        Vector3 move = dir * moveSpeed;

        if (controller.isGrounded && yVelocity < 0)
        {
            yVelocity = -2f;
        }

        yVelocity += -9.81f * Time.deltaTime;
        move.y = yVelocity;

        controller.Move(move * Time.deltaTime);
    }

    void TryKnockback(bool isPull)
    {
        Vector3 boxCenter = transform.position + transform.forward * 2f + Vector3.up * 0.5f;
        Vector3 boxHalfExtents = new Vector3(1.5f, 1f, 2f);

        Collider[] hits = Physics.OverlapBox(boxCenter, boxHalfExtents, transform.rotation);

        foreach (var hit in hits)
        {
            KnockbackDummy dummy = hit.GetComponentInParent<KnockbackDummy>();

            if (dummy != null)
            {
                Vector3 dir;

                if (isPull)
                    dir = Vector3.zero;
                else
                    dir = (hit.transform.position - transform.position).normalized;

                dummy.StartKnockback(dir, power, isPull, transform.position);
            }
        }
    }

    void SpawnDummy()
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        obj.transform.position = transform.position + transform.forward * 3f;

        obj.AddComponent<CharacterController>();
        obj.AddComponent<KnockbackDummy>();
    }
}