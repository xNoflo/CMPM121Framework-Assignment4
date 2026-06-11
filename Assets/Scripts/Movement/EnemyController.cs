using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.VirtualTexturing;
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
    public float chaseDistance = 60f;
    
    public enum Status
    {
        Idle,
        Chase,
        Reroute,
        Dead
    }
    public Status status = Status.Idle;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        target = GameManager.Instance.player.transform;
        hp.OnDeath += Die;
        healthui.SetHealth(hp);
        unit = GetComponent<Unit>();
        body = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        
        InvokeRepeating(nameof(IdleCoroutine), 0f, 3f);
        
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
        
        Vector2 directionToTarget = target.position - transform.position;
        
        if (directionToTarget.magnitude < 2f)
        {
            DoAttack();
        }
        // on top of each other, but different platform
        else if (Mathf.Abs(transform.position.y - target.position.y) > 2f &&
                 transform.position.x - target.position.x < 2f && 
                 status != Status.Reroute)
        {
            status = Status.Reroute;
            StartCoroutine(RerouteCoroutine());
        } 
        // close and not rerouting, or close but on the same level (success reroute)
        else if (directionToTarget.magnitude < chaseDistance && 
                 (status != Status.Reroute || transform.position.y - target.position.y < 2f))
        {
            status = Status.Chase;
            Vector2 chaseMovement = GetChaseMovement();
            unit.movement = chaseMovement;
        }
        
        if (status != Status.Idle) StopCoroutine(nameof(IdleCoroutine)); 
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
            status = Status.Dead;
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
        
        /*
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 4f;
        body.freezeRotation = true;
        body.linearDamping = 0f;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        */

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

    public IEnumerator RerouteCoroutine()
    {
        unit.movement = new Vector2(Mathf.Sign(Random.Range(-1f, 1f)) * speed, 0f);
        yield return new WaitForSeconds(2f);
    }
    
    public void IdleCoroutine()
    {
        unit.movement = new Vector2(Mathf.Sign(Random.Range(-1f, 1f)) * speed, 0f);
    }
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        //if (!IsPlatformerScene()) return;
        
        if (collision.gameObject.CompareTag("jump_point"))
        {
            if (status == Status.Chase)
            {
                if (target.position.y <= transform.position.y) return;
            
                if (target.position.x < transform.position.x && collision.gameObject.GetComponent<JumpPoint>().kind != JumpPoint.Direction.Right) 
                {
                    unit.QueueJump();
                }
                else if (target.position.x > transform.position.x && collision.gameObject.GetComponent<JumpPoint>().kind != JumpPoint.Direction.Left)
                {
                    unit.QueueJump();
                }
            }
            else if (status == Status.Idle)
            {
                if (collision.gameObject.GetComponent<JumpPoint>().kind == JumpPoint.Direction.Right)
                {
                    unit.movement = new Vector2(speed, 0f);
                }
                else if (collision.gameObject.GetComponent<JumpPoint>().kind == JumpPoint.Direction.Left)
                {
                    unit.movement = new Vector2(-speed, 0f);
                }
                
                unit.QueueJump();
            }
        }
    }
}
