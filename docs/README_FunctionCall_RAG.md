# Function-Call 和 RAG 系统使用说明

## 概述

本项目实现了一个完整的角色扮演AI系统，包含以下核心功能：

1. **Function-Call 功能**：允许AI调用预定义的函数来获取游戏信息
2. **世界观存储器**：存储和管理游戏世界的背景知识（支持YAML文件）
3. **Function-Call RAG系统**：基于function-call的智能信息检索系统

## 系统架构

### 核心组件

- **CharacterCard**: 角色卡，定义角色的各种属性
- **AICommunication**: AI通信模块，负责与DashScope API交互
- **FunctionManager**: 函数管理器，注册和执行可调用函数
- **WorldLoreManager**: 世界观管理器，存储和检索世界背景信息
- **WorldLoreLoader**: YAML数据加载器，支持外部文件管理
- **RoleplayManager**: 角色扮演管理器，协调各个组件

## Function-Call 功能

### 预定义函数

系统包含以下预定义函数：

1. **get_world_info**: 获取世界信息（时间、天气、地点等）
2. **get_character_status**: 获取角色状态（生命值、魔法值、经验等）
3. **get_inventory**: 获取物品栏信息
4. **get_quests**: 获取任务信息
5. **get_relationships**: 获取NPC关系信息
6. **retrieve_world_lore**: 检索世界观相关信息（RAG核心功能）
7. **get_character_lore**: 获取角色相关的世界观信息

### 使用方法

在对话中询问相关问题，AI会自动调用相应函数：

```
用户: "我的状态怎么样？"
AI: [自动调用get_character_status函数，然后以角色身份回复]

用户: "我的物品栏里有什么？"
AI: [自动调用get_inventory函数，然后以角色身份回复]

用户: "现在是什么时间？"
AI: [自动调用get_world_info函数，然后以角色身份回复]

用户: "告诉我关于魔法学院的信息"
AI: [自动调用retrieve_world_lore函数，然后以角色身份回复]

用户: "作为魔法师，我应该了解什么？"
AI: [自动调用get_character_lore函数，然后以角色身份回复]
```

## Function-Call RAG 系统

### 为什么使用Function-Call实现RAG？

相比传统的提示词增强RAG，Function-Call RAG具有以下优势：

1. **智能检索**: AI可以主动决定何时检索什么信息
2. **减少Token消耗**: 只在需要时获取信息，不增加基础提示词长度
3. **交互式检索**: 支持多轮检索和精化查询
4. **灵活策略**: AI可以根据上下文选择不同的检索策略

### RAG函数详解

#### retrieve_world_lore (世界观检索)

**功能**: 检索世界观相关信息，支持多种搜索策略

**参数**:
- `query`: 关键词查询
- `category`: 分类搜索（如：geography, history, magic）
- `tags`: 标签搜索（逗号分隔）
- `max_results`: 最大结果数量（默认5）
- `search_type`: 搜索类型（smart, category, tags, keyword）

**示例调用**:
```json
{
  "query": "魔法学院",
  "search_type": "keyword",
  "max_results": 3
}
```

#### get_character_lore (角色相关检索)

**功能**: 获取与当前角色相关的世界观信息

**参数**:
- `profession`: 角色职业
- `faction`: 角色派别
- `skills`: 角色技能（逗号分隔）
- `max_results`: 最大结果数量（默认3）

**示例调用**:
```json
{
  "profession": "魔法师",
  "faction": "魔法学院",
  "skills": "火系魔法,冰系魔法"
}
```

### 检索策略

1. **智能搜索 (smart)**: 结合关键词、分类、标签的综合搜索
2. **分类搜索 (category)**: 按预定义分类检索
3. **标签搜索 (tags)**: 按标签检索
4. **关键词搜索 (keyword)**: 基于内容的全文搜索

## 世界观存储器

### YAML文件支持

世界观数据现在存储在 `data/world_lore.yaml` 文件中，支持：

- **分类组织**: geography, history, magic, culture, races等
- **标签系统**: 支持多个标签，便于检索
- **重要性等级**: 1-10级，影响检索排序
- **版本管理**: 支持数据版本和更新时间

