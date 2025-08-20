using Godot;
using Threshold.Core.Utils;

namespace Threshold.Test
{
    public partial class ScriptExecutorTest : Node
    {
        public override void _Ready()
        {
            GD.Print("=== ScriptExecutor 测试开始 ===");
            
            // 测试简单的脚本执行
            TestSimpleScript();
            
            // 测试复杂的脚本执行
            TestComplexScript();
            
            // 测试路径操作
            TestPathOperations();
            
            GD.Print("=== ScriptExecutor 测试完成 ===");
        }
        
        private void TestSimpleScript()
        {
            GD.Print("--- 测试简单脚本 ---");
            
            try
            {
                // 测试基本返回值
                var result1 = ScriptExecutor.Instance.ExecuteScript("static func test() -> int: return 42");
                GD.Print($"简单返回值测试: {result1}");
                
                // 测试条件语句
            }
            catch (System.Exception ex)
            {
                GD.PrintErr($"简单脚本测试失败: {ex.Message}");
            }
        }
        
        private void TestComplexScript()
        {
            GD.Print("--- 测试复杂脚本 ---");
            
            try
            {
                // 测试循环和数组
                var script = @"
static func execute() -> int:
	return GameManager.CharacterManager.GetCharacterCount();
                ";
                
                var result = ScriptExecutor.Instance.ExecuteScript(script);
                GD.Print($"复杂脚本测试: {result}");
            }
            catch (System.Exception ex)
            {
                GD.PrintErr($"复杂脚本测试失败: {ex.Message}");
            }
        }
        
        private void TestPathOperations()
        {
            GD.Print("--- 测试路径操作 ---");
            
            try
            {
                // 创建测试对象
                var testObj = new TestObject
                {
                    Name = "测试对象",
                    Value = 123,
                    Nested = new TestObject { Name = "嵌套对象", Value = 456 }
                };
                
                // 测试路径获取
                var name = ScriptExecutor.GetValue(testObj, "Name");
                var value = ScriptExecutor.GetValue(testObj, "Value");
                var nestedName = ScriptExecutor.GetValue(testObj, "Nested.Name");
                
                GD.Print($"路径获取测试:");
                GD.Print($"  Name: {name}");
                GD.Print($"  Value: {value}");
                GD.Print($"  Nested.Name: {nestedName}");
                
                // 测试路径设置
                ScriptExecutor.SetValue(testObj, "Value", 999);
                var newValue = ScriptExecutor.GetValue(testObj, "Value");
                GD.Print($"路径设置测试: Value = {newValue}");
                
                // 测试路径存在性
                var exists1 = ScriptExecutor.Exists(testObj, "Name");
                var exists2 = ScriptExecutor.Exists(testObj, "NonExistent");
                GD.Print($"路径存在性测试: Name={exists1}, NonExistent={exists2}");
            }
            catch (System.Exception ex)
            {
                GD.PrintErr($"路径操作测试失败: {ex.Message}");
            }
        }
        
        // 测试用的简单类
        private class TestObject
        {
            public string Name { get; set; }
            public int Value { get; set; }
            public TestObject Nested { get; set; }
        }
    }
}
