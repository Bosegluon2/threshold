using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Threshold.Core.Data;

namespace Threshold.Core.Agent
{
    /// <summary>
    /// Agent加载器 - 从YAML文件加载并组装完整的Agent对象
    /// </summary>
    public static class AgentLoader
    {
        private static readonly string CHARACTERS_DIR = "./data/characters/";
        
        /// <summary>
        /// 从YAML文件加载所有Agent
        /// </summary>
        public static List<Agent> LoadAllAgents()
        {
            var agents = new List<Agent>();
            
            try
            {
                GD.Print("=== 开始从YAML文件加载Agent数据 ===");
                
                // 获取characters目录下的所有.yaml文件
                var dir = DirAccess.Open(CHARACTERS_DIR);
                if (dir == null)
                {
                    GD.PrintErr($"无法打开目录: {CHARACTERS_DIR}");
                    return agents;
                }
                
                dir.ListDirBegin();
                var fileName = dir.GetNext();
                
                while (!string.IsNullOrEmpty(fileName))
                {
                    if (fileName.EndsWith(".yaml") || fileName.EndsWith(".yml"))
                    {
                        var agent = LoadAgentFromFile(fileName);
                        if (agent != null)
                        {
                            agents.Add(agent);
                            GD.Print($"成功加载Agent: {agent.AgentName} (ID: {agent.AgentId})");
                        }
                    }
                    fileName = dir.GetNext();
                }
                
                dir.ListDirEnd();
                
                GD.Print($"Agent加载完成，共加载 {agents.Count} 个Agent");
                
                // 确保玩家Agent被加载
                if (!agents.Any(a => a.AgentId == "player"))
                {
                    GD.Print("未找到玩家Agent，将创建默认玩家Agent");
                    var playerAgent = CreateDefaultPlayerAgent();
                    if (playerAgent != null)
                    {
                        agents.Add(playerAgent);
                        GD.Print("已创建默认玩家Agent");
                    }
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"加载Agent数据时发生错误: {ex.Message}");
                GD.PrintErr($"错误位置: {ex.StackTrace}");
            }
            
            return agents;
        }
        
        /// <summary>
        /// 从单个YAML文件加载Agent
        /// </summary>
        private static Agent LoadAgentFromFile(string fileName)
        {
            try
            {
                var filePath = CHARACTERS_DIR + fileName;
                var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read);
                
                if (file == null)
                {
                    GD.PrintErr($"无法打开文件: {filePath}");
                    return null;
                }
                
                var yamlContent = file.GetAsText();
                file.Close();
                
                if (string.IsNullOrEmpty(yamlContent))
                {
                    GD.PrintErr($"文件内容为空: {filePath}");
                    return null;
                }
                
                // 使用YamlDotNet反序列化
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .Build();
                
                var characterData = deserializer.Deserialize<Dictionary<string, object>>(yamlContent);
                
                // 创建并组装Agent
                var agent = CreateAgentFromData(characterData);
                
                return agent;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"加载Agent文件 {fileName} 时发生错误: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 从数据创建完整的Agent对象
        /// </summary>
        private static Agent CreateAgentFromData(Dictionary<string, object> data)
        {
            try
            {
                // 基本属性
                var id = GetStringValue(data, "id");
                var name = GetStringValue(data, "name");
                
                // 验证必要字段
                if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(name))
                {
                    GD.PrintErr($"Agent ID或Name为空 - ID: '{id}', Name: '{name}'");
                    return null;
                }
                
                // 创建Agent
                var agent = new Agent(id, name);
                
                // 创建CharacterCard
                var character = CreateCharacterCardFromData(data);
                if (character == null)
                {
                    GD.PrintErr($"CharacterCard创建失败，跳过Agent创建");
                    return null;
                }
                
                // 设置角色到Agent
                agent.SetCharacter(character);
                
                // 加载动态数据
                LoadDynamicData(agent, data);
                
                return agent;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"从数据创建Agent时发生错误: {ex.Message}");
                GD.PrintErr($"错误堆栈: {ex.StackTrace}");
                return null;
            }
        }
        
