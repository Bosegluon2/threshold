# 统一架构 - 多Agent角色扮演系统

## 架构概述

经过重构，系统现在采用统一的架构设计：

- **删除了CharacterManager**：角色管理功能已整合到AgentManager中
- **统一的ID系统**：CharacterCard和Agent共享ID命名空间，确保唯一性
- **简化的依赖关系**：所有管理功能集中在AgentManager中

## 核心组件

### 1. AgentManager（统一管理器）
- 管理所有Agent实例
- 管理所有CharacterCard实例
- 提供统一的ID生成和管理
- 初始化核心系统（世界观、函数、AI通信）

### 2. Agent（独立Agent类）
- 完全独立的Agent实例
- 包含自己的对话历史和状态
- 可以设置不同的角色
- 支持多Agent并行运行

### 3. CharacterCard（角色卡）
- 包含完整的角色信息
- 支持Evaluation.cs中的所有状态描述
- 统一的ID系统

## 架构优势

### 1. 简化管理
- 单一管理器，避免重复初始化
- 统一的ID命名空间
- 清晰的依赖关系

### 2. 支持多Agent
- 每个Agent独立运行
- 可以扮演不同角色
- 支持Agent间交流

### 3. 完整的角色信息
- AI可以通过function-call获取角色卡完整信息
- 包括Evaluation.cs中的状态描述
- 支持Warped System属性

## 使用方法

### 1. 创建Agent
```csharp
var agent = AgentManager.Instance.CreateAgent("MyAgent");
```

### 2. 创建角色
```csharp
var character = AgentManager.Instance.CreateCharacter("角色名", "性格", "性别", 年龄, "职业", "派别", "背景");
```

### 3. 设置Agent角色
```csharp
agent.SetCharacter(character);
```

### 4. AI Function-Call支持
AI可以通过以下函数获取信息：

- `get_character_card` - 获取角色卡完整信息
- `retrieve_world_lore` - 检索世界观信息
- `get_character_lore` - 获取角色相关世界观
- 其他游戏相关函数

## 测试场景

使用 `scenes/test/TestUnifiedArchitecture.tscn` 来测试重构后的系统。

## 技术特点

1. **单例模式**：AgentManager使用单例确保全局唯一
2. **信号系统**：Agent和UI通过信号通信，解耦设计
3. **资源管理**：统一的资源创建和销毁
4. **扩展性**：易于添加新的Agent和角色

## 注意事项

1. 确保场景中只有一个AgentManager实例
2. Agent和Character的ID是全局唯一的
3. 删除Agent时会自动清理相关资源
4. 所有function-call都通过统一的FunctionManager管理
