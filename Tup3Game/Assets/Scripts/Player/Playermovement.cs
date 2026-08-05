using System.Collections;
using TMPro;
using Unity.Burst.Intrinsics;
using Unity.VisualScripting;
using UnityEngine;

public class Playermovement : MonoBehaviour
{
    private const float DistanceEpsilon = 0.001f;
    public Dash_animation dashEffectController;
    
    [Header("애니메이션")]
    public Animator animator;
    public SpriteRenderer spriteRenderer;

    [Header("레이캐스트 설정")]
    public LayerMask collisionMask;
    public int horizontalRayCount = 4;
    public int verticalRayCount = 4;
    public float skinWidth = 0.02f;

    [Header("이동 설정")]
    public float moveSpeed = 8f;
    public float jumpForce = 16f;
    public float gravity = -40f;
    public int maxJumpCount = 2;
    public float fallGravityMultiplier = 1f;

    private int jumpCount;

    [Header("대쉬 설정")]
    public float dashSpeed = 25f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 1.0f;

    [Header("점프 인정시간")]
    public float coyoteTime = 0.15f;
    private float coyoteTimer = 0f;

    [Header("가변 점프")]
    [Range(0f, 1f)]
    public float jumpCutMultiplier = 0.5f;

    [Header("미끄러운 바닥")]
    public float slipperyAcceleration = 10f;
    private bool isOnSlippery = false;

    [Header("Water Swim Settings")]
    [SerializeField] private bool isInWater = false;
    [SerializeField] private float waterFlapForce = 8f;      // 점프키 눌렀을 때 위로 튀는 힘
    [SerializeField] private float waterSinkSpeed = 2f;       // 물속 기본 하강 속도 (느리게)
    [SerializeField] private float waterMaxFallSpeed = 3f;    // 하강 속도 상한선 (계속 빨라지지 않게)
    [SerializeField] private float waterGravityMultiplier = 0.3f;

    private BoxCollider2D col;
    private Vector2 velocity;

    private bool canDash = true;
    private bool isDashing = false;
    private float facingDirection = 1f;
    public CollisionInfo collisions;

    private struct RaycastOrigins
    {
        public Vector2 topLeft, topRight, bottomLeft, bottomRight;
    }

    private RaycastOrigins raycastOrigins;


    public float BodySizeX => col.bounds.size.x;
    public float BodySizeY => col.bounds.size.y;

    private Skills skills;
    private ComboAttack combo;

    public struct CollisionInfo
    {
        public bool above, below;
        public bool left, right;

        public void Reset()
        {
            above = below = left = right = false;
        }
    }

