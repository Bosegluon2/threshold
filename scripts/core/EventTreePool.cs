using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Threshold.Core.Conditions;

namespace Threshold.Core
{
    public class EventTreePool
    {
        private Dictionary<string, EventTree> allEventTrees = new Dictionary<string, EventTree>();
        private List<EventTree> requiredEventTrees = new List<EventTree>();
        private List<EventTree> optionalEventTrees = new List<EventTree>();
        
        private IDeserializer yamlDeserializer;
        
        public EventTreePool()
        {
            yamlDeserializer = new DeserializerBuilder()
                .Build();
        }
        
        public void InitializeEventTreePool(ulong seed)
        {
            GD.Print($"=== 开始初始化事件树池，种子: {seed} ===");
            
            allEventTrees.Clear();
            requiredEventTrees.Clear();
            optionalEventTrees.Clear();
            
            try
            {
                LoadAllEventTrees();
                SeparateRequiredAndOptionalTrees();
                GD.Print($"事件树池初始化完成，共 {allEventTrees.Count} 棵事件树");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"初始化事件树池时发生错误: {ex.Message}");
                GD.PrintErr($"堆栈跟踪: {ex.StackTrace}");
            }
        }
        
        private void LoadAllEventTrees()
        {
            var eventsDirectory = "data/events/";
            
            if (!DirAccess.DirExistsAbsolute(eventsDirectory))
            {
                GD.PrintErr($"事件目录不存在: {eventsDirectory}");
                return;
            }
            
            LoadEventTreesFromDirectory(eventsDirectory, "main");
            LoadEventTreesFromDirectory(eventsDirectory, "side|world|daily|character");
            
            GD.Print($"成功加载 {allEventTrees.Count} 棵事件树");
        }
        
        private void LoadEventTreesFromDirectory(string directory, string typeFilter)
        {
            try
            {
                var dir = DirAccess.Open(directory);
                if (dir == null)
                {
                    GD.PrintErr($"无法打开目录: {directory}");
                    return;
                }
                
                dir.ListDirBegin();
                var fileName = dir.GetNext();
                
                while (!string.IsNullOrEmpty(fileName))
                {
                    if (fileName.EndsWith(".yaml") || fileName.EndsWith(".yml"))
                    {
                        var filePath = Path.Combine(directory, fileName);
                        if (ShouldLoadFile(fileName, typeFilter))
                        {
                            LoadEventTreeFromFile(filePath);
                        }
                    }
                    fileName = dir.GetNext();
                }
                
                dir.ListDirEnd();
            }
            catch (Exception ex)
            {
                GD.PrintErr($"加载目录 {directory} 时发生错误: {ex.Message}");
            }
        }
        
        private bool ShouldLoadFile(string fileName, string typeFilter)
        {
            if (string.IsNullOrEmpty(typeFilter))
                return true;
                
            var types = typeFilter.Split('|');
            return types.Any(type => fileName.Contains(type));
        }
        
        private void LoadEventTreeFromFile(string filePath)
        {
            try
            {
                if (!Godot.FileAccess.FileExists(filePath))
                {
                    GD.PrintErr($"事件树文件不存在: {filePath}");
                    return;
                }
                
                var file = Godot.FileAccess.Open(filePath, Godot.FileAccess.ModeFlags.Read);
                if (file == null)
                {
                    GD.PrintErr($"无法打开文件: {filePath}");
                    return;
                }
                
                var content = file.GetAsText();
                file.Close();
                
                GD.Print($"正在加载事件树文件: {filePath}");
                GD.Print($"文件内容长度: {content.Length} 字符");
                
                var eventTreeData = yamlDeserializer.Deserialize<Dictionary<string, object>>(content);
                GD.Print($"YAML解析成功，根键: {string.Join(", ", eventTreeData.Keys)}");
                GD.Print($"event_tree 的类型: {eventTreeData["event_tree"]?.GetType().Name ?? "null"}");
                
                // 检查event_tree的具体内容
                if (eventTreeData["event_tree"] != null)
                {
                    var eventTreeObj = eventTreeData["event_tree"];
                    GD.Print($"event_tree 对象的类型: {eventTreeObj.GetType().Name}");
                    
                    if (eventTreeObj is Dictionary<string, object> dict)
                    {
                        GD.Print($"event_tree 字典的键: {string.Join(", ", dict.Keys)}");
                    }
                    else
                    {
                        GD.Print($"event_tree 不是字典类型，实际内容: {eventTreeObj}");
                    }
                }
                
                var eventTree = CreateEventTreeFromData(eventTreeData);
                
                if (eventTree != null)
                {
                    allEventTrees[eventTree.TreeId] = eventTree;
                    GD.Print($"成功加载事件树: {eventTree.TreeName} ({eventTree.TreeId})");
                }
                else
                {
                    GD.PrintErr($"创建事件树失败: {filePath}");
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"加载事件树文件 {filePath} 时发生错误: {ex.Message}");
                GD.PrintErr($"错误堆栈: {ex.StackTrace}");
            }
        }
        
