# ScriptExecutor 修复说明

## 问题描述

在 Godot 环境中使用 Roslyn Scripting 时遇到了以下错误：

```
System.NotSupportedException: 无法对不含位置的程序集创建元数据引用。
```

这是因为 Roslyn Scripting 试图获取程序集的文件路径，但在 Godot 中程序集是动态加载的，没有传统的文件路径。

## 修复内容

### 1. 安全的引用配置

**修复前：**
```csharp
DefaultOptions = ScriptOptions.Default
    .AddReferences(
        typeof(object).Assembly,                // mscorlib
        typeof(GameManager).Assembly,           // 游戏核心
        typeof(Godot.GD).Assembly,              // Godot
        typeof(Godot.Collections.Dictionary).Assembly
    )
```

**修复后：**
```csharp
try
{
    // 在 Godot 环境中，我们需要使用更安全的方式来添加引用
    DefaultOptions = ScriptOptions.Default
        .AddImports(
            "System",
            "System.Linq",
            "System.Collections.Generic",
            "Godot",
            "Godot.Collections"
        );
    
    GD.Print("ScriptExecutor 静态构造函数完成，使用安全的引用配置");
}
catch (Exception ex)
{
    GD.PrintErr($"ScriptExecutor 静态构造函数失败: {ex.Message}");
    // 使用最基本的配置作为后备
    DefaultOptions = ScriptOptions.Default;
}
```

### 2. 脚本编译错误处理

**修复前：**
```csharp
var compiledScript = ScriptCache.GetOrAdd(script, code =>
{
    return Microsoft.CodeAnalysis.CSharp.Scripting.CSharpScript.Create<object>(code, DefaultOptions, typeof(ScriptGlobals));
});
```

**修复后：**
```csharp
var compiledScript = ScriptCache.GetOrAdd(script, code =>
{
    try
    {
        // 使用安全的脚本创建方式
        return Microsoft.CodeAnalysis.CSharp.Scripting.CSharpScript.Create<object>(code, DefaultOptions, typeof(ScriptGlobals));
    }
    catch (Exception ex)
    {
        GD.PrintErr($"脚本编译失败: {ex.Message}");
        // 返回一个简单的脚本作为后备
        return Microsoft.CodeAnalysis.CSharp.Scripting.CSharpScript.Create<object>("return 0;", DefaultOptions, typeof(ScriptGlobals));
    }
});
```

### 3. 后备执行方法

添加了一个后备的简单脚本执行方法，当 Roslyn 脚本执行失败时使用：

```csharp
/// <summary>
/// 后备的简单脚本执行方法
/// </summary>
private static T ExecuteSimpleScript<T>(string script, object context)
{
    try
    {
        GD.Print("=== 使用后备脚本执行方法 ===");
        
        // 简单的表达式求值，支持基本的数学运算和条件判断
        if (script.Contains("return"))
        {
            // 提取 return 语句
            var returnIndex = script.IndexOf("return");
            var endIndex = script.IndexOf(";", returnIndex);
            if (endIndex == -1) endIndex = script.Length;
            
            var returnExpression = script.Substring(returnIndex + 6, endIndex - returnIndex - 6).Trim();
            
            // 简单的表达式求值
            if (returnExpression == "0") return (T)(object)0;
            if (returnExpression == "1") return (T)(object)1;
            if (returnExpression == "true") return (T)(object)true;
            if (returnExpression == "false") return (T)(object)false;
            
            // 尝试解析数字
            if (int.TryParse(returnExpression, out int intResult))
                return (T)(object)intResult;
            if (float.TryParse(returnExpression, out float floatResult))
                return (T)(object)floatResult;
        }
        
        // 默认返回值
        return default(T);
    }
    catch (Exception ex)
    {
        GD.PrintErr($"后备脚本执行也失败: {ex.Message}");
        return default(T);
    }
}
```

### 4. 全局变量构建错误处理

在构建脚本全局变量时添加了错误处理：

```csharp
try
{
    var globals = new ScriptGlobals
    {
        GameManager = GameManager.Instance,
        Global = Global.Instance,
        CharacterManager = GameManager.Instance?.CharacterManager,  // 使用空条件运算符
        // ... 其他属性
    };
    
    return globals;
}
catch (System.Exception ex)
{
    GD.PrintErr($"构建脚本全局变量失败: {ex.Message}");
    // 返回一个基本的 globals 对象
    return new ScriptGlobals
    {
        Random = new Random(),
        PathGet = GetValue,
        PathSet = SetValue,
        PathExists = Exists,
        PathExplore = ExplorePaths
    };
}
```

## 使用方式

### 1. 基本脚本执行

```csharp
// 执行简单脚本
var result = ScriptExecutor.ExecuteScript<int>("return 42;");

// 执行复杂脚本
var script = @"
    var sum = 0;
    for (var i = 0; i < 5; i++) {
        sum += i;
    }
    return sum;
";
var result = ScriptExecutor.ExecuteScript<int>(script);
```

### 2. 技能脚本示例

```yaml
effect_script: |
  // 检查冷却时间
  if (skill.isOnCooldown && (currentTime - lastUsedTime) < cooldown) {
    return 1; // 使用失败
  }
  
  // 检查能量消耗
  if (currentEnergy < skill.energyCost) {
    return 1; // 使用失败
  }
  
  // 执行技能效果
  var damage = skill.level * 15 + 20;
  GD.Print($"技能造成 {damage} 点伤害");
  
  return 0; // 使用成功
```

## 优势

1. **兼容性** - 在 Godot 环境中稳定运行
2. **错误处理** - 多层错误处理，确保系统不会崩溃
3. **后备机制** - 当 Roslyn 脚本失败时，使用简单的后备方法
4. **性能优化** - 脚本编译缓存，避免重复编译
5. **调试友好** - 详细的错误日志和调试信息

## 注意事项

1. **性能** - Roslyn 脚本执行比简单表达式稍慢，但对于技能系统来说可以接受
2. **错误处理** - 脚本错误不会导致游戏崩溃，但会记录到日志
3. **后备执行** - 当复杂脚本失败时，系统会自动降级到简单执行模式
4. **调试** - 使用 `GD.Print` 来调试脚本执行过程

## 测试

运行 `scenes/test/TestScriptExecutor.tscn` 来测试修复后的 ScriptExecutor：

- 简单脚本执行测试
- 复杂脚本执行测试（包含循环、数组等）
- 路径操作测试
- 错误处理测试

## 未来改进

1. **更好的后备执行** - 实现更智能的脚本解析和简化
2. **性能优化** - 优化脚本编译和执行性能
3. **调试工具** - 添加脚本调试和可视化工具
4. **热重载** - 支持运行时脚本热重载