        /// <summary>
        /// 从数据创建CharacterCard
        /// </summary>
        private static CharacterCard CreateCharacterCardFromData(Dictionary<string, object> data)
        {
            var character = new CharacterCard();
            
            // 基本信息
            character.Id = GetStringValue(data, "id");
            character.Name = GetStringValue(data, "name");
            character.Personality = GetStringValue(data, "personality");
            character.Gender = GetStringValue(data, "gender");
            character.Age = GetIntValue(data, "age");
            character.Profession = GetStringValue(data, "profession");
            character.Faction = GetStringValue(data, "faction");
            character.BackgroundStory = GetStringValue(data, "background_story");
            character.Education = GetStringValue(data, "education");
            
            // 特征
            var traits = GetListValue(data, "traits");
            if (traits != null)
            {
                GD.Print($"加载traits: {traits.Count} 个");
                foreach (var trait in traits)
                {
                    if (!string.IsNullOrEmpty(trait))
                    {
                        character.Traits.Add(trait);
                        GD.Print($"  - 添加特征: {trait}");
                    }
                }
            }
            else
            {
                GD.Print("未找到traits字段或traits为空");
            }
            
            // 信息类属性（带secrecy值）
            character.Appearance = CreateInformationFromData(data, "appearance", "appearance_secrecy");
            character.SpeechStyle = CreateInformationFromData(data, "speech_style", "speech_style_secrecy");
            character.Goals = CreateInformationFromData(data, "goals", "goals_secrecy");
            character.Fears = CreateInformationFromData(data, "fears", "fears_secrecy");
            character.Secrets = CreateInformationFromData(data, "secrets", "secrets_secrecy");
            
            // 技能
            var skills = GetListValue(data, "skills");
            if (skills != null)
            {
                foreach (var skill in skills)
                {
                    character.Skills.Add(skill);
                }
            }
            
            // 基础状态（最大值）
            character.MaxHealth = GetIntValue(data, "max_health");
            character.MaxEnergy = GetIntValue(data, "max_energy");
            character.MaxThirst = GetIntValue(data, "max_thirst");
            character.MaxSatiety = GetIntValue(data, "max_satiety");
            character.ThirstConsumptionRate = GetIntValue(data, "thirst_consumption_rate");
            character.SatietyConsumptionRate = GetIntValue(data, "satiety_consumption_rate");
            
            // WARPED基础属性
            var baseWarpedInfo = new WarpedInfo();
            baseWarpedInfo.Warfare = GetIntValue(data, "base_warfare");
            baseWarpedInfo.Adaptability = GetIntValue(data, "base_adaptability");
            baseWarpedInfo.Reasoning = GetIntValue(data, "base_reasoning");
            baseWarpedInfo.Perception = GetIntValue(data, "base_perception");
            baseWarpedInfo.Endurance = GetIntValue(data, "base_endurance");
            baseWarpedInfo.Dexterity = GetIntValue(data, "base_dexterity");
            character.BaseWarpedInfo = baseWarpedInfo;
            
            // 知识等级
            var knowledgeLevelStr = GetStringValue(data, "knowledge_level");
            character.KnowledgeLevel = ParseKnowledgeLevel(knowledgeLevelStr);
            
            // 爱好与兴趣
            var likes = GetListValue(data, "likes");
            if (likes != null)
            {
                foreach (var like in likes)
                {
                    character.Likes.Add(like);
                }
            }
            
            var dislikes = GetListValue(data, "dislikes");
            if (dislikes != null)
            {
                foreach (var dislike in dislikes)
                {
                    character.Dislikes.Add(dislike);
                }
            }
            
            // 食物偏好
            var favoriteFoods = GetListValue(data, "favorite_foods");
            if (favoriteFoods != null)
            {
                foreach (var food in favoriteFoods)
                {
                    if (food != null && food.ToString() != "null")
                    {
                        character.FavoriteFoods.Add(food.ToString());
                    }
                }
            }
            
            var dislikedFoods = GetListValue(data, "disliked_foods");
            if (dislikedFoods != null)
            {
                foreach (var food in dislikedFoods)
                {
                    if (food != null && food.ToString() != "null")
                    {
                        character.DislikedFoods.Add(food.ToString());
                    }
                }
            }
            
            var dietaryRestrictions = GetListValue(data, "dietary_restrictions");
            if (dietaryRestrictions != null)
            {
                foreach (var restriction in dietaryRestrictions)
                {
                    if (restriction != null && restriction.ToString() != "null")
                    {
                        character.DietaryRestrictions.Add(restriction.ToString());
                    }
                }
            }
            
            // 生活习惯
            character.WakeUpTime = GetIntValue(data, "wake_up_time");
            character.SleepTime = GetIntValue(data, "sleep_time");
            character.PersonalHygiene = GetStringValue(data, "personal_hygiene");
            
            // 情感与心理状态
            character.Triggers = GetStringValue(data, "triggers");
            character.CopingMechanisms = GetStringValue(data, "coping_mechanisms");
            character.ComfortMethod = GetStringValue(data, "comfort_method");
            
            // 价值观与信仰
            var coreValues = GetListValue(data, "core_values");
            if (coreValues != null)
            {
                foreach (var value in coreValues)
                {
                    character.CoreValues.Add(value);
                }
            }
            
            character.MoralCode = GetStringValue(data, "moral_code");
            character.LifePhilosophy = GetStringValue(data, "life_philosophy");
            
            // OCEAN评分
            var oceanInfo = new OceanInfo();
            oceanInfo.Openness = GetIntValue(data, "openness");
            oceanInfo.Conscientiousness = GetIntValue(data, "conscientiousness");
            oceanInfo.Extraversion = GetIntValue(data, "extraversion");
            oceanInfo.Agreeableness = GetIntValue(data, "agreeableness");
            oceanInfo.Neuroticism = GetIntValue(data, "neuroticism");
            character.OceanInfo = oceanInfo;
            
            // 生活细节
            character.PersonalStyle = GetStringValue(data, "personal_style");
            
            // 时间戳
            character.CreatedDate = ParseDateTime(GetStringValue(data, "created_date"));
            character.LastUpdated = ParseDateTime(GetStringValue(data, "last_updated"));
            
            // 绑定信息
            character.BoundAgentId = GetStringValue(data, "bound_agent_id");
            character.IsBound = GetBoolValue(data, "is_bound");
            
            GD.Print($"CharacterCard {character.Name} 创建完成，包含 {character.Traits.Count} 个特征，{character.Skills.Count} 个技能");
            
            return character;
        }

