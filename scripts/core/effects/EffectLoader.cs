using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Threshold.Core.Effects
{
    /// <summary>
    /// 效果加载器 - 从配置文件加载效果模板
    /// </summary>
    public class EffectLoader
    {
        private static EffectLoader _instance;
        public static EffectLoader Instance => _instance ??= new EffectLoader();
        
        private IDeserializer _yamlDeserializer;
        private Dictionary<string, EffectReference> _effectTemplates = new Dictionary<string, EffectReference>();
        private Dictionary<string, List<EffectReference>> _effectCombinations = new Dictionary<string, List<EffectReference>>();
        private Dictionary<string, string> _pathMappings = new Dictionary<string, string>();
        
        private EffectLoader()
        {
            _yamlDeserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();
        }
        
        /// <summary>
        /// 加载效果配置文件
        /// </summary>
        public void LoadEffectConfigs()
        {
            try
            {
                var effectsDirectory = "data/effects/";
                
                if (!DirAccess.DirExistsAbsolute(effectsDirectory))
                {
                    GD.PrintErr($"效果配置目录不存在: {effectsDirectory}");
                    return;
                }
                
                LoadEffectConfigFromDirectory(effectsDirectory);
                GD.Print($"成功加载 {_effectTemplates.Count} 个效果模板");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"加载效果配置时发生错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 从目录加载效果配置
        /// </summary>
        private void LoadEffectConfigFromDirectory(string directory)
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
                        LoadEffectConfigFromFile(filePath);
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
        
        /// <summary>
        /// 从文件加载效果配置
        /// </summary>
        private void LoadEffectConfigFromFile(string filePath)
        {
            try
            {
                if (!Godot.FileAccess.FileExists(filePath))
                {
                    GD.PrintErr($"效果配置文件不存在: {filePath}");
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
                
                GD.Print($"正在加载效果配置文件: {filePath}");
                
                var configData = _yamlDeserializer.Deserialize<Dictionary<string, object>>(content);
                
                // 加载效果模板
                if (configData.ContainsKey("effect_templates"))
                {
                    var templatesData = configData["effect_templates"] as Dictionary<string, object>;
                    if (templatesData != null)
                    {
                        foreach (var kvp in templatesData)
                        {
                            var templateData = kvp.Value as Dictionary<string, object>;
                            if (templateData != null)
                            {
                                var template = CreateEffectTemplateFromData(kvp.Key, templateData);
                                if (template != null)
                                {
                                    _effectTemplates[kvp.Key] = template;
                                }
                            }
                        }
                    }
                }
                
                // 加载效果组合
                if (configData.ContainsKey("effect_combinations"))
                {
                    var combinationsData = configData["effect_combinations"] as Dictionary<string, object>;
                    if (combinationsData != null)
                    {
                        foreach (var kvp in combinationsData)
                        {
                            var combinationData = kvp.Value as Dictionary<string, object>;
                            if (combinationData != null)
                            {
                                var effects = CreateEffectCombinationFromData(combinationData);
                                if (effects != null)
                                {
                                    _effectCombinations[kvp.Key] = effects;
                                }
                            }
                        }
                    }
                }
                
                // 加载路径映射
                if (configData.ContainsKey("path_mappings"))
                {
                    var mappingsData = configData["path_mappings"] as Dictionary<string, object>;
                    if (mappingsData != null)
                    {
                        foreach (var category in mappingsData)
                        {
                            var categoryData = category.Value as Dictionary<string, object>;
                            if (categoryData != null)
                            {
                                foreach (var mapping in categoryData)
                                {
                                    _pathMappings[mapping.Key] = mapping.Value.ToString();
                                }
                            }
                        }
                    }
                }
                
                GD.Print($"成功加载效果配置: {filePath}");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"加载效果配置文件 {filePath} 时发生错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 从数据创建效果模板
        /// </summary>
        private EffectReference CreateEffectTemplateFromData(string templateId, Dictionary<string, object> data)
        {
            try
            {
                var template = new EffectReference();
                template.EffectId = templateId;
                
                if (data.ContainsKey("effect_script"))
                    template.EffectScript = data["effect_script"].ToString();
                
                if (data.ContainsKey("category") && Enum.TryParse<EffectCategory>(data["category"].ToString(), out var category))
                    template.Category = category;
                
                if (data.ContainsKey("target_path"))
                    template.TargetPath = data["target_path"].ToString();
                
                if (data.ContainsKey("default_parameters"))
                {
                    var defaultParams = data["default_parameters"] as Dictionary<string, object>;
                    if (defaultParams != null)
                    {
                        foreach (var kvp in defaultParams)
                        {
                            template.Parameters[kvp.Key] = kvp.Value;
                        }
                    }
                }
                
                return template;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"创建效果模板时发生错误: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 从数据创建效果组合
        /// </summary>
        private List<EffectReference> CreateEffectCombinationFromData(Dictionary<string, object> data)
        {
            try
            {
                var effects = new List<EffectReference>();
                
                if (data.ContainsKey("effects"))
                {
                    var effectsData = data["effects"] as List<object>;
                    if (effectsData != null)
                    {
                        foreach (var effectData in effectsData)
                        {
                            var effectDict = effectData as Dictionary<string, object>;
                            if (effectDict != null)
                            {
                                var effect = CreateEffectFromCombinationData(effectDict);
                                if (effect != null)
                                {
                                    effects.Add(effect);
                                }
                            }
                        }
                    }
                }
                
                return effects;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"创建效果组合时发生错误: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 从组合数据创建效果
        /// </summary>
        private EffectReference CreateEffectFromCombinationData(Dictionary<string, object> data)
        {
            try
            {
                var effect = new EffectReference();
                
                if (data.ContainsKey("template") && _effectTemplates.TryGetValue(data["template"].ToString(), out var template))
                {
                    // 复制模板属性
                    effect.EffectScript = template.EffectScript;
                    effect.Category = template.Category;
                    effect.TargetPath = template.TargetPath;
                    
                    // 复制默认参数
                    foreach (var kvp in template.Parameters)
                    {
                        effect.Parameters[kvp.Key] = kvp.Value;
                    }
                }
                
                // 应用实例参数
                if (data.ContainsKey("parameters"))
                {
                    var instanceParams = data["parameters"] as Dictionary<string, object>;
                    if (instanceParams != null)
                    {
                        foreach (var kvp in instanceParams)
                        {
                            effect.Parameters[kvp.Key] = kvp.Value;
                        }
                    }
                }
                
                return effect;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"从组合数据创建效果时发生错误: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 获取效果模板
        /// </summary>
        public EffectReference GetEffectTemplate(string templateId)
        {
            return _effectTemplates.TryGetValue(templateId, out var template) ? template : null;
        }
        
        /// <summary>
        /// 获取效果组合
        /// </summary>
        public List<EffectReference> GetEffectCombination(string combinationId)
        {
            return _effectCombinations.TryGetValue(combinationId, out var combination) ? combination : null;
        }
        
        /// <summary>
        /// 解析路径映射
        /// </summary>
        public string ResolvePathMapping(string shortPath)
        {
            return _pathMappings.TryGetValue(shortPath, out var fullPath) ? fullPath : shortPath;
        }
        
        /// <summary>
        /// 获取所有效果模板
        /// </summary>
        public Dictionary<string, EffectReference> GetAllEffectTemplates()
        {
            return new Dictionary<string, EffectReference>(_effectTemplates);
        }
        
        /// <summary>
        /// 获取所有效果组合
        /// </summary>
        public Dictionary<string, List<EffectReference>> GetAllEffectCombinations()
        {
            return new Dictionary<string, List<EffectReference>>(_effectCombinations);
        }
    }
}
