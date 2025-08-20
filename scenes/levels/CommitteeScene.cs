using Godot;
using Threshold.Core;
using System;
using Threshold.Core.Agent;
public partial class CommitteeScene : Node3D
{
	// Called when the node enters the scene tree for the first time.
	CommitteeManager committeeManager;
	CommitteeUi committeeUi;
	public override void _Ready()
	{
		
		committeeManager = GameManager.Instance.CommitteeManager;
		committeeManager.UpdateAvailableAgents();
		PackedScene characterScene = GD.Load<PackedScene>("res://scenes/characters/Character.tscn");
		// 10度间隔
		float angle = MathF.PI / 180.0f * 20.0f;
		float radius = 6.0f;
		int count = committeeManager.GetAvailableAgents().Count;
		float startAngle = -(count - 1) * 0.5f * angle;
		foreach (var agent in committeeManager.GetAvailableAgents())
		{
			Character character = characterScene.Instantiate<Character>();
			character.Id = agent.AgentId;
			AddChild(character);
			character.Position = new Vector3(MathF.Cos(startAngle), 0, MathF.Sin(startAngle)) * radius;
			startAngle += angle;
		}
		committeeUi = GetNode<CommitteeUi>("CommitteeUi");
		committeeUi.SetStatus("休庭");
		committeeUi.Reset();
		
		// 使用Connect方法订阅事件，这样可以在_ExitTree中正确取消订阅
		committeeManager.PolicyNegotiated += OnPolicyNegotiated;
		committeeManager.PolicySettled += OnPolicySettled;
		
		GD.Print("CommitteScene Ready");
	}

	private void OnPolicyNegotiated(Policy policy, Agent agent, PolicyPersonalOpinion opinion)
	{
		// 检查UI是否仍然有效
		if (committeeUi != null && IsInstanceValid(committeeUi))
		{
			if (opinion.status == PolicyStatusType.Approved)
			{
				committeeUi.AddApprove(agent);
			}
			else if (opinion.status == PolicyStatusType.Rejected)
			{
				committeeUi.AddReject(agent);
			}
			committeeUi.AddComment(opinion);
		}
	}

	private async void OnPolicySettled(Policy policy, PolicyStatusType status)
	{
		// 检查UI是否仍然有效
		if (committeeUi != null && IsInstanceValid(committeeUi))
		{
			await committeeUi.Settlement(policy, status);
		}
	}

	public override void _ExitTree()
	{
		// 取消事件订阅，防止在场景销毁后仍然调用已销毁的UI对象
		if (committeeManager != null)
		{
			committeeManager.PolicyNegotiated -= OnPolicyNegotiated;
			committeeManager.PolicySettled -= OnPolicySettled;
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{

	}
}
