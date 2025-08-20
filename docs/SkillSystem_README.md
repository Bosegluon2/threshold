# 技能系统重构说明

## 概述

技能系统已经重构为使用 `effect_script` 字段，通过 GDScript 脚本执行器来执行技能效果。这种设计提供了最大的灵活性和可扩展性。

## 主要变化

### 1. 移除 SkillEffect 类
- 删除了 `SkillEffect` 类及其相关实现
- 技能效果现在完全通过 `effect_script` 字段定义

### 2. 统一技能逻辑
- 将 `CanUse` 和 `Use` 方法合并为单个 `Use` 方法
- 所有检查逻辑（冷却、能量、目标等）都在脚本中处理
- 脚本返回值表示执行结果：`0` 表示成功，`1` 表示失败

### 3. 脚本执行上下文
技能脚本执行时会自动获得以下上下文变量：

```gdscript
# 技能相关
skill          # 技能对象本身
caster         # 施法者对象
target         # 目标对象
level          # 技能等级

# 系统状态
energyCost     # 能量消耗
currentTime    # 当前时间
currentEnergy  # 当前能量
lastUsedTime   # 上次使用时间
cooldown       # 冷却时间
isOnCooldown   # 是否在冷却中
```

## 脚本执行器集成

新的技能系统使用 `ScriptExecutor` 来执行 `effect_script`。这个执行器使用 GDScript 作为脚本引擎，支持：

- **GDScript 语法**：包括 `if-elif-else`、`for` 循环、`return`、数组操作等
- **动态路径访问**：通过 `Context.PathGet`、`Context.PathSet`、`Context.PathExists` 等函数访问和修改游戏数据
- **上下文变量**：通过 `Context` 对象提供技能执行所需的所有上下文信息
- **错误处理**：脚本执行失败时返回默认值，不会导致游戏崩溃

## 技能示例

### 1. 火球术 (fireball.yaml)
```gdscript
# 检查冷却时间
if skill.isOnCooldown and (currentTime - lastUsedTime) < cooldown:
  Context.Print("技能还在冷却中")
  return 1  # 使用失败

# 检查能量消耗
if currentEnergy < skill.energyCost:
  Context.Print("能量不足")
  return 1  # 使用失败

# 检查目标
if target == null:
  Context.Print("没有指定目标")
  return 1  # 使用失败

# 执行技能效果
var damage = skill.level * 15 + 20
var fireDamage = damage * 1.5

# 对目标造成伤害
var targetHealth = Context.PathGet(target, "currentHealth")
var newHealth = targetHealth - fireDamage
Context.PathSet(target, "currentHealth", newHealth)

Context.Print("火球术对目标造成 " + str(fireDamage) + " 点火焰伤害")
return 0  # 使用成功
```

### 2. 治疗术 (heal.yaml)
```gdscript
# 检查目标生命值
var currentHealth = Context.PathGet(target, "currentHealth")
var maxHealth = Context.PathGet(target, "maxHealth")

if currentHealth >= maxHealth:
  return 1  # 目标生命值已满

# 执行治疗效果
var healAmount = skill.level * 12 + 25
var newHealth = min(currentHealth + healAmount, maxHealth)
Context.PathSet(target, "currentHealth", newHealth)

var actualHeal = newHealth - currentHealth
Context.Print("治疗术恢复了 " + str(actualHeal) + " 点生命值")
return 0  # 使用成功
```

### 3. 战斗精通 (combat_mastery.yaml)
```gdscript
# 提升攻击力
var currentAttack = Context.PathGet(caster, "attackPower")
if currentAttack == null:
  currentAttack = 0
var attackBonus = level * 3
Context.PathSet(caster, "attackPower", currentAttack + attackBonus)

# 提升防御力
var currentDefense = Context.PathGet(caster, "defensePower")
if currentDefense == null:
  currentDefense = 0
var defenseBonus = level * 2
Context.PathSet(caster, "defensePower", currentDefense + defenseBonus)

Context.Print("战斗精通 " + str(level) + " 级: 攻击+" + str(attackBonus) + ", 防御+" + str(defenseBonus))
return 0  # 使用成功
```

### 4. 多重射击 (multi_shot.yaml)
```gdscript
# 计算箭矢数量和伤害
var arrowCount = skill.level + 2
var baseDamage = skill.level * 8 + 15
var totalDamage = 0

# 使用for循环发射多支箭矢
for i in range(arrowCount):
  var arrowDamage = baseDamage + Context.Random.Next(-3, 4)
  
  var targetHealth = Context.PathGet(target, "currentHealth")
  if targetHealth != null:
    var newHealth = targetHealth - arrowDamage
    Context.PathSet(target, "currentHealth", newHealth)
    totalDamage += arrowDamage
    
    Context.Print("第" + str(i + 1) + "支箭矢造成 " + str(arrowDamage) + " 点伤害")

Context.Print("多重射击总共造成 " + str(totalDamage) + " 点伤害")
return 0  # 使用成功
```

