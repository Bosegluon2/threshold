# CharacterCard 角色卡增强功能说明

## 概述

CharacterCard 类已经大幅增强，新增了大量角色深度相关的属性，让AI能更好地理解和扮演角色，提供更加丰富和真实的角色扮演体验。

## 新增属性分类

### 1. 爱好与兴趣 (Hobbies & Interests)
- `Hobbies`: 角色的爱好列表
- `Interests`: 兴趣领域
- `Dislikes`: 讨厌的事物
- `FavoriteColor`: 最喜欢的颜色
- `FavoriteSeason`: 最喜欢的季节
- `FavoriteWeather`: 最喜欢的天气

### 2. 食物偏好 (Food Preferences)
- `FavoriteFoods`: 喜欢的食物列表
- `DislikedFoods`: 讨厌的食物列表
- `DietaryRestrictions`: 饮食限制（素食、过敏等）
- `CookingSkill`: 烹饪技能水平
- `FavoriteDrink`: 最喜欢的饮品

### 3. 生活习惯 (Lifestyle)
- `SleepSchedule`: 作息时间
- `MorningRoutine`: 晨间习惯
- `EveningRoutine`: 晚间习惯
- `PersonalHygiene`: 个人卫生习惯
- `OrganizationStyle`: 整理风格（整洁/混乱）
- `Punctuality`: 守时程度

### 4. 情感与心理状态 (Emotional & Psychological)
- `CurrentMood`: 当前心情
- `EmotionalState`: 情感状态
- `StressLevel`: 压力水平
- `CopingMechanisms`: 应对机制
- `Triggers`: 触发点（什么会让他们生气/难过等）
- `ComfortItems`: 安慰物品/活动

### 5. 价值观与信仰 (Values & Beliefs)
- `CoreValues`: 核心价值观列表
- `MoralCode`: 道德准则
- `ReligiousBeliefs`: 宗教信仰
- `PoliticalViews`: 政治观点
- `LifePhilosophy`: 人生哲学
- `JusticeSense`: 正义感

### 6. 社交偏好 (Social Preferences)
- `SocialEnergy`: 社交能量（内向/外向）
- `CommunicationStyle`: 沟通风格
- `ConflictResolution`: 冲突解决方式
- `LeadershipStyle`: 领导风格
- `TeamworkPreference`: 团队合作偏好
- `SocialBoundaries`: 社交边界

### 7. 个人特质 (Personal Traits)
- `SenseOfHumor`: 幽默感
- `Creativity`: 创造力
- `PersonalAdaptability`: 个人适应性（性格层面）
- `Perfectionism`: 完美主义倾向
- `RiskTolerance`: 风险承受度
- `DecisionMaking`: 决策方式

### 8. 生活细节 (Life Details)
- `LivingSpace`: 居住环境描述
- `PersonalStyle`: 个人风格（穿着、装饰等）
- `PetPreference`: 宠物偏好
- `TravelStyle`: 旅行风格
- `Entertainment`: 娱乐偏好
- `LearningStyle`: 学习方式

## 新增方法

### 设置方法
- `SetHobbies(params string[] hobbies)`: 设置爱好
- `SetFoodPreferences(string[] favoriteFoods, string[] dislikedFoods, string dietaryRestrictions)`: 设置食物偏好
- `SetCoreValues(params string[] values)`: 设置核心价值观
- `UpdateEmotionalState(string mood, string emotionalState, string stressLevel)`: 更新情感状态
- `SetLifestyle(string sleepSchedule, string morningRoutine, string eveningRoutine, string organizationStyle, string punctuality)`: 设置生活习惯
- `SetSocialPreferences(string socialEnergy, string communicationStyle, string conflictResolution, string leadershipStyle)`: 设置社交偏好

### 添加方法
- `AddInterest(string interest)`: 添加兴趣领域
- `AddDislike(string dislike)`: 添加讨厌的事物

### 查询方法
- `GetCharacterDepthSummary()`: 获取角色深度摘要
- `HasPreference(string preferenceType, string value)`: 检查是否有特定偏好
- `GetAttitudeTowards(string thing)`: 获取对特定事物的态度

## 使用示例

### 创建角色卡
```csharp
// 使用静态方法创建示例角色
var character = CharacterCard.CreateExampleCharacter();

// 或者手动创建并设置属性
var myCharacter = new CharacterCard("角色名", "性格", "性别", 年龄, "职业", "派别", "背景故事");
myCharacter.SetHobbies("阅读", "音乐", "旅行");
myCharacter.SetFoodPreferences(
    new[] { "咖啡", "巧克力" },
    new[] { "苦瓜", "香菜" },
    "无特殊限制"
);
myCharacter.SetCoreValues("诚实", "勇敢", "善良");
```

### 动态更新角色状态
```csharp
// 更新情感状态
character.UpdateEmotionalState("兴奋", "积极", "无压力");

// 添加新的兴趣
character.AddInterest("摄影");

// 添加讨厌的事物
character.AddDislike("噪音");
```

### 查询角色信息
```csharp
// 获取完整描述（包含所有新增属性）
string fullDescription = character.GetFullDescription();

// 获取角色深度摘要
string depthSummary = character.GetCharacterDepthSummary();

// 检查特定偏好
bool likesCoffee = character.HasPreference("food", "咖啡");

// 获取对特定事物的态度
string attitude = character.GetAttitudeTowards("音乐"); // 返回"热爱"
```

## 角色扮演提示词增强

`GetRoleplayPrompt()` 方法现在会包含所有新增的属性信息，让AI能够：

1. **更好地理解角色性格**: 通过爱好、兴趣、价值观等了解角色的内在特质
2. **提供更真实的反应**: 基于食物偏好、生活习惯等做出符合角色设定的回应
3. **展现情感深度**: 根据当前心情、压力水平等调整对话风格
4. **体现个人特色**: 通过说话风格、幽默感等展现角色的独特性

## 注意事项

1. **性能考虑**: 新增的属性较多，在序列化/反序列化时要注意性能
2. **数据完整性**: 建议为重要角色设置完整的属性，次要角色可以只设置关键属性
3. **动态更新**: 角色的情感状态、心情等可以在游戏过程中动态更新
4. **兼容性**: 新增属性不会影响现有的角色卡数据

## 未来扩展

可以考虑添加：
- 角色成长系统（技能、属性随时间变化）
- 情感记忆系统（记住重要情感事件）
- 社交网络分析（分析角色间的关系复杂度）
- 个性化AI行为模式（基于角色特质调整AI行为）

通过这些增强功能，CharacterCard 现在能够支持更加丰富和真实的角色扮演体验，让AI能够更好地理解和扮演角色，提供更加沉浸式的游戏体验。
