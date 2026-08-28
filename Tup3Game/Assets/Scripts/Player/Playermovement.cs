using System.Collections;
using System.Collections.Generic;
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
    public LayerMask verticalCollisionMask;
    public LayerMask horizontalCollisionMask;
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
    [SerializeField] private float slipperyStartAcceleration = 20f;
    [SerializeField] private float slipperyStopDeceleration = 28f;
    [SerializeField] private float slipperyTurnAcceleration = 36f;
    private bool isOnSlippery = false;

    [Header("Water Swim Settings")]
    [SerializeField] private bool isInWater = false;
    [SerializeField] private float waterFlapForce = 8f;      // 점프키 눌렀을 때 위로 튀는 힘
    [SerializeField] private float waterMaxFallSpeed = 3f;    // 하강 속도 상한선 (계속 빨라지지 않게)
    [SerializeField] private float waterFastDescendSpeed = 8f;
    [SerializeField] private KeyCode waterDescendKey = KeyCode.DownArrow;
    [SerializeField] private float waterGravityMultiplier = 0.3f;
    private bool legacyWaterState;
    private readonly HashSet<int> activeWaterSources = new HashSet<int>();

    private BoxCollider2D col;
    private Vector2 velocity;
    private Vector2 externalVelocity;
    private Vector2 stormVelocity;
    private float previous_y;
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
        legacyWaterState = isInWater;
    }

    void Update()
    {
        if (PauseManager.IsPaused || DialogueManager.IsDialogueActive) return;

        float horizontalInput = Input.GetAxisRaw("Horizontal");
        
        bool isAiming = skills != null && skills.IsAiming;
        bool isLunging = combo != null && combo.IsLunging;
        bool isAttacking = combo != null && combo.IsAttacking;

        if (horizontalInput != 0 && !isAiming && !isLunging)
        {
            facingDirection = horizontalInput > 0 ? 1f : -1f;
            if (isDashing)
            {
                if (dashEffectController != null)
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

                if (isOnSlippery)
                {
                    float response = GetSlipperyResponse(targetVelocityX);
                    velocity.x = Mathf.MoveTowards(
                        velocity.x,
                        targetVelocityX,
                        response * Time.deltaTime
                    );
                }
                else
                {
                    velocity.x = targetVelocityX;
                }
            }
            if (isInWater)
            {
                HandleWaterMovement();
                if (collisions.below)
                {
                    if (Mathf.Abs(transform.position.y - previous_y) < 0.001f)
                    {
                        if (animator != null)
                        {
                            animator.SetBool("IsGround", true);
                        }
                    }
                }

                previous_y = transform.position.y;
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

            velocity += externalVelocity;
            externalVelocity = Vector2.zero;

            if (Input.GetKeyDown(KeyCode.Z) && canDash && !isLunging)
            {
                StartCoroutine(DoDash());
                if (animator != null)
                    animator.SetTrigger("DashTrigger");
            }
        }
        else if (isAiming)
        {
            velocity.x = 0f;
        }

        if (animator != null)
        {
            if (!isAttacking)
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
        }
        if (!isDashing)
        {
            Vector2 finalVelocity = velocity;

            finalVelocity.x += stormVelocity.x;

            Move(finalVelocity * Time.deltaTime);

            stormVelocity = Vector2.zero;
        }
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

            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, horizontalCollisionMask);
            Debug.DrawRay(rayOrigin, Vector2.right * directionX * rayLength, Color.red);

            if (hit)
            {
                // 단방향 플랫폼은 옆면을 벽으로 사용하지 않는다.
                // 점프로 플랫폼을 통과하는 도중 옆 레이가 플랫폼 내부를 감지하면
                // 이동량이 0으로 고정되어 플레이어가 끼는 현상이 발생한다.
                if (IsPassThroughPlatform(hit.collider))
                    continue;

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

            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, verticalCollisionMask);
            Debug.DrawRay(rayOrigin, Vector2.up * directionY * rayLength, Color.red);

            if (hit)
            {
                bool isPassThroughPlatform = IsPassThroughPlatform(hit.collider);

                if (isPassThroughPlatform)
                {
                    // 상승 중에는 통과한다.
                    if (directionY > 0f)
                        continue;

                    // 플랫폼 내부 또는 아래에서 하강을 시작했다면 계속 통과한다.
                    // 플레이어의 발이 플랫폼 윗면까지 완전히 올라온 뒤에만 착지시켜
                    // 플랫폼 한가운데서 충돌이 복구되어 끼는 것을 방지한다.
                    float playerBottomY = col.bounds.min.y;
                    float platformTopY = hit.collider.bounds.max.y;
                    if (playerBottomY < platformTopY - DistanceEpsilon)
                        continue;
                }

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

    private static bool IsPassThroughPlatform(Collider2D target)
    {
        return target != null &&
               (target.CompareTag("ChangeablePlatform") || target.CompareTag("Slippery"));
    }

    private float GetSlipperyResponse(float targetVelocityX)
    {
        if (Mathf.Abs(targetVelocityX) < 0.01f)
            return slipperyStopDeceleration;

        bool isTurning = Mathf.Abs(velocity.x) > 0.01f &&
                         Mathf.Sign(velocity.x) != Mathf.Sign(targetVelocityX);

        return isTurning
            ? slipperyTurnAcceleration
            : slipperyStartAcceleration;
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
            if (PauseManager.IsPaused)
            {
                yield return null;
                continue;
            }

            if (animator != null)
            {
                animator.Play("Dash");
            }

            Vector2 dashMove = new Vector2(facingDirection * dashSpeed * Time.deltaTime, 0f);
            Move(dashMove);    
            timer += Time.deltaTime;
            yield return null;
        }
        if (dashEffectController  != null)
        dashEffectController.HideEffect();
        
        isDashing = false;
        velocity.y = 0f;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    public void SetInWater(bool value)
    {
        legacyWaterState = value;
        RefreshWaterState();
    }

    public void SetInWater(Object source, bool value)
    {
        if (ReferenceEquals(source, null))
        {
            SetInWater(value);
            return;
        }

        int sourceId = source.GetInstanceID();
        if (value)
            activeWaterSources.Add(sourceId);
        else
            activeWaterSources.Remove(sourceId);

        RefreshWaterState();
    }

    private void RefreshWaterState()
    {
        bool nextState = legacyWaterState || activeWaterSources.Count > 0;
        if (isInWater == nextState)
            return;

        isInWater = nextState;
        if (!isInWater)
            return;

        velocity.y = 0f;

        if (animator != null)
            animator.SetBool("IsGround", false);

        // 물 진입 시 기존 수직 속도를 한 번만 초기화한다.
    }

    private void HandleWaterMovement()
    {
        if (Input.GetKey(waterDescendKey))
        {
            velocity.y = -waterFastDescendSpeed;
        }
        else if (Input.GetKeyDown(KeyCode.X)) // 기존 점프 입력 감지 변수 재활용
        {
            velocity.y = waterFlapForce; // 기존 velocity 무시하고 즉시 덮어씀 -> 플래피버드 느낌
            if (animator != null)
                animator.SetTrigger("JumpTrigger");
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

    public Vector3 GetCurrentPosition()
    {
        return transform.position;
    }

    public void ApplyGravityPull(Vector3 targetPosition, float pullPower)
    {
        float directionX = targetPosition.x - transform.position.x;

        if (Mathf.Abs(directionX) < 0.01f)
            return;

        stormVelocity.x = Mathf.Sign(directionX) * pullPower;
    }

    private void OnValidate()
    {
        horizontalRayCount = Mathf.Max(2, horizontalRayCount);
        verticalRayCount = Mathf.Max(2, verticalRayCount);
        skinWidth = Mathf.Max(0.001f, skinWidth);
        slipperyStartAcceleration = Mathf.Max(0f, slipperyStartAcceleration);
        slipperyStopDeceleration = Mathf.Max(0f, slipperyStopDeceleration);
        slipperyTurnAcceleration = Mathf.Max(0f, slipperyTurnAcceleration);
        moveSpeed = Mathf.Max(0.01f, moveSpeed);
        waterFlapForce = Mathf.Max(0f, waterFlapForce);
        waterMaxFallSpeed = Mathf.Max(0f, waterMaxFallSpeed);
        waterFastDescendSpeed = Mathf.Max(0f, waterFastDescendSpeed);
        waterGravityMultiplier = Mathf.Max(0f, waterGravityMultiplier);
    }

    public bool IsDashing() => isDashing;
    public bool IsInWater => isInWater;
    public bool IsGrounded => collisions.below;
    public void ResetVerticalVelocity() => velocity.y = 0f;
    public float GetFacingDirection() => facingDirection;



}

/* [파일 노트]
 * 일시정지 대응 : Update 첫 줄에서 PauseManager.IsPaused 를 검사해 입력/중력/이동을 전부 멈추고,
 * DoDash 코루틴도 루프 안에서 일시정지 동안 프레임을 흘려보내 대시 이동/타이머가 진행되지 않게 했다.
 * 대시 쿨다운(WaitForSeconds)은 일시정지 중에도 실시간으로 흐른다(플레이어에게 불리하지 않아 허용).
 */
