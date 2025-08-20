# ScriptExecutor 脚本执行器系统

## 概述

ScriptExecutor 是一个基于 **DynamicExpresso** 的脚本执行器，用于在游戏运行时执行 C# 表达式和语句。它比自定义解释器更强大、更安全、更易维护。

## 为什么选择 DynamicExpresso？

### 自定义解释器的问题
- ❌ **语法解析复杂**：需要处理各种边界情况、错误处理
- ❌ **类型安全**：缺乏编译时检查，运行时错误难以调试
- ❌ **性能问题**：字符串解析和反射调用效率低
- ❌ **维护成本**：需要持续修复bug和添加新功能
- ❌ **功能限制**：难以支持复杂的表达式和逻辑

### DynamicExpresso 的优势
- ✅ **成熟稳定**：经过大量生产环境验证
- ✅ **类型安全**：支持强类型检查和编译时验证
- ✅ **性能优秀**：支持预编译，执行效率高
- ✅ **功能强大**：支持完整的 C# 表达式语法
- ✅ **易于维护**：由专业团队维护，bug修复及时

## 核心功能

### 1. 基础脚本执行
```csharp
// 简单表达式
var result = ScriptExecutor.ExecuteScript("gv[\"player_level\"]");

// 复杂逻辑
var canProceed = ScriptExecutor.ExecuteScriptAsBool(
    "gv[\"player_level\"] >= 10 && gv[\"magic_skill\"] >= 50"
);
```

### 2. 参数化脚本
```csharp
var parameters = new Dictionary<string, object>
{
    ["threshold"] = 15,
    ["skillName"] = "magic_theory"
};

var result = ScriptExecutor.ExecuteScriptWithParameters(
    "gv[\"player_level\"] >= threshold && gv[skillName] >= 50",
    parameters
);
```

### 3. 类型安全执行
```csharp
// 返回指定类型
int level = ScriptExecutor.ExecuteScriptAsType<int>("gv[\"player_level\"]");
string name = ScriptExecutor.ExecuteScriptAsType<string>("gv[\"player_name\"]");
```

### 4. 脚本编译（性能优化）
```csharp
// 预编译脚本，提高重复执行性能
var compiledScript = ScriptExecutor.CompileScript("gv[\"level\"] >= 10", typeof(bool));
bool result = (bool)compiledScript.DynamicInvoke();
```

### 5. 批量执行
```csharp
var scripts = new List<string>
{
    "gv[\"level\"] >= 10",
    "gv[\"gold\"] >= 1000",
    "gv[\"quest_completed\"] == true"
};

var results = ScriptExecutor.ExecuteMultipleScripts(scripts);
```

## 全局变量和快捷引用

### 内置变量
- `Global` / `global` - Global.Instance 引用
- `gv` - Global.Instance.globalVariables 的简写
- `gameManager` / `gm` - 游戏管理器引用
- `true`, `false`, `null` - 字面量值

### 内置类型和方法
- `Math` - 数学函数库
- `String` - 字符串操作
- `Convert` - 类型转换

## 使用示例

### 1. 简单条件判断
```yaml
# YAML 配置
enable_conditions:
  - type: "script"
    condition_id: "level_check"
    description: "检查玩家等级"
    script: "gv[\"player_level\"] >= 10"
```

### 2. 复杂逻辑组合
```yaml
enable_conditions:
  - type: "script"
    condition_id: "advanced_check"
    description: "复杂的条件组合"
    script: |
      gv["player_level"] >= 10 && 
      gv["magic_skill"] >= 50 && 
      gv["gold"] >= 1000 &&
      gv["quest_completed"] == true
```

### 3. 数学计算
```yaml
enable_conditions:
  - type: "script"
    condition_id: "score_check"
    description: "计算综合评分"
    script: "gv[\"combat_score\"] * 0.4 + gv[\"magic_score\"] * 0.6 >= 80"
```

### 4. 字符串操作
```yaml
enable_conditions:
  - type: "script"
    condition_id: "name_check"
    description: "检查玩家名称"
    script: "gv[\"player_name\"].Contains(\"Hero\") && gv[\"player_name\"].Length >= 5"
```

