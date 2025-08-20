using Godot;
using Godot.Collections;
using System;
// using System.Collections.Generic;
using Threshold.Core.Data;
using Threshold.Core.Utils;
using Threshold.Core.Agent;
using System.Linq;
using Threshold.Core;

namespace Threshold.Test
{
    /// <summary>
    /// 测试新的script-based状态系统
    /// </summary>
    public partial class StatusScriptTest : Node
    {
        public override void _Ready()
        {
            GD.Print("=== 开始测试新的script-based状态系统 ===");
            
            TestStatusScripts();
            
            GD.Print("=== 状态系统测试完成 ===");
        }
        
        /// <summary>
        /// 测试状态脚本
        /// </summary>
        private void TestStatusScripts()
        {
           
            // var status = DataGenerator.Instance.GenerateClass(typeof(Status)).Result;
            // GD.Print($"AI生成的数据: {status}");
            try
            {
                // 测试健康状态
                TestHealthStatus();

                // 测试受伤状态
                TestInjuredStatus();

                // 测试自信状态
                TestConfidentStatus();

                // 测试专注状态
                TestFocusStatus();

                // 测试疲惫状态
                TestExhaustedStatus();

                // 测试正常状态
                TestNormalStatus();
            }
            catch (Exception ex)
            {
                GD.PrintErr($"测试状态脚本时发生错误: {ex.Message}");
                GD.PrintErr($"错误堆栈: {ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// 测试健康状态
        /// </summary>
        private void TestHealthStatus()
        {
            GD.Print("\n--- 测试健康状态 ---");
            
            var status = new Status
            {
                Id = "healthy",
                Name = "健康",
                Description = "身体状态良好，各项机能正常",
                EffectScript = @"
# 健康状态效果脚本
var status = GetContext('status')
var target = GetContext('target')
print(target.CurrentHealth)
# 每一回合执行一次，所以不需要elapsedTime
var currentHealth = target.CurrentHealth
var maxHealth = target.Character.MaxHealth
if currentHealth < maxHealth:
    var newHealth = min(currentHealth + 1, maxHealth)
    target.SetHealth(newHealth)
    print('健康状态恢复1点生命值')

return true
"
            };
            
            // 使用简单的模拟数据
            var mockTarget = new Agent("test1", "test1");
            mockTarget.SetHealth(80);
            mockTarget.SetCharacter(new CharacterCard());
            mockTarget.Character.MaxHealth = 100;
            
            mockTarget.AddStatus(status);
            mockTarget.Step(1);
            GD.Print($"健康状态脚本执行结果: {mockTarget.CurrentHealth}");
        }
        
        /// <summary>
        /// 测试受伤状态
        /// </summary>
        private void TestInjuredStatus()
        {
            GD.Print("\n--- 测试受伤状态 ---");
            
            var status = new Status
            {
                Id = "injured",
                Name = "受伤",
                Description = "身体受到伤害，行动能力下降",
                EffectScript = @"
# 受伤状态效果脚本
var target=GetContext('target')
# 每分钟减少1点生命值
var currentHealth = target.CurrentHealth
if currentHealth > 0:
    var newHealth = max(currentHealth - 1, 0)
    target.SetHealth(newHealth)
    print('受伤状态减少1点生命值')
return true
"
            };
            
            var mockTarget = new Agent("test1", "test1");
            mockTarget.SetCharacter(new CharacterCard());
            mockTarget.Character.MaxHealth = 100;
            mockTarget.SetHealth(50);
            
            mockTarget.AddStatus(status);
            mockTarget.Step(1);
            GD.Print($"受伤状态脚本执行结果: {mockTarget.CurrentHealth}");
        }
        
        /// <summary>
        /// 测试自信状态
        /// </summary>
        private void TestConfidentStatus()
        {
            GD.Print("\n--- 测试自信状态 ---");
            
            var status = new Status
            {
                Id = "confident",
                Name = "自信",
                Description = "充满自信，社交和战斗能力提升",
                EffectScript = @"
# 自信状态效果脚本
var status = GetContext('status')
var target = GetContext('target')


# 社交能力提升15%
var currentSocial = target.CurrentWarpedInfo.Reasoning
var boostedSocial = int(currentSocial * 1.15)
target.CurrentWarpedInfo.Reasoning = boostedSocial
print('自信状态：社交能力提升15%')
print(""In Script: "", target.CurrentWarpedInfo.Reasoning)
return true
"
            };
            
            var mockTarget = new Agent("test1", "test1");
            mockTarget.SetCharacter(new CharacterCard());
            mockTarget.Character.BaseWarpedInfo.Reasoning = 10;
            mockTarget.CurrentWarpedInfo.Reasoning = 10;
            mockTarget.AddStatus(status);
            mockTarget.Step(1);
            GD.Print("Out Script: ", mockTarget.CurrentWarpedInfo.Reasoning);
        }

        /// <summary>
        /// 测试专注状态
        /// </summary>
        private void TestFocusStatus()
        {
            GD.Print("\n--- 测试专注状态 ---");

            var status = new Status
            {
                Id = "focus",
                Name = "专注",
                Description = "精神高度集中，学习和工作效率提升",
                EffectScript = @"
# 专注状态效果脚本
var status = GetContext('status')
var target = GetContext('target')



for i in target.CurrentStatus:
    if i.Id == 'skill_bonus':
        i.CurrentDuration += 15
        break
print('专注状态：技能效果提升15%')

return true
"
            };

            var mockTarget = new Agent("test1", "test1");
            mockTarget.SetCharacter(new CharacterCard());
            mockTarget.Character.BaseWarpedInfo.Reasoning = 10;
            mockTarget.CurrentStatus.Add(new Status("skill_bonus", "技能效果提升15%", "技能效果提升15%", 100));
            mockTarget.AddStatus(status);
            mockTarget.Step(1);
            // 使用LINQ的FirstOrDefault来查找skill_bonus状态
            var skillBonusStatus = mockTarget.CurrentStatus.FirstOrDefault(s => s.Id == "skill_bonus");
            if (skillBonusStatus != null)
            {
                GD.Print("Out Script: ", skillBonusStatus.CurrentDuration);
                GD.Print($"专注状态脚本执行结果: {skillBonusStatus.CurrentDuration}");
            }
            else
            {
                GD.Print("未找到 skill_bonus 状态");
            }
        }
        /// <summary>
        /// 测试疲惫状态
        /// </summary>
        private void TestExhaustedStatus()
        {
            GD.Print("\n--- 测试疲惫状态 ---");

            var status = new Status
            {
                Id = "exhausted",
                Name = "疲惫",
                Description = "身体和精神都感到疲惫，需要休息",
                EffectScript = @"
# 疲惫状态效果脚本
var status = GetContext('status')
var target = GetContext('target')
var current_turn = GetContext('current_turn')
var currentEnergy = target.CurrentEnergy
if currentEnergy > 0:
    var newEnergy = max(currentEnergy - 2, 0)
    target.SetEnergy(newEnergy)
    print('疲惫状态：损失2点能量值')

return true
"
            };

            var mockTarget = new Agent("test1", "test1");
            mockTarget.SetCharacter(new CharacterCard());
            mockTarget.Character.BaseWarpedInfo.Reasoning = 10;
            mockTarget.CurrentWarpedInfo.Reasoning = 10;
            mockTarget.AddStatus(status);
            mockTarget.Step(1);
            GD.Print("Out Script: ", mockTarget.CurrentWarpedInfo.Reasoning);

            GD.Print($"疲惫状态脚本执行结果: {mockTarget.CurrentEnergy}");
        }
        
        /// <summary>
        /// 测试正常状态
        /// </summary>
        private void TestNormalStatus()
        {
            GD.Print("\n--- 测试正常状态 ---");
            
            var status = new Status
            {
                Id = "normal",
                Name = "正常",
                Description = "身体和精神状态正常，没有特殊效果",
                EffectScript = @"
# 正常状态效果脚本
var status = GetContext('status')
var target = GetContext('target')

# 正常状态没有特殊效果，只是保持基础状态
print('正常状态：身体和精神状态正常')

return true
"
            };
            
            var mockTarget = new Agent("test1", "test1");
            mockTarget.SetCharacter(new CharacterCard());
            mockTarget.Character.BaseWarpedInfo.Reasoning = 10;
            mockTarget.CurrentWarpedInfo.Reasoning = 10;
            mockTarget.AddStatus(status);
            mockTarget.Step(1);
            GD.Print("Out Script: ", mockTarget.CurrentWarpedInfo.Reasoning);
            
            GD.Print($"正常状态脚本执行结果: {mockTarget.CurrentHealth}");
        }
    }
}
