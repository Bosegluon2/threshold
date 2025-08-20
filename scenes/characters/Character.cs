using Godot;
using Threshold.Core;
using Threshold.Core.Agent;
using System;

public partial class Character : Node3D
{
	public SpeechBubble speechBubble;
	[Export] public string Id;
	public Agent agent;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		speechBubble = GetNode<SpeechBubble>("SpeechBubble");
		speechBubble.Visible = false;
		agent = GameManager.Instance.CharacterManager.GetCharacterById(Id);
		if (agent == null)
		{
			GD.Print("Character not found: " + Id);
			return;
		}
		GameManager.Instance.CommitteeManager.PolicyNegotiated += OnPolicyNegotiated;
		
	}

    private void OnPolicyNegotiated(Policy policy, Agent agent, PolicyPersonalOpinion opinion)
    {
        
        if (this.agent.AgentId == agent.AgentId)
        {
			GD.Print("OnPolicyNegotiated: " + agent.AgentId + " " + opinion.status.ToString());
            Speak(opinion.status.ToString());
        }
    }

    public void Speak(string text)
	{
		speechBubble.Speak(text);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