### 5. 动态状态检查
```yaml
enable_conditions:
  - type: "script"
    condition_id: "state_check"
    description: "检查游戏状态"
    script: |
      var currentTime = gv["game_time"];
      var dayPhase = currentTime % 24;
      dayPhase >= 6 && dayPhase <= 18  // 白天时间
```

## 性能优化建议

### 1. 使用预编译脚本
```csharp
// 对于频繁执行的脚本，使用预编译
private Delegate compiledCondition;

public void Initialize()
{
    compiledCondition = ScriptExecutor.CompileScript(
        "gv[\"level\"] >= 10 && gv[\"gold\"] >= 1000",
        typeof(bool)
    );
}

public bool CheckCondition()
{
    return (bool)compiledCondition.DynamicInvoke();
}
```

### 2. 避免重复解析
```csharp
// 缓存解释器实例
private Interpreter interpreter;

public void Initialize()
{
    interpreter = new Interpreter();
    ScriptExecutor.AddGlobalReferences(interpreter);
    interpreter.SetVariable("gameManager", gameManager);
}

public object ExecuteScript(string script)
{
    return interpreter.Eval(script);
}
```

### 3. 批量执行
```csharp
// 一次性执行多个相关脚本
var scripts = new List<string>
{
    "gv[\"level\"] >= 10",
    "gv[\"gold\"] >= 1000",
    "gv[\"quest_completed\"] == true"
};

var results = ScriptExecutor.ExecuteMultipleScripts(scripts);
```

## 错误处理和调试

### 1. 语法验证
```csharp
// 在执行前验证脚本语法
if (ScriptExecutor.ValidateScript(script))
{
    var result = ScriptExecutor.ExecuteScript(script);
}
else
{
    GD.PrintErr("脚本语法错误");
}
```

### 2. 返回类型检查
```csharp
// 获取脚本的返回类型
var returnType = ScriptExecutor.GetScriptReturnType(script);
GD.Print($"脚本返回类型: {returnType?.Name}");
```

### 3. 异常处理
```csharp
try
{
    var result = ScriptExecutor.ExecuteScript(script);
    return result;
}
catch (Exception ex)
{
    GD.PrintErr($"脚本执行失败: {ex.Message}");
    return false;
}
```

## 最佳实践

### 1. 脚本设计原则
- **简洁明了**：避免过于复杂的表达式
- **可读性强**：使用有意义的变量名和注释
- **性能考虑**：避免在脚本中进行大量计算
- **错误处理**：考虑边界情况和异常情况

### 2. 命名规范
- 使用 `gv` 作为 GlobalVariables 的简写
- 使用 `gm` 作为 GameManager 的简写
- 变量名使用小驼峰命名法
- 常量使用全大写

### 3. 安全性考虑
- 避免执行用户输入的脚本
- 限制脚本的执行权限
- 监控脚本的执行时间和资源消耗

## 与旧系统的对比

| 特性 | 自定义解释器 | DynamicExpresso |
|------|-------------|-----------------|
| 语法支持 | 有限 | 完整的C#语法 |
| 类型安全 | 无 | 强类型支持 |
| 性能 | 较低 | 较高（支持预编译） |
| 维护成本 | 高 | 低 |
| 错误处理 | 基础 | 完善 |
| 调试支持 | 有限 | 完整 |

## 总结

使用 DynamicExpresso 替代自定义解释器是一个明智的选择：

1. **更强大**：支持完整的 C# 表达式语法
2. **更安全**：类型安全和编译时检查
3. **更高效**：支持预编译和性能优化
4. **更易维护**：由专业团队维护，bug修复及时
5. **更易调试**：完整的错误信息和调试支持

通过 ScriptExecutor 系统，开发者可以：
- 在 YAML 配置中直接写 C# 表达式
- 实现复杂的游戏逻辑和条件判断
- 享受类型安全和性能优化的好处
- 专注于游戏逻辑而不是脚本引擎的维护