### 5. 元素精通 (elemental_mastery.yaml)
```gdscript
# 根据环境元素类型计算加成
if environmentElement == "fire":
  elementBonus = skill.level * 0.3
  Context.Print("火元素环境，火系技能威力增强")
elif environmentElement == "ice":
  elementBonus = skill.level * 0.25
  Context.Print("冰元素环境，冰系技能威力增强")
# ... 其他元素类型

# 创建元素加成数组
var elementBonuses = ["fire", "ice", "lightning", "earth", "arcane"]

# 使用for循环应用所有元素加成
for element in elementBonuses:
  var currentBonus = Context.PathGet(caster, "elementalBonus." + element)
  if currentBonus == null:
    currentBonus = 0.0
  var newBonus = currentBonus + elementBonus
  
  Context.PathSet(caster, "elementalBonus." + element, newBonus)
  totalBonus += newBonus

Context.Print("总元素加成: " + str(totalBonus))
return 0  # 使用成功
```

## 路径操作函数

技能脚本中可以使用以下路径操作函数：

### 1. Context.PathGet(target, path)
获取目标对象的属性值
```gdscript
var health = Context.PathGet(target, "currentHealth")
var name = Context.PathGet(caster, "characterName")
```

### 2. Context.PathSet(target, path, value)
设置目标对象的属性值
```gdscript
Context.PathSet(target, "currentHealth", newHealth)
Context.PathSet(caster, "attackPower", attackPower + bonus)
```

### 3. Context.PathExists(target, path)
检查目标对象的属性是否存在
```gdscript
if Context.PathExists(target, "currentHealth"):
  # 属性存在，可以安全访问
```

### 4. Context.PathExplore(obj)
探索对象的所有可访问路径（调试用）
```gdscript
Context.PathExplore(target)  # 打印目标对象的所有路径
```

## 工具函数

### 1. Context.Print(message)
打印调试信息
```gdscript
Context.Print("技能执行成功")
Context.Print("造成伤害: " + str(damage))
```

### 2. Context.PrintErr(message)
打印错误信息
```gdscript
Context.PrintErr("目标无效")
Context.PrintErr("能量不足")
```

### 3. Context.Random
随机数生成器
```gdscript
var randomDamage = Context.Random.Next(1, 100)
var randomChance = Context.Random.NextDouble()
```

## 最佳实践

### 1. 错误处理
- 始终检查目标对象是否有效
- 验证属性值是否存在
- 使用适当的返回值表示执行结果

### 2. 性能优化
- 避免在循环中重复调用路径操作函数
- 缓存频繁访问的值
- 使用适当的循环结构

### 3. 调试支持
- 使用 `Context.Print` 输出关键信息
- 在复杂逻辑中添加注释
- 测试边界情况

### 4. 代码可读性
- 使用清晰的变量命名
- 添加适当的注释
- 保持代码结构清晰

## 迁移指南

### 从旧系统迁移
1. **移除 SkillEffect 引用**：删除所有对 `Effects` 属性的引用
2. **更新技能调用**：将 `CanUse` + `Use` 改为单个 `Use` 调用
3. **转换脚本语法**：将 C# 语法转换为 GDScript 语法
4. **测试验证**：确保所有技能功能正常

### 语法转换要点
- `//` 注释改为 `#`
- `&&` 改为 `and`
- `||` 改为 `or`
- `{ }` 改为 `:` 和缩进
- `;` 语句结束符可以省略
- `GD.Print` 改为 `Context.Print`
- `PathGet` 改为 `Context.PathGet`

## 未来扩展

### 1. 更多脚本特性
- 支持更复杂的 GDScript 语法
- 添加更多内置函数
- 支持脚本模块化

### 2. 性能优化
- 脚本编译缓存优化
- 减少上下文变量传递开销
- 支持异步脚本执行

### 3. 开发工具
- 脚本编辑器集成
- 实时调试支持
- 性能分析工具

## 总结

新的技能系统通过 GDScript 脚本提供了前所未有的灵活性，允许设计师和开发者创建复杂的技能效果，而无需修改核心代码。这种数据驱动的方法使得技能系统更加模块化、可维护和可扩展。
