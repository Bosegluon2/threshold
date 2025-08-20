# 技能系统重构说明

## 概述

技能系统已经从硬编码的效果类型重构为灵活的脚本系统。现在每个技能都可以通过 YAML 配置文件中的 `effect_script` 字段来定义其效果，这提供了最大的可扩展性。

## 主要变化

### 1. 删除的组件
- `SkillEffect` 类 - 不再需要
- 硬编码的效果类型系统
- 固定的效果参数

### 2. 新增的组件
- `EffectScript` 字段 - 存储技能效果脚本
- 脚本执行上下文 - 包含技能、施法者、目标等信息
- 动态脚本执行系统

## 技能配置格式

### 基础字段
```yaml
id: "skill_id"
name: "技能名称"
description: "技能描述"
category: "combat"  # combat, utility, knowledge, passive
type: "active"      # active, passive, toggle
element: "fire"     # physical, fire, ice, lightning, arcane, etc.
level: 1
max_level: 10
energy_cost: 25
cooldown: 5.0
range: 20.0
area_of_effect: 0.0
icon: "res://assets/icons/skills/skill.png"
animation: "cast_animation"
sound_effect: "res://assets/audio/sfx/skill.wav"
```

### 效果脚本字段
```yaml
effect_script: |
  // 技能效果脚本
  var damage = skill.level * 15 + 20;
  // ... 更多逻辑
```

## 脚本执行上下文

每个技能脚本在执行时都可以访问以下变量：

### 内置变量
- `skill` - 技能对象本身
- `caster` - 施法者对象
- `target` - 目标对象
- `level` - 技能等级
- `energyCost` - 能量消耗

### 路径操作函数
- `PathGet(object, string)` - 获取对象属性值
- `PathSet(object, string, object)` - 设置对象属性值
- `PathExists(object, string)` - 检查属性是否存在
- `PathExplore(object)` - 探索对象的所有可访问路径

## 技能示例

### 1. 火球术（主动攻击技能）
```yaml
effect_script: |
  var damage = skill.level * 15 + 20;
  var fireDamage = damage * 1.5;
  
  if (target != null) {
    var targetHealth = PathGet(target, "currentHealth");
    var newHealth = targetHealth - fireDamage;
    PathSet(target, "currentHealth", newHealth);
    
    GD.Print($"火球术造成 {fireDamage} 点伤害");
  }
  
  fireDamage
```

### 2. 治疗术（主动治疗技能）
```yaml
effect_script: |
  var healAmount = skill.level * 12 + 25;
  
  if (target != null) {
    var currentHealth = PathGet(target, "currentHealth");
    var maxHealth = PathGet(target, "maxHealth");
    var newHealth = Math.Min(currentHealth + healAmount, maxHealth);
    PathSet(target, "currentHealth", newHealth);
    
    newHealth - currentHealth
  } else {
    0
  }
```

### 3. 战斗精通（被动技能）
```yaml
effect_script: |
  if (caster != null) {
    var level = skill.level;
    var attackBonus = level * 3;
    var defenseBonus = level * 2;
    
    var currentAttack = PathGet(caster, "attackPower") ?? 0;
    var currentDefense = PathGet(caster, "defensePower") ?? 0;
    
    PathSet(caster, "attackPower", currentAttack + attackBonus);
    PathSet(caster, "defensePower", currentDefense + defenseBonus);
    
    return 0; // 返回成功
  } else {
    return 1; // 返回失败
  }
```

### 4. 多重射击（展示for循环）
```yaml
effect_script: |
  // 检查条件...
  if (target == null) return 1;
  
  var arrowCount = skill.level + 2;
  var totalDamage = 0;
  
  // 使用for循环发射多支箭矢
  for (var i = 0; i < arrowCount; i++) {
    var arrowDamage = baseDamage + Random.Next(-3, 4);
    // 应用伤害...
    totalDamage += arrowDamage;
  }
  
  return 0; // 返回成功
```

### 5. 元素精通（展示switch语句和数组）
```yaml
effect_script: |
  // 检查条件...
  var environmentElement = PathGet(caster, "currentEnvironment.element");
  
  // 使用switch语句根据元素类型计算加成
  switch (environmentElement) {
    case "fire":
      elementBonus = skill.level * 0.3;
      break;
    case "ice":
      elementBonus = skill.level * 0.25;
      break;
    default:
      elementBonus = skill.level * 0.1;
      break;
  }
  
  // 创建数组并遍历
  var elementBonuses = new object[5];
  for (var i = 0; i < elementBonuses.Length; i++) {
    // 应用元素加成...
  }
  
  return 0; // 返回成功
```

## 脚本语法特性

### 支持的功能
- **完整的 C# 语法支持**：
  - 变量声明和赋值：`var damage = skill.level * 15 + 20;`
  - 条件语句：`if-else`、`switch-case`
  - 循环语句：`for`、`while`、`foreach`
  - 数学运算：`+`、`-`、`*`、`/`、`%`、`Math.Max()`、`Math.Min()`
  - 函数调用：`GD.Print()`、`PathGet()`、`PathSet()`
  - 路径操作：动态访问对象属性
  - 错误处理：`try-catch`、异常处理
  - 数组和集合操作：创建数组、遍历元素
  - 字符串操作：字符串插值、格式化
  - 随机数生成：`Random.Next()`、`Random.NextDouble()`

### 注意事项
- 脚本必须返回一个值（通常是效果的结果）
- 使用 `PathGet` 和 `PathSet` 来访问对象属性
- 脚本中的错误会被捕获并记录到日志
- 支持多行脚本，使用 YAML 的 `|` 语法

## 使用方式

### 1. 创建技能
```csharp
var skill = new Skill("fireball", "火球术", "发射火球", 1, 25);
skill.EffectScript = "var damage = skill.level * 15 + 20; damage";
```

### 2. 使用技能
```csharp
// 使用技能，传递施法者和目标
var result = skill.Use(Time.GetTimeDictFromSystem(), caster, target, currentEnergy);

// 检查结果
if (result.ToString() == "0") {
    GD.Print("技能使用成功");
} else if (result.ToString() == "1") {
    GD.Print("技能使用失败");
} else {
    GD.Print($"技能执行完成，返回值: {result}");
}
```

### 3. 手动触发效果
```csharp
// 直接触发技能效果
skill.TriggerEffects(caster, target);
```

## 优势

1. **最大灵活性** - 可以定义任何类型的技能效果
2. **易于扩展** - 添加新效果类型不需要修改代码
3. **数据驱动** - 技能效果完全通过配置文件定义
4. **运行时修改** - 可以在游戏运行时修改技能效果
5. **复杂逻辑** - 支持复杂的条件判断和计算

## 注意事项

1. **性能考虑** - 脚本执行比硬编码稍慢，但对于技能系统来说可以接受
2. **错误处理** - 脚本错误不会导致游戏崩溃，但会记录到日志
3. **调试** - 使用 `GD.Print` 来调试脚本执行过程
4. **类型安全** - 脚本是动态执行的，需要注意类型转换

## 未来扩展

1. **技能组合** - 多个技能可以组合产生特殊效果
2. **条件效果** - 基于环境、状态等条件的动态效果
3. **连锁效果** - 技能效果可以触发其他技能或效果
4. **AI 技能** - AI 可以使用相同的脚本系统来定义技能
