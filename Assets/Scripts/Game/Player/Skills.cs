﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using UnityEngine;

[Serializable]
public class Skills : IComparable
{
    private readonly ConcurrentDictionary<string, SkillStat> skills
        = new ConcurrentDictionary<string, SkillStat>();

    private SkillStat[] skillList;

    public SkillStat Attack;
    public SkillStat Defense;
    public SkillStat Strength;
    public SkillStat Health;
    public SkillStat Magic;
    public SkillStat Ranged;
    public SkillStat Healing;
    public SkillStat Farming;
    public SkillStat Cooking;
    public SkillStat Crafting;
    public SkillStat Mining;
    public SkillStat Fishing;
    public SkillStat Woodcutting;
    public SkillStat Slayer;
    public SkillStat Sailing;
    public static Skills Zero => new Skills();

    public Skills()
    {
        Attack = new SkillStat(nameof(Attack), 1, 0);
        Defense = new SkillStat(nameof(Defense), 1, 0);
        Strength = new SkillStat(nameof(Strength), 1, 0);
        Health = new SkillStat(nameof(Health), 10, 1000);
        Woodcutting = new SkillStat(nameof(Woodcutting), 1, 0);
        Fishing = new SkillStat(nameof(Fishing), 1, 0);
        Mining = new SkillStat(nameof(Mining), 1, 0);
        Crafting = new SkillStat(nameof(Crafting), 1, 0);
        Cooking = new SkillStat(nameof(Cooking), 1, 0);
        Farming = new SkillStat(nameof(Farming), 1, 0);
        Magic = new SkillStat(nameof(Magic), 1, 0);
        Ranged = new SkillStat(nameof(Ranged), 1, 0);
        Slayer = new SkillStat(nameof(Slayer), 1, 0);
        Sailing = new SkillStat(nameof(Sailing), 1, 0);
        Healing = new SkillStat(nameof(Healing), 1, 0);

        skills = new ConcurrentDictionary<string, SkillStat>(
            SkillList.ToDictionary(x => x.Name.ToLower(), x => x));
    }

    public Skills(RavenNest.Models.Skills skills)
    {
        Attack = new SkillStat(nameof(Attack), skills.AttackLevel, skills.Attack);
        Defense = new SkillStat(nameof(Defense), skills.DefenseLevel, skills.Defense);
        Strength = new SkillStat(nameof(Strength), skills.StrengthLevel, skills.Strength);
        Health = new SkillStat(nameof(Health), skills.HealthLevel, skills.Health);
        Woodcutting = new SkillStat(nameof(Woodcutting), skills.WoodcuttingLevel, skills.Woodcutting);
        Fishing = new SkillStat(nameof(Fishing), skills.FishingLevel, skills.Fishing);
        Mining = new SkillStat(nameof(Mining), skills.MiningLevel, skills.Mining);
        Crafting = new SkillStat(nameof(Crafting), skills.CraftingLevel, skills.Crafting);
        Cooking = new SkillStat(nameof(Cooking), skills.CookingLevel, skills.Cooking);
        Farming = new SkillStat(nameof(Farming), skills.FarmingLevel, skills.Farming);
        Magic = new SkillStat(nameof(Magic), skills.MagicLevel, skills.Magic);
        Ranged = new SkillStat(nameof(Ranged), skills.RangedLevel, skills.Ranged);
        Slayer = new SkillStat(nameof(Slayer), skills.SlayerLevel, skills.Slayer);
        Sailing = new SkillStat(nameof(Sailing), skills.SailingLevel, skills.Sailing);
        Healing = new SkillStat(nameof(Healing), skills.HealingLevel, skills.Healing);

        this.skills = new ConcurrentDictionary<string, SkillStat>(
            SkillList.ToDictionary(x => x.Name.ToLower(), x => x));
    }
    public bool IsDead => Health.CurrentValue <= 0;
    public int CombatLevel => (int)((Attack.Level + Defense.Level + Strength.Level + Health.Level) / 4f + (Ranged.Level + Magic.Level + Healing.Level) / 8f);
    public double TotalExperience => SkillList.Sum(x => x.Experience);
    public double[] ExperienceList => SkillList.Select(x => x.Experience).ToArray();
    public int[] LevelList => SkillList.Select(x => x.Level).ToArray();
    public float HealthPercent => Health.CurrentValue / (float)Health.Level;
    public SkillStat[] SkillList => skillList ??
        (skillList = new SkillStat[]
        {
                Attack,
                Defense,
                Strength,
                Health,
                Woodcutting,
                Fishing,
                Mining,
                Crafting,
                Cooking,
                Farming,
                Slayer,
                Magic,
                Ranged,
                Sailing,
                Healing
        });

