# 信息分享系统说明

## 概述
本系统现在支持基于秘密程度和信任程度的信息分享决策，使用sigmoid函数来模拟真实的人际关系中的信息分享行为。

## 核心概念

### Information 类
- **Content**: 信息的具体内容
- **SecrecyLevel**: 秘密程度 (0-100)
  - 0: 完全公开
  - 20: 基本公开（如外貌、说话风格）
  - 40: 一般信息（如目标、背景）
  - 60: 内部信息（如恐惧）
  - 80: 机密信息（如秘密）
  - 100: 绝密信息

### 信息分享决策机制
使用sigmoid函数计算分享概率，具有以下特点：
- **陡峭的过渡区间**: 在信任程度和秘密程度差异小于5时，有记录的变化
- **明确的边界**: 其他情况下几乎100%拒绝或100%成功
- **考虑因素**: 信任程度 + 关系等级 vs 信息秘密程度

## 使用方法

### 1. 函数调用
```json
{
  "name": "decide_information_sharing",
  "arguments": {
    "information_type": "goals",
    "target_name": "雷克斯",
    "trust_level": 70,
    "relationship_level": 80
  }
}
```

### 2. 参数说明
- **information_type**: 要分享的信息类型
  - `appearance`: 外貌
  - `speech_style`: 说话风格
  - `goals`: 目标
  - `fears`: 恐惧
  - `secrets`: 秘密
  - `background`: 背景故事
  - `personality`: 性格
- **target_id/target_name**: 目标角色
- **trust_level**: 信任程度 (0-100)
- **relationship_level**: 关系等级 (0-100)

### 3. 返回结果
- **成功**: `true` + 决策原因
- **失败**: `false` + 决策原因

## 决策逻辑示例

### 高度信任情况
- 信任程度: 90, 关系等级: 85
- 信息秘密程度: 40 (目标)
- 结果: 几乎100%分享
- 原因: "高度信任，完全愿意分享"

### 低信任情况
- 信任程度: 20, 关系等级: 25
- 信息秘密程度: 80 (秘密)
- 结果: 几乎100%拒绝
- 原因: "信任程度极低，绝对不分享"

### 边界情况
- 信任程度: 45, 关系等级: 50
- 信息秘密程度: 50 (内部信息)
- 结果: 使用sigmoid函数计算，有记录的变化

## 系统特性

### 1. 智能信息过滤
- 根据信任程度显示不同详细程度的信息
- 支持渐进式信息揭示
- 保护角色隐私

### 2. 真实的人际关系模拟
- 基于信任和关系的动态决策
- 考虑信息的敏感程度
- 支持复杂的社交互动

### 3. 可扩展性
- 支持自定义信息类型
- 可调整秘密程度阈值
- 支持多种信任模型

## 使用场景

### 1. 角色扮演
- AI角色根据关系决定分享信息
- 模拟真实的人际交往
- 增加角色深度和真实感

### 2. 游戏机制
- 信息收集任务
- 信任建立系统
- 社交技能挑战

### 3. 故事发展
- 渐进式信息揭示
- 角色关系发展
- 剧情悬念营造

## 技术实现

### Sigmoid函数
```csharp
private double CalculateShareProbability(double trustScore, int secrecyLevel)
{
    var normalizedTrust = Mathf.Clamp(trustScore / 100.0, 0.0, 1.0);
    var normalizedSecrecy = secrecyLevel / 100.0;
    var difference = normalizedTrust - normalizedSecrecy;
    
    // 陡峭的sigmoid函数
    var steepness = 15.0;
    var sigmoid = 1.0 / (1.0 + Math.Exp(-steepness * difference));
    
    // 明确的边界
    if (difference < -0.3) return 0.01; // 几乎不可能分享
    if (difference > 0.3) return 0.99;  // 几乎肯定会分享
    
    return sigmoid;
}
```

### 信息类型映射
```csharp
private Information GetInformationByType(CharacterCard character, string informationType)
{
    switch (informationType.ToLower())
    {
        case "appearance": return character.Appearance;
        case "speech_style": return character.SpeechStyle;
        case "goals": return character.Goals;
        case "fears": return character.Fears;
        case "secrets": return character.Secrets;
        case "background": return new Information(character.BackgroundStory, 30);
        case "personality": return new Information(character.Personality, 15);
        default: return null;
    }
}
```

## 注意事项

1. **秘密程度设置**: 根据信息的敏感程度合理设置
2. **信任评估**: 考虑角色的性格和背景
3. **关系发展**: 信任程度会随着互动变化
4. **隐私保护**: 高秘密程度的信息需要高信任才能分享

## 扩展建议

1. **动态信任系统**: 信任程度随互动变化
2. **群体关系**: 支持复杂的社交网络
3. **文化因素**: 不同文化背景的信任模型
4. **时间因素**: 信任随时间衰减或增长
