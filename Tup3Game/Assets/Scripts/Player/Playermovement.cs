using System.Collections;
using TMPro;
using Unity.Burst.Intrinsics;
using Unity.VisualScripting;
using UnityEngine;

public class Playermovement : MonoBehaviour
{
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

    private bool wasGrounded = true;
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

        if (horizontalInput != 0 && !isAiming)
        {
            facingDirection = horizontalInput > 0 ? 1f : -1f;
        }
        if (spriteRenderer != null)
            spriteRenderer.flipX = facingDirection < 0;


        bool isAttacking = combo != null && combo.IsAttacking;

        if (!isDashing && !isAiming)
        {
            velocity.x = horizontalInput * moveSpeed;

           

            if (collisions.below)
            {
                velocity.y = -0.3f;
                jumpCount = maxJumpCount;
            }
            else
            {
                float appliedGravity = (velocity.y > 0f) ? gravity : gravity * fallGravityMultiplier;
                velocity.y += appliedGravity * Time.deltaTime;
                if (!collisions.below && jumpCount == maxJumpCount)
                {
                    jumpCount = maxJumpCount - 1;
                }
            }


            if (Input.GetKeyDown(KeyCode.X) && jumpCount > 0)
            {
                if (animator != null)
                {
                    animator.Play("Jump", 0, 0f);
                }
                velocity.y = jumpForce;
                jumpCount--;
            }

            if (Input.GetKeyDown(KeyCode.Z) && canDash)
            {
                StartCoroutine(DoDash());
            }

            else if (isAiming)
            {
                velocity.x = 0f;
            }

            // 공격 입력 처리:
            // - 공격 중이 아니면 콤보 시작
            // - 이미 공격 중이면 "다음 타 예약"만 해둔다 (선입력 버퍼)
        }

        if (!isDashing)
            Move(velocity * Time.deltaTime);
        //애니메이션처리하는부분입니다
        bool isGrounded = collisions.below;
        if (!isDashing)
        {
            if (!isGrounded && velocity.y < 0f)
            {
                if (animator != null)
                    animator.Play("Land", 0, 0f);
            }

            if (!wasGrounded && isGrounded)
            {
                if (animator != null)
                    animator.Play("Stand", 0, 0f);
            }
        }

        wasGrounded = isGrounded;

        if (animator != null)
            animator.SetFloat("Speed", Mathf.Abs(velocity.x));
        if (animator != null)
            animator.SetFloat("y-velocity", velocity.y);
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

        float bottomOffset = col.bounds.size.y * 0.25f;
        float usableHeight = col.bounds.size.y - bottomOffset;
        float raySpacing = usableHeight / (horizontalRayCount - 1);

        for (int i = 0; i < horizontalRayCount; i++)
        {
            Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
            rayOrigin += Vector2.up * (bottomOffset + i * raySpacing);

            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);
            Debug.DrawRay(rayOrigin, Vector2.right * directionX * rayLength, Color.red);

            if (hit)
            {
                if (hit.distance == 0)
                    continue;

                float correctedDistance = Mathf.Max(hit.distance - skinWidth, 0f);
                moveAmount.x = correctedDistance * directionX;
                rayLength = hit.distance;

                collisions.left = directionX == -1;
                collisions.right = directionX == 1;
            }
        }
    }

    private void VerticalCollisions(ref Vector2 moveAmount)
    {
        float directionY = Mathf.Sign(moveAmount.y);
        float rayLength = Mathf.Abs(moveAmount.y) + skinWidth;

        for (int i = 0; i < verticalRayCount; i++)
        {
            Vector2 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
            rayOrigin += Vector2.right * (i * (col.bounds.size.x / (verticalRayCount - 1)) + moveAmount.x);

            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, collisionMask);
            Debug.DrawRay(rayOrigin, Vector2.up * directionY * rayLength, Color.red);

            if (hit)
            {
                float correctedDistance = hit.distance - skinWidth;

                if (correctedDistance < 0)
                {
                    correctedDistance = 0;
                }

                moveAmount.y = correctedDistance * directionY;
                rayLength = hit.distance;

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

        if (animator != null)
        {
            animator.Play("Dash", 0, 0f);
        }

        float timer = 0f;
        while (timer < dashDuration)
        {
            Vector2 dashMove = new Vector2(facingDirection * dashSpeed * Time.deltaTime, 0f);
            Move(dashMove);     
            timer += Time.deltaTime;
            yield return null;
        }

        isDashing = false;
        velocity.y = 0f;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }


    public bool IsDashing() => isDashing;
    public void ResetVerticalVelocity() => velocity.y = 0f;
    public float GetFacingDirection() => facingDirection;

}
