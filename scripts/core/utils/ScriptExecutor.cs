using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Collections.Concurrent;

namespace Threshold.Core.Utils
{
    /// <summary>
    /// 脚本执行器（GDScript 版本）
    /// 支持 GDScript 语法、上下文传递、编译缓存
    /// 保留原有路径解析、快捷变量功能
    /// </summary>
    public partial class ScriptExecutor :Node
    {
        private static ScriptExecutor _instance;
        public static ScriptExecutor Instance => _instance;
        
        public override void _Ready()
        {
            _instance = this;
        }

        private const int MAX_RECURSION_DEPTH = 3;

        // GDScript 编译缓存
        private readonly ConcurrentDictionary<string, GDScript> ScriptCache = new();

        // 全局上下文对象，用于在 GDScript 中访问游戏数据
        private Godot.Collections.Dictionary<string,Variant> _globalContext;

        /// <summary>
        /// 对外暴露的全局上下文对象
        /// </summary>
        public Godot.Collections.Dictionary<string,Variant> Context => _globalContext;


        /// <summary>
        /// 脚本上下文类，用于在 GDScript 中访问游戏数据
        /// </summary>
        public partial class ScriptContext : GodotObject
        {
            // 快捷变量
            public GameManager GameManager => GameManager.Instance;
            public Global Global => Global.Instance;
            public CharacterManager CharacterManager => GameManager.Instance?.CharacterManager;
            public WorldManager WorldManager => GameManager.Instance?.WorldManager;
            //public EventManager EventManager => GameManager.Instance?.EventManager;
            public ResourceManager ResourceManager => GameManager.Instance?.ResourceManager;
            public SaveManager SaveManager => GameManager.Instance?.SaveManager;
            public Library Library => GameManager.Instance?.Library;
            public CommitteeManager CommitteeManager => GameManager.Instance?.CommitteeManager;

            // 路径操作函数
            public object PathGet(object target, string path) => ScriptExecutor.GetValue(target, path);
            public bool PathSet(object target, string path, object value) => ScriptExecutor.SetValue(target, path, value);
            public bool PathExists(object target, string path) => ScriptExecutor.Exists(target, path);
            public void PathExplore(object obj) => ScriptExecutor.ExplorePaths(obj);

            // 工具函数
            public void Print(object message) => GD.Print(message);
            public void PrintErr(object message) => GD.PrintErr(message);
            public Random Random => new Random();
        }

        /// <summary>
        /// 执行 GDScript 并返回结果
        /// </summary>
        public Variant ExecuteScript(string script, Godot.Collections.Dictionary<string,Variant> context = null)
        {
            _globalContext = context;
            if (string.IsNullOrWhiteSpace(script))
            {
                GD.PrintErr("脚本内容为空");
                return Variant.CreateFrom("");
            }

            try
            {
                GD.Print("=== 开始执行 GDScript ===");

                // 从缓存获取或编译脚本
                var gdScript = ScriptCache.GetOrAdd(script, code =>
                {
                    GD.Print("首次编译 GDScript，加入缓存...");
                    var newScript = new GDScript();
                    // 将code的每一行开头添加indent
                    code = string.Join("\n", code.Split('\n').Select(line => "    " + line));
                    // 添加方法头
                    newScript.SetSourceCode("extends RefCounted\n \nstatic func GetContext(key): return ScriptExecutor.Context[key]\nstatic func execute():\n" + code);
                    GD.Print($"编译后的脚本内容:\n{newScript.GetSourceCode()}");
                    newScript.Reload();
                    return newScript;
                });

                // 创建脚本实例
                var scriptInstanceObj = gdScript.New();
                if (scriptInstanceObj.Equals(null))
                {
                    GD.PrintErr("无法创建脚本实例");
                    return Variant.CreateFrom("");
                }

                // 注意：New() 返回的是 RefCounted，需要强制转换为 GodotObject
                var scriptInstance = (RefCounted)scriptInstanceObj;
                if (scriptInstance.Equals(null))
                {
                    GD.PrintErr("脚本实例不是 GodotObject 类型");
                    return Variant.CreateFrom("");
                }

                // 设置上下文变量
                if (context != null)
                {
                    SetContextVariables(scriptInstance, context);
                }

                // 不再设置 Context 到脚本实例，直接通过 ScriptExecutor.Context 访问

                // 执行脚本
                var result = scriptInstance.Call("execute");
                GD.Print($"脚本执行完成，返回值: {result}");
                return result;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"GDScript 执行失败: {ex.Message}");
                return Variant.CreateFrom("");
            }
        }

