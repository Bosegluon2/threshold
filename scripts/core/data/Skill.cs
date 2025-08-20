using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using Threshold.Core.Utils;

namespace Threshold.Core.Data
{
    public partial class Skill : Resource
    {
        [Export] public string Id { get; set; }
        [Export] public string Name { get; set; }
        [Export] public string Description { get; set; }
        [Export] public string Category { get; set; } = "combat"; // combat, utility, knowledge, passive
        [Export] public string Type { get; set; } = "active"; // active, passive, toggle
        [Export] public string Element { get; set; } = "physical"; // physical, fire, ice, lightning, arcane, etc.
        [Export] public int Level { get; set; } = 1;
        [Export] public int MaxLevel { get; set; } = 10;
        [Export] public int EnergyCost { get; set; } = 0;
        [Export] public float Cooldown { get; set; } = 0f; // 冷却时间（秒）
        [Export] public float Range { get; set; } = 0f; // 施法范围
        [Export] public float AreaOfEffect { get; set; } = 0f; // 影响范围
        [Export] public string Icon { get; set; } = "";
        [Export] public string Animation { get; set; } = "";
        [Export] public string SoundEffect { get; set; } = "";
        
        // 技能效果脚本
        [Export] public string EffectScript { get; set; } = "";
        
        // 技能要求
        [Export] public Array<SkillRequirement> Requirements { get; set; } = new Array<SkillRequirement>();
        
        // 技能升级
        [Export] public Array<SkillUpgrade> Upgrades { get; set; } = new Array<SkillUpgrade>();
        
        // 技能状态
        private float lastUsedTime = 0f;
        private bool isOnCooldown = false;
        private int currentLevel = 1;

        public Skill() { }

        public Skill(string id, string name, string description, int level, int energyCost)
        {
            Id = id;
            Name = name;
            Description = description;
            Level = level;
            EnergyCost = energyCost;
        }

        /// <summary>
        /// 创建完整的Skill对象
        /// </summary>
        public Skill(string id, string name, string description, string category, string type, string element,
                     int level, int maxLevel, int energyCost, float cooldown, float range, float areaOfEffect,
                     string icon, string animation, string soundEffect, string effectScript = "")
        {
            Id = id;
            Name = name;
            Description = description;
            Category = category;
            Type = type;
            Element = element;
            Level = level;
            MaxLevel = maxLevel;
            EnergyCost = energyCost;
            Cooldown = cooldown;
            Range = range;
            AreaOfEffect = areaOfEffect;
            Icon = icon;
            Animation = animation;
            SoundEffect = soundEffect;
            EffectScript = effectScript;
        }

        /// <summary>
        /// 使用技能（检查和使用合并）
        /// </summary>
        public object Use(float currentTime, Agent.Agent caster = null, Agent.Agent target = null, int currentEnergy = 0)
        {
            try
            {
                // 执行技能脚本，脚本内部处理所有检查逻辑
                var result = ExecuteSkillScript(currentTime, caster, target, currentEnergy);
                
                // 如果脚本返回 0，表示使用成功，更新技能状态
                if (result != null && result.ToString() == "0")
                {
                    lastUsedTime = currentTime;
                    isOnCooldown = true;
                    GD.Print($"技能 {Name} 使用成功");
                }
                else if (result != null && result.ToString() == "1")
                {
                    GD.Print($"技能 {Name} 使用失败");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"技能 {Name} 执行失败: {ex.Message}");
                return 1; // 返回失败
            }
        }

        /// <summary>
        /// 执行技能脚本（包含所有检查逻辑）
        /// </summary>
        private object ExecuteSkillScript(float currentTime, Agent.Agent caster, Agent.Agent target, int currentEnergy)
        {
            if (string.IsNullOrWhiteSpace(EffectScript))
                return 0; // 没有脚本的技能默认成功