        private EventTree CreateEventTreeFromData(Dictionary<string, object> data)
        {
            try
            {
                if (!data.ContainsKey("event_tree"))
                {
                    GD.PrintErr("事件树数据缺少 'event_tree' 键");
                    GD.PrintErr($"可用的键: {string.Join(", ", data.Keys)}");
                    return null;
                }
                
                // 规范化 event_tree 为 Dictionary<string, object>
                var treeData = NormalizeToStringObjectDictionary(data["event_tree"]);
                if (treeData == null)
                {
                    GD.PrintErr("事件树数据格式错误");
                    GD.PrintErr($"event_tree 的类型是: {data["event_tree"]?.GetType().Name ?? "null"}");
                    GD.PrintErr($"event_tree 的值是: {data["event_tree"]}");
                    return null;
                }
                
                var eventTree = new EventTree();
                
                eventTree.TreeId = GetStringValue(treeData, "tree_id");
                eventTree.TreeName = GetStringValue(treeData, "tree_name");
                eventTree.TreeType = ParseEventType(GetStringValue(treeData, "tree_type"));
                eventTree.Priority = GetIntValue(treeData, "priority");
                eventTree.IsRequired = GetBoolValue(treeData, "is_required");
                
                if (treeData.ContainsKey("root_event"))
                {
                    var rootEventData = NormalizeToStringObjectDictionary(treeData["root_event"]);
                    if (rootEventData != null)
                    {
                        var rootEvent = CreateEventNodeFromData(rootEventData);
                        if (rootEvent != null)
                        {
                            eventTree.RootEvent = rootEvent;
                            eventTree.AddEvent(rootEvent);
                        }
                    }
                }
                
                if (treeData.ContainsKey("events"))
                {
                    var eventsData = NormalizeToListOfObjects(treeData["events"]);
                    if (eventsData != null)
                    {
                        foreach (var eventData in eventsData)
                        {
                            var eventDict = NormalizeToStringObjectDictionary(eventData);
                            if (eventDict != null)
                            {
                                var eventNode = CreateEventNodeFromData(eventDict);
                                if (eventNode != null)
                                {
                                    eventTree.AddEvent(eventNode);
                                }
                            }
                        }
                    }
                }
                
                return eventTree;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"创建事件树时发生错误: {ex.Message}");
                GD.PrintErr($"错误堆栈: {ex.StackTrace}");
                GD.PrintErr($"数据键: {string.Join(", ", data.Keys)}");
                return null;
            }
        }
        
        private EventNode CreateEventNodeFromData(Dictionary<string, object> data)
        {
            try
            {
                var eventNode = new EventNode();
                
                eventNode.EventId = GetStringValue(data, "event_id");
                eventNode.EventName = GetStringValue(data, "event_name");
                eventNode.Description = GetStringValue(data, "description");
                eventNode.Type = ParseEventType(GetStringValue(data, "type"));
                
                eventNode.ActivationTurn = GetIntValue(data, "activation_turn");
                eventNode.ExpirationTurn = GetIntValue(data, "expiration_turn");
                eventNode.Duration = GetIntValue(data, "duration");
                eventNode.IsPersistent = GetBoolValue(data, "is_persistent");
                
                eventNode.IsEnding = GetBoolValue(data, "is_ending");
                eventNode.EndingType = GetStringValue(data, "ending_type");
                
                if (data.ContainsKey("required_tasks"))
                {
                    var requiredTasksData = NormalizeToListOfObjects(data["required_tasks"]);
                    if (requiredTasksData != null)
                    {
                        foreach (var taskData in requiredTasksData)
                        {
                            var taskDict = NormalizeToStringObjectDictionary(taskData);
                            if (taskDict != null)
                            {
                                var task = CreateTaskFromData(taskDict);
                                if (task != null)
                                {
                                    eventNode.RequiredTasks.Add(task);
                                }
                            }
                        }
                    }
                }
                
                if (data.ContainsKey("cyclic_tasks"))
                {
                    var cyclicTasksData = NormalizeToListOfObjects(data["cyclic_tasks"]);
                    if (cyclicTasksData != null)
                    {
                        foreach (var taskData in cyclicTasksData)
                        {
                            var taskDict = NormalizeToStringObjectDictionary(taskData);
                            if (taskDict != null)
                            {
                                var task = CreateTaskFromData(taskDict);
                                if (task != null)
                                {
                                    eventNode.CyclicTasks.Add(task);
                                }
                            }
                        }
                    }
                }
                
                if (data.ContainsKey("branches"))
                {
                    var branchesData = NormalizeToListOfObjects(data["branches"]);
                    if (branchesData != null)
                    {
                        foreach (var branchData in branchesData)
                        {
                            var branchDict = NormalizeToStringObjectDictionary(branchData);
                            if (branchDict != null)
                            {
                                var branch = CreateEventBranchFromData(branchDict);
                                if (branch != null)
                                {
                                    eventNode.Branches.Add(branch);
                                }
                            }
                        }
                    }
                }
                
                if (data.ContainsKey("world_effects"))
                {
                    var worldEffectsData = NormalizeToListOfObjects(data["world_effects"]);
                    if (worldEffectsData != null)
                    {
                        foreach (var effectData in worldEffectsData)
                        {
                            var effectDict = NormalizeToStringObjectDictionary(effectData);
                            if (effectDict != null)
                            {
                                var effect = CreateEffectReferenceFromData(effectDict);
                                if (effect != null)
                                {
                                    eventNode.WorldEffects.Add(effect);
                                }
                            }
                        }
                    }
                }
                
                if (data.ContainsKey("character_effects"))
                {
                    var characterEffectsData = NormalizeToListOfObjects(data["character_effects"]);
                    if (characterEffectsData != null)
                    {
                        foreach (var effectData in characterEffectsData)
                        {
                            var effectDict = NormalizeToStringObjectDictionary(effectData);
                            if (effectDict != null)
                            {
                                var effect = CreateEffectReferenceFromData(effectDict);
                                if (effect != null)
                                {
                                    eventNode.CharacterEffects.Add(effect);
                                }
                            }
                        }
                    }
                }
                
                return eventNode;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"创建事件节点时发生错误: {ex.Message}");
                GD.PrintErr($"错误堆栈: {ex.StackTrace}");
                GD.PrintErr($"数据键: {string.Join(", ", data.Keys)}");
                return null;
            }
        }
        
        private Task CreateTaskFromData(Dictionary<string, object> data)
        {
            try
            {
                var task = new Task();
                
                task.TaskId = GetStringValue(data, "task_id");
                task.TaskName = GetStringValue(data, "task_name");
                task.Description = GetStringValue(data, "description");
                task.Type = ParseTaskType(GetStringValue(data, "task_type"));
                task.RequiredProgress = GetIntValue(data, "required_progress");
                task.VoteWeight = GetFloatValue(data, "vote_weight");
                task.Cooldown = GetIntValue(data, "cooldown");
                
                if (data.ContainsKey("objectives"))
                {
                    var objectivesData = NormalizeToListOfObjects(data["objectives"]);
                    if (objectivesData != null)
                    {
                        foreach (var objectiveData in objectivesData)
                        {
                            var objectiveDict = NormalizeToStringObjectDictionary(objectiveData);
                            if (objectiveDict != null)
                            {
                                var objective = CreateTaskObjectiveFromData(objectiveDict);
                                if (objective != null)
                                {
                                    task.Objectives.Add(objective);
                                }
                            }
                        }
                    }
                }
                
                if (data.ContainsKey("rewards"))
                {
                    var rewardsData = NormalizeToListOfObjects(data["rewards"]);
                    if (rewardsData != null)
                    {
                        foreach (var rewardData in rewardsData)
                        {
                            var rewardDict = NormalizeToStringObjectDictionary(rewardData);
                            if (rewardDict != null)
                            {
                                var reward = CreateRewardFromData(rewardDict);
                                if (reward != null)
                                {
                                    task.Rewards.Add(reward);
                                }
                            }
                        }
                    }
                }
                
                return task;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"创建任务时发生错误: {ex.Message}");
                GD.PrintErr($"错误堆栈: {ex.StackTrace}");
                GD.PrintErr($"数据键: {string.Join(", ", data.Keys)}");
                return null;
            }
        }
        
        private TaskObjective CreateTaskObjectiveFromData(Dictionary<string, object> data)
        {
            try
            {
                var objective = new TaskObjective();
                
                objective.Type = GetStringValue(data, "type");
                objective.Target = GetStringValue(data, "target");
                objective.Required = GetIntValue(data, "required");
                
                return objective;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"创建任务目标时发生错误: {ex.Message}");
                GD.PrintErr($"错误堆栈: {ex.StackTrace}");
                GD.PrintErr($"数据键: {string.Join(", ", data.Keys)}");
                return null;
            }
        }
        
        private Reward CreateRewardFromData(Dictionary<string, object> data)
        {
            try
            {
                var reward = new Reward();
                
                reward.Type = GetStringValue(data, "type");
                reward.Target = GetStringValue(data, "target");
                reward.Value = data.ContainsKey("value") ? data["value"] : null;
                reward.Quantity = GetIntValue(data, "quantity");
                
                return reward;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"创建奖励时发生错误: {ex.Message}");
                GD.PrintErr($"错误堆栈: {ex.StackTrace}");
                GD.PrintErr($"数据键: {string.Join(", ", data.Keys)}");
                return null;
            }
        }
        
        private EventBranch CreateEventBranchFromData(Dictionary<string, object> data)
        {
            try
            {
                var branch = new EventBranch();
                
                branch.BranchId = GetStringValue(data, "branch_id");
                branch.BranchName = GetStringValue(data, "branch_name");
                branch.Probability = GetFloatValue(data, "probability");
                
                // 设置分支类型
                var typeStr = GetStringValue(data, "type").ToLower();
                branch.Type = typeStr switch
                {
                    "manual" => BranchType.Manual,
                    "auto" => BranchType.Auto,
                    _ => BranchType.Manual
                };
                
                // 设置enable条件
                if (data.ContainsKey("enable_conditions"))
                {
                    var enableConditionsData = NormalizeToListOfObjects(data["enable_conditions"]);
                    if (enableConditionsData != null)
                    {
                        branch.EnableConditions = ConditionLoader.Instance.CreateConditionsFromData(enableConditionsData);
                    }
                }
                
                if (data.ContainsKey("next_events"))
                {
                    var nextEventsData = NormalizeToListOfObjects(data["next_events"]);
                    if (nextEventsData != null)
                    {
                        foreach (var nextEventData in nextEventsData)
                        {
                            var nextEventDict = NormalizeToStringObjectDictionary(nextEventData);
                            if (nextEventDict != null)
                            {
                                var nextEvent = CreateEventReferenceFromData(nextEventDict);
                                if (nextEvent != null)
                                {
                                    branch.NextEvents.Add(nextEvent);
                                }
                            }
                        }
                    }
                }
                
                return branch;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"创建事件分支时发生错误: {ex.Message}");
                GD.PrintErr($"错误堆栈: {ex.StackTrace}");
                GD.PrintErr($"数据键: {string.Join(", ", data.Keys)}");
                return null;
            }
        }
        
        private EventReference CreateEventReferenceFromData(Dictionary<string, object> data)
        {
            try
            {
                var eventRef = new EventReference();
                
                eventRef.EventId = GetStringValue(data, "event_id");
                eventRef.EventName = GetStringValue(data, "event_name");
                eventRef.ActivationDelay = GetIntValue(data, "activation_delay");
                
                return eventRef;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"创建事件引用时发生错误: {ex.Message}");
                GD.PrintErr($"错误堆栈: {ex.StackTrace}");
                GD.PrintErr($"数据键: {string.Join(", ", data.Keys)}");
                return null;
            }
        }
        
        private EffectReference CreateEffectReferenceFromData(Dictionary<string, object> data)
        {
            try
            {
                var effect = new EffectReference();
                
                effect.EffectId = GetStringValue(data, "effect_id");
                effect.EffectType = GetStringValue(data, "effect_type");
                
                if (data.ContainsKey("parameters"))
                {
                    var parametersData = NormalizeToStringObjectDictionary(data["parameters"]);
                    if (parametersData != null)
                    {
                        effect.Parameters = new Dictionary<string, object>(parametersData);
                    }
                }
                
                return effect;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"创建效果引用时发生错误: {ex.Message}");
                GD.PrintErr($"错误堆栈: {ex.StackTrace}");
                GD.PrintErr($"数据键: {string.Join(", ", data.Keys)}");
                return null;
            }
        }
        
        private void SeparateRequiredAndOptionalTrees()
        {
            requiredEventTrees = allEventTrees.Values
                .Where(t => t.IsRequired)
                .ToList();
                
            optionalEventTrees = allEventTrees.Values
                .Where(t => !t.IsRequired)
                .ToList();
                
            GD.Print($"必需事件树: {requiredEventTrees.Count} 棵");
            GD.Print($"可选事件树: {optionalEventTrees.Count} 棵");
        }
        
        public Dictionary<string, EventTree> GetAllEventTrees()
        {
            return new Dictionary<string, EventTree>(allEventTrees);
        }
        
        public List<EventTree> GetRequiredEventTrees()
        {
            return new List<EventTree>(requiredEventTrees);
        }
        
        public List<EventTree> GetOptionalEventTrees()
        {
            return new List<EventTree>(optionalEventTrees);
        }
        
        public List<EventTree> SelectRandomOptionalTrees(int maxCount, int seed)
        {
            var random = new Random(seed);
            return optionalEventTrees
                .OrderBy(x => random.Next())
                .Take(Math.Min(maxCount, optionalEventTrees.Count))
                .ToList();
        }
        
        private string GetStringValue(Dictionary<string, object> data, string key, string defaultValue = "")
        {
            return data.ContainsKey(key) && data[key] != null ? data[key].ToString() : defaultValue;
        }
        
        private int GetIntValue(Dictionary<string, object> data, string key, int defaultValue = 0)
        {
            if (data.ContainsKey(key) && data[key] != null)
            {
                if (int.TryParse(data[key].ToString(), out var value))
                {
                    return value;
                }
            }
            return defaultValue;
        }
        
        private float GetFloatValue(Dictionary<string, object> data, string key, float defaultValue = 0f)
        {
            if (data.ContainsKey(key) && data[key] != null)
            {
                if (float.TryParse(data[key].ToString(), out var value))
                {
                    return value;
                }
            }
            return defaultValue;
        }
        
        private bool GetBoolValue(Dictionary<string, object> data, string key, bool defaultValue = false)
        {
            if (data.ContainsKey(key) && data[key] != null)
            {
                if (bool.TryParse(data[key].ToString(), out var value))
                {
                    return value;
                }
            }
            return defaultValue;
        }

        // 将任意对象规范化为 Dictionary<string, object>
        private Dictionary<string, object> NormalizeToStringObjectDictionary(object obj)
        {
            if (obj == null)
            {
                return null;
            }
            
            if (obj is Dictionary<string, object> dict)
            {
                return dict;
            }
            
            if (obj is Dictionary<object, object> objDict)
            {
                var result = new Dictionary<string, object>();
                foreach (var kv in objDict)
                {
                    var keyStr = kv.Key?.ToString() ?? string.Empty;
                    result[keyStr] = kv.Value;
                }
                return result;
            }
            
            return null;
        }
        
        // 将任意对象规范化为 List<object>
        private List<object> NormalizeToListOfObjects(object obj)
        {
            if (obj == null)
            {
                return null;
            }
            
            if (obj is List<object> list)
            {
                return list;
            }
            
            if (obj is System.Collections.IEnumerable enumerable)
            {
                var result = new List<object>();
                foreach (var item in enumerable)
                {
                    result.Add(item);
                }
                return result;
            }
            
            return null;
        }
        
        private EventType ParseEventType(string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr))
                return EventType.Side;
                
            return typeStr.ToLower() switch
            {
                "main" => EventType.Main,
                "side" => EventType.Side,
                "world" => EventType.World,
                "daily" => EventType.Daily,
                "character" => EventType.Character,
                _ => EventType.Side
            };
        }
        
        private TaskType ParseTaskType(string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr))
                return TaskType.Exploration;
                
            return typeStr.ToLower() switch
            {
                "collection" => TaskType.Collection,
                "combat" => TaskType.Combat,
                "dialogue" => TaskType.Dialogue,
                "exploration" => TaskType.Exploration,
                "production" => TaskType.Production,
                "training" => TaskType.Training,
                _ => TaskType.Exploration
            };
        }
    }
}
