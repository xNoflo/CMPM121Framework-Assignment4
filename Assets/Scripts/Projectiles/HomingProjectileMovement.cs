using UnityEngine;

public class HomingProjectileMovement : ProjectileMovement
{
    float angle;
    float turn_rate;
    GameObject target;
    float next_target_update;
    const float TARGET_UPDATE_DELAY = 0.15f;

    public HomingProjectileMovement(float speed) : base(speed)
    {
        angle = float.NaN;
        turn_rate = 0.25f;
        target = null;
        next_target_update = 0;
    }

    public override void Movement(Transform transform)
    {
        if (float.IsNaN(angle))
        {
            Vector3 direction = transform.rotation * new Vector3(1, 0, 0);
            angle = Mathf.Atan2(direction.y, direction.x);
        }
        if (Time.time >= next_target_update || target == null)
        {
            target = GameManager.Instance.GetClosestEnemy(transform.position);
            next_target_update = Time.time + TARGET_UPDATE_DELAY;
        }

        if (target == null)
        {
            Vector3 direction = transform.rotation * new Vector3(1, 0, 0);
            angle = Mathf.Atan2(direction.y, direction.x);
            transform.Translate(new Vector3(speed * Time.deltaTime, 0, 0), Space.Self);
        }
        else
        {
            Vector3 new_direction = (target.transform.position - transform.position).normalized;
            float new_angle = Mathf.Atan2(new_direction.y, new_direction.x);
            if (Mathf.Abs(angle - new_angle) > Mathf.Epsilon)
            {
                float da = new_angle - angle;
                if (da > Mathf.PI)
                {
                    da -= 2 * Mathf.PI;
                }
                if (da < -Mathf.PI)
                {
                    da += 2 * Mathf.PI;
                }
                angle += Mathf.Clamp(da, -turn_rate * Mathf.Deg2Rad, turn_rate * Mathf.Deg2Rad);

            }
            Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
            transform.Translate(direction.normalized * speed * Time.deltaTime, Space.World);
        }
    }
}
