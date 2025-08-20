# Place脚本系统说明

## 概述
Place类已经重构为使用脚本驱动的效果系统，替代了之前的PlaceEffect系统。这种设计更加简洁和灵活，避免了过多的Effect实现。

## 设计原则
- **简化系统**：只保留必要的Effect实现（WorldEffect + Character Status）
- **脚本驱动**：使用GDScript脚本处理Agent进入/离开逻辑
- **分布式管理**：每个系统管理自己的Effect，避免统一管理

## 主要变化

### 1. 删除的内容
- `PlaceEffect`类完全删除
- `Array<PlaceEffect> Effects`属性删除
- Library中的PlaceEffect管理删除

### 2. 新增的内容
- `OnAgentEnterScript`：Agent进入时执行的脚本
- `OnAgentLeaveScript`：Agent离开时执行的脚本

## 脚本系统

### 1. 进入时脚本 (OnAgentEnterScript)
```yaml
on_agent_enter_script: |
  # Agent进入地点时执行的脚本
  print("Agent进入地点")
  
  # 检查Agent状态
  if agent.Skills.ContainsKey("magic_theory"):
      var level = agent.Skills["magic_theory"].Level
      print("魔法理论技能等级: ", level)
  
  # 设置Agent状态
  agent.Status["in_location"] = true
  
  # 通过EventManager添加持续性效果
  var eventManager = GameManager.EventManager
  if eventManager != null:
      var effect = {
          "effect_id": "location_effect",
          "effect_type": "status_boost",
          "duration": 5,
          "target": agent.Id
      }
      eventManager.AddCharacterEffect(effect)
  
  return true
```

### 2. 离开时脚本 (OnAgentLeaveScript)
```yaml
on_agent_leave_script: |
  # Agent离开地点时执行的脚本
  print("Agent离开地点")
  
  # 清理状态
  if agent.Status.ContainsKey("in_location"):
      agent.Status.Remove("in_location")
  
  # 记录访问历史
  var visits = Global.globalVariables.get("location_visits", {})
  visits[agent.Id] = (visits.get(agent.Id, 0) + 1)
  Global.globalVariables["location_visits"] = visits
  
  return true
```

## 脚本上下文

### 1. 可用对象
- `agent`：进入/离开的Agent对象
- `place`：当前Place对象
- `gameManager`：游戏管理器
- `global`：全局变量管理器

### 2. 常用操作
```gdscript
# 访问Agent属性
var agentName = agent.Name
var agentSkills = agent.Skills
var agentStatus = agent.Status

# 访问Place属性
var placeName = place.Name
var placeType = place.Type
var placeCapacity = place.Capacity

# 访问游戏管理器
var charManager = GameManager.CharacterManager
var eventManager = GameManager.EventManager

# 访问全局变量
var currentTurn = Global.globalVariables.get("current_turn", 0)
Global.globalVariables["key"] = "value"
```

## 优势

### 1. 系统简化
- 减少了Effect类的数量
- 统一了脚本执行方式
- 简化了数据加载逻辑

### 2. 灵活性提升
- 脚本可以执行任意逻辑
- 可以动态添加/移除效果
- 支持复杂的条件判断

### 3. 维护性改善
- 减少了代码重复
- 统一了错误处理
- 简化了调试过程

## 使用示例

### 1. 魔法图书馆
```yaml
on_agent_enter_script: |
  # 检查魔法技能等级
  if agent.Skills.ContainsKey("magic_theory"):
      var level = agent.Skills["magic_theory"].Level
      if level >= 10:
          # 添加知识增益效果
          var eventManager = GameManager.EventManager
          if eventManager != null:
              var effect = {
                  "effect_id": "library_knowledge_boost",
                  "effect_type": "knowledge_boost",
                  "duration": 5,
                  "target": agent.Id,
                  "skill": "magic_theory",
                  "value": 2
              }
              eventManager.AddCharacterEffect(effect)
  
  # 设置状态
  agent.Status["in_magic_library"] = true
  return true
```

### 2. 训练场
```yaml
on_agent_enter_script: |
  # 添加训练效果
  var eventManager = GameManager.EventManager
  if eventManager != null:
      var effect = {
          "effect_id": "training_boost",
          "effect_type": "skill_training",
          "duration": 3,
          "target": agent.Id,
          "intensity": 1.5
      }
      eventManager.AddCharacterEffect(effect)
  
  return true

on_agent_leave_script: |
  # 记录训练时间
  var trainingTime = Global.globalVariables.get("training_time", {})
  trainingTime[agent.Id] = (trainingTime.get(agent.Id, 0) + 1)
  Global.globalVariables["training_time"] = trainingTime
  
  return true
```

## 注意事项

1. **脚本语法**：使用GDScript语法，不是C#
2. **返回值**：脚本应该返回`true`表示成功
3. **错误处理**：脚本中的错误会被捕获并记录
4. **性能**：避免在脚本中执行复杂的计算
5. **调试**：使用`print()`语句调试脚本执行

## 迁移指南

### 1. 从PlaceEffect迁移
```yaml
# 旧格式
effects:
  - id: "magical_atmosphere"
    name: "魔法氛围"
    type: "magical"
    intensity: 1.2

# 新格式
on_agent_enter_script: |
  # 通过脚本实现魔法氛围效果
  var eventManager = GameManager.EventManager
  if eventManager != null:
      var effect = {
          "effect_id": "magical_atmosphere",
          "effect_type": "atmosphere",
          "duration": -1,
          "intensity": 1.2
      }
      eventManager.AddWorldEffect(effect)
  
  return true
```

### 2. 更新YAML文件
- 删除`effects`部分
- 添加`on_agent_enter_script`
- 添加`on_agent_leave_script`（如果需要）

现在Place系统更加简洁和灵活，通过脚本驱动实现各种效果！