    void Awake()
    {
        col = GetComponent<BoxCollider2D>();
        jumpCount = maxJumpCount;
        combo = GetComponent<ComboAttack>();
        skills = GetComponent<Skills>();
        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        
        bool isAiming = skills != null && skills.IsAiming;
        bool isLunging = combo != null && combo.IsLunging;

        if (horizontalInput != 0 && !isAiming && !isLunging)
        {
            facingDirection = horizontalInput > 0 ? 1f : -1f;
            if (isDashing)
            {
                dashEffectController.Change_direction(facingDirection);
            }
        }

        if (spriteRenderer != null)
            spriteRenderer.flipX = facingDirection < 0;
       
        

        if (!isDashing && !isAiming)
        {
            if (isKnockedBack)
            {
                float decel = externalVelocityDecel * Time.deltaTime;
                if (Mathf.Abs(velocity.x) <= decel)
                {
                    velocity.x = 0f;
                    isKnockedBack = false;
                }
                else
                {
                    velocity.x -= Mathf.Sign(velocity.x) * decel;
                }
            }
            else
            {
                float targetVelocityX = isLunging ? 0f : horizontalInput * moveSpeed;
                velocity.x = isOnSlippery
                    ? Mathf.MoveTowards(velocity.x, targetVelocityX, slipperyAcceleration * Time.deltaTime)
                    : targetVelocityX;
            }
            if (isInWater)
            {
                HandleWaterMovement();
            }
            else
            {
                if (collisions.below)
                {
                    if (animator != null)
                    {
                        animator.SetBool("IsGround", true);
                    }

                    velocity.y = -0.3f;
                    jumpCount = maxJumpCount;
                    coyoteTimer = coyoteTime;

                }
                else
                {
                    if (animator != null)
                    {
                        animator.SetBool("IsGround", false);
                    }

                    float appliedGravity = (velocity.y > 0f) ? gravity : gravity * fallGravityMultiplier;
                    velocity.y += appliedGravity * Time.deltaTime;
                    if (!collisions.below && jumpCount == maxJumpCount)
                    {
                        jumpCount = maxJumpCount - 1;
                    }

                    coyoteTimer -= Time.deltaTime;
                }


                if (Input.GetKeyDown(KeyCode.X) && (jumpCount > 0 || coyoteTimer > 0f))
                {
                    Debug.Log($"X pressed. jumpCount={jumpCount}, collisions.below={collisions.below}");
                    velocity.y = jumpForce;
                    if (coyoteTimer > 0f && jumpCount == maxJumpCount - 1)
                    {
                        // 코요테 타임으로 발동된 첫 점프는 이단점프 자원을 소모하지 않음
                    }
                    else
                    {
                        jumpCount--;
                    }
                    coyoteTimer = 0f;
                    if (animator != null)
                        animator.SetTrigger("JumpTrigger");
                }
                if (Input.GetKeyUp(KeyCode.X) && velocity.y > 0f)
                {
                    velocity.y *= jumpCutMultiplier;
                }
            }

            if (Input.GetKeyDown(KeyCode.Z) && canDash && !isLunging)
            {
                if (isLunging)
                {
                    float Changeddirection = horizontalInput > 0 ? 1f : -1f;
                    if (Changeddirection != facingDirection)
                    {
                        facingDirection = Changeddirection;
                    }
                }
                StartCoroutine(DoDash());
                if (animator != null)
                    animator.SetTrigger("DashTrigger");
            }


            else if (isAiming)
            {
                velocity.x = 0f;
            }

            // 공격 입력 처리:
            // - 공격 중이 아니면 콤보 시작
            // - 이미 공격 중이면 "다음 타 예약"만 해둔다 (선입력 버퍼)
        }

        if (animator != null)
        {
            animator.SetFloat("Speed", Mathf.Abs(velocity.x));
            animator.SetFloat("VerticalVelocity", velocity.y);
            
            if (collisions.below && !isDashing)
            {
                float speedRatio = Mathf.Abs(velocity.x) / moveSpeed;
                animator.speed = Mathf.Clamp(speedRatio, 0.7f, 1.3f);
            }
            else
            {
                animator.speed = 1f;   // 공중이나 대쉬 중엔 정상 속도로 복구
            }
        }
        if (!isDashing)
            Move(velocity * Time.deltaTime);
    }

    public void Move(Vector2 moveAmount)
    {
        UpdateRaycastOrigins();
        collisions.Reset();

        if (moveAmount.x != 0)
        {
            HorizontalCollisions(ref moveAmount);
        }

        if (moveAmount.y != 0)
        {
            VerticalCollisions(ref moveAmount);
        }

        transform.Translate(moveAmount, Space.World);

        Physics2D.SyncTransforms();
    }

