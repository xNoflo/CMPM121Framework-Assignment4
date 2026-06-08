using UnityEngine;

public class EnemyController : MonoBehaviour
{
    private Unit unit;

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

        Vector3 direction = target.position - transform.position;
        if (direction.magnitude < 2f)
        {
            DoAttack();
        }
        else
        {
            if (unit != null) unit.movement = direction.normalized * speed;
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
}
