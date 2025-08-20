# 状态YAML文件更新说明

## 概述
所有的状态YAML文件已经更新为与新的GDScript语法兼容的格式。主要变化包括：

## 主要更改

### 1. 移除的字段
- `default_value` - 不再需要，状态值现在通过脚本动态管理
- `max_value` - 不再需要，最大值现在通过脚本动态管理
- `change_rate` - 不再需要，变化率现在通过脚本动态管理
- `change_modifier` - 不再需要，变化修饰符现在通过脚本动态管理

### 2. 保留的字段
- `id` - 状态唯一标识符
- `name` - 状态名称
- `description` - 状态描述
- `icon` - 状态图标路径
- `category` - 状态类别
- `priority` - 状态优先级
- `duration` - 状态持续时间（-1表示永久）
- `stackable` - 是否可叠加
- `removable` - 是否可移除
- `effect_script` - 状态效果脚本（GDScript格式）

### 3. 脚本语法更改

#### 旧语法（已废弃）
```gdscript
var status = Context.Get('status')
var target = Context.Get('target')
var value = Context.Get('value')
var elapsedTime = Context.Get('elapsedTime')

# 使用PathGet和PathSet
var currentHealth = Context.PathGet(target, 'CurrentHealth') or 0
Context.PathSet(target, 'CurrentHealth', newHealth)
Context.Print('消息')
```

#### 新语法（当前使用）
```gdscript
var status = ScriptExecutor.Context['status']
var target = ScriptExecutor.Context['target']

# 直接访问对象属性
var currentHealth = target.CurrentHealth
target.SetHealth(newHealth)
print('消息')
```

## 更新后的状态文件

### 1. healthy.yaml - 健康状态
- 每回合恢复1点生命值
- 每回合恢复2点能量值
- 80%几率免疫疾病状态

### 2. injured.yaml - 受伤状态
- 每回合减少1点生命值
- 战斗技能降低20%
- 适应能力降低15%
- 移动速度降低20%

### 3. confident.yaml - 自信状态
- 社交能力提升15%
- 战斗能力提升10%
- 士气提升20%

### 4. focus.yaml - 专注状态
- 技能效果提升15%
- 学习效率提升20%
- 每回合消耗1点能量值

### 5. exhausted.yaml - 疲惫状态
- 每回合损失2点能量值
- 技能效果降低10%
- 休息时恢复速度提升2倍

### 6. normal.yaml - 正常状态
- 无特殊效果，保持基础状态

## 使用说明

### 1. 状态脚本执行
状态脚本在以下情况下执行：
- 状态被添加时
- 每回合更新时（通过`Agent.Step()`方法）

### 2. 脚本上下文
脚本可以访问以下上下文变量：
- `status` - 当前状态对象
- `target` - 目标Agent对象
- `current_duration` - 当前持续时间
- `isActive` - 状态是否激活

### 3. 常用方法
- `target.SetHealth(value)` - 设置生命值
- `target.SetEnergy(value)` - 设置能量值
- `target.CurrentHealth` - 获取当前生命值
- `target.CurrentEnergy` - 获取当前能量值
- `target.Character.BaseWarpedInfo.Attribute` - 获取/设置基础属性
- `target.CurrentWarpedInfo.Attribute` - 获取/设置当前属性

## 注意事项

1. **脚本语法**：所有脚本必须使用GDScript语法，不是C#
2. **返回值**：脚本应该返回`true`表示成功，`false`表示失败
3. **错误处理**：脚本中的错误会被捕获并记录到日志中
4. **性能**：避免在脚本中执行复杂的计算或循环
5. **状态管理**：脚本负责管理自己的状态效果，包括添加、修改和移除

## 测试
使用`TestStatusScript.tscn`场景来测试所有状态脚本的功能。测试脚本会验证：
- 状态脚本的正确执行
- 属性修改的正确应用
- 状态效果的预期行为
