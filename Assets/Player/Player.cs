using UnityEngine;
using UnityEngine.UI;
using System.Collections;
public class Player : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] Sprite[] JumpWindupAnimRight;
    [SerializeField] Sprite[] JumpWindupAnimLeft;
    [SerializeField] Sprite[] AirAnimRight;
    [SerializeField] Sprite[] AirAnimLeft;
    [SerializeField] Sprite[] LandAnimRight;
    [SerializeField] Sprite[] LandAnimLeft;
    [SerializeField] Sprite[] IdleAnim;
    public Sprite[] currentAnimation;
    SpriteRenderer spriteRenderer;
    int frameIndex = 0;
    public float animationSpeed;
    float animationTimer = 0f;
    Coroutine currentAnimationCoroutine;
    bool isWindingUp = false;
    bool isInAir = false;
    bool isFacingRight = true;
    Color defaultColor = Color.white;
    [SerializeField] Color windupColor = Color.red;
    [Header("Jump Charge")]
    [SerializeField] float jumpPower = 0f;
    [SerializeField] float maxJumpPower = 5f;
    [SerializeField] float chargeRate = 1f;
    Rigidbody2D rb;


    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            defaultColor = spriteRenderer.color;
    }

    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            jumpPower += chargeRate * Time.deltaTime;
            jumpPower = Mathf.Min(jumpPower, maxJumpPower);
            
            // Start windup animation if not already playing
            if (!isWindingUp)
            {
                isWindingUp = true;
                // choose left/right windup based on mouse position
                Camera cam = Camera.main;
                if (cam != null)
                {
                    Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
                    mouseWorld.z = transform.position.z;
                    isFacingRight = mouseWorld.x >= transform.position.x;
                }

                Sprite[] chosenWindup = isFacingRight ? JumpWindupAnimRight : JumpWindupAnimLeft;
                ChangeAnimation(chosenWindup);
                if (currentAnimationCoroutine != null)
                    StopCoroutine(currentAnimationCoroutine);
                currentAnimationCoroutine = StartCoroutine(WindupRoutine(chosenWindup));
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            isWindingUp = false;
            if (currentAnimationCoroutine != null)
                StopCoroutine(currentAnimationCoroutine);
            // reset color when leaving windup
            if (spriteRenderer != null)
                spriteRenderer.color = defaultColor;
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

        // start in-air state and animation
        jumpPower = 0f;
        isInAir = true;
        // choose appropriate air animation based on direction
        isFacingRight = jumpDirection.x >= 0f;
        Sprite[] airAnim = isFacingRight ? AirAnimRight : AirAnimLeft;
        ChangeAnimation(airAnim);
        if (currentAnimationCoroutine != null)
            StopCoroutine(currentAnimationCoroutine);
        currentAnimationCoroutine = StartCoroutine(PlayLoopingAnimation(airAnim));
    }
    public void ChangeAnimation(Sprite[] animationToChangeTo)
    {
        if(currentAnimation != animationToChangeTo)
        {
            currentAnimation = animationToChangeTo;
            animationSpeed = currentAnimation.Length;
            frameIndex = 0;
            animationTimer = 0f;
        }
    }
    IEnumerator WindupRoutine(Sprite[] animFrames)
    {
        while (isWindingUp && animFrames == currentAnimation)
        {
            // Calculate animation progress based on jump power progress
            float chargeProgress = Mathf.Clamp01(jumpPower / maxJumpPower);
            
            // Map charge progress to animation frame index
            frameIndex = Mathf.Clamp(Mathf.FloorToInt(chargeProgress * (animFrames.Length - 1)), 0, animFrames.Length - 1);
            
            // Update sprite and tint
            if (spriteRenderer != null && frameIndex < animFrames.Length)
            {
                spriteRenderer.sprite = animFrames[frameIndex];
                spriteRenderer.color = windupColor;
            }

            // If we've reached the last frame but still winding up, loop animation frames
            if (frameIndex >= animFrames.Length - 1 && isWindingUp)
            {
                frameIndex = 0;
            }

            yield return new WaitForSeconds(1f / Mathf.Max(1f, animationSpeed));
        }

        // Restore color when windup ends
        if (spriteRenderer != null)
            spriteRenderer.color = defaultColor;
    }

    IEnumerator PlayLoopingAnimation(Sprite[] animFrames)
    {
        int idx = 0;
        while (isInAir && animFrames == currentAnimation)
        {
            if (spriteRenderer != null)
                spriteRenderer.sprite = animFrames[idx];

            idx = (idx + 1) % animFrames.Length;
            yield return new WaitForSeconds(1f / Mathf.Max(1f, animationSpeed));
        }
        currentAnimationCoroutine = null;
    }

    IEnumerator PlayOnceAnimation(Sprite[] animFrames)
    {
        for (int i = 0; i < animFrames.Length; i++)
        {
            if (spriteRenderer != null)
                spriteRenderer.sprite = animFrames[i];
            yield return new WaitForSeconds(1f / Mathf.Max(1f, animationSpeed));
        }

        // after landing animation, go to idle
        ChangeAnimation(IdleAnim);
        if (spriteRenderer != null)
            spriteRenderer.color = defaultColor;
        currentAnimationCoroutine = null;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isInAir)
        {
            isInAir = false;
            if (currentAnimationCoroutine != null)
                StopCoroutine(currentAnimationCoroutine);

            Sprite[] landAnim = isFacingRight ? LandAnimRight : LandAnimLeft;
            ChangeAnimation(landAnim);
            currentAnimationCoroutine = StartCoroutine(PlayOnceAnimation(landAnim));
        }
    }
}
