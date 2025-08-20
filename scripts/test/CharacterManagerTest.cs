using Godot;
using System;
using Threshold.Core;
using Threshold.Core.Agent;

namespace Threshold.Test
{
    /// <summary>
    /// CharacterManager功能测试脚本
    /// </summary>
    public partial class CharacterManagerTest : Node
    {
        public override void _Ready()
        {
            GD.Print("=== CharacterManager测试开始 ===");
            
            // 等待一帧确保GameManager已初始化
            CallDeferred(nameof(RunTests));
        }
        
        private void RunTests()
        {
            try
            {
                if (GameManager.Instance?.CharacterManager == null)
                {
                    GD.PrintErr("GameManager或CharacterManager未初始化，无法进行测试");
                    return;
                }
                
                var characterManager = GameManager.Instance.CharacterManager;
                
                // 测试基本功能
                TestBasicFunctions(characterManager);
                
                // 测试系统功能
                TestSystemFunctions(characterManager);
                
                // 测试房间管理
                TestRoomManagement(characterManager);
                
                GD.Print("=== CharacterManager测试完成 ===");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"测试过程中发生错误: {ex.Message}");
            }
        }
        
        private void TestBasicFunctions(CharacterManager characterManager)
        {
            GD.Print("--- 测试基本功能 ---");
            
            // 测试角色统计
            var stats = characterManager.GetCharacterStatistics();
            GD.Print($"角色统计: {stats}");
            
            // 测试角色数量
            var count = characterManager.GetCharacterCount();
            GD.Print($"角色数量: {count}");
            
            // 测试获取所有角色
            var allCharacters = characterManager.GetAllAgents();
            GD.Print($"所有角色数量: {allCharacters.Count}");
            
            // 测试创建新角色
            var newAgent = characterManager.CreateCharacter(
                "测试角色",
                "勇敢、善良",
                "男",
                28,
                "测试职业",
                "测试派别",
                "这是一个测试角色"
            );
            
            if (newAgent != null)
            {
                GD.Print($"新角色创建成功: {newAgent.AgentName}");
                
                // 测试查找角色
                var foundAgent = characterManager.GetCharacterByName("测试角色");
                if (foundAgent != null)
                {
                    GD.Print($"角色查找成功: {foundAgent.AgentName}");
                }
                
                // 测试删除角色
                characterManager.DeleteCharacter(newAgent.AgentId);
                GD.Print("测试角色已删除");
            }
        }
        
        private void TestSystemFunctions(CharacterManager characterManager)
        {
            GD.Print("--- 测试系统功能 ---");
            
            // 测试函数管理器
            var functionManager = characterManager.GetFunctionManager();
            if (functionManager != null)
            {
                var functions = functionManager.GetAvailableFunctions();
                GD.Print($"可用函数数量: {functions.Count}");
                foreach (var func in functions)
                {
                    GD.Print($"  - {func.Name}: {func.Description}");
                }
            }
            
            // 测试世界观管理器
            var worldLoreManager = characterManager.GetWorldLoreManager();
            if (worldLoreManager != null)
            {
                var totalEntries = worldLoreManager.GetTotalEntries();
                GD.Print($"世界观条目数量: {totalEntries}");
            }
            
            // 测试AI通信
            var aiCommunication = characterManager.GetAICommunication();
            if (aiCommunication != null)
            {
                GD.Print("AI通信模块可用");
            }
            
            // 测试RAG系统
            var ragSystem = characterManager.GetRAGSystem();
            if (ragSystem != null)
            {
                GD.Print("RAG系统可用");
            }
            
            // 测试系统统计
            var systemStats = characterManager.GetSystemStats();
            GD.Print($"系统统计: {systemStats}");
        }
        
        private void TestRoomManagement(CharacterManager characterManager)
        {
            GD.Print("--- 测试房间管理 ---");
            
            // 创建测试房间
            var room = characterManager.CreateRoom("测试房间", "这是一个测试用的房间");
            if (room != null)
            {
                GD.Print($"房间创建成功: {room.Name}");
                
                // 测试房间统计
                var roomStats = characterManager.GetRoomStats();
                GD.Print($"房间统计: {roomStats}");
                
                // 测试获取房间
                var foundRoom = characterManager.GetRoomById(room.Id);
                if (foundRoom != null)
                {
                    GD.Print($"房间查找成功: {foundRoom.Name}");
                }
                
                // 测试删除房间
                var deleteResult = characterManager.DeleteRoom(room.Id);
                GD.Print($"房间删除结果: {deleteResult}");
            }
        }
    }
}

