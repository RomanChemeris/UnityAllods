﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using Spells;

public class Spell
{
    // order should be same as data.bin spells or locale spell.txt
    public enum Spells
    {
        NoneSpell = 0,
        Fire_Arrow,
        Fire_Ball,
        Wall_of_Fire,
        Protection_from_Fire,
        Ice_Missile,
        Poison_Cloud,
        Blizzard,
        Protection_from_Water,
        Acid_Spray,
        Lightning,
        Prismatic_Spray,
        Invisibility,
        Protection_from_Air,
        Darkness,
        Light,
        Diamond_Dust,
        Wall_of_Earth,
        Stone_Curse,
        Protection_from_Earth,
        Bless,
        Haste,
        Control_Spirit,
        Teleport,
        Heal,
        Summon,
        Drain_Life,
        Shield,
        Curse,
        Slow
    }

    public MapUnit User = null;
    public Item Item = null;
    public Templates.TplSpell Template;
    public Spells SpellID = 0;

    private int OwnSkill = -1;
    public int Skill
    {
        get
        {
            if (User != null && Item == null && OwnSkill < 0)
            {
                switch (Template.Sphere)
                {
                    case 1:
                        return User.Stats.SkillFire;
                    case 2:
                        return User.Stats.SkillWater;
                    case 3:
                        return User.Stats.SkillAir;
                    case 4:
                        return User.Stats.SkillEarth;
                    case 5:
                        return User.Stats.SkillAstral;
                    default:
                        return OwnSkill;
                }
            }
            else return Math.Max(0, OwnSkill);
        }

        set
        {
            OwnSkill = value;
        }
    }

    private static List<Spells> AttackSpells = new List<Spells>(new Spells[]
    {
        Spells.Fire_Arrow,
        Spells.Fire_Ball,
        Spells.Wall_of_Fire,
        Spells.Lightning,
        Spells.Prismatic_Spray,
        Spells.Diamond_Dust,
        Spells.Stone_Curse,
        Spells.Ice_Missile,
        Spells.Poison_Cloud,
        Spells.Blizzard,
        Spells.Acid_Spray,
        Spells.Curse,
        Spells.Darkness,
        Spells.Slow,
        Spells.Drain_Life
    });

    public static bool IsAttackSpell(Spells id)
    {
        return AttackSpells.Contains(id);
    }

    public Spell(int id, MapUnit unit = null)
    {
        SpellID = (Spells)id;
        Template = TemplateLoader.GetSpellById(id-1);
        if (Template == null)
            Debug.LogFormat("Invalid spell created (id={0})", id);
        User = unit;
    }

