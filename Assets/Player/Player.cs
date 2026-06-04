using UnityEngine;
using UnityEngine.UI;
using System.Collections;
public class Player : MonoBehaviour
{
    [SerializeField] Image[] endTexts;
    [Header("Animation")]
    [SerializeField] Sprite[] JumpWindupAnim;
    [SerializeField] Sprite[] AirAnim;
    [SerializeField] Sprite[] LandAnim;
    [SerializeField] Sprite[] IdleAnim;
    public Sprite[] currentAnimation;
    SpriteRenderer spriteRenderer;
    int frameIndex = 0;
    public float animationSpeed;
    Coroutine currentAnimationCoroutine;
    bool isWindingUp = false;
    bool isInAir = false;
    bool isFacingRight = true;
    [Header("Jump Charge")]
    [SerializeField] float jumpPower = 0f;
    [SerializeField] float maxJumpPower = 5f;
    [SerializeField] float chargeRate = 1.5f;
    Rigidbody2D rb;


    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            jumpPower += chargeRate * Time.deltaTime;
            jumpPower = Mathf.Min(jumpPower, maxJumpPower);
            
            // ändra riktning på sprite så att potatisen alltid "tittar" mot musen
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
                mouseWorld.z = transform.position.z;
                isFacingRight = mouseWorld.x >= transform.position.x;
                UpdateSpriteDirection();
            }
            
            // check om vi inte redan är i windup, så att vi inte startar om animationen varje frame
            if (!isWindingUp)
            {
                isWindingUp = true;
                ChangeAnimation(JumpWindupAnim);
                if (currentAnimationCoroutine != null)
                    StopCoroutine(currentAnimationCoroutine);
                currentAnimationCoroutine = StartCoroutine(WindupRoutine(JumpWindupAnim));
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            isWindingUp = false;
            if (currentAnimationCoroutine != null)
                StopCoroutine(currentAnimationCoroutine);
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

        // state och animation i luften
        jumpPower = 0f;
        isInAir = true;
        isFacingRight = jumpDirection.x >= 0f;
        UpdateSpriteDirection();
        ChangeAnimation(AirAnim);
        if (currentAnimationCoroutine != null)
            StopCoroutine(currentAnimationCoroutine);
        currentAnimationCoroutine = StartCoroutine(PlayLoopingAnimation(AirAnim));
    }
    
    public void ChangeAnimation(Sprite[] animationToChangeTo)
    {
        if(currentAnimation != animationToChangeTo)
        {
            currentAnimation = animationToChangeTo;
            animationSpeed = currentAnimation.Length;
            frameIndex = 0;
        }
    }

    void UpdateSpriteDirection()
    {
        if (spriteRenderer != null)
        {
            // vänd sprite baserat på isFacingRight så att potatisen alltid "tittar" mot musen
            spriteRenderer.flipX = !isFacingRight;
        }
    }
    IEnumerator WindupRoutine(Sprite[] animFrames)
    {
        while (isWindingUp && animFrames == currentAnimation)
        {
            // updaterar frame index baserat på hur mycket jumpPower har laddats, så att det ser ut som att potatisen laddar upp ju mer jumpPower den har
            // sista frame i animFrames är full charge, första är ingen charge, och däremellan är det proportionellt
            float chargeProgress = Mathf.Clamp01(jumpPower / maxJumpPower);
            frameIndex = Mathf.Clamp(Mathf.FloorToInt(chargeProgress * (animFrames.Length - 1)), 0, animFrames.Length - 1);

            if (spriteRenderer != null && frameIndex < animFrames.Length)
            {
                spriteRenderer.sprite = animFrames[frameIndex];
            }

            // väntar bara en frame så att animationen inte går snabbare än den borde även om animationSpeed är hög
            yield return null;
        }
    }

    IEnumerator PlayLoopingAnimation(Sprite[] animFrames)
    {
        while (isInAir && animFrames == currentAnimation)
        {
            // Benen är i "sväv" när potat rör sig uppåt, och i "fall" när den rör sig nedåt
            int spriteIdx = rb.linearVelocity.y > 0f ? 0 : 1;
            
            // Olikt från windup, här vill vi inte att frame index ska gå utanför arrayen även om animationSpeed är hög, så vi klämmer det istället
            spriteIdx = Mathf.Min(spriteIdx, animFrames.Length - 1);
            
            if (spriteRenderer != null && spriteIdx < animFrames.Length)
                spriteRenderer.sprite = animFrames[spriteIdx];

            yield return null;
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

        // vid landning vill vi inte gå tillbaka till idle direkt
        currentAnimationCoroutine = null;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isInAir)
        {
            isInAir = false;
            if (currentAnimationCoroutine != null)
                StopCoroutine(currentAnimationCoroutine);

            Sprite[] landAnim = LandAnim;
            ChangeAnimation(landAnim);
            currentAnimationCoroutine = StartCoroutine(PlayOnceAnimation(landAnim));
        }

        if(collision.gameObject.tag == "Finish")
        {
            // Handle finish collision

        }
    }
}
