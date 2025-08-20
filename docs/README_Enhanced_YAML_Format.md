# 增强角色YAML文件格式说明

## 概述

CharacterLoader 现在支持加载和保存包含丰富角色深度属性的YAML文件。这些新属性让AI能够更好地理解和扮演角色，提供更加真实和沉浸式的角色扮演体验。

## 基本结构

每个角色YAML文件包含以下主要部分：

### 1. 基础信息 (Basic Information)
```yaml
id: "unique_character_id"
name: "角色名称"
personality: "性格描述"
gender: "性别"
age: 年龄
profession: "职业"
faction: "派别"
background_story: "背景故事"
```

### 2. 核心属性 (Core Attributes)
```yaml
# 外观和说话风格
appearance: "外貌描述"
appearance_secrecy: 20  # 保密等级
speech_style: "说话风格"
speech_style_secrecy: 10

# 目标和秘密
goals: "角色目标"
goals_secrecy: 40
fears: "恐惧"
fears_secrecy: 60
secrets: "秘密"
secrets_secrecy: 80
```

### 3. 技能和状态 (Skills & Status)
```yaml
skills:
  - "技能1"
  - "技能2"
  - "技能3"

status:
  - "状态1"
  - "状态2"
```

### 4. 数值属性 (Numerical Attributes)
```yaml
max_health: 100
current_health: 100
max_energy: 100
current_energy: 100
warfare: 30
adaptability: 60
reasoning: 70
perception: 75
endurance: 50
dexterity: 65
knowledge_level: "Intermediate"  # None, Basic, Intermediate, Advanced, Expert
```

## 新增角色深度属性

### 5. 爱好与兴趣 (Hobbies & Interests)
```yaml
hobbies:
  - "爱好1"
  - "爱好2"
  - "爱好3"

interests:
  - "兴趣领域1"
  - "兴趣领域2"

dislikes:
  - "讨厌的事物1"
  - "讨厌的事物2"

favorite_color: "最喜欢的颜色"
favorite_season: "最喜欢的季节"
favorite_weather: "最喜欢的天气"
```

### 6. 食物偏好 (Food Preferences)
```yaml
favorite_foods:
  - "喜欢的食物1"
  - "喜欢的食物2"

disliked_foods:
  - "讨厌的食物1"
  - "讨厌的食物2"

dietary_restrictions: "饮食限制"
cooking_skill: "烹饪技能水平"
favorite_drink: "最喜欢的饮品"
```

### 7. 生活习惯 (Lifestyle)
```yaml
sleep_schedule: "作息时间"
morning_routine: "晨间习惯"
evening_routine: "晚间习惯"
personal_hygiene: "个人卫生习惯"
organization_style: "整理风格"
punctuality: "守时程度"
```

### 8. 情感与心理状态 (Emotional & Psychological)
```yaml
current_mood: "当前心情"
emotional_state: "情感状态"
stress_level: "压力水平"
coping_mechanisms: "应对机制"
triggers: "触发点"
comfort_items: "安慰物品/活动"
```

### 9. 价值观与信仰 (Values & Beliefs)
```yaml
core_values:
  - "核心价值观1"
  - "核心价值观2"

moral_code: "道德准则"
religious_beliefs: "宗教信仰"
political_views: "政治观点"
life_philosophy: "人生哲学"
justice_sense: "正义感"
```

### 10. 社交偏好 (Social Preferences)
```yaml
social_energy: "社交能量"
communication_style: "沟通风格"
conflict_resolution: "冲突解决方式"
leadership_style: "领导风格"
teamwork_preference: "团队合作偏好"
social_boundaries: "社交边界"
```

### 11. 个人特质 (Personal Traits)
```yaml
sense_of_humor: "幽默感"
creativity: "创造力"
personal_adaptability: "个人适应性"
perfectionism: "完美主义倾向"
risk_tolerance: "风险承受度"
decision_making: "决策方式"
```

### 12. 生活细节 (Life Details)
```yaml
living_space: "居住环境描述"
personal_style: "个人风格"
pet_preference: "宠物偏好"
travel_style: "旅行风格"
entertainment: "娱乐偏好"
learning_style: "学习方式"
```

### 13. 系统信息 (System Information)
```yaml
created_date: "2024-01-01T00:00:00"
last_updated: "2024-01-01T00:00:00"
room_id: ""
bound_agent_id: ""
is_bound: false
```

### 14. 记忆系统 (Memory System)
```yaml
memories: []
important_events: []
personal_secrets: []
```

## 使用示例

### 创建简单角色
```yaml
id: "simple_char"
name: "简单角色"
personality: "性格描述"
gender: "男"
age: 25
profession: "职业"
faction: "派别"
background_story: "背景故事"

# 只设置必要的属性
skills:
  - "基础技能"
status:
  - "健康"
```

### 创建完整角色
参考 `data/characters/example_enhanced_character.yaml` 文件，该文件展示了如何设置所有可用的属性。

## 加载和保存

### 自动加载
CharacterLoader 会自动检测并加载YAML文件中存在的属性。如果某个属性不存在，会使用默认值。

### 保存角色
```csharp
// 保存角色到YAML文件
bool success = CharacterLoader.SaveCharacterToFile(character);
```

### 加载角色
```csharp
// 从YAML文件加载所有角色
List<CharacterCard> characters = CharacterLoader.LoadAllCharacters();
```

## 最佳实践

### 1. 属性设置建议
- **重要角色**: 设置完整的属性，包括所有深度属性
- **次要角色**: 只设置核心属性，其他使用默认值
- **NPC角色**: 根据角色重要性决定详细程度

### 2. 属性值建议
- **爱好和兴趣**: 3-5个，避免过多
- **食物偏好**: 2-4个喜欢的，1-3个讨厌的
- **价值观**: 3-5个核心价值观
- **描述性文本**: 保持简洁明了，避免过长

### 3. 文件组织
- 每个角色一个YAML文件
- 文件名使用角色ID
- 保持YAML格式的整洁和可读性

## 兼容性

- 新增属性完全向后兼容
- 旧的角色文件仍然可以正常加载
- 缺失的属性会使用默认值
- 保存时会自动包含所有已设置的属性

## 性能考虑

- 属性越多，加载和保存时间越长
- 建议为重要角色设置完整属性
- 次要角色可以只设置核心属性
- 内存使用会随着属性数量增加

通过这些增强功能，你的角色卡现在可以包含丰富的深度信息，让AI能够更好地理解和扮演角色，提供更加真实和沉浸式的游戏体验。
