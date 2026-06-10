using UnityEngine;
using System;

public class Unit : MonoBehaviour
{
    public Vector2 movement;
    public float distance;
    public event Action<float> OnMove;

    [Header("Platformer")]
    [SerializeField] bool forcePlatformerMovement;
    [SerializeField] float horizontalAcceleration = 90f;
    [SerializeField] float horizontalDeceleration = 110f;
    [SerializeField] float airHorizontalAcceleration = 75f;
    [SerializeField] float airHorizontalDeceleration = 90f;
    [SerializeField] float gravity = 45f;
    [SerializeField] float maxFallSpeed = 22f;
    [SerializeField] float jumpVelocity = 14f;
    [SerializeField] float coyoteTime = 0.12f;
    [SerializeField] float jumpBufferTime = 0.15f;
    [SerializeField] float collisionSkin = 0.02f;
    [SerializeField] float groundedProbeDistance = 0.08f;
    [SerializeField] bool infiniteJumpForTesting = true;

    bool wasMoving;
    bool isGrounded;
    bool hasPlayerController;
    float currentHorizontalVelocity;
    float verticalVelocity;
    float coyoteTimer;
    float jumpBufferTimer;
    Rigidbody2D rb;
    Collider2D unitCollider;
    readonly RaycastHit2D[] castHits = new RaycastHit2D[8];
    readonly Collider2D[] overlapHits = new Collider2D[8];
    ContactFilter2D contactFilter = new ContactFilter2D();

    public bool UsesPlatformerMovement => forcePlatformerMovement || hasPlayerController;
    public bool IsGrounded => isGrounded;
    public bool InfiniteJumpForTesting
    {
        get => infiniteJumpForTesting;
        set => infiniteJumpForTesting = value;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        unitCollider = GetComponent<Collider2D>();
        hasPlayerController = GetComponent<PlayerController>() != null;

        contactFilter.useTriggers = false;
        contactFilter.useLayerMask = false;
    }

    void FixedUpdate()
    {
        if (UsesPlatformerMovement)
        {
            FixedUpdatePlatformer();
            return;
        }

        bool isMoving = movement.sqrMagnitude > 0.01f;

        MoveWithCollisions(new Vector2(movement.x, 0f) * Time.fixedDeltaTime);
        MoveWithCollisions(new Vector2(0f, movement.y) * Time.fixedDeltaTime);
        distance += movement.magnitude * Time.fixedDeltaTime;

        PublishMovementEvents(isMoving);
    }

    public void QueueJump()
    {
        jumpBufferTimer = jumpBufferTime;
    }

    void FixedUpdatePlatformer()
    {
        float deltaTime = Time.fixedDeltaTime;

        ResolveOverlaps();
        UpdateGroundedState();

        if (isGrounded)
        {
            coyoteTimer = coyoteTime;
            if (verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }
        }
        else
        {
            coyoteTimer = Mathf.Max(0f, coyoteTimer - deltaTime);
            verticalVelocity = Mathf.Max(verticalVelocity - gravity * deltaTime, -maxFallSpeed);
        }

        jumpBufferTimer = Mathf.Max(0f, jumpBufferTimer - deltaTime);

        if (jumpBufferTimer > 0f && (coyoteTimer > 0f || infiniteJumpForTesting))
        {
            verticalVelocity = jumpVelocity;
            isGrounded = false;
            coyoteTimer = 0f;
            jumpBufferTimer = 0f;
        }

        float targetHorizontalVelocity = movement.x;
        float acceleration;
        if (Mathf.Abs(targetHorizontalVelocity) > 0.01f)
        {
            acceleration = isGrounded ? horizontalAcceleration : airHorizontalAcceleration;
        }
        else
        {
            acceleration = isGrounded ? horizontalDeceleration : airHorizontalDeceleration;
        }

        currentHorizontalVelocity = Mathf.MoveTowards(currentHorizontalVelocity, targetHorizontalVelocity, acceleration * deltaTime);

        Vector2 delta = new Vector2(currentHorizontalVelocity, verticalVelocity) * deltaTime;
        Vector2 actualHorizontal = MoveWithCollisions(new Vector2(delta.x, 0f));
        Vector2 actualVertical = MoveWithCollisions(new Vector2(0f, delta.y));

        if (!Mathf.Approximately(delta.x, 0f) && Mathf.Abs(actualHorizontal.x) < Mathf.Abs(delta.x))
        {
            currentHorizontalVelocity = 0f;
        }

        if (!Mathf.Approximately(delta.y, 0f) && Mathf.Abs(actualVertical.y) < Mathf.Abs(delta.y))
        {
            verticalVelocity = 0f;
        }

        UpdateGroundedState();

        bool isMoving = Mathf.Abs(currentHorizontalVelocity) > 0.01f || Mathf.Abs(verticalVelocity) > 0.01f;
        distance += new Vector2(actualHorizontal.x, actualVertical.y).magnitude;

        PublishMovementEvents(isMoving);
    }

