# GDScript ScriptExecutor 使用说明

## 概述

新的 `ScriptExecutor` 使用 GDScript 作为脚本引擎，与 Godot 引擎有更好的集成性。虽然失去了 C# 语法的灵活性，但获得了更好的稳定性和性能。

## 主要特性

### 1. GDScript 集成
- 使用 Godot 原生的 GDScript 引擎
- 支持 GDScript 语法和特性
- 编译缓存，提高性能

### 2. 上下文传递
- 通过 `Context` 对象在 GDScript 中访问游戏数据
- 支持自定义上下文变量
- 路径操作函数集成

### 3. 路径解析功能
- 保留原有的路径解析和探索功能
- 支持动态属性访问
- 递归路径探索

## 使用方法

### 1. 基本脚本执行

```csharp
// 执行简单脚本
var result = ScriptExecutor.ExecuteScript("return 42;");

// 执行带上下文的脚本
var context = new Dictionary<string, object>
{
    ["player"] = playerObject,
    ["level"] = 5
};
var result = ScriptExecutor.ExecuteScript("return level * 10;", context);
```

### 2. 类型化执行

```csharp
// 返回指定类型
var intResult = ScriptExecutor.ExecuteScript<int>("return 42;");
var stringResult = ScriptExecutor.ExecuteScript<string>("return \"Hello\";");
```

### 3. GDScript 模板生成

```csharp
var scriptContent = "return Context.CharacterManager.GetCharacterCount();";
var fullScript = ScriptExecutor.GenerateGDScriptTemplate(scriptContent);
```

## GDScript 中的使用方式

### 1. 访问游戏管理器

```gdscript
# 通过 Context 访问各种管理器
var characterCount = Context.CharacterManager.GetCharacterCount()
var currentWeather = Context.WorldManager.GetCurrentWeather()
var playerLevel = Context.Global.globalVariables["player_level"]
```

### 2. 路径操作函数

```gdscript
# 获取属性值
var characterName = Context.PathGet(Context.CharacterManager, "characters.char_0001.name")

# 设置属性值
Context.PathSet(Context.Global, "globalVariables.test_value", 100)

# 检查属性是否存在
var exists = Context.PathExists(Context.CharacterManager, "characters.char_0001")

# 探索对象路径
Context.PathExplore(Context.WorldManager)
```

### 3. 工具函数

```gdscript
# 打印调试信息
Context.Print("调试信息")
Context.PrintErr("错误信息")

# 随机数生成
var randomValue = Context.Random.Next(1, 100)
```

## 上下文变量

### 1. 内置上下文

- `Context.GameManager` - 游戏管理器
- `Context.Global` - 全局变量管理器
- `Context.CharacterManager` - 角色管理器
- `Context.WorldManager` - 世界管理器
- `Context.EventManager` - 事件管理器
- `Context.ResourceManager` - 资源管理器
- `Context.SaveManager` - 保存管理器
- `Context.Library` - 游戏库
- `Context.CommitteeManager` - 委员会管理器

### 2. 自定义上下文

```csharp
var customContext = new Dictionary<string, object>
{
    ["skill"] = skillObject,
    ["caster"] = casterObject,
    ["target"] = targetObject,
    ["level"] = 5,
    ["energyCost"] = 25,
    ["currentTime"] = DateTime.Now,
    ["currentEnergy"] = 100,
    ["lastUsedTime"] = lastUsed,
    ["cooldown"] = 10.0f,
    ["isOnCooldown"] = false
};

var result = ScriptExecutor.ExecuteScript(script, customContext);
```

## 技能系统集成

### 1. 技能脚本示例

```gdscript
# 火球术效果脚本
extends RefCounted

var Context: ScriptContext

func execute():
    # 检查冷却时间
    if skill.isOnCooldown and (currentTime - lastUsedTime) < cooldown:
        Context.Print("技能还在冷却中")
        return 1  # 使用失败
    
    # 检查能量消耗
    if currentEnergy < skill.energyCost:
        Context.Print("能量不足")
        return 1  # 使用失败
    
    # 执行技能效果
    var damage = skill.level * 15 + 20
    Context.Print("技能造成 " + str(damage) + " 点伤害")
    
    return 0  # 使用成功
```

### 2. 技能脚本调用

```csharp
var skillScript = @"
    # 检查冷却时间
    if skill.isOnCooldown and (currentTime - lastUsedTime) < cooldown:
        return 1
    
    # 检查能量消耗
    if currentEnergy < skill.energyCost:
        return 1
    
    # 执行技能效果
    var damage = skill.level * 15 + 20
    return 0
";

var result = ScriptExecutor.ExecuteScript<int>(skillScript, skillContext);
```

## 优势

### 1. 稳定性
- 使用 Godot 原生引擎，兼容性更好
- 减少外部依赖，降低出错概率
- 更好的错误处理和调试信息

### 2. 性能
- GDScript 编译缓存
- 原生 Godot 对象，无需类型转换
- 更快的执行速度

### 3. 集成性
- 与 Godot 引擎无缝集成
- 支持所有 Godot 类型和功能
- 更好的编辑器支持

## 限制

### 1. 语法限制
- 不支持完整的 C# 语法
- 不支持复杂的控制流结构
- 语法相对简单

### 2. 调试能力
- 调试信息相对有限
- 错误堆栈可能不够详细
- 需要依赖 Godot 的调试工具

## 迁移指南

### 从 Roslyn Scripting 迁移

1. **语法调整**
   - 将 C# 语法转换为 GDScript 语法
   - 使用 `Context` 对象访问游戏数据
   - 调整变量声明和函数调用

2. **类型转换**
   - 使用 `ExecuteScript<T>` 进行类型化执行
   - 注意 GDScript 的类型系统差异
   - 处理返回值类型转换

3. **错误处理**
   - 使用 GDScript 的错误处理机制
   - 添加适当的调试输出
   - 测试脚本执行结果

## 测试

运行 `scenes/test/TestScriptExecutor.tscn` 来测试新的 ScriptExecutor：

- GDScript 脚本执行测试
- 上下文变量传递测试
- 路径操作函数测试
- 类型转换测试

## 未来改进

1. **更好的 GDScript 支持**
   - 支持更多 GDScript 特性
   - 改进错误处理和调试信息
   - 添加语法高亮和智能提示

2. **性能优化**
   - 优化脚本编译缓存
   - 改进上下文变量传递
   - 减少内存分配

3. **开发工具**
   - 添加 GDScript 编辑器支持
   - 集成调试工具
   - 提供更多示例和模板