        /// <summary>
        /// 加载动态数据到Agent
        /// </summary>
        private static void LoadDynamicData(Agent agent, Dictionary<string, object> data)
        {
            // 加载初始位置
            var initialPlaceId = GetStringValue(data, "initial_place");
            GD.Print($"Agent {agent.AgentName} 的initial_place字段值: '{initialPlaceId}'");

            if (!string.IsNullOrEmpty(initialPlaceId))
            {
                // 检查Library是否可用
                if (Library.Instance == null)
                {
                    GD.PrintErr("Library.Instance为null，无法获取位置信息");
                    return;
                }

                GD.Print($"尝试从Library获取位置: {initialPlaceId}");
                var place = Library.Instance.GetPlace(initialPlaceId);

                if (place != null)
                {
                    agent.CurrentPlace = place;
                    GD.Print($"Agent {agent.AgentName} 已设置初始位置: {place.Name}");
                }
                else
                {
                    GD.PrintErr($"未找到初始位置: {initialPlaceId}");
                    GD.Print($"Library中可用的位置数量: {Library.Instance.GetAllPlaces().Count}");
                    var allPlaces = Library.Instance.GetAllPlaces();
                    foreach (var p in allPlaces)
                    {
                        GD.Print($"  - {p.Id}: {p.Name}");
                    }
                }
            }
            else
            {
                GD.Print($"Agent {agent.AgentName} 没有设置initial_place字段");
            }

            // 加载当前状态
            var currentStatus = GetListValue(data, "current_status");
            if (currentStatus != null)
            {
                foreach (var statusId in currentStatus)
                {
                    // 这里需要实现状态加载逻辑
                }
            }

            // 加载记忆
            var memories = GetListValue(data, "memories");
            if (memories != null)
            {
                foreach (var memory in memories)
                {
                    agent.AddMemory(memory);
                }
            }

            // 加载重要事件
            var importantEvents = GetListValue(data, "important_events");
            if (importantEvents != null)
            {
                foreach (var importantEvent in importantEvents)
                {
                    agent.AddMemory(importantEvent, "event");
                }
            }

            // 加载个人秘密
            var personalSecrets = GetListValue(data, "personal_secrets");
            if (personalSecrets != null)
            {
                foreach (var secret in personalSecrets)
                {
                    agent.AddMemory(secret);
                }
            }
            // 加载投票权重
            agent.SetVoteWeight(GetFloatValue(data, "vote_weight"));
            // 加载饥饿度消耗
            agent.SetThirstConsumptionRate(GetIntValue(data, "thirst_consumption_rate"));
            agent.SetSatietyConsumptionRate(GetIntValue(data, "satiety_consumption_rate"));
        }
        
        // 辅助方法
        private static string GetStringValue(Dictionary<string, object> data, string key)
        {
            return data.ContainsKey(key) && data[key] != null ? data[key].ToString() : "";
        }
        
