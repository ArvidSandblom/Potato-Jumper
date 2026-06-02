using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Jump Charge")]
    [SerializeField] float jumpPower = 0f;
    [SerializeField] float maxJumpPower = 20f;
    [SerializeField] float chargeRate = 10f;

    Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            jumpPower += chargeRate * Time.deltaTime;
            jumpPower = Mathf.Min(jumpPower, maxJumpPower);
        }

        if (Input.GetMouseButtonUp(0))
        {
            JumpTowardMouse();
        }
    }

    void JumpTowardMouse()
    {
        if (jumpPower <= 0f)
            return;

        Camera cam = Camera.main;
        if (cam == null)
            return;

        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = transform.position.z;

        Vector2 jumpDirection = (mouseWorld - transform.position).normalized;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(jumpDirection * jumpPower, ForceMode2D.Impulse);

        jumpPower = 0f;
    }
}
