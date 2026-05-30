using UnityEngine;
using System.Collections;

public class Spell
{
    public float last_cast;
    public SpellCaster owner;
    public Hittable.Team team;

    private readonly SpellDefinition definition;

    public Spell(SpellCaster owner)
    {
        this.owner = owner;
    }

    public Spell(SpellCaster owner, SpellDefinition definition)
    {
        this.owner = owner;
        this.definition = definition;
    }

    public virtual string GetName()
    {
        return definition != null ? definition.Name : "Bolt";
    }

    public virtual string GetDescription()
    {
        return definition != null ? definition.Description : "A straight-flying bolt.";
    }

    public virtual int GetManaCost()
    {
        int spellPower = GetSpellPower();
        int wave = GetWaveNumber();
        SpellModifierContext context = BuildModifierContext(spellPower, wave);
        return GetModifiedManaCost(context, spellPower, wave);
    }

    public virtual int GetDamage()
    {
        int spellPower = GetSpellPower();
        int wave = GetWaveNumber();
        SpellModifierContext context = BuildModifierContext(spellPower, wave);
        return GetModifiedDamage(context, spellPower, wave);
    }

    public virtual float GetCooldown()
    {
        int spellPower = GetSpellPower();
        int wave = GetWaveNumber();
        SpellModifierContext context = BuildModifierContext(spellPower, wave);
        return GetModifiedCooldown(context, spellPower, wave);
    }

    public virtual int GetIcon()
    {
        return definition != null ? definition.Icon : 0;
    }

    public bool IsReady()
    {
        return last_cast + GetCooldown() < Time.time;
    }

    public virtual IEnumerator Cast(Vector3 where, Vector3 target, Hittable.Team team)
    {
        yield return Cast(where, target, team, GetSpellPower(), GetWaveNumber());
    }

    public virtual IEnumerator Cast(Vector3 where, Vector3 target, Hittable.Team team, int spellPower, int wave)
    {
        SpellModifierContext context = BuildModifierContext(spellPower, wave);
        last_cast = Time.time;
        yield return CastWithContext(where, target, team, spellPower, wave, context);
    }

    protected internal virtual IEnumerator CastWithContext(Vector3 where, Vector3 target, Hittable.Team team, int spellPower, int wave, SpellModifierContext context)
    {
        this.team = team;

        int sprite = GetProjectileSprite(spellPower, wave);
        string trajectory = GetProjectileTrajectory(context);
        float speed = GetProjectileSpeed(context, spellPower, wave);
        float lifetime = GetProjectileLifetime(spellPower, wave);
        Vector3 direction = target - where;
        int damage = GetModifiedDamage(context, spellPower, wave);
        Damage.Type damageType = GetDamageType();

        if (direction.sqrMagnitude <= 0)
        {
            direction = Vector3.right;
        }

        bool hasSecondaryProjectile = definition != null && definition.HasField("secondary_projectile");
        int shotCount = hasSecondaryProjectile ? 1 : GetProjectileCount(spellPower, wave);
        float spray = GetSpray(spellPower, wave);

        for (int i = 0; i < shotCount; i++)
        {
            Vector3 shotDirection = direction;

            if (spray > 0)
            {
                float sprayAngle = Random.Range(-spray / 2f, spray / 2f) * Mathf.Rad2Deg;
                shotDirection = Quaternion.Euler(0, 0, sprayAngle) * shotDirection;
            }

            if (lifetime > 0)
            {
                GameManager.Instance.projectileManager.CreateProjectile(sprite, trajectory, where, shotDirection, speed, (other, impact) => OnHit(other, impact, damage, damageType, context, sprite, speed), lifetime);
            }
            else
            {
                GameManager.Instance.projectileManager.CreateProjectile(sprite, trajectory, where, shotDirection, speed, (other, impact) => OnHit(other, impact, damage, damageType, context, sprite, speed));
            }
        }

        yield return new WaitForEndOfFrame();
    }

    protected virtual void OnHit(Hittable other, Vector3 impact, int damage, Damage.Type damageType, SpellModifierContext context, int sprite, float speed)
    {
        if (other.team != team)
        {
            if (context.stunDuration > 0f && other.owner != null)
            {
                EnemyController enemy = other.owner.GetComponent<EnemyController>();

                if (enemy != null)
                {
                    enemy.Freeze(context.stunDuration);
                }
            }

            other.Damage(new Damage(damage, damageType));

            if (HasSecondaryProjectile())
            {
                SpawnSecondaryProjectiles(impact, GetSpellPower(), GetWaveNumber(), context);
            }

            if (context.splitOnHit)
            {
                SpawnSplitProjectiles(impact, Mathf.Max(1, Mathf.RoundToInt(damage * context.splitDamageMultiplier)), damageType, sprite, speed * context.splitSpeedMultiplier, context.splitLifetime);
            }
        }
    }

    private bool HasSecondaryProjectile()
    {
        return definition != null && definition.HasField("secondary_projectile");
    }

    private int GetProjectileCount(int spellPower, int wave)
    {
        if (definition == null || !definition.HasField("N"))
        {
            return 1;
        }

        return Mathf.Max(1, definition.GetInt("N", 1, spellPower, wave));
    }

    private float GetSpray(int spellPower, int wave)
    {
        if (definition == null || !definition.HasField("spray"))
        {
            return 0f;
        }

        return Mathf.Max(0f, definition.GetFloat("spray", 0f, spellPower, wave));
    }

