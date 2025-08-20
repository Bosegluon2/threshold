# 条件系统使用说明

## 概述

条件系统是一个强大的数据驱动条件评估框架，用于在事件分支、任务完成等场景中动态评估游戏状态。系统基于PathResolver实现，支持复杂的条件组合和灵活的数据路径访问。

## 核心组件

### 1. BaseCondition
所有条件的抽象基类，定义了条件的基本接口：
- `Evaluate(GameManager gameManager)`: 评估条件
- `GetDisplayString()`: 获取条件的字符串表示

### 2. SimpleCondition
简单条件，支持单一比较操作：
- **目标路径**: 使用PathResolver访问游戏数据
- **操作符**: 支持等于、大于、小于、包含等比较操作
- **期望值**: 与目标值进行比较的基准值

### 3. ConditionCombination
条件组合，支持逻辑运算：
- **AND**: 所有子条件都为真
- **OR**: 任一子条件为真
- **NOT**: 子条件为假

### 4. ConditionManager
条件管理器，提供条件评估和模板管理功能。

### 5. ConditionLoader
条件加载器，从YAML数据创建条件对象。

## 数据路径格式

### 基础路径
```
gameManager.CurrentTurn
gameManager.WorldABC.worldState
gameManager.CharacterManager.characters
```

### 字典访问
```
gameManager.WorldABC.worldState["magic_academy_available"]
gameManager.CharacterManager.characters["char_0001"]
```

### 嵌套属性
```
gameManager.CharacterManager.characters["char_0001"].skills["magic_theory"]
gameManager.WorldABC.worldState["quest_progress"]
```

## YAML配置示例

### 简单条件
```yaml
enable_conditions:
  - condition_id: "cond_magic_available"
    type: "simple"
    target_path: "gameManager.WorldABC.worldState[\"magic_academy_available\"]"
    operator: "equals"
    expected_value: true
```

### 条件组合
```yaml
enable_conditions:
  - condition_id: "cond_complex"
    type: "and"
    sub_conditions:
      - condition_id: "cond_level"
        type: "simple"
        target_path: "gameManager.CharacterManager.characters[\"char_0001\"].level"
        operator: "greater_than"
        expected_value: 10
      - condition_id: "cond_skill"
        type: "simple"
        target_path: "gameManager.CharacterManager.characters[\"char_0001\"].skills[\"magic_theory\"]"
        operator: "greater_than"
        expected_value: 5
```

### 分支配置
```yaml
branches:
  - branch_id: "branch_001"
    branch_name: "选择路径A"
    type: "manual"  # 手动分支
    probability: 0.5
    enable_conditions:
      - condition_id: "cond_path_available"
        type: "simple"
        target_path: "gameManager.WorldABC.worldState[\"path_a_available\"]"
        operator: "equals"
        expected_value: true
    next_events:
      - event_id: "next_event"
        event_name: "后续事件"
        activation_delay: 1
```

## 支持的操作符

| 操作符 | 描述 | 示例 |
|--------|------|------|
| `equals` | 等于 | `value == 5` |
| `not_equals` | 不等于 | `value != 5` |
| `greater_than` | 大于 | `value > 5` |
| `less_than` | 小于 | `value < 5` |
| `greater_equal` | 大于等于 | `value >= 5` |
| `less_equal` | 小于等于 | `value <= 5` |
| `contains` | 包含 | `"hello" in value` |
| `not_contains` | 不包含 | `"hello" not in value` |
| `exists` | 存在 | `value != null` |
| `not_exists` | 不存在 | `value == null` |

## 分支类型

### 手动分支 (Manual)
- 每个选项都有enable条件
- 用户可以看到所有可用选项并手动选择
- 支持复杂的条件组合

### 自动分支 (Auto)
- 每个选项都有enable条件
- 按index顺序逐个评估，返回第一个满足条件的选项
- 系统自动处理，无需用户选择

## 数据路径探索工具

使用`DataPathExplorer`工具来探索和测试数据路径：

```csharp
// 探索所有可访问路径
DataPathExplorer.ExploreGameManagerPaths(gameManager);

// 测试特定路径
DataPathExplorer.TestPath(gameManager, "CurrentTurn");

// 生成路径使用示例
DataPathExplorer.GeneratePathExamples();
```

## 最佳实践

### 1. 路径命名
- 使用清晰的路径结构：`gameManager.WorldABC.worldState["key"]`
- 避免过深的嵌套：`gameManager.A.B.C.D.E.F`
- 使用有意义的键名：`magic_academy_available` 而不是 `flag1`

### 2. 条件设计
- 保持条件简单，复杂逻辑使用条件组合
- 为每个条件提供清晰的描述
- 使用适当的操作符和期望值

### 3. 性能考虑
- 避免在条件中执行复杂计算
- 缓存频繁访问的数据路径
- 限制条件组合的深度

### 4. 错误处理
- 为所有条件提供合理的默认值
- 记录条件评估失败的原因
- 在条件失败时提供用户友好的反馈

## 扩展指南

### 添加新的操作符
1. 在`ConditionOperator`枚举中添加新值
2. 在`SimpleCondition.CompareValues`中实现逻辑
3. 在`ConditionLoader.ParseOperator`中添加解析支持

### 添加新的目标类型
1. 在`ConditionTargetType`枚举中添加新值
2. 在`SimpleCondition.GetTargetValue`中实现获取逻辑
3. 在`ConditionLoader.InferTargetType`中添加类型推断

### 添加新的条件类型
1. 继承`BaseCondition`类
2. 实现`Evaluate`和`GetDisplayString`方法
3. 在`ConditionLoader`中添加创建逻辑

## 故障排除

### 常见问题

1. **路径不存在**
   - 检查路径拼写是否正确
   - 确认目标对象是否已初始化
   - 使用`DataPathExplorer`验证路径

2. **条件评估失败**
   - 检查操作符是否支持目标值类型
   - 确认期望值的格式是否正确
   - 查看控制台错误信息

3. **性能问题**
   - 避免在条件中执行复杂操作
   - 考虑缓存频繁访问的数据
   - 优化条件组合的结构

### 调试技巧

1. 使用`GD.Print`输出条件评估过程
2. 使用`DataPathExplorer.TestPath`测试特定路径
3. 检查YAML配置的语法和结构
4. 验证GameManager的数据状态

## 总结

条件系统为事件管理提供了强大的数据驱动能力，通过PathResolver实现了灵活的数据访问，支持复杂的条件组合和逻辑运算。系统设计简洁，易于扩展，为游戏逻辑提供了坚实的基础。
