using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Threshold.Core.Agent
{
    /// <summary>
    /// 世界观条目
    /// </summary>
    public partial class WorldLoreEntry : Resource
    {
        [Export] public string Title { get; set; } = "";
        [Export] public string Content { get; set; } = "";
        [Export] public string Category { get; set; } = "";
        [Export] public Array<string> Tags { get; set; } = new Array<string>();
        [Export] public int Importance { get; set; } = 1;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime LastUpdated { get; set; } = DateTime.Now;
        
        public WorldLoreEntry() { }
        
        public WorldLoreEntry(string title, string content, string category, Array<string> tags = null, int importance = 1)
        {
            Title = title;
            Content = content;
            Category = category;
            Tags = tags ?? new Array<string>();
            Importance = Mathf.Clamp(importance, 1, 10);
            CreatedDate = DateTime.Now;
            LastUpdated = DateTime.Now;
        }
        
        public string GetSummary(int maxLength = 200)
        {
            if (Content.Length <= maxLength)
                return Content;
            return Content.Substring(0, maxLength) + "...";
        }
        
        public bool ContainsKeywords(string query)
        {
            var lowerQuery = query.ToLower();
            return Title.ToLower().Contains(lowerQuery) || 
                   Content.ToLower().Contains(lowerQuery) ||
                   Tags.Any(tag => tag.ToLower().Contains(lowerQuery));
        }
    }
    
    /// <summary>
    /// 世界观管理器
    /// </summary>
    public partial class WorldLoreManager : Resource
    {
        private System.Collections.Generic.Dictionary<string, WorldLoreEntry> loreEntries = new System.Collections.Generic.Dictionary<string, WorldLoreEntry>();
        private System.Collections.Generic.Dictionary<string, List<string>> categoryIndex = new System.Collections.Generic.Dictionary<string, List<string>>();
        private System.Collections.Generic.Dictionary<string, List<string>> tagIndex = new System.Collections.Generic.Dictionary<string, List<string>>();
        
        public void AddLoreEntry(WorldLoreEntry entry)
        {
            if (string.IsNullOrEmpty(entry.Title))
                return;
            
            var key = entry.Title.ToLower();
            loreEntries[key] = entry;
            
            if (!string.IsNullOrEmpty(entry.Category))
            {
                if (!categoryIndex.ContainsKey(entry.Category))
                    categoryIndex[entry.Category] = new List<string>();
                categoryIndex[entry.Category].Add(entry.Title);
            }
            
            foreach (var tag in entry.Tags)
            {
                if (!string.IsNullOrEmpty(tag))
                {
                    var lowerTag = tag.ToLower();
                    if (!tagIndex.ContainsKey(lowerTag))
                        tagIndex[lowerTag] = new List<string>();
                    tagIndex[lowerTag].Add(entry.Title);
                }
            }
        }
        
        public WorldLoreEntry GetLoreEntry(string title)
        {
            var key = title.ToLower();
            return loreEntries.ContainsKey(key) ? loreEntries[key] : null;
        }
        
        public Array<WorldLoreEntry> SearchLore(string query, int maxResults = 10)
        {
            if (string.IsNullOrEmpty(query))
                return new Array<WorldLoreEntry>();
            
            var results = new List<(WorldLoreEntry entry, float score)>();
            var lowerQuery = query.ToLower();
            
            foreach (var entry in loreEntries.Values)
            {
                float score = 0;
                
                if (entry.Title.ToLower().Contains(lowerQuery))
                    score += 10;
                
                if (entry.Content.ToLower().Contains(lowerQuery))
                    score += 5;
                
                foreach (var tag in entry.Tags)
                {
                    if (tag.ToLower().Contains(lowerQuery))
                        score += 3;
                }
                
                if (entry.Category.ToLower().Contains(lowerQuery))
                    score += 2;
                
                score += entry.Importance * 0.1f;
                
                if (score > 0)
                    results.Add((entry, score));
            }
            
            var sortedResults = results.OrderByDescending(r => r.score).Take(maxResults);
            var resultArray = new Array<WorldLoreEntry>();
            
            foreach (var result in sortedResults)
                resultArray.Add(result.entry);
            
            return resultArray;
        }
        
        public Array<WorldLoreEntry> GetLoreByCategory(string category)
        {
            if (!categoryIndex.ContainsKey(category))
                return new Array<WorldLoreEntry>();
            
            var resultArray = new Array<WorldLoreEntry>();
            foreach (var title in categoryIndex[category])
            {
                var entry = GetLoreEntry(title);
                if (entry != null)
                    resultArray.Add(entry);
            }
            
            return resultArray;
        }
        
        public Array<WorldLoreEntry> GetLoreByTag(string tag)
        {
            var lowerTag = tag.ToLower();
            if (!tagIndex.ContainsKey(lowerTag))
                return new Array<WorldLoreEntry>();
            
            var resultArray = new Array<WorldLoreEntry>();
            foreach (var title in tagIndex[lowerTag])
            {
                var entry = GetLoreEntry(title);
                if (entry != null)
                    resultArray.Add(entry);
            }
            
            return resultArray;
        }
        
        public Array<string> GetAllCategories()
        {
            return new Array<string>(categoryIndex.Keys);
        }
        
        public Array<string> GetAllTags()
        {
            return new Array<string>(tagIndex.Keys);
        }
        
        public int GetTotalEntries()
        {
            return loreEntries.Count;
        }
        
        /// <summary>
        /// 从YAML文件重新加载世界观数据
        /// </summary>
        public void ReloadFromYaml(string filePath = null)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                filePath = "res://data/world_lore.yaml";
            }
            
            GD.Print($"=== 重新加载世界观数据: {filePath} ===");
            
            // 清空现有数据
            loreEntries.Clear();
            categoryIndex.Clear();
            tagIndex.Clear();
            
            // 从YAML文件加载
            var yamlEntries = WorldLoreLoader.LoadFromYaml(filePath);
            foreach (var entry in yamlEntries)
            {
                AddLoreEntry(entry);
            }
            
            GD.Print($"重新加载完成，共加载 {GetTotalEntries()} 个条目");
        }
        
        /// <summary>
        /// 保存当前世界观数据到YAML文件
        /// </summary>
        public void SaveToYaml(string filePath = null)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                filePath = "user://world_lore_backup.yaml";
            }
            
            var entries = new Array<WorldLoreEntry>();
            foreach (var entry in loreEntries.Values)
            {
                entries.Add(entry);
            }
            
            WorldLoreLoader.SaveToYaml(entries, filePath);
        }
        
        /// <summary>
        /// 添加自定义世界观条目
        /// </summary>
        public void AddCustomLoreEntry(string title, string content, string category, Array<string> tags = null, int importance = 1)
        {
            var entry = new WorldLoreEntry(title, content, category, tags, importance);
            AddLoreEntry(entry);
            
            // 自动保存到用户数据目录
            SaveToYaml();
        }
        
        public void InitializeDefaultLore()
        {
            GD.Print("=== 初始化默认世界观内容 ===");
            
            // 尝试从YAML文件加载
            var yamlEntries = WorldLoreLoader.LoadDefaultWorldLore();
            if (yamlEntries.Count > 0)
            {
                GD.Print($"从YAML文件加载了 {yamlEntries.Count} 个条目");
                foreach (var entry in yamlEntries)
                {
                    AddLoreEntry(entry);
                }
            }
            else
            {
                GD.Print("YAML文件加载失败，请检查文件路径与格式是否正确");
                
            }
            
            GD.Print($"世界观内容初始化完成，共添加 {GetTotalEntries()} 个条目");
        }
    }
}
