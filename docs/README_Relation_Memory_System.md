# 关系和记忆系统说明

## 概述
本系统现在支持智能的关系管理和记忆系统，AI角色不再需要手动输入信任程度和关系等级，系统会自动维护这些数据。

## 核心组件

### 1. Relation 类
管理两个角色之间的关系信息：

#### 主要属性
- **TrustLevel**: 信任程度 (0-100)
- **RelationshipLevel**: 关系等级 (0-100)
- **RelationshipType**: 关系类型（陌生人、熟人、朋友、密友等）
- **Impression**: 对目标角色的印象
- **InteractionCount**: 互动次数
- **SharedSecrets**: 已分享的秘密记录
- **Favors**: 互相帮助的记录
- **Conflicts**: 冲突记录

#### 主要方法
- `UpdateTrust(change, reason)`: 更新信任程度
- `UpdateRelationship(change, reason)`: 更新关系等级
- `RecordInteraction(description, trustChange, relationshipChange)`: 记录互动
- `RecordSharedSecret(secretType, secretContent)`: 记录分享的秘密
- `RecordFavor(favorDescription, isGiving)`: 记录帮助行为
- `RecordConflict(conflictDescription, severity)`: 记录冲突
- `CanShareInformation(secrecyLevel)`: 检查是否可以分享信息

### 2. CharacterCard 增强
现在包含完整的关系和记忆管理：

#### 新增属性
- **Relationships**: `Dictionary<string, Relation>` - 与其他角色的关系映射
- **Memories**: `Array<string>` - 一般记忆
- **ImportantEvents**: `Array<string>` - 重要事件记忆
- **PersonalSecrets**: `Array<string>` - 个人秘密记忆

#### 新增方法
- `GetOrCreateRelation(targetId, targetName, initialTrust)`: 获取或创建关系
- `UpdateRelation(targetId, interaction, trustChange, relationshipChange)`: 更新关系
- `RecordSharedSecret(targetId, secretType, secretContent)`: 记录分享秘密
- `RecordFavor(targetId, favorDescription, isGiving)`: 记录帮助行为
- `RecordConflict(targetId, conflictDescription, severity)`: 记录冲突
- `CanShareInformation(targetId, secrecyLevel)`: 检查是否可以分享信息
- `GetRelationshipsSummary()`: 获取关系摘要
- `GetMemoriesSummary(maxMemories)`: 获取记忆摘要

## 系统特性

### 1. 自动关系管理
- **初次见面**: 自动创建关系记录，设置默认信任度
- **动态更新**: 每次互动自动更新信任和关系等级
- **智能分类**: 根据综合分数自动调整关系类型

### 2. 智能记忆系统
- **时间戳**: 所有记忆都带有精确的时间戳
- **分类存储**: 一般记忆、重要事件、个人秘密分别存储
- **自动记录**: 系统自动记录所有重要互动

### 3. 信息分享决策
- **无需AI输入**: 系统自动获取信任程度和关系等级
- **智能判断**: 基于历史互动数据做出决策
- **自动记录**: 分享行为自动记录到系统中

## 使用方法

### 1. 函数调用（简化版）
```json
{
  "name": "decide_information_sharing",
  "arguments": {
    "information_type": "goals",
    "target_name": "雷克斯"
  }
}
```

**注意**: 不再需要 `trust_level` 和 `relationship_level` 参数！

### 2. 关系建立示例
```csharp
// 获取或创建与雷克斯的关系
var relation = character.GetOrCreateRelation("char_0002", "雷克斯", 60);

// 记录一次友好的互动
character.UpdateRelation("char_0002", "一起完成了魔法学院的任务", 5, 3);

// 记录分享秘密
character.RecordSharedSecret("char_0002", "目标", "成为最强大的魔法师");
```

### 3. 记忆管理示例
```csharp
// 添加一般记忆
character.AddMemory("今天在图书馆学习了一整天", false);

// 添加重要事件
character.AddMemory("成功通过了魔法师资格考试", true);

// 添加个人秘密
character.AddPersonalSecret("我其实有一个隐藏的魔法天赋");
```

## 关系类型自动分类

| 综合分数 | 关系类型 | 描述 |
|---------|---------|------|
| 90-100  | 密友     | 生死之交，完全信任 |
| 80-89   | 好朋友   | 亲密无间，高度信任 |
| 70-79   | 朋友     | 关系密切，比较信任 |
| 60-69   | 熟人     | 关系良好，一般信任 |
| 40-59   | 点头之交 | 关系一般，略有怀疑 |
| 20-39   | 陌生人   | 关系疏远，比较怀疑 |
| 10-19   | 不信任   | 关系紧张，高度怀疑 |
| 0-9     | 敌人     | 势不两立，完全不信任 |

## 信任和关系变化规则

### 正面互动
- **分享秘密**: 信任+5, 关系+3
- **给予帮助**: 信任+3, 关系+2
- **接受帮助**: 信任+5, 关系+4
- **一般互动**: 信任+1-3, 关系+1-2

### 负面互动
- **冲突**: 信任-5×严重程度, 关系-3×严重程度
- **背叛**: 信任-20, 关系-15
- **欺骗**: 信任-15, 关系-10

## 信息分享决策逻辑

### 决策流程
1. **获取关系数据**: 从系统维护的关系映射中获取
2. **计算分享概率**: 使用sigmoid函数，基于信任+关系 vs 信息秘密程度
3. **自动记录**: 如果决定分享，自动记录到系统中
4. **关系更新**: 分享行为会影响后续的关系发展

### 决策因素
- **信任程度**: 系统维护的历史信任数据
- **关系等级**: 系统维护的历史关系数据
- **信息秘密程度**: 信息的敏感程度
- **历史互动**: 之前的分享记录和互动历史

## 系统优势

### 1. 数据一致性
- 所有关系数据由系统统一维护
- 避免AI输入不一致的问题
- 支持复杂的关系网络

### 2. 智能决策
- 基于历史数据做出合理判断
- 支持渐进式关系发展
- 模拟真实的人际关系

### 3. 自动记录
- 所有重要行为自动记录
- 支持长期的角色发展
- 为后续互动提供上下文

### 4. 可扩展性
- 支持自定义关系类型
- 支持复杂的社交网络
- 支持多种记忆分类

## 使用场景

### 1. 角色扮演
- AI角色根据历史关系做出决策
- 支持长期的角色发展
- 模拟真实的人际交往

### 2. 游戏机制
- 关系系统影响游戏进程
- 记忆系统提供任务线索
- 支持复杂的社交任务

### 3. 故事发展
- 关系变化推动剧情发展
- 记忆系统提供背景信息
- 支持动态的故事世界

## 注意事项

1. **关系初始化**: 新角色默认信任度为50（中立）
2. **记忆限制**: 建议定期清理过期的记忆数据
3. **关系平衡**: 避免关系变化过于剧烈
4. **数据持久化**: 关系数据会自动保存到YAML文件

## 扩展建议

1. **群体关系**: 支持复杂的社交网络
2. **文化因素**: 不同文化背景的关系模型
3. **时间衰减**: 关系随时间自然变化
4. **情感系统**: 更复杂的情感状态管理