        /// <summary>
        /// 执行脚本并返回指定类型的结果
        /// </summary>
        public T ExecuteScript<T>(string script, Godot.Collections.Dictionary<string,Variant> context = null)
        {
            try
            {
                var result = ExecuteScript(script, context);
                return ConvertVariantToType<T>(result);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"脚本执行或类型转换失败: {ex.Message}");
                return default(T);
            }
        }

        /// <summary>
        /// 设置脚本实例的上下文变量
        /// </summary>
        private static void SetContextVariables(GodotObject scriptInstance, object context)
        {
            try
            {
                if (context is Dictionary<string, object> contextDict)
                {
                    foreach (var kvp in contextDict)
                    {
                        scriptInstance.Set(kvp.Key, ConvertToVariant(kvp.Value));
                    }
                }
                else if (context is IDictionary<string, object> dict)
                {
                    foreach (var kvp in dict)
                    {
                        scriptInstance.Set(kvp.Key, ConvertToVariant(kvp.Value));
                    }
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"设置上下文变量失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 将 C# 对象转换为 Godot Variant
        /// </summary>
        private static Variant ConvertToVariant(object value)
        {
            if (value == null) return Variant.CreateFrom("");
            
            try
            {
                switch (value)
                {
                    case int intVal: return Variant.CreateFrom(intVal);
                    case float floatVal: return Variant.CreateFrom(floatVal);
                    case double doubleVal: return Variant.CreateFrom((float)doubleVal);
                    case bool boolVal: return Variant.CreateFrom(boolVal);
                    case string stringVal: return Variant.CreateFrom(stringVal);
                    case GodotObject godotObj: return Variant.CreateFrom(godotObj);
                    default: return Variant.CreateFrom(value.ToString());
                }
            }
            catch
            {
                return Variant.CreateFrom(value.ToString());
            }
        }

        /// <summary>
        /// 将 Godot Variant 转换为指定类型
        /// </summary>
        private static T ConvertVariantToType<T>(Variant variant)
        {
            try
            {
                if (typeof(T) == typeof(int))
                    return (T)(object)variant.AsInt32();
                if (typeof(T) == typeof(float))
                    return (T)(object)variant.AsSingle();
                if (typeof(T) == typeof(double))
                    return (T)(object)variant.AsDouble();
                if (typeof(T) == typeof(bool))
                    return (T)(object)variant.AsBool();
                if (typeof(T) == typeof(string))
                    return (T)(object)variant.AsString();
                if (typeof(T) == typeof(object))
                    return (T)variant.Obj;
                
                return default(T);
            }
            catch
            {
                return default(T);
            }
        }

        /// <summary>
        /// 生成 GDScript 模板
        /// </summary>
        public static string GenerateGDScriptTemplate(string scriptContent)
        {
            return $@"
extends RefCounted

# 脚本执行入口函数
func execute():
{scriptContent}
";
        }

        #region 路径解析功能
        public static object GetValue(object target, string path)
        {
            if (target == null || string.IsNullOrEmpty(path))
                return null;

            var pathParts = path.Split('.');
            var current = target;

            foreach (var part in pathParts)
            {
                if (current == null)
                    return null;

                current = GetPropertyValue(current, part);
            }

            return current;
        }

        public static bool SetValue(object target, string path, object value)
        {
            if (target == null || string.IsNullOrEmpty(path))
                return false;

            var pathParts = path.Split('.');
            var current = target;

            for (int i = 0; i < pathParts.Length - 1; i++)
            {
                if (current == null)
                    return false;

                current = GetPropertyValue(current, pathParts[i]);
            }

            if (current == null)
                return false;

            return SetPropertyValue(current, pathParts[^1], value);
        }

        public static bool Exists(object target, string path)
        {
            return GetValue(target, path) != null;
        }

        public static void ExplorePaths(object obj)
        {
            if (obj == null)
            {
                GD.PrintErr("对象为空，无法探索路径");
                return;
            }

            var objName = obj.GetType().Name;
            GD.Print($"=== {objName} 数据路径探索 ===");
            
            var paths = GetAllAccessiblePaths(obj, objName.ToLower(), 0);
            
            GD.Print($"发现 {paths.Count} 个可访问路径:");
            foreach (var path in paths.OrderBy(p => p))
            {
                GD.Print($"  {path}");
            }
            
            GD.Print("=== 路径探索完成 ===");
        }

        private static object GetPropertyValue(object target, string propertyName)
        {
            if (target == null)
                return null;

            if (target is IDictionary<string, object> dict)
            {
                return dict.ContainsKey(propertyName) ? dict[propertyName] : null;
            }

            var type = target.GetType();
            var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property != null && property.CanRead)
            {
                return property.GetValue(target);
            }

            var field = type.GetField(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (field != null)
            {
                return field.GetValue(target);
            }

            return null;
        }

        private static bool SetPropertyValue(object target, string propertyName, object value)
        {
            if (target == null)
                return false;

            if (target is IDictionary<string, object> dict)
            {
                dict[propertyName] = value;
                return true;
            }

            var type = target.GetType();
            var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property != null && property.CanWrite)
            {
                try
                {
                    var convertedValue = Convert.ChangeType(value, property.PropertyType);
                    property.SetValue(target, convertedValue);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            var field = type.GetField(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (field != null)
            {
                try
                {
                    var convertedValue = Convert.ChangeType(value, field.FieldType);
                    field.SetValue(target, convertedValue);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        private static List<string> GetAllAccessiblePaths(object obj, string currentPath, int currentDepth)
        {
            var paths = new List<string>();
            
            if (obj == null || currentDepth >= MAX_RECURSION_DEPTH) 
            {
                if (currentDepth >= MAX_RECURSION_DEPTH)
                {
                    GD.Print($"达到最大递归深度 {MAX_RECURSION_DEPTH}，停止探索路径: {currentPath}");
                }
                return paths;
            }
            
            var type = obj.GetType();
            paths.Add(currentPath);
            
            // 如果是GameManager，添加快捷路径
            if (currentDepth == 0 && obj is GameManager)
            {
                AddShortcutPaths(paths);
            }
            
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var property in properties)
            {
                try
                {
                    if (property.CanRead && !property.GetIndexParameters().Any())
                    {
                        // 跳过委托类型的属性（如快捷方法）
                        if (typeof(Delegate).IsAssignableFrom(property.PropertyType))
                        {
                            continue;
                        }
                        
                        var value = property.GetValue(obj);
                        var propertyPath = $"{currentPath}.{property.Name}";
                        
                        paths.Add(propertyPath);
                        
                        if (value != null && 
                            !value.GetType().IsPrimitive && 
                            value.GetType() != typeof(string) &&
                            !typeof(Delegate).IsAssignableFrom(value.GetType()) &&
                            !paths.Contains(propertyPath))
                        {
                            var subPaths = GetAllAccessiblePaths(value, propertyPath, currentDepth + 1);
                            paths.AddRange(subPaths);
                        }
                    }
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"探索属性 {property.Name} 时发生错误: {ex.Message}");
                }
            }
            
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                try
                {
                    // 跳过委托类型的字段
                    if (typeof(Delegate).IsAssignableFrom(field.FieldType))
                    {
                        continue;
                    }
                    
                    var value = field.GetValue(obj);
                    var fieldPath = $"{currentPath}.{field.Name}";
                    
                    paths.Add(fieldPath);
                    
                    if (value != null && 
                        !value.GetType().IsPrimitive && 
                        value.GetType() != typeof(string) &&
                        !typeof(Delegate).IsAssignableFrom(value.GetType()) &&
                        !paths.Contains(fieldPath))
                    {
                        var subPaths = GetAllAccessiblePaths(value, fieldPath, currentDepth + 1);
                        paths.AddRange(subPaths);
                    }
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"探索字段 {field.Name} 时发生错误: {ex.Message}");
                }
            }
            
            if (obj is IDictionary<string, object> dict)
            {
                foreach (var kvp in dict)
                {
                    var dictPath = $"{currentPath}[\"{kvp.Key}\"]";
                    paths.Add(dictPath);
                    
                    if (kvp.Value != null && 
                        !kvp.Value.GetType().IsPrimitive && 
                        kvp.Value.GetType() != typeof(string) &&
                        !typeof(Delegate).IsAssignableFrom(kvp.Value.GetType()))
                    {
                        var subPaths = GetAllAccessiblePaths(kvp.Value, dictPath, currentDepth + 1);
                        paths.AddRange(subPaths);
                    }
                }
            }
            
            if (obj is System.Collections.IList list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var listPath = $"{currentPath}[{i}]";
                    paths.Add(listPath);
                    
                    var item = list[i];
                    if (item != null && 
                        !item.GetType().IsPrimitive && 
                        item.GetType() != typeof(string) &&
                        !typeof(Delegate).IsAssignableFrom(item.GetType()))
                    {
                        var subPaths = GetAllAccessiblePaths(item, listPath, currentDepth + 1);
                        paths.AddRange(subPaths);
                    }
                }
            }
            
            return paths.Distinct().ToList();
        }

        /// <summary>
        /// 添加快捷路径（与脚本中的快捷变量对应）
        /// </summary>
        private static void AddShortcutPaths(List<string> paths)
        {
            try
            {
                // 添加快捷路径，与静态构造函数中设置的变量对应
                paths.Add("CharacterManager");
                paths.Add("WorldManager");
                paths.Add("EventManager");
                paths.Add("ResourceManager");
                paths.Add("SaveManager");
                paths.Add("Library");
                paths.Add("CommitteeManager");
                paths.Add("Global");
                paths.Add("Random");
                
                // 添加路径操作函数
                paths.Add("PathGet");
                paths.Add("PathSet");
                paths.Add("PathExists");
                paths.Add("PathExplore");
                
                GD.Print("已添加快捷路径，与脚本中的快捷变量对应");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"添加快捷路径失败: {ex.Message}");
            }
        }
        #endregion
    }
} 