    public int CompareTo(object obj)
    {
        if (obj == null) return 1;
        if (obj is Skills skills)
            return CombatLevel - skills.CombatLevel;
        return 0;
    }

    internal void TakeBestOf(Skills target)
    {
        for (int i = 0; i < SkillList.Length; i++)
        {
            SkillStat skill = SkillList[i];
            SkillStat tskill = target.SkillList[i];
            if (tskill.Experience > skill.Experience)
            {
                skill.Set(tskill.Level, tskill.Experience);
                //skill.SetExp(tskill.Experience);
            }
        }
    }

    internal void CopyTo(Skills target)
    {
        for (int i = 0; i < SkillList.Length; i++)
        {
            SkillStat skill = SkillList[i];
            SkillStat tskill = target.SkillList[i];
            tskill.Set(skill.Level, skill.Experience);
        }
    }

    public static implicit operator RavenNest.Models.Skills(Skills skills)
    {
        return new RavenNest.Models.Skills
        {
            Attack = skills.Attack.Experience,
            AttackLevel = skills.Attack.Level,

            Cooking = skills.Cooking.Experience,
            CookingLevel = skills.Cooking.Level,

            Crafting = skills.Crafting.Experience,
            CraftingLevel = skills.Crafting.Level,

            Defense = skills.Defense.Experience,
            DefenseLevel = skills.Defense.Level,

            Farming = skills.Farming.Experience,
            FarmingLevel = skills.Farming.Level,

            Fishing = skills.Fishing.Experience,
            FishingLevel = skills.Fishing.Level,

            Health = skills.Health.Experience,
            HealthLevel = skills.Health.Level,

            Magic = skills.Magic.Experience,
            MagicLevel = skills.Magic.Level,

            Healing = skills.Healing.Experience,
            HealingLevel = skills.Healing.Level,

            Mining = skills.Mining.Experience,
            MiningLevel = skills.Mining.Level,

            Ranged = skills.Ranged.Experience,
            RangedLevel = skills.Ranged.Level,

            Sailing = skills.Sailing.Experience,
            SailingLevel = skills.Sailing.Level,

            Slayer = skills.Slayer.Experience,
            SlayerLevel = skills.Slayer.Level,

            Strength = skills.Strength.Experience,
            StrengthLevel = skills.Strength.Level,

            Woodcutting = skills.Woodcutting.Experience,
            WoodcuttingLevel = skills.Woodcutting.Level
        };
    }

    internal static int IndexOf(Skills skills, SkillStat activeSkill)
    {
        return Array.IndexOf(skills.SkillList, activeSkill);
    }

    //public SkillStat this[int index]
    //{
    //    get => this.skillList[index];
    //}

    public SkillStat this[Skill skill]
    {
        get => this.skillList[(int)skill];
    }

    internal SkillStat GetCombatSkill(CombatSkill skill)
    {
        switch (skill)
        {
            case CombatSkill.Attack: return Attack;
            case CombatSkill.Defense: return Defense;
            case CombatSkill.Strength: return Strength;
            case CombatSkill.Health: return Health;
            case CombatSkill.Magic: return Magic;
            case CombatSkill.Ranged: return Ranged;
            case CombatSkill.Healing: return Healing;
        }
        return null;
    }
    public SkillStat GetSkill(Skill skill)
    {
        return this.skillList[(int)skill];
    }

    [Obsolete("Please use GetSkill instead.")]
    public SkillStat GetSkill(TaskSkill skill)
    {
        switch (skill)
        {
            case TaskSkill.Woodcutting: return Woodcutting;
            case TaskSkill.Fishing: return Fishing;
            case TaskSkill.Crafting: return Crafting;
            case TaskSkill.Cooking: return Cooking;
            case TaskSkill.Mining: return Mining;
            case TaskSkill.Farming: return Farming;
            case TaskSkill.Slayer: return Slayer;
            case TaskSkill.Sailing: return Sailing;
        }
        return null;
    }

