using System.Collections;
using UnityEngine;

public class ModifierSpell : Spell
{
    protected readonly Spell innerSpell;
    protected readonly SpellDefinition modifierDefinition;

    public ModifierSpell(Spell innerSpell, SpellDefinition modifierDefinition)
        : base(innerSpell.owner)
    {
        this.innerSpell = innerSpell;
        this.modifierDefinition = modifierDefinition;
    }

    public override string GetName()
    {
        if (modifierDefinition == null)
        {
            return innerSpell.GetName();
        }

        return modifierDefinition.Name + " " + innerSpell.GetName();
    }

    public override int GetIcon()
    {
        return innerSpell.GetIcon();
    }

    public override string GetDescription()
    {
        if (modifierDefinition == null)
        {
            return innerSpell.GetDescription();
        }

        return modifierDefinition.Name + ": " + modifierDefinition.Description + "\n" + innerSpell.GetDescription();
    }

    protected internal override void AddModifiers(SpellModifierContext context, int spellPower, int wave)
    {
        innerSpell.AddModifiers(context, spellPower, wave);
        AddOwnModifiers(context, spellPower, wave);
    }

    protected virtual void AddOwnModifiers(SpellModifierContext context, int spellPower, int wave)
    {
    }

    protected internal override int GetModifiedManaCost(SpellModifierContext context, int spellPower, int wave)
    {
        return innerSpell.GetModifiedManaCost(context, spellPower, wave);
    }

    protected internal override int GetModifiedDamage(SpellModifierContext context, int spellPower, int wave)
    {
        return innerSpell.GetModifiedDamage(context, spellPower, wave);
    }

    protected internal override float GetModifiedCooldown(SpellModifierContext context, int spellPower, int wave)
    {
        return innerSpell.GetModifiedCooldown(context, spellPower, wave);
    }

    protected internal override IEnumerator CastWithContext(Vector3 where, Vector3 target, Hittable.Team team, int spellPower, int wave, SpellModifierContext context)
    {
        yield return innerSpell.CastWithContext(where, target, team, spellPower, wave, context);
    }
}
