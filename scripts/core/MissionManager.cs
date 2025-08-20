using Godot;
using Godot.Collections;
using Threshold.Core.Agent;
using Threshold.Core.Data;
using System;
using System.Collections.Generic;

namespace Threshold.Core
{
    /// <summary>
    /// 任务管理器，负责管理所有正在进行的任务
    /// </summary>
    public partial class MissionManager : Node
    {
        #region Events
        [Signal] public delegate void MissionStartedEventHandler(MissionSimulator mission);
        [Signal] public delegate void MissionCompletedEventHandler(MissionSimulator mission, MissionOutcome outcome);
        [Signal] public delegate void MissionFailedEventHandler(MissionSimulator mission, MissionOutcome outcome);
        [Signal] public delegate void MissionStepCompletedEventHandler(MissionSimulator mission, int turn);
        #endregion

        #region Properties
        /// <summary>
        /// 所有正在进行的任务
        /// </summary>
        [Export] public Array<MissionSimulator> ActiveMissions { get; private set; } = new Array<MissionSimulator>();
        
        /// <summary>
        /// 已完成的任务历史
        /// </summary>
        [Export] public Array<MissionSimulator> CompletedMissions { get; private set; } = new Array<MissionSimulator>();
        
        /// <summary>
        /// 任务ID计数器
        /// </summary>
        private int nextMissionId = 1;
        #endregion

        public override void _Ready()
        {
            GD.Print("MissionManager 已初始化");
        }