    public SkillStat GetSkillByName(string skillName)
    {
        if (skills.TryGetValue(skillName.ToLower(), out var skill))
        {
            return skill;
        }

        return null;
    }

    public RavenNest.Models.Skills ToServerModel()
    {
        var output = new RavenNest.Models.Skills();
        foreach (var prop in output
            .GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.Name.Contains("Id"))
            {
                continue;
            }

            if (prop.Name.Contains("Level"))
            {
                var skill = GetSkillByName(prop.Name.Replace("Level", ""));
                if (skill != null)
                {
                    prop.SetValue(output, skill.Level);
                }
            }
            else
            {
                var skill = GetSkillByName(prop.Name);
                if (skill != null)
                {
                    prop.SetValue(output, skill.Experience);
                }
            }
        }

        return output;
    }

    public static Skills operator *(Skills srcSkills, float num)
    {
        var newSkills = new Skills();
        if (srcSkills == null) return newSkills;
        for (int i = 0; i < newSkills.SkillList.Length; i++)
        {
            var outSkill = newSkills.SkillList[i];
            var skill = srcSkills.SkillList[i];

            var high = (skill.Level * num);
            var low = (int)high;
            var progress = high - low;
            var additionalExp = GameMath.ExperienceForLevel(low + 1) * progress;

            if (low >= GameMath.MaxLevel)
            {
                outSkill.Set(low, 0, false);
                continue;
            }

            outSkill.Set(low, additionalExp, false);
            outSkill.AddExp(skill.Experience * num);
        }

        return newSkills;
    }

    public static Skills operator +(Skills valueA, Skills valueB)
    {
        var newSkills = new Skills();
        if (valueA == null || valueB == null) return newSkills;
        for (int i = 0; i < newSkills.SkillList.Length; i++)
        {
            var newSkill = newSkills.SkillList[i];
            var a = valueA.SkillList[i];
            var b = valueB.SkillList[i];
            var level = (a.Level + b.Level);
            newSkill.Set(level, 0, false);

            if (level < GameMath.MaxLevel)
            {
                newSkill.AddExp(a.Experience + b.Experience);
            }
        }

        return newSkills;
    }

    /// <summary>
    /// Gets the highest value out of the two given stats.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static Skills Max(Skills a, Skills b)
    {
        var newSkills = new Skills();
        for (int i = 0; i < newSkills.SkillList.Length; i++)
        {
            var newSkill = newSkills.SkillList[i];
            var skillA = a.SkillList[i];
            var skillB = b.SkillList[i];
            newSkill.Set(Math.Max(skillA.Level, skillB.Level), 0, false);
        }
        return newSkills;
    }

    /// <summary>
    /// Lerps between the two given stats
    /// </summary>
    /// <param name="valueFrom"></param>
    /// <param name="valueTo"></param>
    /// <param name="amount"></param>
    /// <returns></returns>
    public static Skills Lerp(Skills valueFrom, Skills valueTo, float amount)
    {
        var newSkills = new Skills();
        for (int i = 0; i < newSkills.SkillList.Length; i++)
        {
            var newSkill = newSkills.SkillList[i];
            var lowSkill = valueFrom.SkillList[i];
            var higSkill = valueTo.SkillList[i];
            var newLevel = (int)(Mathf.Lerp(lowSkill.Level, higSkill.Level, amount));
            newSkill.Set(Math.Max(1, newLevel), 0, false);
        }
        return newSkills;
    }

    /// <summary>
    /// Gets random skills given the lower and upper range
    /// </summary>
    /// <param name="rngLowStats"></param>
    /// <param name="rngHighStats"></param>
    /// <returns></returns>
    public static Skills Random(Skills rngLowStats, Skills rngHighStats)
    {
        var newSkills = new Skills();
        for (int i = 0; i < newSkills.SkillList.Length; i++)
        {
            var newSkill = newSkills.SkillList[i];
            var lowSkill = rngLowStats.SkillList[i];
            var higSkill = rngHighStats.SkillList[i];
            var newLevel = (int)(UnityEngine.Random.Range(lowSkill.Level, higSkill.Level));
            newSkill.Set(Math.Max(1, newLevel), 0, false);
        }
        return newSkills;
    }
}