    Vector2 MoveWithCollisions(Vector2 delta)
    {
        if (rb == null || unitCollider == null || delta.sqrMagnitude <= Mathf.Epsilon)
        {
            return Vector2.zero;
        }

        float distanceToCast = delta.magnitude + collisionSkin;
        int hitCount = rb.Cast(delta.normalized, contactFilter, castHits, distanceToCast);
        float allowedDistance = delta.magnitude;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = castHits[i];
            if (hit.collider == null)
            {
                continue;
            }

            if (ShouldIgnoreCollisionForAxis(delta, hit))
            {
                continue;
            }

            allowedDistance = Mathf.Min(allowedDistance, Mathf.Max(0f, hit.distance - collisionSkin));
        }

        Vector2 actualDelta = delta.normalized * allowedDistance;
        if (actualDelta.sqrMagnitude > Mathf.Epsilon)
        {
            rb.position += actualDelta;
        }

        return actualDelta;
    }

    bool ShouldIgnoreCollisionForAxis(Vector2 delta, RaycastHit2D hit)
    {
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
        {
            return Mathf.Abs(hit.normal.y) > 0.6f;
        }

        if (Mathf.Abs(delta.y) > Mathf.Abs(delta.x))
        {
            return Mathf.Abs(hit.normal.x) > 0.6f;
        }

        return false;
    }

    void ResolveOverlaps()
    {
        if (unitCollider == null)
        {
            return;
        }

        int overlapCount = unitCollider.Overlap(contactFilter, overlapHits);
        for (int i = 0; i < overlapCount; i++)
        {
            Collider2D other = overlapHits[i];
            if (other == null || other == unitCollider)
            {
                continue;
            }

            ColliderDistance2D separation = unitCollider.Distance(other);
            if (!separation.isOverlapped)
            {
                continue;
            }

            Vector2 correction = separation.normal * (separation.distance - collisionSkin);
            if (correction.sqrMagnitude <= Mathf.Epsilon)
            {
                continue;
            }

            rb.position += correction;
        }
    }

    void UpdateGroundedState()
    {
        if (rb == null || unitCollider == null)
        {
            isGrounded = false;
            return;
        }

        int hitCount = rb.Cast(Vector2.down, contactFilter, castHits, groundedProbeDistance + collisionSkin);
        isGrounded = false;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = castHits[i];
            if (hit.collider == null)
            {
                continue;
            }

            if (hit.normal.y >= 0.25f)
            {
                isGrounded = true;
                return;
            }
        }
    }

    void PublishMovementEvents(bool isMoving)
    {
        if (distance > 0.5f)
        {
            OnMove?.Invoke(distance);

            if (hasPlayerController)
            {
                EventBus.Instance.DoPlayerMoved(distance);
            }

            distance = 0f;
        }

        if (wasMoving && !isMoving && hasPlayerController)
        {
            EventBus.Instance.DoPlayerStoppedMoving();
        }

        wasMoving = isMoving;
    }
}