### 文件结构示例

```yaml
version: "1.0"
last_updated: "2024-01-01"

geography:
  - title: "魔法学院"
    content: "魔法学院位于大陆中央的魔法之都..."
    tags: ["魔法", "教育", "建筑", "结界"]
    importance: 8

history:
  - title: "魔法战争"
    content: "三百年前，大陆上爆发了一场规模空前的魔法战争..."
    tags: ["战争", "魔法", "封印", "和平"]
    importance: 10
```

### 动态管理

- **自动加载**: 启动时自动从YAML文件加载
- **热重载**: 支持运行时重新加载数据
- **自定义添加**: 支持程序化添加新的世界观条目
- **自动备份**: 支持保存到用户数据目录

## 使用方法

### 1. 启动项目

运行 `scenes/test/TestFunctionCall.tscn` 场景来测试功能。

### 2. 选择角色

从下拉菜单中选择一个预设角色（艾莉娅或雷克斯）。

### 3. 开始对话

在输入框中输入消息，可以尝试：

- 询问角色状态
- 查询游戏信息
- 了解世界观背景
- 进行角色扮演对话

### 4. RAG功能测试

尝试以下查询来测试RAG系统：

```
"告诉我关于魔法学院的信息"
"作为魔法师，我应该了解什么？"
"搜索与战争相关的历史"
"查找所有关于魔法的内容"
"我的职业有什么特殊背景？"
```

### 5. 查看状态

UI界面会显示：
- 函数调用状态
- 世界观初始化状态
- RAG系统状态（Function-Call模式）

### 6. 监控调试信息

在Godot控制台中查看详细的调试输出，包括：
- 函数调用过程
- RAG检索过程
- API通信过程

## 扩展功能

### 添加新函数

1. 实现 `IFunctionExecutor` 接口
2. 在 `RoleplayManager.InitializeFunctionManager()` 中注册
3. 函数会自动被AI识别和调用

### 添加世界观内容

1. 编辑 `data/world_lore.yaml` 文件
2. 或使用 `WorldLoreManager.AddCustomLoreEntry()` 方法
3. 内容会自动被RAG函数检索

### 自定义RAG策略

可以修改RAG函数中的检索逻辑，实现更复杂的检索策略。

## 技术特点

- **模块化设计**: 各组件职责明确，易于扩展
- **Function-Call优先**: RAG系统基于function-call，更智能高效
- **YAML支持**: 世界观数据外部化，便于管理和扩展
- **调试友好**: 详细的日志输出，便于问题排查
- **性能优化**: 使用索引加速检索，支持大量世界观条目
- **Godot集成**: 完全兼容Godot引擎，使用Godot的数据类型

## 注意事项

1. 确保DashScope API密钥有效
2. 网络请求可能需要时间，请耐心等待
3. 函数调用会增加API调用次数
4. 世界观内容越多，RAG检索效果越好
5. YAML文件格式必须严格遵循规范

## 故障排除

### 常见问题

1. **函数调用失败**: 检查函数是否正确注册
2. **RAG检索无结果**: 检查世界观内容是否丰富
3. **YAML加载失败**: 检查文件格式和路径
4. **API请求失败**: 检查网络连接和API密钥
5. **调试信息过多**: 可以在生产环境中减少日志输出

### 调试技巧

1. 查看控制台输出的详细日志
2. 检查各个组件的初始化状态
3. 验证函数注册和世界观内容加载
4. 监控API请求和响应状态
5. 检查YAML文件格式是否正确

## 与传统RAG的对比

| 特性 | 传统RAG | Function-Call RAG |
|------|---------|-------------------|
| 实现方式 | 提示词增强 | 函数调用 |
| Token消耗 | 高（增加提示词长度） | 低（按需获取） |
| 检索策略 | 固定 | 智能选择 |
| 交互性 | 单向 | 多轮交互 |
| 灵活性 | 有限 | 高度灵活 |
| 性能 | 中等 | 高效 |
