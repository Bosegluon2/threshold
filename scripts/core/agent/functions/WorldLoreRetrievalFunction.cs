using Godot;
using Godot.Collections;
using System;

namespace Threshold.Core.Agent.Functions
{
    /// <summary>
    /// 世界观检索函数 - RAG系统的function-call实现
    /// </summary>
    public partial class WorldLoreRetrievalFunction : Resource, IFunctionExecutor
    {
        private WorldLoreManager worldLoreManager;
        
        public string Name => "retrieve_world_lore";
        public string Description => "检索世界观相关信息，支持按分类、标签、关键词搜索";
        
        public WorldLoreRetrievalFunction() { }
        
        public WorldLoreRetrievalFunction(WorldLoreManager loreManager)
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
                GD.Print($"=== 执行世界观检索函数 ===");
                GD.Print($"参数: {Json.Stringify(arguments)}");
                
                if (worldLoreManager == null)
                {
                    return new FunctionResult(Name, "世界观管理器未初始化", false, "世界观管理器未初始化");
                }
                
                var query = arguments.ContainsKey("query") ? arguments["query"].AsString() : "";
                var category = arguments.ContainsKey("category") ? arguments["category"].AsString() : "";
                var tags = arguments.ContainsKey("tags") ? arguments["tags"].AsString() : "";
                var maxResults = arguments.ContainsKey("max_results") ? arguments["max_results"].AsInt32() : 5;
                var searchType = arguments.ContainsKey("search_type") ? arguments["search_type"].AsString() : "smart";
                
                if (string.IsNullOrEmpty(query) && string.IsNullOrEmpty(category) && string.IsNullOrEmpty(tags))
                {
                    return new FunctionResult(Name, "请提供至少一个搜索条件", false, "缺少搜索条件");
                }
                
                Array<WorldLoreEntry> results;
                
                // 根据搜索类型选择检索策略
                switch (searchType.ToLower())
                {
                    case "category":
                        results = worldLoreManager.GetLoreByCategory(category);
                        break;
                    case "tags":
                        var tagArray = new Array<string>();
                        if (!string.IsNullOrEmpty(tags))
                        {
                            foreach (var tag in tags.Split(','))
                            {
                                tagArray.Add(tag.Trim());
                            }
                        }
                        results = SearchByTags(tagArray, maxResults);
                        break;
                    case "keyword":
                        results = worldLoreManager.SearchLore(query, maxResults);
                        break;
                    case "smart":
                    default:
                        results = SmartSearch(query, category, tags, maxResults);
                        break;
                }
                
                if (results.Count == 0)
                {
                    return new FunctionResult(Name, "未找到相关信息", true, "未找到相关信息");
                }
                
                // 格式化检索结果
                var resultText = FormatSearchResults(results, searchType);
                
                GD.Print($"检索完成，找到 {results.Count} 条结果");
                return new FunctionResult(Name, resultText, true, "");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"世界观检索函数执行失败: {ex.Message}");
                return new FunctionResult(Name, "", false, $"执行失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 智能搜索 - 结合多种条件
        /// </summary>
        private Array<WorldLoreEntry> SmartSearch(string query, string category, string tags, int maxResults)
        {
            var allResults = new Array<WorldLoreEntry>();
            
            // 1. 关键词搜索
            if (!string.IsNullOrEmpty(query))
            {
                var keywordResults = worldLoreManager.SearchLore(query, maxResults);
                foreach (var result in keywordResults)
                {
                    if (!ContainsEntry(allResults, result))
                    {
                        allResults.Add(result);
                    }
                }
            }
            
            // 2. 分类搜索
            if (!string.IsNullOrEmpty(category))
            {
                var categoryResults = worldLoreManager.GetLoreByCategory(category);
                foreach (var result in categoryResults)
                {
                    if (!ContainsEntry(allResults, result) && allResults.Count < maxResults)
                    {
                        allResults.Add(result);
                    }
                }
            }
            
            // 3. 标签搜索
            if (!string.IsNullOrEmpty(tags))
            {
                var tagArray = new Array<string>();
                foreach (var tag in tags.Split(','))
                {
                    tagArray.Add(tag.Trim());
                }
                var tagResults = SearchByTags(tagArray, maxResults);
                foreach (var result in tagResults)
                {
                    if (!ContainsEntry(allResults, result) && allResults.Count < maxResults)
                    {
                        allResults.Add(result);
                    }
                }
            }
            
            // 限制结果数量
            var finalResults = new Array<WorldLoreEntry>();
            for (int i = 0; i < Math.Min(allResults.Count, maxResults); i++)
            {
                finalResults.Add(allResults[i]);
            }
            
            return finalResults;
        }
        
        /// <summary>
        /// 按标签搜索
        /// </summary>
        private Array<WorldLoreEntry> SearchByTags(Array<string> tags, int maxResults)
        {
            var results = new Array<WorldLoreEntry>();
            
            foreach (var tag in tags)
            {
                if (string.IsNullOrEmpty(tag)) continue;
                
                var tagResults = worldLoreManager.GetLoreByTag(tag);
                foreach (var result in tagResults)
                {
                    if (!ContainsEntry(results, result) && results.Count < maxResults)
                    {
                        results.Add(result);
                    }
                }
            }
            
            return results;
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
        /// 格式化检索结果
        /// </summary>
        private string FormatSearchResults(Array<WorldLoreEntry> results, string searchType)
        {
            var resultText = $"找到 {results.Count} 条相关信息：\n\n";
            
            foreach (var entry in results)
            {
                resultText += $"【{entry.Title}】\n";
                resultText += $"分类: {entry.Category}\n";
                resultText += $"重要性: {entry.Importance}/10\n";
                resultText += $"标签: {string.Join(", ", entry.Tags)}\n";
                resultText += $"内容: {entry.GetSummary(150)}\n\n";
            }
            
            resultText += $"搜索类型: {searchType}\n";
            resultText += "提示: 您可以继续询问具体条目的详细信息，或使用其他搜索条件。";
            
            return resultText;
        }

        public string GetFunctionName()
        {
            return "retrieve_world_lore";
        }

        public string GetFunctionDescription()
        {
            return "检索世界观相关信息，支持按分类、标签、关键词搜索";
        }

        public Array<FunctionParameter> GetParameters()
        {
            return new Array<FunctionParameter>
            {
                new FunctionParameter("query", "string", "搜索关键词"),
                new FunctionParameter("category", "string", "分类"),
                new FunctionParameter("tags", "string", "标签"),
                new FunctionParameter("max_results", "int", "最大结果数")
            };
        }
    }
}
