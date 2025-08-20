using Godot;
using System;
using Threshold.Core;

namespace Threshold.Test
{
    /// <summary>
    /// 简单的CharacterManager测试脚本
    /// </summary>
    public partial class SimpleCharacterManagerTest : Node
    {
        public override void _Ready()
        {
            GD.Print("=== 简单CharacterManager测试开始 ===");
            
            // 等待一帧确保GameManager已初始化
            CallDeferred(nameof(RunSimpleTests));
        }
        
        private void RunSimpleTests()
        {
            try
            {
                // 检查GameManager
                if (GameManager.Instance == null)
                {
                    GD.PrintErr("❌ GameManager.Instance 为 null");
                    return;
                }
                GD.Print("✅ GameManager.Instance 可用");
                
                // 检查CharacterManager
                if (GameManager.Instance.CharacterManager == null)
                {
                    GD.PrintErr("❌ GameManager.CharacterManager 为 null");
                    return;
                }
                GD.Print("✅ GameManager.CharacterManager 可用");
                
                var characterManager = GameManager.Instance.CharacterManager;
                
                // 测试基本属性
                TestBasicProperties(characterManager);
                
                // 测试系统状态
                TestSystemStatus(characterManager);
                
                GD.Print("=== 简单测试完成 ===");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"测试过程中发生错误: {ex.Message}");
                GD.PrintErr($"堆栈跟踪: {ex.StackTrace}");
            }
        }
        
        private void TestBasicProperties(CharacterManager characterManager)
        {
            GD.Print("--- 测试基本属性 ---");
            
            try
            {
                // 测试角色列表
                if (characterManager.AllCharacters == null)
                {
                    GD.PrintErr("❌ AllCharacters 为 null");
                }
                else
                {
                    GD.Print($"✅ AllCharacters 可用，数量: {characterManager.AllCharacters.Count}");
                }
                
                // 测试角色数量
                var count = characterManager.GetCharacterCount();
                GD.Print($"✅ 角色数量: {count}");
                
                // 测试角色统计
                var stats = characterManager.GetCharacterStatistics();
                GD.Print($"✅ 角色统计: {stats}");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"测试基本属性时发生错误: {ex.Message}");
            }
        }
        
        private void TestSystemStatus(CharacterManager characterManager)
        {
            GD.Print("--- 测试系统状态 ---");
            
            try
            {
                // 测试函数管理器
                var functionManager = characterManager.GetFunctionManager();
                if (functionManager == null)
                {
                    GD.PrintErr("❌ 函数管理器为 null");
                }
                else
                {
                    GD.Print("✅ 函数管理器可用");
                    var functionCount = functionManager.GetAvailableFunctions().Count;
                    GD.Print($"✅ 可用函数数量: {functionCount}");
                }
                
                // 测试世界观管理器
                var worldLoreManager = characterManager.GetWorldLoreManager();
                if (worldLoreManager == null)
                {
                    GD.PrintErr("❌ 世界观管理器为 null");
                }
                else
                {
                    GD.Print("✅ 世界观管理器可用");
                    var entryCount = worldLoreManager.GetTotalEntries();
                    GD.Print($"✅ 世界观条目数量: {entryCount}");
                }
                
                // 测试AI通信
                var aiCommunication = characterManager.GetAICommunication();
                if (aiCommunication == null)
                {
                    GD.PrintErr("❌ AI通信模块为 null");
                }
                else
                {
                    GD.Print("✅ AI通信模块可用");
                }
                
                // 测试RAG系统
                var ragSystem = characterManager.GetRAGSystem();
                if (ragSystem == null)
                {
                    GD.PrintErr("❌ RAG系统为 null");
                }
                else
                {
                    GD.Print("✅ RAG系统可用");
                }
                
                // 测试函数系统可用性
                var isAvailable = characterManager.IsFunctionSystemAvailable();
                GD.Print($"✅ 函数系统可用性: {isAvailable}");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"测试系统状态时发生错误: {ex.Message}");
            }
        }
    }
}
