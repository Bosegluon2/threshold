using Godot;
using Godot.Collections;
using System;

namespace Threshold.Core.Agent.Functions
{
    /// <summary>
    /// 角色相关世界观检索函数
    /// </summary>
    public partial class CharacterLoreFunction : Resource, IFunctionExecutor
    {
        private WorldLoreManager worldLoreManager;
        
        public string Name => "get_character_lore";
        public string Description => "获取与当前角色相关的世界观信息，包括职业、派别、技能相关的背景知识";
        
        public CharacterLoreFunction() { }
        
        public CharacterLoreFunction(WorldLoreManager loreManager)
        {
            worldLoreManager = loreManager;
        }
        
        public void SetWorldLoreManager(WorldLoreManager loreManager)
        {
            worldLoreManager = loreManager;
        }
        
        public FunctionResult Execute(Godot.Collections.Dictionary arguments, System.Collections.Generic.Dictionary<string, object> extraParams = null)
        {
            try
            {
                GD.Print($"=== 执行角色世界观检索函数 ===");
                GD.Print($"参数: {Json.Stringify(arguments)}");
                
                if (worldLoreManager == null)
                {
                    return new FunctionResult(Name, "世界观管理器未初始化", false, "世界观管理器未初始化");
                }
                
                var profession = arguments.ContainsKey("profession") ? arguments["profession"].AsString() : "";
                var faction = arguments.ContainsKey("faction") ? arguments["faction"].AsString() : "";
                var skills = arguments.ContainsKey("skills") ? arguments["skills"].AsString() : "";
                var maxResults = arguments.ContainsKey("max_results") ? arguments["max_results"].AsInt32() : 3;
                
                if (string.IsNullOrEmpty(profession) && string.IsNullOrEmpty(faction) && string.IsNullOrEmpty(skills))
                {
                    return new FunctionResult(Name, "请提供角色信息", false, "缺少角色信息");
                }
                
                var relevantLore = new Array<WorldLoreEntry>();
                
                // 1. 基于职业检索
                if (!string.IsNullOrEmpty(profession))
                {
                    var professionResults = worldLoreManager.SearchLore(profession, maxResults);
                    foreach (var result in professionResults)
                    {
                        if (!ContainsEntry(relevantLore, result))
                        {
                            relevantLore.Add(result);
                        }
                    }
                }
                
                // 2. 基于派别检索
                if (!string.IsNullOrEmpty(faction))
                {
                    var factionResults = worldLoreManager.SearchLore(faction, maxResults);
                    foreach (var result in factionResults)
                    {
                        if (!ContainsEntry(relevantLore, result) && relevantLore.Count < maxResults)
                        {
                            relevantLore.Add(result);
                        }
                    }
                }
                
                // 3. 基于技能检索
                if (!string.IsNullOrEmpty(skills))
                {
                    var skillArray = new Array<string>();
                    foreach (var skill in skills.Split(','))
                    {
                        skillArray.Add(skill.Trim());
                    }
                    
                    foreach (var skill in skillArray)
                    {
                        if (string.IsNullOrEmpty(skill)) continue;
                        
                        var skillResults = worldLoreManager.SearchLore(skill, 2);
                        foreach (var result in skillResults)
                        {
                            if (!ContainsEntry(relevantLore, result) && relevantLore.Count < maxResults)
                            {
                                relevantLore.Add(result);
                            }
                        }
                    }
                }
                
                if (relevantLore.Count == 0)
                {
                    return new FunctionResult(Name, "未找到与角色相关的世界观信息", true, "未找到相关信息");
                }
                
                // 格式化结果
                var resultText = FormatCharacterLoreResults(relevantLore, profession, faction, skills);
                
                GD.Print($"角色世界观检索完成，找到 {relevantLore.Count} 条相关信息");
                return new FunctionResult(Name, resultText, true, "");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"角色世界观检索函数执行失败: {ex.Message}");
                return new FunctionResult(Name, "", false, $"执行失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 检查是否已包含条目
        /// </summary>
        private bool ContainsEntry(Array<WorldLoreEntry> entries, WorldLoreEntry entry)
        {
            foreach (var existingEntry in entries)
            {
                if (existingEntry.Title == entry.Title)
                {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// 格式化角色世界观检索结果
        /// </summary>
        private string FormatCharacterLoreResults(Array<WorldLoreEntry> results, string profession, string faction, string skills)
        {
            var resultText = $"与角色相关的世界观信息：\n\n";
            
            if (!string.IsNullOrEmpty(profession))
            {
                resultText += $"职业相关 ({profession}):\n";
                var professionEntries = GetEntriesByRelevance(results, profession);
                foreach (var entry in professionEntries)
                {
                    resultText += $"• {entry.Title}: {entry.GetSummary(100)}\n";
                }
                resultText += "\n";
            }
            
            if (!string.IsNullOrEmpty(faction))
            {
                resultText += $"派别相关 ({faction}):\n";
                var factionEntries = GetEntriesByRelevance(results, faction);
                foreach (var entry in factionEntries)
                {
                    resultText += $"• {entry.Title}: {entry.GetSummary(100)}\n";
                }
                resultText += "\n";
            }
            
            if (!string.IsNullOrEmpty(skills))
            {
                resultText += $"技能相关 ({skills}):\n";
                var skillEntries = GetEntriesByRelevance(results, skills);
                foreach (var entry in skillEntries)
                {
                    resultText += $"• {entry.Title}: {entry.GetSummary(100)}\n";
                }
                resultText += "\n";
            }
            
            resultText += $"共找到 {results.Count} 条相关信息\n";
            resultText += "提示: 这些信息可以帮助您更好地理解角色的背景和世界观。";
            
            return resultText;
        }
        
        /// <summary>
        /// 根据相关性获取条目
        /// </summary>
        private Array<WorldLoreEntry> GetEntriesByRelevance(Array<WorldLoreEntry> allEntries, string keyword)
        {
            var relevantEntries = new Array<WorldLoreEntry>();
            
            foreach (var entry in allEntries)
            {
                if (entry.Title.ToLower().Contains(keyword.ToLower()) || 
                    entry.Content.ToLower().Contains(keyword.ToLower()) ||
                    entry.Category.ToLower().Contains(keyword.ToLower()))
                {
                    relevantEntries.Add(entry);
                }
            }
            
            return relevantEntries;
        }

        public string GetFunctionName()
        {
            return Name;
        }

        public string GetFunctionDescription()
        {
            return Description;
        }

        public Array<FunctionParameter> GetParameters()
        {
            return new Array<FunctionParameter>
            {
                new FunctionParameter("profession", "string", "职业"),
                new FunctionParameter("faction", "string", "派别"),
                new FunctionParameter("skills", "string", "技能"),
                new FunctionParameter("max_results", "int", "最大结果数")
            };
        }
    }
}
