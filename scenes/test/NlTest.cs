using Godot;
using Threshold.Core;
using Threshold.Core.Agent;
using Threshold.Core.Utils;
using System;
using System.Threading.Tasks;

public partial class NlTest : Node
{
	// 当节点第一次进入场景树时调用。
	public override void _Ready()
	{
		var mission = new MissionSimulator();
		AddChild(mission);
		var agent = GameManager.Instance.CharacterManager.GetCharacterById("dr_cw");
		mission.AddAgent(agent);
		mission.MissionBasePlace = GameManager.Instance.Library.GetPlace("prometheus_observatory_hall");
		mission.MissionTargetPlace = GameManager.Instance.Library.GetPlace("farm");
		mission.MissionType = MissionType.Production;
		mission.MissionDangerLevel = 3.0f;
		mission.FoodSupply = mission.GetResourceLimits().MaxFood;
		mission.WaterSupply = mission.GetResourceLimits().MaxWater;
		mission.StartMission();
		int currentTurn = 0;
		// 轮询式运行
		while (!mission.IsMissionFinished())
		{
			mission.Step(currentTurn);
			GD.Print("--------------------------------");
			GD.Print("currentTurn: " + currentTurn);
			GD.Print("--------------------------------");
			GD.Print("MissionAgents: " + mission.MissionAgents.Count);
			foreach (var missionAgent in mission.MissionAgents)
			{
				missionAgent.Step(currentTurn);
				GD.Print(missionAgent.AgentName);
				GD.Print(missionAgent.CurrentPlace.Name);
				GD.Print(missionAgent.CurrentHealth);
				GD.Print(missionAgent.CurrentEnergy);
			}
			// 获取详细状态
			var status = mission.GetMissionStatus();
			var runningStatus = mission.GetMissionRunningStatus();

			GD.Print(status);
			GD.Print(runningStatus);

			// 等待下一回合
			currentTurn++;
		}
		GD.Print(mission.GetMissionStatus());
		GD.Print("任务结束");
		GD.Print(agent.GetStatusInfo());
	}

	// 每帧调用一次。'delta' 是前一帧后经过的时间。
	public override void _Process(double delta)
	{
	}
}
