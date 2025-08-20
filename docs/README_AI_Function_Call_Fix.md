# AI函数调用修复说明

## 修复的问题

### 1. AI不知道当前用户身份
- **问题**: AI调用`get_character_status`函数时，`requester_id`为`"player"`，但系统中不存在这个ID
- **原因**: AI通信模块没有向AI传递当前用户的身份信息
- **影响**: 函数调用失败，无法获取角色状态信息

### 2. 函数参数验证过于严格
- **问题**: `GetCharacterStatusFunction`要求必须提供`requester_id`，但AI不知道应该使用什么ID
- **原因**: 函数设计时没有考虑AI的上下文信息
- **影响**: 即使AI想获取自己的信息，也会因为缺少ID而失败

## 修复方案

### 1. 增强系统消息
在AI通信模块的`BuildMessages`方法中，向AI提供完整的当前用户身份信息：

```csharp
var systemContent = characterCard.GetRoleplayPrompt() + 
    $"\n\n【重要提示】\n" +
    $"当前用户身份信息：\n" +
    $"- 角色ID: {characterCard.Id}\n" +
    $"- 角色名称: {characterCard.Name}\n" +
    $"- 职业: {characterCard.Profession}\n" +
    $"- 派别: {characterCard.Faction}\n\n" +
    $"当需要调用函数获取角色信息时，请使用以下参数：\n" +
    $"- requester_id: \"{characterCard.Id}\" (你的角色ID)\n" +
    $"- 如果不指定target_id或target_name，默认查看自己的信息\n" +
    $"- 可以请求的信息类型：basic, health, combat, skills, relationships, full";
```

### 2. 改进函数参数说明
更新`GetCharacterStatusFunction`的参数描述，明确`requester_id`是可选的：

```csharp
new FunctionParameter("requester_id", "string", "请求者角色ID（可选，如果不提供会从当前Agent推断）")
```

### 3. 智能身份推断
在`GetCharacterStatusFunction`中添加逻辑，当没有提供`requester_id`时，尝试从当前上下文推断：

```csharp
// 如果没有提供requester_id，尝试从当前上下文推断
CharacterCard requester = null;
if (!string.IsNullOrEmpty(requesterId))
{
    requester = AgentManager.Instance.GetCharacterById(requesterId);
    if (requester == null)
    {
        GD.PrintErr($"指定的请求者ID不存在: {requesterId}");
        return new FunctionResult(Name, "请求者不存在", false, "无效的请求者ID");
    }
}
else
{
    // 尝试从当前活跃的Agent获取角色信息
    var currentAgent = GetCurrentActiveAgent();
    if (currentAgent != null && currentAgent.Character != null)
    {
        requester = currentAgent.Character;
        GD.Print($"从当前Agent推断请求者: {requester.Name} (ID: {requester.Id})");
    }
    else
    {
        GD.PrintErr("无法推断请求者身份，且未提供requester_id");
        return new FunctionResult(Name, "缺少请求者信息", false, "请提供requester_id或确保有活跃的Agent");
    }
}
```

## 修复后的工作流程

### 1. 用户发送消息
用户向AI发送消息，如"你的信息？"

### 2. AI接收增强的系统消息
AI收到包含当前用户身份信息的系统消息，知道：
- 自己的角色ID
- 如何正确调用函数
- 可用的参数选项

### 3. AI正确调用函数
AI使用正确的`requester_id`调用`get_character_status`函数：
```json
{
    "name": "get_character_status",
    "arguments": "{\"requested_info\": \"full\", \"requester_id\": \"角色实际ID\"}"
}
```

### 4. 函数成功执行
函数验证请求者身份，返回相应的角色状态信息

## 测试建议

### 1. 基本功能测试
- 发送"你的信息？"消息
- 检查AI是否正确调用函数
- 验证返回的角色信息

### 2. 权限测试
- 测试不同职业角色的权限
- 验证信息显示的详细程度
- 检查隐私保护是否正常

### 3. 错误处理测试
- 测试无效的角色ID
- 验证错误消息的友好性
- 检查系统稳定性

## 注意事项

1. **系统消息长度**: 增强的系统消息会增加token消耗，但提高了AI的准确性
2. **角色ID一致性**: 确保角色ID在系统中是唯一的
3. **权限控制**: 函数仍然会验证权限，确保信息安全
4. **向后兼容**: 现有的函数调用方式仍然有效

## 预期效果

修复后，AI应该能够：
- 正确识别自己的身份
- 成功调用`get_character_status`函数
- 获取完整的角色状态信息
- 提供更准确的回复

