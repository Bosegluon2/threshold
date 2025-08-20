using Godot;
using Threshold.Core;
using Threshold.Core.Utils;

namespace Threshold.Test
{
    /// <summary>
    /// DataPathExplorer 测试脚本
    /// 用于验证深度递归功能是否正常工作
    /// </summary>
    public partial class DataPathExplorerTest : Node
    {
        public override void _Ready()
        {
            GD.Print("=== DataPathExplorer 深度递归测试 ===");

            // 创建测试用的GameManager
            var gameManager = CreateTestGameManager();

            // 测试深度探索
            GD.Print("\n1. 测试基础路径探索:");
            ScriptExecutor.ExplorePaths(gameManager);



            GD.Print("\n=== 测试完成 ===");
            Global.Instance.globalVariables["test_key"] = "test_value";
        }
        
        
        /// <summary>
        /// 创建测试用的GameManager
        /// </summary>
        private GameManager CreateTestGameManager()
        {
            var gameManager = new GameManager();
            
            // 使用反射设置一些测试数据
            var worldABCType = typeof(GameManager).GetProperty("WorldABC");
            if (worldABCType != null)
            {
                var worldABC = new WorldABC();
                
                // 设置一些嵌套的测试数据
                var worldState = new System.Collections.Generic.Dictionary<string, object>
                {
                    ["test_key"] = "test_value",
                    ["nested"] = new System.Collections.Generic.Dictionary<string, object>
                    {
                        ["level1"] = "value1",
                        ["level2"] = new System.Collections.Generic.Dictionary<string, object>
                        {
                            ["deep_nested"] = "deep_value"
                        }
                    }
                };
                
                var worldABCType2 = typeof(WorldABC).GetProperty("WorldState");
                if (worldABCType2 != null)
                {
                    worldABCType2.SetValue(worldABC, worldState);
                }
                
                worldABCType.SetValue(gameManager, worldABC);
            }
            
            return gameManager;
        }
    }
    
    /// <summary>
    /// 简化的WorldABC类，用于测试
    /// </summary>
    public class WorldABC
    {
        public System.Collections.Generic.Dictionary<string, object> WorldState { get; set; } = new();
    }
}
