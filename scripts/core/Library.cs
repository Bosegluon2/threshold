using Godot;
using System;
using Godot.Collections;
using System.Linq;
using Threshold.Core.Data;

namespace Threshold.Core
{
    /// <summary>
    /// 游戏数据图书馆 - 单例模式，管理所有YAML数据
    /// </summary>
    public partial class Library : Node
    {
        private static Library instance;
        public static Library Instance
        {
            get
            {
                if (instance == null)
                {
                    GD.PrintErr("Library实例未初始化");
                }
                return instance;
            }
        }

        // 数据缓存
        private Dictionary<string, Status> statusCache = new Dictionary<string, Status>();
        private Dictionary<string, Item> itemCache = new Dictionary<string, Item>();
        private Dictionary<string, Place> placeCache = new Dictionary<string, Place>();
        private Dictionary<string, Skill> skillCache = new Dictionary<string, Skill>();

        public override void _Ready()
        {
            if (instance != null && instance != this)
            {
                GD.PrintErr("Library实例已存在，销毁重复实例");
                QueueFree();
                return;
            }

            instance = this;
            GD.Print("=== Library单例初始化 ===");
        }

        public override void _ExitTree()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        #region Status 管理

        public void AddStatus(Status status)
        {
            if (status != null && !string.IsNullOrEmpty(status.Id))
            {
                statusCache[status.Id] = status;
            }
        }

        public Status GetStatus(string id)
        {
            return statusCache.ContainsKey(id) ? statusCache[id] : null;
        }

        public Array<Status> GetAllStatuses()
        {
            return new Array<Status>(statusCache.Values.ToArray());
        }

        public Array<Status> SearchStatuses(string keyword)
        {
            return new Array<Status>(
                statusCache.Values
                    .Where(s => s.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                                s.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    .ToArray()
            );
        }

        #endregion

        #region Item 管理

        public void AddItem(Item item)
        {
            if (item != null && !string.IsNullOrEmpty(item.Id))
            {
                itemCache[item.Id] = item;
            }
        }

        public Item GetItem(string id)
        {
            return itemCache.ContainsKey(id) ? itemCache[id] : null;
        }

        public Array<Item> GetAllItems()
        {
            return new Array<Item>(itemCache.Values.ToArray());
        }

        public Array<Item> SearchItems(string keyword)
        {
            return new Array<Item>(
                itemCache.Values
                    .Where(i => i.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                                i.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    .ToArray()
            );
        }

        #endregion

        #region Place 管理

        public void AddPlace(Place place)
        {
            if (place != null && !string.IsNullOrEmpty(place.Id))
            {
                placeCache[place.Id] = place;
            }
        }

        public Place GetPlace(string id)
        {
            return placeCache.ContainsKey(id) ? placeCache[id] : null;
        }

        public Array<Place> GetAllPlaces()
        {
            return new Array<Place>(placeCache.Values.ToArray());
        }

        public Array<Place> SearchPlaces(string keyword)
        {
            return new Array<Place>(
                placeCache.Values
                    .Where(p => p.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                                p.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    .ToArray()
            );
        }

        #endregion

        #region Skill 管理

        public void AddSkill(Skill skill)
        {
            if (skill != null && !string.IsNullOrEmpty(skill.Id))
            {
                skillCache[skill.Id] = skill;
            }
        }

        public Skill GetSkill(string id)
        {
            return skillCache.ContainsKey(id) ? skillCache[id] : null;
        }

        public Array<Skill> GetAllSkills()
        {
            return new Array<Skill>(skillCache.Values.ToArray());
        }

        public Array<Skill> SearchSkills(string keyword)
        {
            return new Array<Skill>(
                skillCache.Values
                    .Where(s => s.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                                s.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    .ToArray()
            );
        }

        #endregion

        public string GetDataStatistics()
        {
            var stats = "=== 游戏数据统计 ===\n";
            stats += $"状态: {statusCache.Count} 个\n";
            stats += $"物品: {itemCache.Count} 个\n";
            stats += $"地点: {placeCache.Count} 个\n";
            stats += $"技能: {skillCache.Count} 个\n";
            stats += $"总计: {statusCache.Count + itemCache.Count + placeCache.Count + skillCache.Count} 个数据项";
            return stats;
        }

        public void ClearAllData()
        {
            statusCache.Clear();
            itemCache.Clear();
            placeCache.Clear();
            skillCache.Clear();
            GD.Print("所有游戏数据已清空");
        }
    }
}
