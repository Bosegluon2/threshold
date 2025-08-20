# 玩家角色系统说明

## 概述
本系统现在支持玩家角色，AI角色能够识别"我在对谁说话"，从而正确地调用关系更新和信息分享函数。玩家角色拥有固定的ID `player`，名字为"探索者查理"。

## 核心特性

### 1. 玩家角色保护
- **固定ID**: 玩家角色ID固定为 `player`，不能被其他角色使用
- **名称保护**: 名称"探索者查理"被保留，其他角色不能使用
- **自动创建**: 如果找不到玩家角色文件，系统会自动创建默认玩家角色

### 2. AI角色识别
- **对话目标传递**: AI通信系统自动传递玩家角色信息
- **智能默认值**: 关系更新和信息分享函数默认使用玩家角色作为目标
- **上下文感知**: AI知道"我在对谁说话"

### 3. 关系系统集成
- **自动关系建立**: AI角色与玩家角色互动时自动建立关系记录
- **记忆系统**: 所有互动都会记录到AI角色的记忆中
- **关系发展**: 支持长期的关系发展和变化

## 玩家角色设定

### 基本信息
- **ID**: `player` (固定，不可更改)
- **姓名**: 探索者查理
- **性别**: 男
- **年龄**: 28岁
- **职业**: 探索者
- **派别**: 自由探索者协会

### 性格特征
- **好奇**: 对魔法世界充满探索欲望
- **勇敢**: 敢于面对未知和挑战
- **善良**: 愿意帮助当地居民
- **富有同情心**: 理解并关心他人

### 背景故事
查理是一名来自现代世界的探索者，意外穿越到了这个魔法世界。他拥有丰富的现代知识和探索经验，对这个世界充满好奇，希望能够帮助这里的居民，同时找到回到原来世界的方法。

### 技能属性
- **现代科技知识**: 80分
- **探索技能**: 75分
- **适应性**: 80分
- **推理能力**: 75分
- **感知力**: 70分
- **耐力**: 60分
- **敏捷性**: 65分
- **战斗能力**: 20分

## 使用方法

### 1. 自动加载
玩家角色会在系统启动时自动加载，无需手动干预：

```csharp
// 系统会自动检查并加载玩家角色
var playerCharacter = AgentManager.Instance.GetCharacterById("player");
if (playerCharacter != null)
{
    GD.Print($"玩家角色已加载: {playerCharacter.Name}");
}
```

### 2. AI角色互动
AI角色现在可以正确地与玩家角色互动：

```json
{
  "name": "update_relationship",
  "arguments": {
    "interaction_type": "share_secret",
    "description": "我向探索者查理透露了我对魔法的研究心得",
    "is_important": true
  }
}
```

**注意**: 不需要指定 `target_name` 或 `target_id`，系统会自动使用玩家角色作为目标。

### 3. 信息分享
AI角色可以决定是否向玩家分享信息：

```json
{
  "name": "decide_information_sharing",
  "arguments": {
    "information_type": "goals",
    "is_important": true
  }
}
```

## 系统架构

### 1. 数据流
```
AI角色 → AI通信系统 → 传递玩家信息 → 函数调用 → 关系更新
```

### 2. 关键组件
- **`player.yaml`**: 玩家角色数据文件
- **`AICommunication.cs`**: 自动传递对话目标信息
- **`UpdateRelationshipFunction.cs`**: 支持默认玩家目标
- **`InformationSharingFunction.cs`**: 支持默认玩家目标
- **`CharacterLoader.cs`**: 自动创建玩家角色

### 3. 保护机制
- **ID保护**: 其他角色不能使用 `player` 作为ID
- **名称保护**: 其他角色不能使用"探索者查理"作为名称
- **自动验证**: 系统启动时验证玩家角色完整性

## 使用场景

### 1. 初次见面
当AI角色第一次与玩家互动时：

```json
{
  "name": "update_relationship",
  "arguments": {
    "interaction_type": "general_interaction",
    "description": "初次见到探索者查理，他看起来对这个魔法世界充满好奇",
    "trust_change": 5,
    "relationship_change": 3,
    "is_important": true
  }
}
```

