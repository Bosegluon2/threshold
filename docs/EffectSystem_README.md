# 效果系统使用指南

## 概述

这个效果系统使用 `PathResolver` 类来避免重复定义各种 key 值，提供了一个灵活、可扩展的效果执行框架。

## 核心组件

### 1. EffectReference (效果引用)
- `EffectId`: 效果唯一标识符
- `ExecutorType`: 效果执行器类型
- `Category`: 效果分类（世界、角色、技能、物品、任务、系统）
- `Parameters`: 效果参数
- `TargetPath`: 目标路径（使用路径解析器）

### 2. EffectManager (效果管理器)
- 管理效果执行器
- 执行效果
- 单例模式，全局访问

### 3. EffectLoader (效果加载器)
- 从 YAML 配置文件加载效果模板
- 支持效果组合和路径映射
- 自动解析和验证配置

## 配置文件结构

### 效果模板 (effect_templates.yaml)
```yaml
effect_templates:
  skill_boost:
    executor_type: "skill_boost"
    category: "skill"
    description_template: "提升 {skill} 技能 {value} 点"
    default_parameters:
      skill: "magic_theory"
      value: 10
      target_path: "character:char_0001"
```

### 效果组合
```yaml
effect_combinations:
  magic_academy_reveal:
    name: "魔法学院秘密揭示"
    description: "揭示魔法学院的秘密，提升相关技能和关系"
    effects:
      - template: "world_state"
        parameters:
          state: "magic_world_state"
          value: "revealed"
      - template: "skill_boost"
        parameters:
          skill: "magic_theory"
          value: 50
```

### 路径映射
```yaml
path_mappings:
  characters:
    char_0001: "character:char_0001"
    char_0002: "character:char_0002"
  world_states:
    magic_world: "world:magic_world"
  system_vars:
    global: "system:global"
```

## 使用方法

### 1. 初始化系统
```csharp
// 加载效果配置
EffectLoader.Instance.LoadEffectConfigs();
```

### 2. 执行单个效果
```csharp
var skillEffect = new EffectReference
{
    EffectId = "skill_boost_example",
    ExecutorType = "skill_boost",
    Parameters = new Dictionary<string, object>
    {
        ["skill"] = "magic_theory",
        ["value"] = 25
    }
};

var character = new Dictionary<string, object>
{
    ["magic_theory"] = 50
};

bool success = EffectManager.Instance.ExecuteEffect(skillEffect, character);
```

### 3. 使用效果模板
```csharp
var template = EffectLoader.Instance.GetEffectTemplate("skill_boost");
if (template != null)
{
    var effect = new EffectReference
    {
        EffectId = "custom_skill_boost",
        ExecutorType = template.ExecutorType,
        Category = template.Category,
        Parameters = new Dictionary<string, object>(template.Parameters)
    };
    
    // 覆盖默认参数
    effect.Parameters["skill"] = "combat";
    effect.Parameters["value"] = 15;
    
    EffectManager.Instance.ExecuteEffect(effect, character);
}
```

### 4. 执行效果组合
```csharp
var combination = EffectLoader.Instance.GetEffectCombination("magic_academy_reveal");
if (combination != null)
{
    foreach (var effect in combination)
    {
        object target = GetTargetForEffect(effect);
        EffectManager.Instance.ExecuteEffect(effect, target);
    }
}
```

## 内置效果执行器

### 1. SkillBoostExecutor (技能提升)
- 参数: `skill`, `value`
- 功能: 提升指定技能值

### 2. WorldStateExecutor (世界状态)
- 参数: `state`, `value`
- 功能: 设置世界状态

### 3. RelationshipExecutor (关系提升)
- 参数: `value`
- 功能: 提升关系值

### 4. SystemVariableExecutor (系统变量)
- 参数: `variable`, `value`
- 功能: 设置系统变量

## 扩展自定义执行器

```csharp
public class CustomEffectExecutor : IEffectExecutor
{
    public bool CanExecute(Dictionary<string, object> parameters)
    {
        return parameters.ContainsKey("required_param");
    }
    
    public void Execute(Dictionary<string, object> parameters)
    {
        // 实现自定义效果逻辑
        var value = parameters["required_param"];
        // ... 执行效果
    }
    
    public string GetDescription(Dictionary<string, object> parameters)
    {
        return "自定义效果描述";
    }
}

// 注册执行器
EffectManager.Instance.RegisterExecutor("custom_effect", new CustomEffectExecutor());
```

## 在事件系统中的使用

### 更新 main_story.yaml
```yaml
# 世界影响 - 使用路径解析器
world_effects:
  - effect_id: "world_magic_revealed"
    executor_type: "world_state"
    target_path: "world:magic_world"
    parameters:
      state: "magic_world_state"
      value: "revealed"

# 角色影响 - 使用路径解析器
character_effects:
  - effect_id: "char_knowledge_boost"
    executor_type: "skill_boost"
    target_path: "character:char_0001"
    parameters:
      skill: "magic_theory"
      value: 10
```

## 优势

1. **避免重复定义**: 使用模板和路径映射，减少重复配置
2. **类型安全**: 强类型的效果分类和执行器
3. **易于扩展**: 可以轻松添加新的效果类型和执行器
4. **配置驱动**: 通过 YAML 文件配置，无需修改代码
5. **路径解析**: 使用 PathResolver 统一处理目标对象访问
6. **效果组合**: 支持复杂的效果组合，提高复用性

## 注意事项

1. 确保目标对象支持 PathResolver 的路径访问
2. 效果执行器需要正确实现 IEffectExecutor 接口
3. YAML 配置文件需要遵循正确的格式
4. 路径映射需要与实际的对象结构匹配

## 示例代码

完整的示例代码请参考 `scripts/core/effects/EffectUsageExample.cs`，其中包含了各种使用场景的详细示例。
