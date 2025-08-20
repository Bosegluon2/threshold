using Godot;
using Threshold.Core;
using System;

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
		
		committeeManager.PolicyNegotiated += (policy, agent, opinion) =>
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
		};
		committeeManager.PolicySettled += async (policy, status) =>
		{
			await committeeUi.Settlement(policy, status);
		};
		
		GD.Print("CommitteScene Ready");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{

	}
}
