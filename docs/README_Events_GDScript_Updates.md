# 事件系统GDScript更新说明

## 概述
所有的事件YAML文件已经更新为与新的GDScript语法兼容的格式。主要变化包括将条件系统和效果系统改为脚本实现，提供更大的灵活性和可扩展性。

**重要设计原则**：事件脚本只在触发时执行一次，持续性效果通过Effect系统实现。

## 主要更改

### 1. 条件系统更新

#### 旧的条件格式（已废弃）
```yaml
enable_conditions:
  - condition_id: "cond_magic_available"
    type: "simple"
    target_path: "gameManager.WorldABC.worldState[\"magic_academy_available\"]"
    operator: "equals"
    expected_value: true
```

#### 新的脚本条件格式（当前使用）
```yaml
enable_conditions:
  - condition_id: "cond_magic_available"
    type: "script"
    description: "检查魔法学院是否可用"
    condition_script: |
      # 检查魔法学院是否可用
      var magicAvailable = Global.globalVariables.get("magic_academy_available", false)
      return magicAvailable == true
```

### 2. 效果系统更新

#### 旧的效果格式（已废弃）
```yaml
world_effects:
  - effect_id: "world_test_effect"
    executor_type: "world_state"
    target_path: "world:magic_world"
    parameters:
      state: "magic_world_state"
      value: "revealed"
```

#### 新的脚本效果格式（当前使用）
```yaml
# 事件触发时执行一次的效果脚本
event_script: |
  # 事件触发脚本 - 只执行一次
  print("事件触发！")
  
  # 设置世界状态
  Global.globalVariables["adventure_started"] = true
  
  # 添加持续性效果到EventManager
  var eventManager = GameManager.EventManager
  if eventManager != null:
      var worldEffect = {
          "effect_id": "world_test_effect",
          "effect_type": "world_state",
          "duration": 5,
          "target": "world_state",
          "value": "adventure_started"
      }
      eventManager.AddWorldEffect(worldEffect)
  
  return true

# 世界影响 - 通过Effect系统实现
world_effects: []  # 空列表，效果通过Effect系统管理

# 角色影响 - 通过Effect系统实现
character_effects: []  # 空列表，效果通过Effect系统管理
```

## 设计逻辑说明

### 1. 事件脚本 (event_script)
- **执行时机**：只在事件触发时执行一次
- **主要作用**：
  - 初始化世界状态
  - 添加持续性效果到EventManager
  - 执行一次性的逻辑操作
- **不负责**：持续性效果的实际执行

### 2. Effect系统
- **管理方式**：通过EventManager.AddWorldEffect()和AddCharacterEffect()添加
- **执行方式**：EventManager定期执行这些Effect
- **生命周期**：Effect有自己的duration，到期后自动移除
- **负责内容**：持续性效果的实际执行

### 3. 优势
- **清晰分离**：事件逻辑和效果逻辑分离
- **可维护性**：Effect可以独立管理、调试、修改
- **性能优化**：避免在事件脚本中执行复杂的持续性逻辑
- **灵活性**：Effect可以动态添加、移除、修改

## 支持的条件类型

### 1. 简单条件 (simple)
- 保留用于向后兼容
- 建议迁移到脚本条件

### 2. 脚本条件 (script)
- 使用GDScript语法
- 可以访问游戏管理器、全局变量等
- 支持复杂的逻辑判断

### 3. 组合条件
- `and` - 所有子条件都满足
- `or` - 任一子条件满足
- `not` - 子条件不满足

## 脚本条件可用的上下文

### 1. 游戏管理器 (GameManager)
```gdscript
# 访问角色管理器
var charManager = GameManager.CharacterManager
var character = charManager.GetCharacterById("char_0001")

# 访问任务管理器
var taskManager = GameManager.TaskManager
var task = taskManager.GetTask("task_id")

# 访问世界状态
var worldState = GameManager.WorldABC.worldState
```

### 2. 全局变量 (Global)
```gdscript
# 获取全局变量
var playerLevel = Global.globalVariables.get("player_level", 0)
var currentWeather = Global.globalVariables.get("current_weather", "clear")

# 设置全局变量
Global.globalVariables["adventure_started"] = true
Global.globalVariables["current_turn"] = 10
```

### 3. 条件对象 (condition)
```gdscript
# 访问条件信息
var conditionId = condition.ConditionId
var description = condition.Description
```

## 事件脚本可用的上下文

