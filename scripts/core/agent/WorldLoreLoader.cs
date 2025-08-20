using Godot;
using Godot.Collections;
using System;
using System.IO;
using System.Collections.Generic;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Threshold.Core.Agent
{
    /// <summary>
    /// 世界观数据加载器
    /// </summary>
    public partial class WorldLoreLoader : Resource
    {
        /// <summary>
        /// 从YAML文件加载世界观数据
        /// </summary>
        public static Array<WorldLoreEntry> LoadFromYaml(string filePath)
        {
            var entries = new Array<WorldLoreEntry>();

            try
            {
                GD.Print($"=== 开始加载世界观数据文件: {filePath} ===");

                // 检查文件是否存在
                if (!File.Exists(filePath))
                {
                    GD.PrintErr($"世界观数据文件不存在: {filePath}");
                    return entries;
                }

                // 读取文件内容
                string yamlContent = File.ReadAllText(filePath);

                GD.Print($"文件内容长度: {yamlContent.Length} 字符");

                // 解析YAML内容
                var yamlData = ParseYamlContent(yamlContent);
                if (yamlData != null)
                {
                    entries = ConvertYamlToEntries(yamlData);
                    GD.Print($"成功加载 {entries.Count} 个世界观条目");
                }
                else
                {
                    GD.PrintErr("YAML解析失败");
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"加载世界观数据时发生错误: {ex.Message}");
            }

            return entries;
        }

        /// <summary>
        /// 解析YAML内容
        /// </summary>
        private static System.Collections.Generic.Dictionary<string, object> ParseYamlContent(string yamlContent)
        {
            try
            {
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

                var yamlData = deserializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(yamlContent);
                return yamlData;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"YAML解析失败: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 将YAML数据转换为世界观条目
        /// </summary>
        private static Array<WorldLoreEntry> ConvertYamlToEntries(System.Collections.Generic.Dictionary<string, object> yamlData)
        {
            var entries = new Array<WorldLoreEntry>();

            try
            {
                // 跳过元数据（如version、last_updated），只处理分类
                foreach (var categoryPair in yamlData)
                {
                    var category = categoryPair.Key;
                    if (category == "version" || category == "last_updated")
                        continue;

                    if (categoryPair.Value is IList<object> categoryArray)
                    {
                        GD.Print($"处理分类: {category}, 条目数量: {categoryArray.Count}");

                        foreach (var entryData in categoryArray)
                        {
                            if (entryData is IDictionary<object, object> entryDict)
                            {
                                var entry = CreateEntryFromDict(entryDict, category);
                                if (entry != null)
                                {
                                    entries.Add(entry);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"转换YAML数据时发生错误: {ex.Message}");
            }

            return entries;
        }

        /// <summary>
        /// 从字典创建世界观条目
        /// </summary>
        private static WorldLoreEntry CreateEntryFromDict(IDictionary<object, object> entryDict, string category)
        {
            try
            {
                var title = entryDict.ContainsKey("title") ? entryDict["title"]?.ToString() ?? "" : "";
                var content = entryDict.ContainsKey("content") ? entryDict["content"]?.ToString() ?? "" : "";
                var importance = 1;
                if (entryDict.ContainsKey("importance"))
                {
                    try
                    {
                        importance = Convert.ToInt32(entryDict["importance"]);
                    }
                    catch { importance = 1; }
                }

                if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(content))
                {
                    GD.PrintErr($"条目数据不完整: title={title}, content长度={content?.Length ?? 0}");
                    return null;
                }

                // 处理标签
                var tags = new Array<string>();
                if (entryDict.ContainsKey("tags") && entryDict["tags"] is IList<object> tagsArray)
                {
                    foreach (var tag in tagsArray)
                    {
                        var tagStr = tag?.ToString();
                        if (!string.IsNullOrEmpty(tagStr))
                        {
                            tags.Add(tagStr);
                        }
                    }
                }

                var entry = new WorldLoreEntry(title, content, category, tags, importance);
                GD.Print($"创建条目: {title} (分类: {category}, 重要性: {importance}, 标签: {string.Join(", ", tags)})");

                return entry;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"创建条目时发生错误: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 从默认路径加载世界观数据
        /// </summary>
        public static Array<WorldLoreEntry> LoadDefaultWorldLore()
        {
            var defaultPath = "res://data/world_lore.yaml";
            // Godot的res://路径需要转换为绝对路径
            var absPath = ProjectSettings.GlobalizePath(defaultPath);
            return LoadFromYaml(absPath);
        }

        /// <summary>
        /// 从用户数据目录加载世界观数据
        /// </summary>
        public static Array<WorldLoreEntry> LoadUserWorldLore()
        {
            var userPath = "user://world_lore.yaml";
            var absPath = ProjectSettings.GlobalizePath(userPath);
            return LoadFromYaml(absPath);
        }

        /// <summary>
        /// 保存世界观数据到YAML文件
        /// </summary>
        public static void SaveToYaml(Array<WorldLoreEntry> entries, string filePath)
        {
            try
            {
                GD.Print($"=== 开始保存世界观数据到: {filePath} ===");

                // 构建YAML数据结构
                var yamlData = new System.Collections.Generic.Dictionary<string, object>();
                yamlData["version"] = "1.0";
                yamlData["last_updated"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                // 按分类组织数据
                var categories = new System.Collections.Generic.Dictionary<string, List<System.Collections.Generic.Dictionary<string, object>>>();
                foreach (var entry in entries)
                {
                    if (!categories.ContainsKey(entry.Category))
                    {
                        categories[entry.Category] = new List<System.Collections.Generic.Dictionary<string, object>>();
                    }

                    var entryData = new System.Collections.Generic.Dictionary<string, object>
                    {
                        ["title"] = entry.Title,
                        ["content"] = entry.Content,
                        ["tags"] = new List<string>(entry.Tags),
                        ["importance"] = entry.Importance
                    };

                    categories[entry.Category].Add(entryData);
                }

                // 将分类数据添加到主数据中
                foreach (var categoryPair in categories)
                {
                    yamlData[categoryPair.Key] = categoryPair.Value;
                }

                // 转换为YAML字符串
                var serializer = new SerializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
                    .Build();

                var yamlContent = serializer.Serialize(yamlData);

                // 保存到文件
                File.WriteAllText(filePath, yamlContent);
                GD.Print($"世界观数据已保存到: {filePath}");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"保存世界观数据时发生错误: {ex.Message}");
            }
        }
    }
}
