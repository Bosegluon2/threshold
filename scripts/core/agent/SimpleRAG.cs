using Godot;
using Godot.Collections;
using System;
using System.Linq;

namespace Threshold.Core.Agent
{
    /// <summary>
    /// 简化的RAG系统
    /// </summary>
    public partial class SimpleRAG : Resource
    {
        private WorldLoreManager worldLoreManager;
        
        public SimpleRAG() { }
        
        public SimpleRAG(WorldLoreManager loreManager)
        {
            worldLoreManager = loreManager;
        }
        
        public void SetWorldLoreManager(WorldLoreManager loreManager)
        {
            worldLoreManager = loreManager;
        }
        
        /// <summary>
        /// 检索相关信息
        /// </summary>
        public string RetrieveAndEnhance(string query, CharacterCard character, string basePrompt)
        {
            if (worldLoreManager == null)
                return basePrompt;
            
            GD.Print($"=== RAG系统检索信息 ===");
            GD.Print($"查询: {query}");
            
            var relevantEntries = worldLoreManager.SearchLore(query, 3);
            var characterLore = GetCharacterRelatedLore(character);
            
            var enhancedPrompt = basePrompt + "\n\n";
            enhancedPrompt += "世界观背景信息：\n";
            
            // 添加查询相关的信息
            if (relevantEntries.Count > 0)
            {
                enhancedPrompt += "查询相关背景：\n";
                foreach (var entry in relevantEntries)
                {
                    enhancedPrompt += $"- {entry.Title}: {entry.GetSummary(120)}\n";
                }
                enhancedPrompt += "\n";
            }
            
            // 添加角色相关的信息
            if (characterLore.Count > 0)
            {
                enhancedPrompt += "角色相关背景：\n";
                foreach (var entry in characterLore)
                {
                    enhancedPrompt += $"- {entry.Title}: {entry.GetSummary(100)}\n";
                }
                enhancedPrompt += "\n";
            }
            
            enhancedPrompt += "请基于以上背景信息，以角色身份进行对话。";
            
            GD.Print($"RAG增强完成，添加了 {relevantEntries.Count + characterLore.Count} 条背景信息");
            
            return enhancedPrompt;
        }
        
        /// <summary>
        /// 获取角色相关的世界观信息
        /// </summary>
        private Array<WorldLoreEntry> GetCharacterRelatedLore(CharacterCard character)
        {
            if (character == null)
                return new Array<WorldLoreEntry>();
            
            var relatedEntries = new Array<WorldLoreEntry>();
            
            // 基于角色职业检索
            var professionEntries = worldLoreManager.SearchLore(character.Profession, 2);
            foreach (var entry in professionEntries)
            {
                relatedEntries.Add(entry);
            }
            
            // 基于角色派别检索
            var factionEntries = worldLoreManager.SearchLore(character.Faction, 2);
            foreach (var entry in factionEntries)
            {
                if (!relatedEntries.Any(e => e.Title == entry.Title))
                {
                    relatedEntries.Add(entry);
                }
            }
            
            return relatedEntries;
        }
        
        /// <summary>
        /// 获取RAG统计信息
        /// </summary>
        public string GetRAGStats()
        {
            if (worldLoreManager == null)
                return "RAG系统未初始化";
            
            var stats = $"RAG系统统计信息:\n";
            stats += $"世界观条目总数: {worldLoreManager.GetTotalEntries()}\n";
            stats += $"可用分类: {string.Join(", ", worldLoreManager.GetAllCategories())}\n";
            stats += $"可用标签: {string.Join(", ", worldLoreManager.GetAllTags())}\n";
            
            return stats;
        }
    }
}