        private static int GetIntValue(Dictionary<string, object> data, string key)
        {
            if (data.ContainsKey(key) && data[key] != null)
            {
                if (int.TryParse(data[key].ToString(), out var value))
                {
                    return value;
                }
            }
            return 0;
        }
        
        private static bool GetBoolValue(Dictionary<string, object> data, string key)
        {
            if (data.ContainsKey(key) && data[key] != null)
            {
                if (bool.TryParse(data[key].ToString(), out var value))
                {
                    return value;
                }
            }
            return false;
        }
        
        private static List<string> GetListValue(Dictionary<string, object> data, string key)
        {
            try
            {
                if (!data.ContainsKey(key))
                {
                    return null;
                }
                
                var value = data[key];
                if (value == null)
                {
                    return null;
                }
                
                // 处理List<object>类型
                if (value is List<object> list)
                {
                    var result = new List<string>();
                    foreach (var item in list)
                    {
                        if (item != null)
                        {
                            result.Add(item.ToString());
                        }
                    }
                    return result;
                }
                
                // 处理其他可能的数组类型
                if (value is System.Collections.IEnumerable enumerable)
                {
                    var result = new List<string>();
                    foreach (var item in enumerable)
                    {
                        if (item != null)
                        {
                            result.Add(item.ToString());
                        }
                    }
                    return result;
                }
                
                // 如果不是数组类型，返回null
                return null;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"GetListValue处理字段 '{key}' 时发生错误: {ex.Message}");
                return null;
            }
        }
        private static float GetFloatValue(Dictionary<string, object> data, string key)
        {
            if (data.ContainsKey(key) && data[key] != null)
            {
                if (float.TryParse(data[key].ToString(), out var value))
                {
                    return value;
                }
            }
            return 1.0f;
        }
        /// <summary>
        /// 从数据创建Information对象
        /// </summary>
        private static Information CreateInformationFromData(Dictionary<string, object> data, string contentKey, string secrecyKey)
        {
            try
            {
                var content = GetStringValue(data, contentKey);
                var secrecy = GetIntValue(data, secrecyKey);
                return new Information(content, secrecy);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"CreateInformationFromData处理字段 '{contentKey}' 或 '{secrecyKey}' 时发生错误: {ex.Message}");
                return new Information("", 50); // 返回默认值
            }
        }
        
        /// <summary>
        /// 解析知识等级
        /// </summary>
        private static CharacterCard.Knowledge ParseKnowledgeLevel(string levelStr)
        {
            if (string.IsNullOrEmpty(levelStr)) return CharacterCard.Knowledge.None;
            
            return levelStr.ToLower() switch
            {
                "none" => CharacterCard.Knowledge.None,
                "basic" => CharacterCard.Knowledge.Basic,
                "intermediate" => CharacterCard.Knowledge.Intermediate,
                "advanced" => CharacterCard.Knowledge.Advanced,
                "expert" => CharacterCard.Knowledge.Expert,
                _ => CharacterCard.Knowledge.None
            };
        }
        
        /// <summary>
        /// 解析日期时间
        /// </summary>
        private static DateTime ParseDateTime(string dateTimeStr)
        {
            if (string.IsNullOrEmpty(dateTimeStr)) return DateTime.Now;
            
            if (DateTime.TryParse(dateTimeStr, out var result))
            {
                return result;
            }
            
            return DateTime.Now;
        }
        
        /// <summary>
        /// 创建默认玩家Agent
        /// </summary>
        private static Agent CreateDefaultPlayerAgent()
        {
            try
            {
                var agent = new Agent("player", "探索者查理");
                
                // 创建默认角色卡
                var character = new CharacterCard();
                character.Id =  "player";
                character.Name = "探索者查理";
                character.Personality = "好奇、勇敢、善良、富有同情心";
                character.Gender = "男";
                character.Age = 28;
                character.Profession = "探索者";
                character.Faction = "自由探索者协会";
                character.BackgroundStory = "查理是一名来自现代世界的探索者，意外穿越到了这个魔法世界。他拥有丰富的现代知识和探索经验，对这个世界充满好奇，希望能够帮助这里的居民，同时找到回到原来世界的方法。";
                
                agent.SetCharacter(character);
                
                GD.Print("默认玩家Agent创建成功");
                return agent;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"创建默认玩家Agent时发生错误: {ex.Message}");
                return null;
            }
        }
    }
}
