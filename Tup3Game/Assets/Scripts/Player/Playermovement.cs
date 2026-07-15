using UnityEngine;
using System.Collections;

public class Playermovement : MonoBehaviour
{
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

    private int jumpCount;

    [Header("대쉬 설정")]
    public float dashSpeed = 25f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 1.0f;

    [Header("공격 설정")]
    public BoxCollider2D attackCollider;
    public float attackTime = 0.15f;       
    public float comboDelay = 0.08f;      
    public float comboInputWindow = 0.4f;
    public int maxCombo = 3;             

    private bool isAttacking = false;
    private bool comboQueued = false;      
    private int comboStep = 0;           

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
        attackCollider.enabled = false;
        jumpCount = maxJumpCount;
    }

    void Update()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");

        if (horizontalInput != 0)
        {
            facingDirection = horizontalInput > 0 ? 1f : -1f;
        }

        if (!isDashing)
        {
            velocity.x = horizontalInput * moveSpeed;

            if (collisions.below)
            {
                velocity.y = -0.3f;
                jumpCount = maxJumpCount;
            }
            else
            {
                velocity.y += gravity * Time.deltaTime;
                if (!collisions.below && jumpCount == maxJumpCount)
                {
                    jumpCount = maxJumpCount - 1;
                }
            }


            if (Input.GetKeyDown(KeyCode.X) && jumpCount > 0)
            {
                velocity.y = jumpForce;
                jumpCount--;
            }

            if (Input.GetKeyDown(KeyCode.Z) && canDash)
            {
                StartCoroutine(DoDash());
            }

            // 공격 입력 처리:
            // - 공격 중이 아니면 콤보 시작
            // - 이미 공격 중이면 "다음 타 예약"만 해둔다 (선입력 버퍼)
            if (Input.GetKeyDown(KeyCode.C))
            {
               
                if (!isAttacking)
                    StartCoroutine(ComboAttack());
                else
                    comboQueued = true;
            }
        }

        if (!isDashing)
            Move(velocity * Time.deltaTime);
    }

    private void Move(Vector2 moveAmount)
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

    private IEnumerator ComboAttack()
    {
        isAttacking = true;
        comboStep = 0;
        comboQueued = false;
        try
        {
            while (true)
            {
                comboStep++;
                Debug.Log("현재 콤보 수: " + comboStep);

                // 바라보는 방향으로 공격
                Vector2 pos = attackCollider.transform.localPosition;
                pos.x = Mathf.Abs(pos.x) * facingDirection;
                attackCollider.transform.localPosition = pos;

                // 여기서 comboStep에 따라 애니메이션 트리거, 데미지, 히트박스 크기 등을 다르게 조작
                attackCollider.enabled = true;
                yield return new WaitForSeconds(attackTime);
                attackCollider.enabled = false;

                // 막타였으면 종료
                if (comboStep >= maxCombo)
                    break;
                yield return new WaitForSeconds(comboDelay);

                // 입력 대기 창: comboInputWindow (콤보공격인정시간) 안에 예약이 들어오면 다음 타로
                float timer = 0f;
                while (!comboQueued && timer < comboInputWindow)
                {
                    timer += Time.deltaTime;
                    yield return null;
                }

                // 시간초과 > 콤보 종료
                if (!comboQueued)
                    break;
                comboQueued = false;
            }
        }
        finally
        {
            comboQueued = false;
            comboStep = 0;
            isAttacking = false;
        }
    }


    private void OnDrawGizmos()
    {
        if (attackCollider == null) return;

        
        Vector2 center = attackCollider.transform.position;
        center += (Vector2)attackCollider.offset;


        if (Application.isPlaying && attackCollider.enabled)
        {
            Gizmos.DrawCube(center, attackCollider.size);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(center, attackCollider.size);
        }
        else if (comboStep == 2)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(center, attackCollider.size);
        }
        else if (comboStep == 3)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(center, attackCollider.size);
        }
        else
        {
            // 평소: 연한 초록 테두리만
            Gizmos.color = new Color(0f, 1f, 0f, 0.6f);
            Gizmos.DrawWireCube(center, attackCollider.size);
        }
    }


    public bool IsDashing() => isDashing;
    public float GetFacingDirection() => facingDirection;
    public int GetComboStep() => comboStep;
}
