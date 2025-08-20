# GameManager 游戏管理系统

## 系统概述

这是一个完整的游戏管理系统，采用单例模式设计，总管一局游戏中的所有内容。系统设计为10天游戏时长，每天分为4个时段（早上、中午、晚上、半夜），总共40个回合。

## 核心特性

### 🕐 时间系统
- **回合制**: 每回合可配置持续时间（默认5秒）
- **时段划分**: 早上、中午、晚上、半夜（特殊回合）
- **自动推进**: 时间自动流逝，支持暂停/恢复
- **进度跟踪**: 实时显示游戏进度和剩余时间

### 🎲 事件系统
- **时段事件**: 不同时段触发不同类型的事件
- **随机事件**: 基于概率的随机事件系统
- **事件优先级**: 低、普通、高、关键四个优先级
- **事件管理**: 支持事件触发、完成、失败等状态

### 📦 资源系统
- **多种资源**: 食物、水、药品、弹药、燃料、材料、金钱
- **动态变化**: 支持日常消耗、生产、衰减
- **状态监控**: 实时监控资源状态，预警机制
- **上限管理**: 可配置的资源上限和溢出处理

### 🌍 世界系统
- **动态天气**: 6种天气类型，影响游戏体验
- **危险等级**: 基于时间、天气、进度的动态危险系统
- **世界状态**: 安全、正常、危险、危急、混乱五种状态
- **环境影响**: 天气对视野、移动、收集的影响

### 💾 存档系统
- **多存档槽**: 支持多个存档位置
- **自动保存**: 可配置的自动保存功能
- **数据完整性**: 完整的游戏状态保存

### 🎮 游戏控制
- **状态管理**: 未开始、游戏中、暂停、结束等状态
- **信号系统**: 完整的事件信号系统
- **调试支持**: 丰富的调试信息和统计功能

## 系统架构

```
GameManager (总管理器 - 单例)
├── TimeManager (时间管理)
├── EventManager (事件管理)
├── ResourceManager (资源管理)
├── CharacterManager (角色管理)
├── WorldManager (世界管理)
├── SaveManager (存档管理)
└── UIManager (界面管理)
```

## 使用方法

### 1. 基本初始化

```csharp
// GameManager会自动创建并管理所有子管理器
var gameManager = GameManager.Instance;

// 开始新游戏
gameManager.StartNewGame();

// 暂停/恢复游戏
gameManager.PauseGame();
gameManager.ResumeGame();
```

### 2. 时间控制

```csharp
// 设置回合持续时间
gameManager.TimeManager.SetTurnDuration(3.0f);

// 手动进入下一回合
gameManager.TimeManager.NextTurn();

// 获取时间信息
var currentTime = gameManager.TimeManager.GetCurrentTimeDescription();
var progress = gameManager.TimeManager.GetGameProgress();
```

### 3. 资源管理

```csharp
// 添加资源
gameManager.ResourceManager.AddResource("food", 20, "收集获得");

// 消耗资源
gameManager.ResourceManager.ConsumeResource("water", 5, "日常消耗");

// 检查资源状态
var foodResource = gameManager.ResourceManager.GetResource("food");
if (foodResource.IsSufficient(10))
{
    // 执行需要10个食物的操作
}
```

### 4. 事件系统

```csharp
// 创建自定义事件
var customEvent = new GameEvent("custom_event", "自定义事件", "事件描述");

// 触发事件
gameManager.EventManager.TriggerEvent(customEvent);

// 完成事件
gameManager.EventManager.CompleteEvent("custom_event");
```

### 5. 世界状态

```csharp
// 获取世界状态
var worldState = gameManager.WorldManager.GetWorldStateDescription();
var weatherEffects = gameManager.WorldManager.GetWeatherEffects();

// 检查危险等级
var dangerLevel = gameManager.WorldManager.DangerLevel;
```

## 信号系统

系统使用Godot的信号系统进行通信，主要信号包括：

- `GameStarted`: 游戏开始
- `GamePaused`: 游戏暂停
- `GameResumed`: 游戏恢复
- `GameOver`: 游戏结束
- `TurnChanged`: 回合变化
- `EventTriggered`: 事件触发
- `ResourceChanged`: 资源变化
- `WorldStateChanged`: 世界状态变化

## 配置选项

### 时间配置
- `TotalDays`: 总游戏天数（默认10天）
- `TurnsPerDay`: 每天回合数（默认4回合）
- `TurnDuration`: 每回合持续时间（默认5秒）

### 事件配置
- `RandomEventChance`: 随机事件概率（默认30%）
- `MaxActiveEvents`: 最大活跃事件数（默认5个）
- `EnableRandomEvents`: 是否启用随机事件（默认开启）

### 资源配置
- `EnableResourceDecay`: 是否启用资源衰减（默认开启）
- `GlobalDecayMultiplier`: 全局衰减倍数（默认1.0）
- `CriticalThreshold`: 资源紧缺阈值（默认20%）

## 测试和调试

系统包含完整的测试脚本 `GameManagerTest.cs`，提供：

- 游戏控制按钮（开始、暂停、恢复、下一回合）
- 信息显示面板
- 存档/读档测试
- 实时状态监控

## 扩展建议

### 1. 角色系统
- 集成现有的Character和Relation系统
- 添加角色状态管理（健康、饥饿、疲劳等）
- 实现角色间的互动和关系发展

### 2. 任务系统
- 基于事件系统构建任务系统
- 支持主线任务和支线任务
- 任务奖励和惩罚机制

### 3. 战斗系统
- 集成现有的战斗技能系统
- 基于时间和天气的战斗影响
- 战斗结果对资源的影响

### 4. 建造系统
- 基于材料资源的建造系统
- 建筑对资源生产和消耗的影响
- 防御设施的建造和维护

## 注意事项

1. **单例模式**: GameManager使用单例模式，确保全局只有一个实例
2. **信号连接**: 所有管理器都通过信号系统通信，避免直接引用
3. **资源管理**: 系统会自动管理资源的生命周期，避免内存泄漏
4. **错误处理**: 所有关键操作都包含异常处理，确保系统稳定性

## 性能优化

- 使用对象池管理频繁创建的对象
- 延迟更新非关键系统
- 批量处理资源变化
- 智能的事件触发机制

这个系统为你的游戏提供了一个强大而灵活的基础框架，可以根据具体需求进行扩展和定制。