    private void SpawnSecondaryProjectiles(Vector3 impact, int spellPower, int wave, SpellModifierContext context)
    {
        if (definition == null)
        {
            return;
        }

        int totalShots = GetProjectileCount(spellPower, wave);
        int sprite = Mathf.Max(0, definition.GetInt("secondary_projectile.sprite", GetProjectileSprite(spellPower, wave), spellPower, wave));
        string trajectory = definition.GetString("secondary_projectile.trajectory", "straight");
        float speed = Mathf.Max(1f, definition.GetFloat("secondary_projectile.speed", 10f, spellPower, wave));
        float lifetime = definition.GetFloat("secondary_projectile.lifetime", -1f, spellPower, wave);
        int secondaryDamage;

        if (definition.HasField("secondary_damage.amount"))
        {
            secondaryDamage = definition.GetInt("secondary_damage.amount", 5, spellPower, wave);
        }
        else
        {
            secondaryDamage = definition.GetInt("secondary_damage", 5, spellPower, wave);
        }

        secondaryDamage = Mathf.Max(1, secondaryDamage);

        Damage.Type secondaryDamageType = definition.HasField("secondary_damage.type")
            ? definition.GetDamageType("secondary_damage.type", GetDamageType())
            : GetDamageType();

        for (int i = 0; i < totalShots; i++)
        {
            Vector3 direction = Quaternion.Euler(0, 0, 360f * i / totalShots) * Vector3.up;

            if (lifetime > 0)
            {
                GameManager.Instance.projectileManager.CreateProjectile(sprite, trajectory, impact, direction, speed, (other, hit) => OnHit(other, hit, secondaryDamage, secondaryDamageType, context, sprite, speed), lifetime);
            }
            else
            {
                GameManager.Instance.projectileManager.CreateProjectile(sprite, trajectory, impact, direction, speed, (other, hit) => OnHit(other, hit, secondaryDamage, secondaryDamageType, context, sprite, speed));
            }
        }
    }

    private void SpawnSplitProjectiles(Vector3 impact, int damage, Damage.Type damageType, int sprite, float speed, float lifetime)
    {
        Vector3[] directions = { Vector3.up, Vector3.right, Vector3.down, Vector3.left };

        foreach (Vector3 direction in directions)
        {
            GameManager.Instance.projectileManager.CreateProjectile(
                sprite,
                "straight",
                impact,
                direction,
                Mathf.Max(1, speed),
                (other, hit) =>
                {
                    if (other.team != team)
                    {
                        other.Damage(new Damage(damage, damageType));
                    }
                },
                Mathf.Max(0.1f, lifetime));
        }
    }

    protected internal virtual void AddModifiers(SpellModifierContext context, int spellPower, int wave)
    {
    }

    protected internal virtual int GetModifiedManaCost(SpellModifierContext context, int spellPower, int wave)
    {
        return Mathf.Max(0, context.ApplyManaCost(GetBaseManaCost(spellPower, wave)));
    }

    protected internal virtual int GetModifiedDamage(SpellModifierContext context, int spellPower, int wave)
    {
        return Mathf.Max(0, context.ApplyDamage(GetBaseDamage(spellPower, wave)));
    }

    protected internal virtual float GetModifiedCooldown(SpellModifierContext context, int spellPower, int wave)
    {
        return Mathf.Max(0, context.ApplyCooldown(GetBaseCooldown(spellPower, wave)));
    }

    protected SpellModifierContext BuildModifierContext(int spellPower, int wave)
    {
        SpellModifierContext context = new SpellModifierContext();
        AddModifiers(context, spellPower, wave);
        return context;
    }

    protected virtual int GetSpellPower()
    {
        return owner != null ? owner.GetCurrentSpellPower() : 0;
    }

    protected virtual int GetWaveNumber()
    {
        return owner != null ? owner.wave : 1;
    }

    protected virtual Damage.Type GetDamageType()
    {
        return definition != null ? definition.GetDamageType() : Damage.Type.ARCANE;
    }

    protected virtual int GetProjectileSprite(int spellPower, int wave)
    {
        if (definition == null)
        {
            return 0;
        }

        return Mathf.Max(0, definition.GetInt("projectile.sprite", 0, spellPower, wave));
    }

    protected virtual string GetProjectileTrajectory(SpellModifierContext context)
    {
        if (!string.IsNullOrWhiteSpace(context.projectileTrajectoryOverride))
        {
            return context.projectileTrajectoryOverride;
        }

        return definition != null ? definition.GetString("projectile.trajectory", "straight") : "straight";
    }

    protected virtual float GetProjectileSpeed(SpellModifierContext context, int spellPower, int wave)
    {
        return Mathf.Max(0, context.ApplyProjectileSpeed(GetBaseProjectileSpeed(spellPower, wave)));
    }

    protected virtual float GetProjectileLifetime(int spellPower, int wave)
    {
        if (definition == null)
        {
            return -1f;
        }

        return definition.GetFloat("projectile.lifetime", -1f, spellPower, wave);
    }

    protected virtual int GetBaseManaCost(int spellPower, int wave)
    {
        if (definition == null)
        {
            return 10;
        }

        return definition.GetInt("mana_cost", 10, spellPower, wave);
    }

    protected virtual int GetBaseDamage(int spellPower, int wave)
    {
        if (definition == null)
        {
            return 100;
        }

        return definition.GetInt("damage.amount", 100, spellPower, wave);
    }

    protected virtual float GetBaseCooldown(int spellPower, int wave)
    {
        if (definition == null)
        {
            return 0.75f;
        }

        return definition.GetFloat("cooldown", 0.75f, spellPower, wave);
    }

    protected virtual float GetBaseProjectileSpeed(int spellPower, int wave)
    {
        if (definition == null)
        {
            return 15f;
        }

        return definition.GetFloat("projectile.speed", 15f, spellPower, wave);
    }
}
