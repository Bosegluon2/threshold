using System;
using Godot;
using Godot.Collections;
using Threshold.Core.Enums;
using Threshold.Core.Utils;

namespace Threshold.Core.Data
{
    public partial class Item : Resource
    {
        // 基础属性
        [Export] public string Id { get; set; }
        [Export] public string Name { get; set; }
        [Export] public string Description { get; set; }
        [Export] public ItemType Type { get; set; }
        [Export] public string Category { get; set; } = "misc";
        [Export] public string Rarity { get; set; } = "common";
        [Export] public int Value { get; set; }
        [Export] public int Weight { get; set; }
        [Export] public string Icon { get; set; } = "";
        
        // 数量系统
        [Export] public int Quantity { get; set; } = 1;
        [Export] public int MaxStack { get; set; } = 99;
        
        // 耐久度系统
        [Export] public int Durability { get; set; } = 100;
        [Export] public int MaxDurability { get; set; } = 100;
        
        // 功能标志
        [Export] public bool IsEquippable { get; set; } = false;
        [Export] public bool IsUsable { get; set; } = false;
        [Export] public bool IsConsumable { get; set; } = false;
        
        // 脚本化功能
        [Export] public string UseScript { get; set; } = "";        // 使用脚本
        [Export] public string EquipScript { get; set; } = "";     // 装备脚本
        [Export] public string UnequipScript { get; set; } = "";   // 卸下脚本
        
        // 标签系统
        [Export] public Array<string> Tags { get; set; } = new Array<string>();
        
        public Item() { }

        public Item(string id, string name, string description, int quantity, int weight, ItemType type, int value)
        {
            Id = id;
            Name = name;
            Description = description;
            Quantity = quantity;
            Weight = weight;
            Type = type;
            Value = value;
        }

        /// <summary>
        /// 使用物品
        /// </summary>
        /// <param name="user">使用者</param>
        /// <param name="target">目标（可选）</param>
        /// <returns>使用结果</returns>
        public bool Use(Agent.Agent user, GodotObject target = null)
        {
            if (!IsUsable)
            {
                GD.PrintErr($"物品 {Name} 不可使用");
                return false;
            }

            if (string.IsNullOrEmpty(UseScript))
            {
                GD.Print($"物品 {Name} 没有使用脚本，使用失败");
                return false;
            }

            try
            {
                // 创建脚本上下文
                var context = new Godot.Collections.Dictionary<string, Variant>
                {
                    ["item"] = this,
                    ["user"] = user,
                    ["target"] = Variant.CreateFrom(target),
                    ["quantity"] = Quantity,
                    ["durability"] = Durability
                };

                // 执行使用脚本
                var result = ScriptExecutor.Instance.ExecuteScript(UseScript, context);
                
                // 如果是消耗品，减少数量
                if (IsConsumable && result.AsBool())
                {
                    Quantity = Math.Max(0, Quantity - 1);
                }
                
                return result.AsBool();
            }
            catch (Exception ex)
            {
                GD.PrintErr($"执行物品使用脚本时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 装备物品
        /// </summary>
        /// <param name="user">装备者</param>
        /// <returns>装备结果</returns>
        public bool Equip(Agent.Agent user)
        {
            if (!IsEquippable)
            {
                GD.PrintErr($"物品 {Name} 不可装备");
                return false;
            }

            if (string.IsNullOrEmpty(EquipScript))
            {
                GD.Print($"物品 {Name} 没有装备脚本，装备失败");
                return false;
            }

            try
            {
                var context = new Godot.Collections.Dictionary<string, Variant>
                {
                    ["item"] = this,
                    ["user"] = user
                };

                var result = ScriptExecutor.Instance.ExecuteScript(EquipScript, context);
                return result.AsBool();
            }
            catch (Exception ex)
            {
                GD.PrintErr($"执行物品装备脚本时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 卸下物品
        /// </summary>
        /// <param name="user">卸下者</param>
        /// <returns>卸下结果</returns>
        public bool Unequip(Agent.Agent user)
        {
            if (!IsEquippable)
            {
                GD.PrintErr($"物品 {Name} 不可装备，无法卸下");
                return false;
            }

            if (string.IsNullOrEmpty(UnequipScript))
            {
                GD.Print($"物品 {Name} 没有卸下脚本，卸下失败");
                return false;
            }

            try
            {
                var context = new Godot.Collections.Dictionary<string, Variant>
                {
                    ["item"] = this,
                    ["user"] = user
                };

                var result = ScriptExecutor.Instance.ExecuteScript(UnequipScript, context);
                return result.AsBool();
            }
            catch (Exception ex)
            {
                GD.PrintErr($"执行物品卸下脚本时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 计算总重量
        /// </summary>
        public int CalculateTotalWeight()
        {
            return Quantity * Weight;
        }

        /// <summary>
        /// 检查是否为指定物品
        /// </summary>
        public bool Is(string itemId)
        {
            return Id == itemId;
        }

        /// <summary>
        /// 检查是否已损坏
        /// </summary>
        public bool IsBroken()
        {
            return Durability <= 0;
        }

        /// <summary>
        /// 检查是否可以堆叠
        /// </summary>
        public bool CanStack()
        {
            return MaxStack > 1;
        }

        /// <summary>
        /// 检查是否可以添加数量
        /// </summary>
        public bool CanAddQuantity(int amount)
        {
            return Quantity + amount <= MaxStack;
        }

        /// <summary>
        /// 添加数量
        /// </summary>
        public bool AddQuantity(int amount)
        {
            if (CanAddQuantity(amount))
            {
                Quantity += amount;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 减少数量
        /// </summary>
        public bool RemoveQuantity(int amount)
        {
            if (Quantity >= amount)
            {
                Quantity -= amount;
                return true;
            }
            return false;
        }

        public override bool Equals(object obj)
        {
            return obj is Item item && Id == item.Id;
        }

        public override string ToString()
        {
            var stackInfo = CanStack() ? $" (数量: {Quantity}/{MaxStack})" : "";
            var durabilityInfo = MaxDurability > 0 ? $" [耐久: {Durability}/{MaxDurability}]" : "";
            var scriptInfo = !string.IsNullOrEmpty(UseScript) ? " [可脚本化]" : "";
            
            return $"{Name}: {Description}{stackInfo}{durabilityInfo}{scriptInfo}";
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id);
        }
    }
} 
