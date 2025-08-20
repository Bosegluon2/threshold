using Godot;
using System;
using System.Collections.Generic;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Threshold.Core.Data;

namespace Threshold.Core
{
    /// <summary>
    /// 图书馆数据加载器 - 从YAML文件加载数据到Library单例
    /// </summary>
    public static class LibraryLoader
    {
        private static readonly string STATUS_DIR = "res://data/status/";
        private static readonly string ITEMS_DIR = "res://data/items/";
        private static readonly string PLACES_DIR = "res://data/places/";
        private static readonly string SKILLS_DIR = "res://data/skills/";

        #region 辅助方法

        /// <summary>
        /// 从YAML数组创建Vector3
        /// </summary>
        private static Vector3 CreateVector3FromList(List<object> list)
        {
            if (list.Count >= 3)
            {
                var x = Convert.ToSingle(list[0]);
                var y = Convert.ToSingle(list[1]);
                var z = Convert.ToSingle(list[2]);
                return new Vector3(x, y, z);
            }
            return Vector3.Zero;
        }

        /// <summary>
        /// 从YAML数组创建Godot Array
        /// </summary>
        private static Godot.Collections.Array<string> CreateStringArrayFromList(List<object> list)
        {
            var array = new Godot.Collections.Array<string>();
            foreach (var item in list)
            {
                array.Add(item.ToString());
            }
            return array;
        }

        #endregion

        /// <summary>
        /// 加载所有数据到Library
        /// </summary>
        public static void LoadAllDataToLibrary()
        {
            if (Library.Instance == null)
            {
                GD.PrintErr("Library实例未初始化，无法加载数据");
                return;
            }

            GD.Print("=== 开始加载数据到Library ===");
            
            LoadStatusesToLibrary();
            LoadItemsToLibrary();
            LoadPlacesToLibrary();
            LoadSkillsToLibrary();
            
            GD.Print("=== 所有数据已加载到Library ===");
            GD.Print(Library.Instance.GetDataStatistics());
        }

        #region Status 加载

        public static void LoadStatusesToLibrary()
        {
            try
            {
                var dir = DirAccess.Open(STATUS_DIR);
                if (dir == null) return;

                dir.ListDirBegin();
                var fileName = dir.GetNext();
                var loadedCount = 0;

                while (!string.IsNullOrEmpty(fileName))
                {
                    if (fileName.EndsWith(".yaml") || fileName.EndsWith(".yml"))
                    {
                        var status = LoadStatusFromFile(fileName);
                        if (status != null)
                        {
                            Library.Instance.AddStatus(status);
                            loadedCount++;
                        }
                    }
                    fileName = dir.GetNext();
                }

                dir.ListDirEnd();
                GD.Print($"状态数据加载完成，共 {loadedCount} 个状态");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"加载状态数据时发生错误: {ex.Message}");
            }
        }

