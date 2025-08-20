using Godot;
using System;
using Threshold.Core.Agent;
using Threshold.Core.Agent.Functions;

namespace Threshold.Test
{
    /// <summary>
    /// 简单函数测试脚本
    /// </summary>
    public partial class SimpleFunctionTest : Node
    {
        public override void _Ready()
        {
            GD.Print("=== 简单函数测试开始 ===");
            
            // 直接测试函数，不依赖GameManager
            TestDirectFunctionExecution();
            
            GD.Print("=== 简单函数测试完成 ===");
        }
        
        private void TestDirectFunctionExecution()
        {
            GD.Print("--- 直接测试GetWorldInfoFunction ---");
            
            try
            {
                // 直接创建函数实例
                var function = new GetWorldInfoFunction();
                GD.Print($"✅ 函数创建成功: {function.GetFunctionName()}");
                
                // 创建测试参数
                var arguments = new Godot.Collections.Dictionary
                {
                    ["info_type"] = "all"
                };
                GD.Print($"✅ 测试参数创建成功: {Json.Stringify(arguments)}");
                
                // 直接执行函数
                var result = function.Execute(arguments, null);
                
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
