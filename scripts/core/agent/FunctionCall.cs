using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;

namespace Threshold.Core.Agent
{
    /// <summary>
    /// 函数调用定义
    /// </summary>
    public partial class FunctionDefinition : Resource
    {
        [Export] public string Name { get; set; } = "";
        [Export] public string Description { get; set; } = "";
        [Export] public Array<FunctionParameter> Parameters { get; set; } = new Array<FunctionParameter>();
        
        public FunctionDefinition() { }
        
        public FunctionDefinition(string name, string description)
        {
            Name = name;
            Description = description;
        }
        
        /// <summary>
        /// 获取OpenAI格式的函数定义
        /// </summary>
        public Dictionary GetOpenAIFormat()
        {
            var parameters = new Dictionary();
            var properties = new Dictionary();
            var required = new Array<string>();
            
            foreach (var param in Parameters)
            {
                properties[param.Name] = new Dictionary
                {
                    ["type"] = param.Type,
                    ["description"] = param.Description
                };
                
                if (param.Required)
                {
                    required.Add(param.Name);
                }
            }
            
            parameters["type"] = "object";
            parameters["properties"] = properties;
            parameters["required"] = required;
            
            return new Dictionary
            {
                ["name"] = Name,
                ["description"] = Description,
                ["parameters"] = parameters
            };
        }
    }
    
    /// <summary>
    /// 函数参数定义
    /// </summary>
    public partial class FunctionParameter : Resource
    {
        [Export] public string Name { get; set; } = "";
        [Export] public string Type { get; set; } = "string";
        [Export] public string Description { get; set; } = "";
        [Export] public bool Required { get; set; } = false;
        
        public FunctionParameter() { }
        
        public FunctionParameter(string name, string type, string description, bool required = false)
        {
            Name = name;
            Type = type;
            Description = description;
            Required = required;
        }
    }
    
    /// <summary>
    /// 函数调用请求
    /// </summary>
    public partial class FunctionCall : Resource
    {
        [Export] public string Name { get; set; } = "";
        [Export] public Dictionary Arguments { get; set; } = new Dictionary();
        
        public FunctionCall() { }
        
        public FunctionCall(string name, Dictionary arguments)
        {
            Name = name;
            Arguments = arguments;
        }
    }
    
    /// <summary>
    /// 函数执行结果
    /// </summary>
    public partial class FunctionResult : Resource
    {
        [Export] public string Name { get; set; } = "";
        [Export] public string Content { get; set; } = "";
        [Export] public bool Success { get; set; } = true;
        [Export] public string ErrorMessage { get; set; } = "";
        
        public FunctionResult() { }
        
        public FunctionResult(string name, string content, bool success = true, string errorMessage = "")
        {
            Name = name;
            Content = content;
            Success = success;
            ErrorMessage = errorMessage;
        }
    }
    
    /// <summary>
    /// 函数执行器接口
    /// </summary>
    public interface IFunctionExecutor
    {
        string GetFunctionName();
        string GetFunctionDescription();
        Array<FunctionParameter> GetParameters();
        FunctionResult Execute(Godot.Collections.Dictionary arguments, System.Collections.Generic.Dictionary<string, object> extraParams = null);
    }
    
    /// <summary>
    /// 函数管理器
    /// </summary>
    public partial class FunctionManager : Resource
    {
        private System.Collections.Generic.Dictionary<string, IFunctionExecutor> functionExecutors = new System.Collections.Generic.Dictionary<string, IFunctionExecutor>();
        
        /// <summary>
        /// 注册函数执行器
        /// </summary>
        public void RegisterFunction(IFunctionExecutor executor)
        {
            functionExecutors[executor.GetFunctionName()] = executor;
        }
        
        /// <summary>
        /// 获取所有可用函数定义
        /// </summary>
        public Array<FunctionDefinition> GetAvailableFunctions()
        {
            var functions = new Array<FunctionDefinition>();
            
            foreach (var executor in functionExecutors.Values)
            {
                var functionDef = new FunctionDefinition(
                    executor.GetFunctionName(),
                    executor.GetFunctionDescription()
                )
                {
                    Parameters = executor.GetParameters()
                };
                functions.Add(functionDef);
            }
            
            return functions;
        }
        
        /// <summary>
        /// 执行函数调用
        /// </summary>
        public FunctionResult ExecuteFunction(FunctionCall functionCall, System.Collections.Generic.Dictionary<string, object> extraParams = null)
        {
            GD.Print($"=== 函数管理器执行函数: {functionCall.Name} ===");
            GD.Print($"函数参数: {Json.Stringify(functionCall.Arguments)}");
            if (extraParams != null)
            {
                GD.Print($"额外参数数量: {extraParams.Count}");
                foreach (var kvp in extraParams)
                {
                    GD.Print($"  {kvp.Key}: {kvp.Value}");
                }
            }
            
            if (!functionExecutors.ContainsKey(functionCall.Name))
            {
                GD.PrintErr($"函数 '{functionCall.Name}' 不存在");
                GD.Print($"可用函数: {string.Join(", ", functionExecutors.Keys)}");
                return new FunctionResult(functionCall.Name, "", false, $"函数 '{functionCall.Name}' 不存在");
            }
            
            try
            {
                var executor = functionExecutors[functionCall.Name];
                GD.Print($"找到函数执行器: {executor.GetType().Name}");
                
                GD.Print($"准备调用executor.Execute，参数: arguments={functionCall.Arguments?.Count ?? 0}, extraParams={extraParams?.Count ?? 0}");
                
                var result = executor.Execute(functionCall.Arguments, extraParams);
                
                if (result == null)
                {
                    GD.PrintErr("函数执行器返回了null结果");
                    return new FunctionResult(functionCall.Name, "", false, "函数执行器返回了null结果");
                }
                
                GD.Print($"函数执行完成: 成功={result.Success}, 内容长度={result.Content?.Length ?? 0}");
                if (!result.Success)
                {
                    GD.PrintErr($"函数执行失败: {result.ErrorMessage}");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"执行函数时发生异常: {ex.Message}");
                GD.PrintErr($"异常堆栈: {ex.StackTrace}");
                return new FunctionResult(functionCall.Name, "", false, $"执行函数时发生错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 检查是否支持函数调用
        /// </summary>
        public bool HasFunctions()
        {
            return functionExecutors.Count > 0;
        }
    }
}