    public SpellProc Cast(int tgX, int tgY, MapUnit tgUnit = null)
    {
        // find spellproc instance for this spell
        // 
        Type spt = SpellProc.FindProcTypeFromSpell(this);
        if (spt == null)
            return null;

        try
        {
            ConstructorInfo ci = spt.GetConstructor(new Type[] { typeof(Spell), typeof(int), typeof(int), typeof(MapUnit) });
            SpellProc sp = (SpellProc)ci.Invoke(new object[] { this, tgX, tgY, tgUnit });
            return sp;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public float GetDistance()
    {
        // if from item and this item has range
        //if (weapon.Class.Option.Name == "Staff" || weapon.Class.Option.Name == "Shaman Staff")
        if (Item != null && Item.Class != null && Item.Class.Option != null && Item.Class.Option.Slot == (int)MapUnit.BodySlot.Weapon)
        {
            // get range of weapon
            return Item.Class.Option.Range;
        }

        // maxRange is base range
        if (SpellID == Spells.Teleport)
            return 8 + (float)Skill / 4;
        return Template.MaxRange + (float)Skill / 25;
    }

    private float GetMindModifier()
    {
        float mmod = 1f;
        if (User != null && Item == null && User is MapHuman)
        {
            MapHuman huser = (MapHuman)User;
            mmod = (float)huser.Stats.Mind * 0.02f;
        }

        return mmod;
    }

    public int GetDamageMin()
    {
        if (Template.DamageMin < 0 || Template.DamageMax < 0)
            return 0;
        return Math.Max(1, (int)(GetMindModifier() * Template.DamageMin * ((float)Skill / 25 + 1f)));
    }

    public int GetDamageMax()
    {
        if (Template.DamageMin < 0 || Template.DamageMax < 0)
            return 0;
        return (int)(GetMindModifier() * Template.DamageMax * ((float)Skill / 25 + 1f));
    }

    public int GetDamage()
    {
        int dmin = GetDamageMin();
        int dmax = GetDamageMax();
        return UnityEngine.Random.Range(dmin, dmax);
    }

    public int GetIndirectPower()
    {
        // 3..7
        int pw = (int)(Skill * 0.07f);
        return Math.Max(3, Math.Min(7, pw));
    }

    public float GetDuration()
    {
        if (SpellID == Spells.Stone_Curse)
            return 1f + Skill * 0.15f;
        if (Template.AreaDuration > 0)
            return Template.AreaDuration + Skill * 0.1f;
        if (Template.Duration > 0)
            return (Template.Duration + Skill * (float)Template.Duration * 0.1f * GetMindModifier()) * 1.5f;
        return 0;
    }

    // protection +X
    public int GetProtection()
    {
        return 10 + Skill / 2;
    }
    
    // bless and curse %
    public int GetBlessing()
    {
        return (int)(40 + (float)Skill * 0.75);
    }

    // speed +X
    public int GetSpeed()
    {
        return 2 + Skill / 10;
    }

    // absorbtion +X
    public int GetAbsorbtion()
    {
        return 5 + Skill / 10;
    }

    public string ToVisualString()
    {
        List<string> sp_rows = new List<string>();

        // all spells have mana cost. unless cast from item
        if (User != null && Item == null)
        {
            sp_rows.Add(string.Format("{0}: {1}", Locale.Main[117], Template.ManaCost));
        }

        if (SpellID == Spells.Poison_Cloud)
        {
            sp_rows.Add(string.Format("{0}: {1}", Locale.Main[118], GetIndirectPower()));
        }
        else
        {
            // if spell has attack, add attack
            int dmin = GetDamageMin();
            int dmax = GetDamageMax();
            if (dmin > 0 || dmax > 0)
                sp_rows.Add(string.Format("{0}: {1}-{2}", Locale.Main[118], dmin, dmax));
        }

        // if spell has distance > 0, add distance.
        float dst = GetDistance();
        if (dst > 0)
            sp_rows.Add(string.Format("{0}: {1}", Locale.Main[123], (int)dst));
        // if spell has duration, add duration
        float duration = GetDuration();
        if (duration > 0)
        {
            if (SpellID == Spells.Stone_Curse)
                sp_rows.Add(string.Format("{0}: 0.0-{1:0.0}", Locale.Main[124], duration));
            else sp_rows.Add(string.Format("{0}: {1:0.0}", Locale.Main[124], duration));
        }

        switch (SpellID)
        {
            default:
                break;
            case Spells.Protection_from_Fire:
            case Spells.Protection_from_Water:
            case Spells.Protection_from_Air:
            case Spells.Protection_from_Earth:
                sp_rows.Add(string.Format("{0}: +{1}", Locale.Main[183], GetProtection()));
                break;
            case Spells.Shield:
                sp_rows.Add(string.Format("{0}: +{1}", Locale.Main[24], GetAbsorbtion()));
                break;
            case Spells.Prismatic_Spray:
                sp_rows.Add(string.Format("{0}: {1}", Locale.Main[186], GetIndirectPower()));
                break;
            case Spells.Bless:
                sp_rows.Add(string.Format("{0} +{1}%", Locale.Main[185], GetBlessing()));
                break;
            case Spells.Curse:
                sp_rows.Add(string.Format("{0} +{1}%", Locale.Main[187], GetBlessing()));
                break;
            case Spells.Haste:
                sp_rows.Add(string.Format("{0}: +{1}", Locale.Main[22], GetSpeed()));
                break;
        }

        return string.Join("\n", sp_rows.ToArray());
    }
}
