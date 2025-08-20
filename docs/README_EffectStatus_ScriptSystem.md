# Effect/Status Script System

## 概述

本项目已经将所有Effect和Status相关的类改为使用script实现，提供了最大的灵活性和可扩展性。系统使用GDScript作为脚本语言，通过ScriptExecutor执行。

## 主要变化

### 1. Status类重构

**之前的结构：**
- `Status`类包含`Array<StatusEffect> Effects`属性
- `StatusEffect`类定义具体的效果逻辑

**现在的结构：**
- `Status`类包含`string EffectScript`属性
- 所有效果逻辑通过GDScript脚本实现
- 移除了`StatusEffect`类

**示例：**
```csharp
// 之前
public Array<StatusEffect> Effects { get; set; } = new Array<StatusEffect>();

// 现在
public string EffectScript { get; set; } = "";
```

### 2. PlaceEffect类重构

**之前的结构：**
- 硬编码的效果应用方法（`ApplyEnvironmentalEffect`, `ApplyMagicalEffect`等）
- 复杂的switch语句处理不同效果类型

**现在的结构：**
- 包含`string EffectScript`属性
- 通过脚本执行所有效果逻辑
- 简化的`ApplyToAgent`方法

**示例：**
```csharp
// 之前
private void ApplyEnvironmentalEffect(Agent.Agent agent)
{
    if (EffectData.ContainsKey("perception_modifier"))
    {
        var modifier = EffectData["perception_modifier"].AsSingle() * Intensity;
        // 硬编码的逻辑...
    }
}

// 现在
public Variant ApplyToAgent(Agent.Agent agent)
{
    return ExecuteEffectScript(agent);
}
```

### 3. EffectSystem重构

**之前的结构：**
- `IEffectExecutor`接口
- 多个具体的执行器类（`SkillBoostExecutor`, `WorldStateExecutor`等）
- 复杂的注册和执行机制

**现在的结构：**
- 简化的`EffectManager`
- 通过脚本执行所有效果
- 内置默认脚本模板

**示例：**
```csharp
// 之前
public interface IEffectExecutor
{
    bool CanExecute(Dictionary<string, object> parameters);
    void Execute(Dictionary<string, object> parameters);
    string GetDescription(Dictionary<string, object> parameters);
}

// 现在
public class EffectManager
{
    public Variant ExecuteEffect(EffectReference effectRef, object target = null)
    {
        // 执行脚本...
    }
}
```

## 脚本系统特性

### 1. 上下文变量

所有脚本都可以访问以下上下文变量：

**Status脚本：**
- `status`: 状态对象本身
- `target`: 目标Agent
- `value`: 状态当前值
- `maxValue`: 状态最大值
- `elapsedTime`: 状态持续时间
- `isActive`: 状态是否激活

**PlaceEffect脚本：**
- `effect`: 效果对象本身
- `agent`: 目标Agent
- `intensity`: 效果强度
- `type`: 效果类型
- `effectData`: 效果数据
- `affectedAttributes`: 受影响的属性

**Effect脚本：**
- `effectId`: 效果ID
- `effectType`: 效果类型
- `category`: 效果分类
- `targetPath`: 目标路径
- 所有自定义参数

### 2. 可用的函数

脚本中可以使用以下函数：

- `Context.Get(key)`: 获取上下文变量
- `Context.PathGet(target, path)`: 通过路径获取值
- `Context.PathSet(target, path, value)`: 通过路径设置值
- `Context.PathExists(target, path)`: 检查路径是否存在
- `Context.Print(message)`: 打印消息
- `Context.Random.Next(min, max)`: 生成随机数

### 3. 脚本示例

**健康状态效果：**
```gdscript
# 健康状态效果脚本
var status = Context.Get('status')
var target = Context.Get('target')
var value = Context.Get('value')
var elapsedTime = Context.Get('elapsedTime')

# 每分钟恢复1点生命值
if elapsedTime > 0 and elapsedTime % 60 < 1:
    var currentHealth = Context.PathGet(target, 'CurrentHealth') or 0
    var maxHealth = Context.PathGet(target, 'Character.MaxHealth') or 100
    if currentHealth < maxHealth:
        var newHealth = min(currentHealth + 1, maxHealth)
        Context.PathSet(target, 'CurrentHealth', newHealth)
        Context.Print('健康状态恢复1点生命值')

return true
```

