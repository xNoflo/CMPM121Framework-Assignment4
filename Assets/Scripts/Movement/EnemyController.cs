using UnityEngine;
using UnityEngine.SceneManagement;

public class EnemyController : MonoBehaviour
{
    const string PLATFORMER_SCENE_NAME = "FinishPlatformerLevelScene";
    static PhysicsMaterial2D platformerNoFrictionMaterial;

    private Unit unit;
    private Rigidbody2D body;
    private Collider2D bodyCollider;

    public Transform target;
    public int speed;
    public int damage = 5;
    public Hittable hp;
    public HealthBar healthui;
    public bool dead;

    public float last_attack;
    private float freezeEndTime;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        target = GameManager.Instance.player.transform;
        hp.OnDeath += Die;
        healthui.SetHealth(hp);
        unit = GetComponent<Unit>();
        body = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();

        ConfigureMovementModeForScene();
    }

    public void Freeze(float time)
    {
        freezeEndTime = Mathf.Max(freezeEndTime, Time.time + Mathf.Max(0, time));
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time < freezeEndTime)
        {
            if (unit != null) unit.movement = Vector2.zero;
            return;
        }

        Vector2 chaseMovement = GetChaseMovement();
        Vector2 directionToTarget = target.position - transform.position;

        if (directionToTarget.magnitude < 2f)
        {
            DoAttack();
        }
        else
        {
            if (unit != null) unit.movement = chaseMovement;
        }
    }
    
    void DoAttack()
    {
        if (last_attack + 2 < Time.time)
        {
            last_attack = Time.time;
            target.gameObject.GetComponent<PlayerController>().hp.Damage(new Damage(damage, Damage.Type.PHYSICAL));
        }
    }


    void Die()
    {
        if (!dead)
        {
            dead = true;
            GameManager.Instance.RemoveEnemy(gameObject);
            GameManager.Instance.RegisterEnemyDefeated();
            EventBus.Instance.DoEnemyKilled(gameObject);
            Destroy(gameObject);
        }
    }

    void ConfigureMovementModeForScene()
    {
        if (unit == null || body == null)
        {
            return;
        }

        if (!IsPlatformerScene())
        {
            unit.movementMode = Unit.MovementMode.TopDown;
            return;
        }

        unit.movementMode = Unit.MovementMode.Platformer;
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 4f;
        body.freezeRotation = true;
        body.linearDamping = 0f;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (bodyCollider != null)
        {
            if (platformerNoFrictionMaterial == null)
            {
                platformerNoFrictionMaterial = new PhysicsMaterial2D("PlatformerNoFriction");
                platformerNoFrictionMaterial.friction = 0f;
                platformerNoFrictionMaterial.bounciness = 0f;
            }

            bodyCollider.sharedMaterial = platformerNoFrictionMaterial;
        }
    }

    bool IsPlatformerScene()
    {
        return SceneManager.GetActiveScene().name == PLATFORMER_SCENE_NAME;
    }

    Vector2 GetChaseMovement()
    {
        if (target == null)
        {
            return Vector2.zero;
        }

        Vector2 directionToTarget = target.position - transform.position;
        if (!IsPlatformerScene())
        {
            return directionToTarget.normalized * speed;
        }

        float horizontalDelta = directionToTarget.x;
        if (Mathf.Abs(horizontalDelta) < 0.1f)
        {
            return Vector2.zero;
        }

        return new Vector2(Mathf.Sign(horizontalDelta) * speed, 0f);
    }
}