    private void HorizontalCollisions(ref Vector2 moveAmount)
    {
        float directionX = Mathf.Sign(moveAmount.x);
        float rayLength = Mathf.Abs(moveAmount.x) + skinWidth;

        float raySpacing = (raycastOrigins.topLeft.y - raycastOrigins.bottomLeft.y) / (horizontalRayCount - 1);

        for (int i = 0; i < horizontalRayCount; i++)
        {
            Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
            rayOrigin += Vector2.up * (i * raySpacing);

            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);
            Debug.DrawRay(rayOrigin, Vector2.right * directionX * rayLength, Color.red);

            if (hit)
            {
                float correctedDistance = Mathf.Max(hit.distance - skinWidth, 0f);
                moveAmount.x = correctedDistance * directionX;
                rayLength = Mathf.Max(hit.distance, skinWidth);

                collisions.left = directionX == -1;
                collisions.right = directionX == 1;

                velocity.x = 0f;
            }
        }
    }

    private void VerticalCollisions(ref Vector2 moveAmount)
    {
        float directionY = Mathf.Sign(moveAmount.y);
        float rayLength = Mathf.Abs(moveAmount.y) + skinWidth;
        float raySpacing = (raycastOrigins.bottomRight.x - raycastOrigins.bottomLeft.x) / (verticalRayCount - 1);

        bool groundedThisCheck = false;
        bool slipperyThisCheck = false;

        for (int i = 0; i < verticalRayCount; i++)
        {
            Vector2 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
            rayOrigin += Vector2.right * (i * raySpacing);

            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, collisionMask);
            Debug.DrawRay(rayOrigin, Vector2.up * directionY * rayLength, Color.red);

            if (hit)
            {
                float correctedDistance = Mathf.Max(hit.distance - skinWidth, 0f);
                moveAmount.y = correctedDistance * directionY;
                rayLength = Mathf.Max(hit.distance, skinWidth);
                collisions.below = directionY == -1;
                collisions.above = directionY == 1;

                if (directionY == -1)
                {
                    groundedThisCheck = true;
                    if (hit.collider.CompareTag("Slippery"))
                        slipperyThisCheck = true;
                }

                velocity.y = 0f;
            }
        }
        if (directionY == -1 && groundedThisCheck)
            isOnSlippery = slipperyThisCheck;
    }

    private void UpdateRaycastOrigins()
    {
        Bounds bounds = col.bounds;
        bounds.Expand(skinWidth * -2);

        raycastOrigins.bottomLeft = new Vector2(bounds.min.x, bounds.min.y);
        raycastOrigins.bottomRight = new Vector2(bounds.max.x, bounds.min.y);
        raycastOrigins.topLeft = new Vector2(bounds.min.x, bounds.max.y);
        raycastOrigins.topRight = new Vector2(bounds.max.x, bounds.max.y);
    }

    private IEnumerator DoDash()
    {
        canDash = false;
        isDashing = true;

        if (dashEffectController != null)
        {
            Vector2 pos = dashEffectController.transform.localPosition;
            pos.x = Mathf.Abs(pos.x) * -facingDirection;
            dashEffectController.transform.localPosition = pos;
            dashEffectController.DashEffect(facingDirection);
        }

        float timer = 0f;
        while (timer < dashDuration)
        {
            if (animator != null)
            {
                animator.Play("Dash");
            }

            Vector2 dashMove = new Vector2(facingDirection * dashSpeed * Time.deltaTime, 0f);
            Move(dashMove);    
            timer += Time.deltaTime;
            yield return null;
        }
        dashEffectController.HideEffect();
        isDashing = false;
        velocity.y = 0f;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    public void SetInWater(bool value)
    {
        isInWater = value;
        if (value) velocity.y = 0f; // 물 진입 시 기존 속도 초기화 (자연스러운 진입감)
    }

    private void HandleWaterMovement()
    {
        if (Input.GetKeyDown(KeyCode.X)) // 기존 점프 입력 감지 변수 재활용
        {
            velocity.y = waterFlapForce; // 기존 velocity 무시하고 즉시 덮어씀 -> 플래피버드 느낌
        }
        else
        {
            // 물속 전용 중력: 기존 gravity보다 훨씬 약하게
            velocity.y += gravity * waterGravityMultiplier * Time.deltaTime;

            // 하강 속도 상한 (너무 빨리 가라앉지 않게 클램프)
            velocity.y = Mathf.Max(velocity.y, -waterMaxFallSpeed);
        }
    }


    /*적충돌*/
    private float externalVelocityDecel = 25f;
    private bool isKnockedBack = false;
    public void ApplyKnockback(Vector2 force, float decelSpeed = 25f)
    {
        velocity = force;
        externalVelocityDecel = decelSpeed;
        isKnockedBack = true;
    }
    public bool IsKnockedBack => isKnockedBack;
    public float GetVerticalVelocityForKnockback(float upwardRatio = 0.5f)
    {
        return jumpForce * upwardRatio;
    }

    public void StopHorizontalMovement()
    {
        velocity.x = 0f;
    }


    public bool IsDashing() => isDashing;
    public void ResetVerticalVelocity() => velocity.y = 0f;
    public float GetFacingDirection() => facingDirection;


   
}