**魔法图书馆环境效果：**
```gdscript
# 魔法图书馆环境效果脚本
var effect = Context.Get('effect')
var agent = Context.Get('agent')
var intensity = Context.Get('intensity')

# 魔法理论技能提升50%
var currentMagicTheory = Context.PathGet(agent, 'Skills.magic_theory.Level') or 1
var boostedMagicTheory = int(currentMagicTheory * 1.5)
Context.PathSet(agent, 'Skills.magic_theory.Level', boostedMagicTheory)
Context.Print('魔法图书馆效果：魔法理论技能提升50%')

return true
```

## 配置文件更新

### 1. Status YAML文件

**之前：**
```yaml
effects:
  - type: "attribute_modifier"
    target: "current_health"
    operation: "add"
    value: 1
    scaling: "per_minute"
    interval: 60
    condition: "always"
    duration: -1
```

**现在：**
```yaml
effect_script: |
  # 健康状态效果脚本
  var elapsedTime = Context.Get('elapsedTime')
  
  if elapsedTime > 0 and elapsedTime % 60 < 1:
      var currentHealth = Context.PathGet(target, 'CurrentHealth') or 0
      var newHealth = min(currentHealth + 1, maxHealth)
      Context.PathSet(target, 'CurrentHealth', newHealth)
      Context.Print('健康状态恢复1点生命值')
  
  return true
```

### 2. PlaceEffect YAML文件

**新格式：**
```yaml
id: "magic_library_effect"
name: "魔法图书馆效果"
type: "magical"
intensity: 1.5
effect_script: |
  # 魔法图书馆环境效果脚本
  var agent = Context.Get('agent')
  
  # 魔法理论技能提升50%
  var currentMagicTheory = Context.PathGet(agent, 'Skills.magic_theory.Level') or 1
  var boostedMagicTheory = int(currentMagicTheory * 1.5)
  Context.PathSet(agent, 'Skills.magic_theory.Level', boostedMagicTheory)
  
  return true
```

## 优势

### 1. 灵活性
- 可以编写任意复杂的效果逻辑
- 支持条件判断、循环、函数调用等
- 无需重新编译代码即可修改效果

### 2. 可维护性
- 效果逻辑集中在脚本中
- 易于调试和测试
- 支持版本控制和代码审查

### 3. 性能
- 脚本编译后执行效率高
- 支持缓存和优化
- 减少反射和动态调用的开销

### 4. 扩展性
- 可以轻松添加新的效果类型
- 支持复杂的组合效果
- 可以集成外部脚本库

## 迁移指南

### 1. 从StatusEffect迁移

**步骤1：** 分析现有StatusEffect的逻辑
**步骤2：** 将逻辑转换为GDScript脚本
**步骤3：** 更新Status YAML文件
**步骤4：** 测试新脚本的效果

### 2. 从IEffectExecutor迁移

**步骤1：** 分析现有执行器的逻辑
**步骤2：** 将逻辑转换为GDScript脚本
**步骤3：** 更新EffectReference配置
**步骤4：** 测试新脚本的效果

### 3. 测试和验证

- 确保脚本语法正确
- 验证效果逻辑符合预期
- 测试边界条件和错误情况
- 性能测试和优化

## 最佳实践

### 1. 脚本编写
- 使用清晰的变量命名
- 添加适当的注释
- 处理异常和边界情况
- 保持脚本简洁和可读

### 2. 性能优化
- 避免在脚本中进行复杂计算
- 合理使用缓存
- 减少不必要的路径访问
- 优化循环和条件判断

### 3. 错误处理
- 添加适当的错误检查
- 提供有意义的错误信息
- 实现优雅的降级处理
- 记录详细的执行日志

## 总结

新的script-based Effect/Status系统提供了前所未有的灵活性和可扩展性。通过使用GDScript，开发者可以：

1. 快速实现复杂的效果逻辑
2. 轻松修改和调试效果
3. 支持动态效果配置
4. 实现高度可定制的游戏系统

这个系统为游戏的后续开发和扩展奠定了坚实的基础。