### 1. 游戏管理器 (GameManager)
```gdscript
# 访问各种管理器
var charManager = GameManager.CharacterManager
var taskManager = GameManager.TaskManager
var worldManager = GameManager.WorldManager
```

### 2. 全局变量 (Global)
```gdscript
# 修改世界状态
Global.globalVariables["magic_world_state"] = "revealed"
Global.globalVariables["storm_intensity"] = 0.8
```

### 3. EventManager
```gdscript
# 添加持续性效果
var eventManager = GameManager.EventManager
if eventManager != null:
    var worldEffect = {
        "effect_id": "effect_id",
        "effect_type": "effect_type",
        "duration": 10,
        "target": "target",
        "value": "value"
    }
    eventManager.AddWorldEffect(worldEffect)
```

## 更新后的文件列表

### 1. test_events.yaml
- 测试事件配置
- 包含分支选择、条件检查、效果执行
- 展示完整的脚本条件系统

### 2. script_conditions_example.yaml
- 脚本条件使用示例
- 包含简单条件、复杂条件、条件链
- 展示最佳实践

### 3. main_story.yaml
- 主线故事配置
- 包含善良/邪恶路径选择
- 展示分支条件的使用

### 4. side_story_01.yaml
- 支线剧情配置
- 包含商人帮助/忽略选择
- 展示角色状态检查

### 5. world_events.yaml
- 世界事件配置
- 包含天气系统、危险等级
- 展示环境效果和角色影响

### 6. char_0001_events.yaml
- 角色个人事件配置
- 包含日常魔法练习
- 展示循环任务和技能提升

## 使用说明

### 1. 编写条件脚本
```gdscript
# 基本结构
condition_script: |
  # 检查条件
  var result = check_something()
  
  # 返回布尔值
  return result

# 示例：检查角色状态
condition_script: |
  var charManager = GameManager.CharacterManager
  var character = charManager.GetCharacterById("char_0001")
  if character != null:
      var energy = character.CurrentEnergy
      var health = character.CurrentHealth
      return energy >= 30 and health >= 50
  return false
```

### 2. 编写事件脚本
```gdscript
# 基本结构
event_script: |
  # 事件触发逻辑
  print("事件触发！")
  
  # 设置世界状态
  Global.globalVariables["key"] = "value"
  
  # 添加持续性效果
  var eventManager = GameManager.EventManager
  if eventManager != null:
      var effect = {
          "effect_id": "effect_id",
          "effect_type": "effect_type",
          "duration": 10,
          "target": "target"
      }
      eventManager.AddWorldEffect(effect)
  
  return true
```

## 注意事项

1. **脚本语法**：所有脚本必须使用GDScript语法，不是C#
2. **类名大写**：使用`GameManager`、`Global`等大写形式
3. **事件脚本执行**：只在事件触发时执行一次
4. **持续性效果**：通过Effect系统实现，不在事件脚本中直接处理
5. **返回值**：条件脚本应该返回`true`或`false`，事件脚本应该返回`true`表示成功
6. **错误处理**：脚本中的错误会被捕获并记录到日志中
7. **性能**：避免在事件脚本中执行复杂的计算或循环
8. **调试**：使用`print()`语句来调试脚本执行过程

## 迁移指南

### 1. 从简单条件迁移
```yaml
# 旧格式
- type: "simple"
  target_path: "gameManager.WorldABC.worldState[\"key\"]"
  operator: "equals"
  expected_value: true

# 新格式
- type: "script"
  condition_id: "new_condition"
  description: "检查条件"
  condition_script: |
    var keyValue = Global.globalVariables.get("key", false)
    return keyValue == true
```

### 2. 从执行器效果迁移
```yaml
# 旧格式
- executor_type: "world_state"
  target_path: "world:target"
  parameters:
    key: "value"

# 新格式
event_script: |
  # 设置世界状态
  Global.globalVariables["key"] = "value"
  
  # 添加持续性效果
  var eventManager = GameManager.EventManager
  if eventManager != null:
      var effect = {
          "effect_id": "effect_id",
          "effect_type": "world_state",
          "duration": 10,
          "target": "target",
          "key": "value"
      }
      eventManager.AddWorldEffect(effect)
  
  return true

world_effects: []
character_effects: []
```

## 测试
使用`EventManager`和相关测试场景来验证新的脚本条件系统。测试应该验证：
- 脚本条件的正确执行
- 条件组合的正确评估
- 事件脚本的正确执行（只执行一次）
- Effect系统的正确添加和管理
- 持续性效果的正确执行
- 错误处理和日志记录
