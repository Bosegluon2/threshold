using Godot;
using System;
using System.Collections.Generic;
using Threshold.Core.Utils;

namespace Threshold.Core.Effects
{
    /// <summary>
    /// 效果系统使用示例 - 更新为分布式Effect管理
    /// </summary>
    public class EffectUsageExample
    {
        /// <summary>
        /// 初始化效果系统
        /// </summary>
        public static void InitializeEffectSystem()
        {
            // 加载效果配置
            EffectLoader.Instance.LoadEffectConfigs();
            
            GD.Print("效果系统初始化完成");
        }
        
        /// <summary>
        /// 示例：执行单个效果
        /// </summary>
        public static void ExecuteSingleEffect()
        {
            // 创建技能提升效果
            var skillEffect = new EffectReference
            {
                EffectId = "skill_boost_example",
                EffectScript = @"
# 技能提升效果脚本
var target = GetContext('target')
var skill = GetContext('skill')
var value = GetContext('value')

if target and skill and value:
    var currentValue = PathGet(target, skill) or 0
    var newValue = currentValue + value
    PathSet(target, skill, newValue)
    print('技能提升: ' + str(skill) + ' +' + str(value) + ' = ' + str(newValue))
    return true
else:
    print('技能提升参数不完整')
    return false
",
                Parameters = new Dictionary<string, object>
                {
                    ["skill"] = "magic_theory",
                    ["value"] = 25
                }
            };
            
            // 模拟角色对象（实际使用时应该是真实的角色对象）
            var character = GameManager.Instance.CharacterManager.GetCharacterById("char_0001");
            
            // 直接执行效果脚本
            var context = new Godot.Collections.Dictionary<string, Variant>
            {
                ["target"] = Variant.CreateFrom(character),
                ["skill"] = Variant.CreateFrom("magic_theory"),
                ["value"] = Variant.CreateFrom(25)
            };
            
            var result = ScriptExecutor.Instance.ExecuteScript(skillEffect.EffectScript, context);
            GD.Print($"技能提升效果执行: {result}");
            
            // 查看结果
            var newSkillValue = character.CurrentSkills["magic_theory"];
            GD.Print($"艾莉娅的魔法理论技能现在是: {newSkillValue}");
        }
        
        /// <summary>
        /// 示例：使用效果模板
        /// </summary>
        public static void UseEffectTemplate()
        {
            // 获取效果模板
            var template = EffectLoader.Instance.GetEffectTemplate("skill_boost");
            if (template != null)
            {
                // 创建基于模板的效果实例
                var effect = new EffectReference
                {
                    EffectId = "custom_skill_boost",
                    EffectScript = template.EffectScript,
                    Category = template.Category,
                    TargetPath = template.TargetPath,
                    Parameters = new Dictionary<string, object>(template.Parameters)
                };
                
                // 覆盖默认参数
                effect.Parameters["skill"] = "combat";
                effect.Parameters["value"] = 15;
                
                // 模拟角色对象
                var character = GameManager.Instance.CharacterManager.GetCharacterById("char_0001");
                
                // 直接执行效果脚本
                var context = new Godot.Collections.Dictionary<string, Variant>
                {
                    ["target"] = Variant.CreateFrom(character),
                    ["skill"] = Variant.CreateFrom("combat"),
                    ["value"] = Variant.CreateFrom(15)
                };
                
                var result = ScriptExecutor.Instance.ExecuteScript(effect.EffectScript, context);
                GD.Print($"使用模板的效果执行: {result}");
                
                var newCombatValue = character.CurrentSkills["combat"];
                GD.Print($"艾莉娅的战斗技能现在是: {newCombatValue}");
            }
        }
    }
}
