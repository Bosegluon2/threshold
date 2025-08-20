using Godot;
using System;
using Threshold.Core;
using Threshold.Core.Agent;
using Threshold.Core.Agent.Functions;

namespace Threshold.Test
{
    /// <summary>
    /// 函数调用测试脚本
    /// </summary>
    public partial class FunctionCallTest : Node
    {
        public override void _Ready()
        {
            GD.Print("=== 函数调用测试开始 ===");
            
            // 等待一帧确保GameManager已初始化
            CallDeferred(nameof(RunFunctionTests));
        }
        
        private void RunFunctionTests()
        {
            try
            {
                if (GameManager.Instance?.CharacterManager == null)
                {
                    GD.PrintErr("❌ GameManager或CharacterManager未初始化");
                    return;
                }
                
                var characterManager = GameManager.Instance.CharacterManager;
                var functionManager = characterManager.GetFunctionManager();
                
                if (functionManager == null)
                {
                    GD.PrintErr("❌ 函数管理器未初始化");
                    return;
                }
                
                GD.Print("✅ 函数管理器可用，开始测试函数调用");
                
                // 测试GetWorldInfoFunction
                TestGetWorldInfoFunction(functionManager);
                
                GD.Print("=== 函数调用测试完成 ===");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"测试过程中发生错误: {ex.Message}");
                GD.PrintErr($"堆栈跟踪: {ex.StackTrace}");
            }
        }
        
        private void TestGetWorldInfoFunction(FunctionManager functionManager)
        {
            GD.Print("--- 测试GetWorldInfoFunction ---");
            
            try
            {
                // 创建函数调用
                var arguments = new Godot.Collections.Dictionary
                {
                    ["info_type"] = "all"
                };
                
                var functionCall = new FunctionCall("get_world_info", arguments);
                GD.Print($"创建函数调用: {functionCall.Name}");
                
                // 执行函数
                var result = functionManager.ExecuteFunction(functionCall);
                
                if (result != null)
                {
                    GD.Print($"✅ 函数执行成功: {result.Success}");
                    GD.Print($"✅ 函数名称: {result.Name}");
                    GD.Print($"✅ 函数内容: {result.Content}");
                    if (!result.Success)
                    {
                        GD.PrintErr($"❌ 函数执行失败: {result.ErrorMessage}");
                    }
                }
                else
                {
                    GD.PrintErr("❌ 函数执行返回null");
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"测试GetWorldInfoFunction时发生错误: {ex.Message}");
                GD.PrintErr($"堆栈跟踪: {ex.StackTrace}");
            }
        }
    }
}
