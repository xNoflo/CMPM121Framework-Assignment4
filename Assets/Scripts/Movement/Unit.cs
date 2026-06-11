using UnityEngine;
using System.Collections.Generic;
using System;

public class Unit : MonoBehaviour
{
    public enum MovementMode
    {
        TopDown,
        Platformer
    }

    public Vector2 movement;
    public float distance;
    public event Action<float> OnMove;
    public MovementMode movementMode = MovementMode.TopDown;
    bool wasMoving;
    Rigidbody2D cachedBody;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cachedBody = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (cachedBody == null)
        {
            cachedBody = GetComponent<Rigidbody2D>();
            if (cachedBody == null)
            {
                return;
            }
        }

        if (movementMode == MovementMode.Platformer)
        {
            ApplyPlatformerMovement();
            return;
        }

        bool isMoving = movement.sqrMagnitude > 0.01f;

        Move(new Vector2(movement.x, 0) * Time.fixedDeltaTime);
        Move(new Vector2(0, movement.y) * Time.fixedDeltaTime);
        distance += movement.magnitude * Time.fixedDeltaTime;

        if (distance > 0.5f)
        {
            OnMove?.Invoke(distance);

            if (GetComponent<PlayerController>() != null)
            {
                EventBus.Instance.DoPlayerMoved(distance);
            }

            distance = 0;
        }

        if (wasMoving && !isMoving && GetComponent<PlayerController>() != null)
        {
            EventBus.Instance.DoPlayerStoppedMoving();
        }

        wasMoving = isMoving;
    }

    void ApplyPlatformerMovement()
    {
        Vector2 velocity = cachedBody.linearVelocity;
        velocity.x = movement.x;
        cachedBody.linearVelocity = velocity;

        bool isMoving = Mathf.Abs(movement.x) > 0.01f;
        distance += Mathf.Abs(movement.x) * Time.fixedDeltaTime;

        if (distance > 0.5f)
        {
            OnMove?.Invoke(distance);

            if (GetComponent<PlayerController>() != null)
            {
                EventBus.Instance.DoPlayerMoved(distance);
            }

            distance = 0;
        }

        if (wasMoving && !isMoving && GetComponent<PlayerController>() != null)
        {
            EventBus.Instance.DoPlayerStoppedMoving();
        }

        wasMoving = isMoving;
    }

    public void Move(Vector2 ds)
    {
        List<RaycastHit2D> hits = new List<RaycastHit2D>();
        int n = cachedBody.Cast(ds, hits, ds.magnitude * 2);
        if (n == 0)
        {
            transform.Translate(ds);
        }
    }


}
