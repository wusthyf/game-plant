using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlantSpirit.VerticalSlice
{
    public enum TraitSlot { Root, Stem, Flower }
    public enum TraitTag { Vine, Poison, Armor, Thorn, Burst, Thunder, Seed, Corruption }
    public enum EnemyKind { Vine, Mushroom, Beetle, Treant, Berry, ThornPod }
    public enum RoomKind { Combat, Platform, Elite, Rest, Event, Boss }

    [Serializable]
    public sealed class TraitDefinition
    {
        public string Id;
        public string DisplayName;
        public TraitSlot Slot;
        public TraitTag[] Tags;
        public float Damage;
        public float Range;
        public float Cooldown;
        public float DamageReduction;
        public Color Color;
    }

    [Serializable]
    public sealed class EvolutionDefinition
    {
        public string Id;
        public string DisplayName;
        public TraitTag RequiredTag;
        public float DamageBonus;
        public float CooldownMultiplier = 1f;
        public string Description;
    }

    [Serializable]
    public sealed class FusionDefinition
    {
        public string Id;
        public string DisplayName;
        public string FirstTrait;
        public string SecondTrait;
        public string ResultDescription;
    }

    public static class VerticalSliceCatalog
    {
        public static readonly IReadOnlyList<TraitDefinition> Traits = new List<TraitDefinition>
        {
            Trait("iron_root", "铁甲根", TraitSlot.Root, new [] { TraitTag.Armor }, 0, 0, 0, .25f, new Color(.55f, .65f, .72f)),
            Trait("burst_root", "爆裂根", TraitSlot.Root, new [] { TraitTag.Burst }, 12, 1.7f, 4f, 0, new Color(1f, .35f, .18f)),
            Trait("vine_tendril", "藤蔓触须", TraitSlot.Stem, new [] { TraitTag.Vine }, 8, 2.8f, .7f, 0, new Color(.38f, .9f, .3f)),
            Trait("cactus_stem", "仙人掌茎", TraitSlot.Stem, new [] { TraitTag.Thorn }, 14, 1.5f, 1.1f, 0, new Color(.25f, .68f, .35f)),
            Trait("treant_arm", "树人之臂", TraitSlot.Stem, new [] { TraitTag.Armor }, 17, 1.4f, 1.8f, 0, new Color(.55f, .34f, .15f)),
            Trait("toxic_cap", "毒菌伞", TraitSlot.Flower, new [] { TraitTag.Poison }, 6, 3f, 5f, 0, new Color(.68f, .28f, .88f)),
            Trait("burst_pod", "爆裂果囊", TraitSlot.Flower, new [] { TraitTag.Burst }, 35, 2.5f, 8f, 0, new Color(1f, .28f, .18f)),
            Trait("thunder_flower", "雷鸣花", TraitSlot.Flower, new [] { TraitTag.Thunder, TraitTag.Seed }, 12, 6f, 4f, 0, new Color(.9f, .85f, .18f)),
        };

        public static readonly IReadOnlyList<EvolutionDefinition> Evolutions = new List<EvolutionDefinition>
        {
            Evolution("sharp_vine", "尖锐藤鞭", TraitTag.Vine, 4, .9f, "藤蔓攻击更快且更锋利"),
            Evolution("spore_cloud", "增殖孢子", TraitTag.Poison, 3, .85f, "毒雾范围与持续伤害提升"),
            Evolution("thorn_skin", "棘皮", TraitTag.Armor, 3, 1f, "受击释放短距离尖刺"),
            Evolution("chain_lightning", "链状闪电", TraitTag.Thunder, 5, .9f, "雷种命中后跳向附近敌人"),
            Evolution("volatile_sap", "易燃树液", TraitTag.Burst, 6, .9f, "爆裂攻击造成更高伤害"),
            Evolution("wild_growth", "野性生长", TraitTag.Vine, 3, .95f, "所有攻击的攻击距离提升"),
            Evolution("twin_tendril", "双生触须", TraitTag.Vine, 3, .92f, "藤蔓连击的攻击窗口延长"),
            Evolution("rooted_strike", "扎根猛击", TraitTag.Vine, 5, 1f, "地面攻击获得更强击退"),
            Evolution("poison_residue", "腐殖残留", TraitTag.Poison, 4, .95f, "毒素持续时间延长"),
            Evolution("slow_spores", "迟缓孢子", TraitTag.Poison, 2, .9f, "中毒敌人移动速度降低"),
            Evolution("fungal_bloom", "菌群绽放", TraitTag.Poison, 5, 1f, "毒雾覆盖范围扩大"),
            Evolution("bark_guard", "树皮护甲", TraitTag.Armor, 2, 1f, "护甲根的减伤效果增强"),
            Evolution("counter_thorns", "反击尖刺", TraitTag.Thorn, 4, .95f, "近战命中产生尖刺反击"),
            Evolution("needle_rain", "针刺雨", TraitTag.Thorn, 5, .9f, "尖刺攻击的攻击范围提升"),
            Evolution("thunder_seed", "雷种", TraitTag.Thunder, 4, .92f, "种子弹的飞行速度提高"),
            Evolution("storm_charge", "风暴充能", TraitTag.Thunder, 6, 1f, "连续命中提升雷鸣伤害"),
            Evolution("germination", "快速萌发", TraitTag.Seed, 3, .85f, "种子技能冷却缩短"),
            Evolution("scatter_seeds", "散播种子", TraitTag.Seed, 4, .95f, "种子攻击更容易命中多个目标"),
            Evolution("explosive_bark", "爆裂树皮", TraitTag.Burst, 5, .92f, "爆裂命中造成更强击退"),
            Evolution("delayed_bloom", "延迟绽放", TraitTag.Burst, 7, 1f, "爆裂技能范围增加"),
            Evolution("clean_sap", "净化树液", TraitTag.Corruption, 4, .9f, "低污染时获得额外伤害"),
            Evolution("dark_nectar", "暗蜜", TraitTag.Corruption, 8, .95f, "高污染时强化攻击但增加风险"),
            Evolution("sunlit_leaf", "向阳新叶", TraitTag.Seed, 2, .88f, "空中施放技能恢复少量生命"),
            Evolution("ancient_rhythm", "古树节律", TraitTag.Armor, 4, .9f, "冲刺后下一次攻击伤害提高"),
        };

        public static readonly IReadOnlyList<FusionDefinition> Fusions = new List<FusionDefinition>
        {
            new FusionDefinition { Id = "plague_mycelium", DisplayName = "瘟疫菌丝", FirstTrait = "vine_tendril", SecondTrait = "toxic_cap", ResultDescription = "毒雾感染附近敌人并延长持续时间" },
            new FusionDefinition { Id = "thunder_dandelion", DisplayName = "雷暴蒲公英", FirstTrait = "thunder_flower", SecondTrait = "vine_tendril", ResultDescription = "种子命中落雷并链至邻敌" },
        };

        public static TraitDefinition FindTrait(string id)
        {
            foreach (TraitDefinition trait in Traits) if (trait.Id == id) return trait;
            return null;
        }

        public static void Validate()
        {
            HashSet<string> ids = new HashSet<string>();
            foreach (TraitDefinition trait in Traits)
            {
                if (string.IsNullOrEmpty(trait.Id) || !ids.Add(trait.Id)) Debug.LogError("PlantSpirit data error: trait ID missing or duplicated.");
                if (trait.Tags == null || trait.Tags.Length == 0) Debug.LogError("PlantSpirit data error: trait " + trait.Id + " has no tags.");
            }
            foreach (FusionDefinition fusion in Fusions)
            {
                if (FindTrait(fusion.FirstTrait) == null || FindTrait(fusion.SecondTrait) == null) Debug.LogError("PlantSpirit data error: fusion " + fusion.Id + " references a missing trait.");
            }
            if (Evolutions.Count < 24) Debug.LogError("PlantSpirit data error: vertical slice requires at least 24 evolutions.");
        }

        private static TraitDefinition Trait(string id, string name, TraitSlot slot, TraitTag[] tags, float damage, float range, float cooldown, float reduction, Color color)
        {
            return new TraitDefinition { Id = id, DisplayName = name, Slot = slot, Tags = tags, Damage = damage, Range = range, Cooldown = cooldown, DamageReduction = reduction, Color = color };
        }

        private static EvolutionDefinition Evolution(string id, string name, TraitTag tag, float damage, float cooldown, string description)
        {
            return new EvolutionDefinition { Id = id, DisplayName = name, RequiredTag = tag, DamageBonus = damage, CooldownMultiplier = cooldown, Description = description };
        }
    }
}
