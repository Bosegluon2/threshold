# 函数分离修复说明

## 修复的问题

### 1. 原函数设计缺陷
- **问题**: `GetCharacterStatusFunction`需要`requester_id`参数，但AI不知道应该使用什么ID
- **原因**: 函数设计时没有考虑AI的上下文信息，参数验证过于严格
- **影响**: 函数调用失败，无法获取角色状态信息

### 2. 权限控制混乱
- **问题**: 同一个函数既要处理自己的信息，又要处理他人的信息
- **原因**: 功能耦合，逻辑复杂
- **影响**: 代码难以维护，权限控制不清晰

## 修复方案

### 1. 函数分离
将原来的`GetCharacterStatusFunction`分离为两个独立的函数：

#### `GetSelfInformationFunction` - 获取自己的信息
- **功能**: 获取当前角色的完整信息
- **参数**: 只需要`requested_info`（可选）
- **权限**: 无需验证，直接返回自己的信息
- **使用场景**: AI想了解自己的状态、技能、关系等

#### `GetOtherInformationFunction` - 获取他人的信息
- **功能**: 获取其他角色的信息，根据权限显示不同详细程度
- **参数**: 需要`target_id`或`target_name`，以及`requested_info`（可选）
- **权限**: 根据职业权限控制信息显示
- **使用场景**: AI想了解其他角色的信息

### 2. 架构改进

#### AI通信模块增强
- 添加`currentCharacter`字段存储当前角色引用
- 提供`SetCurrentCharacter`和`GetCurrentCharacter`方法
- 在发送消息时自动设置当前角色

#### 函数注册更新
- 在`AgentManager`中注册两个新函数
- 移除旧的`GetCharacterStatusFunction`
- 保持其他函数不变

#### 系统消息优化
- 更新AI的系统提示，明确说明两个函数的用法
- 提供具体的函数调用示例
- 简化AI的理解和使用

## 修复后的工作流程

### 1. 获取自己的信息
```
AI调用: get_self_information(requested_info: "full")
结果: 返回当前角色的完整信息
```

### 2. 获取他人的信息
```
AI调用: get_other_information(target_id: "char_0001", requested_info: "basic")
结果: 根据权限返回目标角色的基本信息
```

### 3. 权限控制
- **医生职业**: 可以看到详细的健康状态
- **侦察兵职业**: 可以看到详细的战斗能力
- **魔法师职业**: 可以看到特殊信息（目标、恐惧等）
- **普通职业**: 只能看到基本信息

## 代码结构

### 新函数文件
- `scripts/core/agent/Functions/GetSelfInformationFunction.cs`
- `scripts/core/agent/Functions/GetOtherInformationFunction.cs`

### 修改的文件
- `scripts/core/agent/AICommunication.cs` - 添加角色引用管理
- `scripts/core/agent/AgentManager.cs` - 更新函数注册
- 删除 `scripts/core/agent/Functions/GetCharacterStatusFunction.cs`

## 使用方法

### 1. 基本用法
```csharp
// 获取自己的信息
var selfInfo = await aiCommunication.SendMessage("你的信息？", characterCard);

// 获取他人的信息
var otherInfo = await aiCommunication.SendMessage("告诉我char_0001的信息", characterCard);
```

### 2. 函数调用示例
```json
// 获取自己的完整信息
{
    "name": "get_self_information",
    "arguments": "{\"requested_info\": \"full\"}"
}

// 获取他人的基本信息
{
    "name": "get_other_information",
    "arguments": "{\"target_id\": \"char_0001\", \"requested_info\": \"basic\"}"
}
```

## 测试建议

### 1. 基本功能测试
- 发送"你的信息？"消息，验证`get_self_information`函数
- 发送"告诉我char_0001的信息"消息，验证`get_other_information`函数

### 2. 权限测试
- 使用不同职业的角色测试权限控制
- 验证信息显示的详细程度

### 3. 错误处理测试
- 测试无效的目标ID
- 验证错误消息的友好性

## 预期效果

修复后，AI应该能够：
- ✅ 成功调用`get_self_information`获取自己的信息
- ✅ 成功调用`get_other_information`获取他人的信息
- ✅ 根据职业权限显示不同详细程度的信息
- ✅ 提供更清晰、更准确的回复

## 注意事项

1. **向后兼容**: 现有的其他函数调用不受影响
2. **性能优化**: 函数分离后，逻辑更清晰，性能更好
3. **维护性**: 代码结构更清晰，易于维护和扩展
4. **权限控制**: 权限控制更加精确和清晰