            try
            {
                // 创建脚本上下文，包含技能、角色信息和检查条件
                var context = new Godot.Collections.Dictionary<string, Variant>
                {
                    ["skill"] = this,
                    ["caster"] = caster,
                    ["target"] = target,
                    ["level"] = Level,
                    ["energyCost"] = EnergyCost,
                    ["currentTime"] = currentTime,
                    ["currentEnergy"] = currentEnergy,
                    ["lastUsedTime"] = lastUsedTime,
                    ["cooldown"] = Cooldown,
                    ["isOnCooldown"] = isOnCooldown
                };

                // 执行效果脚本
                var result = ScriptExecutor.Instance.ExecuteScript<object>(EffectScript, context);
                GD.Print($"技能 {Name} 脚本执行完成，结果: {result}");
                return result;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"技能 {Name} 脚本执行失败: {ex.Message}");
                return 1; // 返回失败
            }
        }



        /// <summary>
        /// 升级技能
        /// </summary>
        public bool Upgrade()
        {
            if (currentLevel >= MaxLevel) return false;
            
            currentLevel++;
            
            // 应用升级效果
            foreach (var upgrade in Upgrades)
            {
                if (upgrade.Level == currentLevel)
                {
                    ApplyUpgrade(upgrade);
                }
            }
            
            return true;
        }

        /// <summary>
        /// 应用升级效果
        /// </summary>
        private void ApplyUpgrade(SkillUpgrade upgrade)
        {
            // 这里实现升级效果的应用逻辑
            GD.Print($"技能 {Name} 升级到 {currentLevel} 级: {upgrade.Effect}");
        }

        /// <summary>
        /// 检查是否满足技能要求
        /// </summary>
        public bool CheckRequirements(object character)
        {
            foreach (var requirement in Requirements)
            {
                if (!requirement.IsSatisfied(character))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 获取技能描述
        /// </summary>
        public string GetFullDescription()
        {
            var description = $"{Name} (等级 {Level}/{MaxLevel})\n";
            description += $"{Description}\n";
            description += $"类型: {Category} - {Type}\n";
            description += $"元素: {Element}\n";
            description += $"能量消耗: {EnergyCost}\n";
            description += $"冷却时间: {Cooldown}秒\n";
            
            if (Range > 0)
                description += $"施法范围: {Range}米\n";
            if (AreaOfEffect > 0)
                description += $"影响范围: {AreaOfEffect}米\n";
            
            if (!string.IsNullOrWhiteSpace(EffectScript))
            {
                description += $"\n效果脚本: {EffectScript}\n";
            }
            
            return description;
        }

        public bool Is(string skillId)
        {
            return Id == skillId;
        }

        public override bool Equals(object obj)
        {
            return obj is Skill skill &&
                   Id == skill.Id;
        }

        public override string ToString()
        {
            return $"{Name}: {Description} (等级: {Level}/{MaxLevel})";
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id);
        }
    }



    /// <summary>
    /// 技能要求
    /// </summary>  
    public partial class SkillRequirement : Resource
    {
        [Export] public string Type { get; set; } = "";
        [Export] public string Name { get; set; } = "";
        [Export] public float Value { get; set; } = 0f;

        public SkillRequirement() { }

        public SkillRequirement(string type, string name, float value)
        {
            Type = type;
            Name = name;
            Value = value;
        }

        public bool IsSatisfied(object character)
        {
            // 使用PathResolver检查要求
            var currentValue = ScriptExecutor.GetValue(character, Name);
            if (currentValue == null) return false;
            
            if (float.TryParse(currentValue.ToString(), out float numValue))
            {
                return numValue >= Value;
            }
            
            return false;
        }
    }

    /// <summary>
    /// 技能升级
    /// </summary>

    public partial class SkillUpgrade : Resource
    {
        [Export] public int Level { get; set; } = 1;
        [Export] public string Effect { get; set; } = "";

        public SkillUpgrade() { }

        public SkillUpgrade(int level, string effect)
        {
            Level = level;
            Effect = effect;
        }
    }
}
