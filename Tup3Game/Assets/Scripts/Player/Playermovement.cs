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
                velocity.x = isLunging ? 0f : horizontalInput * moveSpeed;
            }

            if (collisions.below)
            {
                if (animator != null)
                {
                    animator.SetBool("IsGround",true);
                }

                velocity.y = -0.3f;
                jumpCount = maxJumpCount; 
                
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
            }


            if (Input.GetKeyDown(KeyCode.X) && jumpCount > 0)
            {
                Debug.Log($"X pressed. jumpCount={jumpCount}, collisions.below={collisions.below}");
                velocity.y = jumpForce;
                jumpCount--;
                if (animator != null)
                    animator.SetTrigger("JumpTrigger");
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

                velocity.y = 0f;
            }
        }
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



    public bool IsDashing() => isDashing;
    public void ResetVerticalVelocity() => velocity.y = 0f;
    public float GetFacingDirection() => facingDirection;


   
}