### 2. 分享秘密
AI角色决定是否向玩家分享个人秘密：

```json
{
  "name": "decide_information_sharing",
  "arguments": {
    "information_type": "secrets",
    "is_important": true
  }
}
```

### 3. 一起冒险
记录与玩家一起完成的冒险：

```json
{
  "name": "update_relationship",
  "arguments": {
    "interaction_type": "adventure",
    "description": "与探索者查理一起探索了古老的魔法遗迹",
    "is_important": true
  }
}
```

### 4. 给予帮助
AI角色帮助玩家：

```json
{
  "name": "update_relationship",
  "arguments": {
    "interaction_type": "give_favor",
    "description": "教探索者查理学习基础魔法",
    "is_important": true
  }
}
```

## 技术实现

### 1. ExtraParams传递
```csharp
// 在AICommunication.cs中
var extraParams = new Dictionary<string, object>();
if (characterCard != null)
{
    extraParams["current_character"] = characterCard;
    
    // 传递玩家角色信息
    var playerCharacter = AgentManager.Instance.GetCharacterById("player");
    if (playerCharacter != null)
    {
        extraParams["conversation_target"] = playerCharacter;
        extraParams["conversation_target_id"] = playerCharacter.Id;
        extraParams["conversation_target_name"] = playerCharacter.Name;
    }
}
```

### 2. 默认目标处理
```csharp
// 在函数中自动使用玩家角色作为默认目标
if (targetCharacter == null)
{
    if (extraParams != null && extraParams.ContainsKey("conversation_target"))
    {
        targetCharacter = extraParams["conversation_target"] as CharacterCard;
    }
}
```

### 3. 角色保护
```csharp
// 在AgentManager.cs中保护玩家ID
if (name.ToLower() == "player" || name.ToLower() == "探索者查理")
{
    GD.PrintErr("角色名称不能使用 'player' 或 '探索者查理'，这些名称已被保留");
    return null;
}
```

## 最佳实践

### 1. 关系发展
- **渐进式**: 从陌生人开始，逐步建立信任
- **互动记录**: 记录所有重要的互动
- **关系平衡**: 避免关系变化过于剧烈

### 2. 信息分享
- **信任基础**: 基于信任程度决定是否分享
- **秘密等级**: 考虑信息的敏感程度
- **关系影响**: 分享行为会影响后续关系

### 3. 记忆管理
- **重要事件**: 标记真正重要的事件
- **时间记录**: 所有记忆都带有时间戳
- **分类存储**: 一般记忆、重要事件、个人秘密分别存储

## 注意事项

1. **ID唯一性**: 玩家角色ID `player` 是系统保留的，不能更改
2. **自动加载**: 系统会自动确保玩家角色存在
3. **关系初始化**: 新AI角色与玩家的初始关系为陌生人（信任度30）
4. **数据持久化**: 关系数据会自动保存到系统中
5. **错误处理**: 如果玩家角色加载失败，系统会创建默认角色

## 扩展建议

1. **多玩家支持**: 未来可以支持多个玩家角色
2. **关系网络**: 支持复杂的多角色关系网络
3. **文化因素**: 考虑不同文化背景的关系模型
4. **时间衰减**: 关系随时间自然变化
5. **情感系统**: 更复杂的情感状态管理

## 故障排除

### 常见问题

1. **玩家角色未加载**
   - 检查 `data/characters/player.yaml` 文件是否存在
   - 查看控制台日志，确认角色加载过程

2. **AI角色无法识别玩家**
   - 确认 `AICommunication.cs` 中的玩家信息传递
   - 检查 `extraParams` 是否包含 `conversation_target`

3. **关系更新失败**
   - 确认目标角色参数设置
   - 检查互动类型和描述是否完整

### 调试信息
系统会输出详细的调试信息，包括：
- 玩家角色加载状态
- 对话目标传递过程
- 关系更新执行结果
- 函数调用参数和结果

通过这些信息，可以快速定位和解决问题。