        /// <summary>
        /// 创建新任务
        /// </summary>
        /// <param name="missionName">任务名称</param>
        /// <param name="missionType">任务类型</param>
        /// <param name="dangerLevel">危险等级</param>
        /// <param name="basePlace">基地地点</param>
        /// <param name="targetPlace">目标地点</param>
        /// <param name="agents">参与人员</param>
        /// <param name="foodSupply">食物补给数量</param>
        /// <param name="waterSupply">水源补给数量</param>
        /// <returns>新创建的任务</returns>
        public MissionSimulator CreateMission(string missionName, MissionType missionType, float dangerLevel, 
            Place basePlace, Place targetPlace, Array<Agent.Agent> agents, int foodSupply = 0, int waterSupply = 0)
        {
            try
            {
                // 创建新任务
                var mission = new MissionSimulator();
                AddChild(mission);
                
                // 设置任务参数
                mission.MissionName = missionName;
                mission.MissionType = missionType;
                mission.MissionDangerLevel = dangerLevel;
                mission.MissionBasePlace = basePlace;
                mission.MissionTargetPlace = targetPlace;
                mission.FoodSupply = foodSupply;
                mission.WaterSupply = waterSupply;
                // 添加人员
                foreach (var agent in agents)
                {
                    if (agent != null)
                    {
                        mission.AddAgent(agent);
                    }
                }
                
                // 添加到活跃任务列表
                ActiveMissions.Add(mission);
                
                GD.Print($"新任务已创建: {missionName} (ID: {nextMissionId})");
                nextMissionId++;
                
                return mission;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"创建任务失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 开始任务
        /// </summary>
        /// <param name="mission">要开始的任务</param>
        /// <returns>是否成功开始</returns>
        public bool StartMission(MissionSimulator mission)
        {
            if (mission == null || !ActiveMissions.Contains(mission))
            {
                GD.PrintErr("无法开始任务：任务不存在或未在活跃列表中");
                return false;
            }

            try
            {
                mission.StartMission();
                EmitSignal(SignalName.MissionStarted, mission);
                GD.Print($"任务已开始: {mission.MissionName}");
                return true;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"开始任务失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 执行任务步进
        /// </summary>
        /// <param name="mission">要步进的任务</param>
        /// <param name="turn">当前回合</param>
        /// <returns>是否成功步进</returns>
        public bool StepMission(MissionSimulator mission, int turn)
        {
            if (mission == null || !ActiveMissions.Contains(mission))
            {
                GD.PrintErr("无法步进任务：任务不存在或未在活跃列表中");
                return false;
            }

            try
            {
                mission.Step(turn);
                EmitSignal(SignalName.MissionStepCompleted, mission, turn);
                
                // 检查任务是否完成
                if (mission.IsMissionFinished())
                {
                    CompleteMission(mission);
                }
                
                return true;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"任务步进失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 完成任务
        /// </summary>
        /// <param name="mission">要完成的任务</param>
        private void CompleteMission(MissionSimulator mission)
        {
            if (mission == null) return;

            try
            {
                // 从活跃任务列表移除
                ActiveMissions.Remove(mission);
                
                // 添加到已完成任务列表
                CompletedMissions.Add(mission);
                
                // 根据任务结果发送相应信号
                var outcome = mission.FinalOutcome;
                if (outcome == MissionOutcome.Success || outcome == MissionOutcome.PartialSuccess)
                {
                    EmitSignal(SignalName.MissionCompleted, mission, outcome.GetHashCode());
                    GD.Print($"任务已完成: {mission.MissionName} - {outcome}");
                }
                else
                {
                    EmitSignal(SignalName.MissionFailed, mission, outcome.GetHashCode());
                    GD.Print($"任务失败: {mission.MissionName} - {outcome}");
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"完成任务处理失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 停止任务
        /// </summary>
        /// <param name="mission">要停止的任务</param>
        /// <returns>是否成功停止</returns>
        public bool StopMission(MissionSimulator mission)
        {
            if (mission == null) return false;

            try
            {
                // 从活跃任务列表移除
                if (ActiveMissions.Contains(mission))
                {
                    ActiveMissions.Remove(mission);
                }
                
                // 释放任务资源
                mission.QueueFree();
                
                GD.Print($"任务已停止: {mission.MissionName}");
                return true;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"停止任务失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取任务统计信息
        /// </summary>
        /// <returns>任务统计信息字符串</returns>
        public string GetMissionStatistics()
        {
            var stats = "=== 任务统计信息 ===\n";
            stats += $"活跃任务数量: {ActiveMissions.Count}\n";
            stats += $"已完成任务数量: {CompletedMissions.Count}\n";
            stats += $"总任务数量: {ActiveMissions.Count + CompletedMissions.Count}\n\n";
            
            if (ActiveMissions.Count > 0)
            {
                stats += "=== 活跃任务 ===\n";
                for (int i = 0; i < ActiveMissions.Count; i++)
                {
                    var mission = ActiveMissions[i];
                    if (mission != null)
                    {
                        stats += $"{i + 1}. {mission.MissionName} - {mission.CurrentPhase} (回合: {mission.MissionCurrentTurn})\n";
                    }
                }
            }
            
            if (CompletedMissions.Count > 0)
            {
                stats += "\n=== 最近完成的任务 ===\n";
                var recentCount = Math.Min(5, CompletedMissions.Count);
                for (int i = CompletedMissions.Count - recentCount; i < CompletedMissions.Count; i++)
                {
                    var mission = CompletedMissions[i];
                    if (mission != null)
                    {
                        stats += $"{i + 1}. {mission.MissionName} - {mission.FinalOutcome}\n";
                    }
                }
            }
            
            return stats;
        }

        /// <summary>
        /// 获取指定任务
        /// </summary>
        /// <param name="missionName">任务名称</param>
        /// <returns>任务实例，如果不存在则返回null</returns>
        public MissionSimulator GetMission(string missionName)
        {
            foreach (var mission in ActiveMissions)
            {
                if (mission != null && mission.MissionName == missionName)
                {
                    return mission;
                }
            }
            return null;
        }

        /// <summary>
        /// 检查是否有同名任务
        /// </summary>
        /// <param name="missionName">任务名称</param>
        /// <returns>是否存在同名任务</returns>
        public bool HasMissionWithName(string missionName)
        {
            return GetMission(missionName) != null;
        }

        /// <summary>
        /// 清理已完成的任务
        /// </summary>
        /// <param name="maxKeepCount">最大保留数量</param>
        public void CleanupCompletedMissions(int maxKeepCount = 10)
        {
            if (CompletedMissions.Count <= maxKeepCount) return;

            var removeCount = CompletedMissions.Count - maxKeepCount;
            for (int i = 0; i < removeCount; i++)
            {
                if (CompletedMissions.Count > 0)
                {
                    var mission = CompletedMissions[0];
                    CompletedMissions.RemoveAt(0);
                    if (mission != null)
                    {
                        mission.QueueFree();
                    }
                }
            }
            
            GD.Print($"已清理 {removeCount} 个已完成任务");
        }
    }
}