        private static Status LoadStatusFromFile(string fileName)
        {
            try
            {
                var filePath = STATUS_DIR + fileName;
                var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read);
                if (file == null) return null;

                var yamlContent = file.GetAsText();
                file.Close();

                if (string.IsNullOrEmpty(yamlContent)) return null;

                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .Build();

                var statusData = deserializer.Deserialize<Dictionary<string, object>>(yamlContent);
                var status = CreateStatusFromData(statusData);
                return status;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"加载状态文件 {fileName} 时发生错误: {ex.Message}");
                return null;
            }
        }

        private static Status CreateStatusFromData(Dictionary<string, object> data)
        {
            try
            {
                var id = GetStringValue(data, "id");
                var name = GetStringValue(data, "name");
                var description = GetStringValue(data, "description");
                var category = GetStringValue(data, "category");
                var priority = GetIntValue(data, "priority");
                var duration = GetIntValue(data, "duration");
                var stackable = GetBoolValue(data, "stackable");
                var removable = GetBoolValue(data, "removable");
                var icon = GetStringValue(data, "icon");
                var effectScript = GetStringValue(data, "effect_script");

                var status = new Status(id, name, description, category, priority, duration, stackable, removable, icon, effectScript);

                return status;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"从数据创建Status时发生错误: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Item 加载

        public static void LoadItemsToLibrary()
        {
            try
            {
                var dir = DirAccess.Open(ITEMS_DIR);
                if (dir == null) return;

                dir.ListDirBegin();
                var fileName = dir.GetNext();
                var loadedCount = 0;

                while (!string.IsNullOrEmpty(fileName))
                {
                    if (fileName.EndsWith(".yaml") || fileName.EndsWith(".yml"))
                    {
                        var item = LoadItemFromFile(fileName);
                        if (item != null)
                        {
                            Library.Instance.AddItem(item);
                            loadedCount++;
                        }
                    }
                    fileName = dir.GetNext();
                }

                dir.ListDirEnd();
                GD.Print($"物品数据加载完成，共 {loadedCount} 个物品");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"加载物品数据时发生错误: {ex.Message}");
            }
        }

        private static Item LoadItemFromFile(string fileName)
        {
            try
            {
                var filePath = ITEMS_DIR + fileName;
                var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read);
                if (file == null) return null;

                var yamlContent = file.GetAsText();
                file.Close();

                if (string.IsNullOrEmpty(yamlContent)) return null;

                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .Build();

                var itemData = deserializer.Deserialize<Dictionary<string, object>>(yamlContent);
                var item = CreateItemFromData(itemData);
                return item;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"加载物品文件 {fileName} 时发生错误: {ex.Message}");
                return null;
            }
        }

        private static Item CreateItemFromData(Dictionary<string, object> data)
        {
            try
            {
                var id = GetStringValue(data, "id");
                var name = GetStringValue(data, "name");
                var description = GetStringValue(data, "description");
                var category = GetStringValue(data, "category");
                var rarity = GetStringValue(data, "rarity");
                var value = GetIntValue(data, "value");
                var weight = GetIntValue(data, "weight");
                var icon = GetStringValue(data, "icon");
                var durability = GetIntValue(data, "durability");
                var maxDurability = GetIntValue(data, "max_durability");
                var isEquippable = GetBoolValue(data, "is_equippable");
                var isUsable = GetBoolValue(data, "is_usable");
                var isConsumable = GetBoolValue(data, "is_consumable");
                var maxStack = GetIntValue(data, "max_stack");
                
                // 脚本化功能
                var useScript = GetStringValue(data, "use_script");
                var equipScript = GetStringValue(data, "equip_script");
                var unequipScript = GetStringValue(data, "unequip_script");

                var item = new Item(id, name, description, 1, weight, Threshold.Core.Enums.ItemType.Misc, value);
                item.Category = category;
                item.Rarity = rarity;
                item.Icon = icon;
                item.Durability = durability;
                item.MaxDurability = maxDurability;
                item.IsEquippable = isEquippable;
                item.IsUsable = isUsable;
                item.IsConsumable = isConsumable;
                item.MaxStack = maxStack > 0 ? maxStack : 99;
                
                // 设置脚本
                item.UseScript = useScript;
                item.EquipScript = equipScript;
                item.UnequipScript = unequipScript;

                return item;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"从数据创建Item时发生错误: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Place 加载

        public static void LoadPlacesToLibrary()
        {
            try
            {
                var dir = DirAccess.Open(PLACES_DIR);
                if (dir == null) return;

                dir.ListDirBegin();
                var fileName = dir.GetNext();
                var loadedCount = 0;

                while (!string.IsNullOrEmpty(fileName))
                {
                    if (fileName.EndsWith(".yaml") || fileName.EndsWith(".yml"))
                    {
                        var place = LoadPlaceFromFile(fileName);
                        if (place != null)
                        {
                            Library.Instance.AddPlace(place);
                            loadedCount++;
                        }
                    }
                    fileName = dir.GetNext();
                }

                dir.ListDirEnd();
                GD.Print($"地点数据加载完成，共 {loadedCount} 个地点");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"加载地点数据时发生错误: {ex.Message}");
            }
        }

        private static Place LoadPlaceFromFile(string fileName)
        {
            try
            {
                var filePath = PLACES_DIR + fileName;
                var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read);
                if (file == null) return null;

                var yamlContent = file.GetAsText();
                file.Close();

                if (string.IsNullOrEmpty(yamlContent)) return null;

                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .WithTypeConverter(new ArrayToVector3Converter())
                    .Build();

                var place = deserializer.Deserialize<Place>(yamlContent);
                // Place现在使用脚本驱动的效果，不再需要PlaceEffect
                return place;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"加载地点文件 {fileName} 时发生错误: {ex.Message}");
                return null;
            }
        }
        // 修复ArrayToVector3Converter的实现，兼容YamlDotNet的ObjectDeserializer和ObjectSerializer的实际用法
        // 修复ArrayToVector3Converter，兼容YamlDotNet的ObjectDeserializer和ObjectSerializer的实际用法
        private class ArrayToVector3Converter : IYamlTypeConverter
        {
            public bool Accepts(Type type)
            {
                return type == typeof(Vector3);
            }

            public object ReadYaml(IParser parser, Type type, ObjectDeserializer nestedObjectDeserializer)
            {
                // 反序列化为object列表
                var list = nestedObjectDeserializer(typeof(List<object>)) as IList<object>;
                if (list == null || list.Count != 3)
                {
                    throw new InvalidOperationException("无法将 YAML 数据转换为 Vector3，数据格式不正确。");
                }
                // 将object列表转换为Vector3
                return new Vector3(Convert.ToSingle(list[0]), Convert.ToSingle(list[1]), Convert.ToSingle(list[2]));
            }


            // 兼容YamlDotNet 12+，实现带ObjectSerializer的WriteYaml
            public void WriteYaml(IEmitter emitter, object value, Type type, ObjectSerializer serializer)
            {
                var vector = (Vector3)value;
                // 兼容YamlDotNet不同版本，移除不存在的SequenceStyle.Any参数
                emitter.Emit(new YamlDotNet.Core.Events.SequenceStart(null, null, false, YamlDotNet.Core.Events.SequenceStyle.Block));
                serializer(vector.X);
                serializer(vector.Y);
                serializer(vector.Z);
                emitter.Emit(new YamlDotNet.Core.Events.SequenceEnd());
            }
            
        }

        


        #endregion

        #region Skill 加载

        public static void LoadSkillsToLibrary()
        {
            try
            {
                var dir = DirAccess.Open(SKILLS_DIR);
                if (dir == null) return;

                dir.ListDirBegin();
                var fileName = dir.GetNext();
                var loadedCount = 0;

                while (!string.IsNullOrEmpty(fileName))
                {
                    if (fileName.EndsWith(".yaml") || fileName.EndsWith(".yml"))
                    {
                        var skill = LoadSkillFromFile(fileName);
                        if (skill != null)
                        {
                            Library.Instance.AddSkill(skill);
                            loadedCount++;
                        }
                    }
                    fileName = dir.GetNext();
                }

                dir.ListDirEnd();
                GD.Print($"技能数据加载完成，共 {loadedCount} 个技能");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"加载技能数据时发生错误: {ex.Message}");
            }
        }

        private static Skill LoadSkillFromFile(string fileName)
        {
            try
            {
                var filePath = SKILLS_DIR + fileName;
                var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read);
                if (file == null) return null;

                var yamlContent = file.GetAsText();
                file.Close();

                if (string.IsNullOrEmpty(yamlContent)) return null;

                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .Build();

                var skillData = deserializer.Deserialize<Dictionary<string, object>>(yamlContent);
                var skill = CreateSkillFromData(skillData);
                return skill;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"加载技能文件 {fileName} 时发生错误: {ex.Message}");
                return null;
            }
        }

        private static Skill CreateSkillFromData(Dictionary<string, object> data)
        {
            try
            {
                var id = GetStringValue(data, "id");
                var name = GetStringValue(data, "name");
                var description = GetStringValue(data, "description");
                var category = GetStringValue(data, "category");
                var type = GetStringValue(data, "type");
                var element = GetStringValue(data, "element");
                var level = GetIntValue(data, "base_level");
                var maxLevel = GetIntValue(data, "max_level");
                var energyCost = GetIntValue(data, "energy_cost");
                var cooldown = GetFloatValue(data, "cooldown");
                var range = GetFloatValue(data, "range");
                var areaOfEffect = GetFloatValue(data, "area_of_effect");
                var icon = GetStringValue(data, "icon");

                var skill = new Skill(id, name, description, category, type, element, level, maxLevel, 
                                   energyCost, cooldown, range, areaOfEffect, icon, "", "");

                return skill;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"从数据创建Skill时发生错误: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region 通用方法

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

        private static float GetFloatValue(Dictionary<string, object> data, string key)
        {
            if (data.ContainsKey(key) && data[key] != null)
            {
                if (float.TryParse(data[key].ToString(), out var value))
                {
                    return value;
                }
            }
            return 0f;
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

        /// <summary>
        /// 从List中获取float值
        /// </summary>
        private static float GetFloatValueFromList(List<object> list, int index)
        {
            if (index >= 0 && index < list.Count && list[index] != null)
            {
                if (float.TryParse(list[index].ToString(), out var value))
                {
                    return value;
                }
            }
            return 0f;
        }

        #endregion
    }
}
