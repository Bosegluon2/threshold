using Godot;
using System;
using Threshold.Core.Agent;
using Threshold.Core.Agent.Functions;

namespace Threshold.Test
{
    /// <summary>
    /// 函数调用调试测试脚本
    /// </summary>
    public partial class FunctionCallDebugTest : Node
    {
        public override void _Ready()
        {
            GD.Print("=== 函数调用调试测试开始 ===");
            
            // 逐步测试每个组件
            TestDictionaryCreation();
            TestFunctionCallCreation();
            TestFunctionExecution();
            
            GD.Print("=== 函数调用调试测试完成 ===");
        }
        
        private void TestDictionaryCreation()
        {
            GD.Print("--- 测试Dictionary创建 ---");
            
            try
            {
                var dict = new Godot.Collections.Dictionary();
                GD.Print("✅ 空Dictionary创建成功");
                
                dict["test"] = "value";
                GD.Print("✅ Dictionary添加元素成功");
                
                var json = Json.Stringify(dict);
                GD.Print($"✅ Dictionary序列化成功: {json}");
                
                var argsDict = new Godot.Collections.Dictionary
                {
                    ["info_type"] = "all"
                };
                GD.Print($"✅ 带参数的Dictionary创建成功，包含{argsDict.Count}个元素");
                
                var argsJson = Json.Stringify(argsDict);
                GD.Print($"✅ 参数Dictionary序列化成功: {argsJson}");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"❌ Dictionary测试失败: {ex.Message}");
                GD.PrintErr($"堆栈跟踪: {ex.StackTrace}");
            }
        }
        
        private void TestFunctionCallCreation()
        {
            GD.Print("--- 测试FunctionCall创建 ---");
            
            try
            {
                var argsDict = new Godot.Collections.Dictionary
                {
                    ["info_type"] = "all"
                };
                
                var functionCall = new FunctionCall("get_world_info", argsDict);
                GD.Print($"✅ FunctionCall创建成功: Name={functionCall.Name}");
                GD.Print($"✅ FunctionCall参数数量: {functionCall.Arguments?.Count ?? 0}");
                
                var argsJson = Json.Stringify(functionCall.Arguments);
                GD.Print($"✅ FunctionCall参数序列化成功: {argsJson}");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"❌ FunctionCall测试失败: {ex.Message}");
                GD.PrintErr($"堆栈跟踪: {ex.StackTrace}");
            }
        }
        
        private void TestFunctionExecution()
        {
            GD.Print("--- 测试函数执行 ---");
            
            try
            {
                // 直接创建函数实例
                var function = new GetWorldInfoFunction();
                GD.Print($"✅ GetWorldInfoFunction创建成功: {function.GetFunctionName()}");
                
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
                GD.PrintErr($"❌ 函数执行测试失败: {ex.Message}");
                GD.PrintErr($"堆栈跟踪: {ex.StackTrace}");
            }
        }
    }
}
