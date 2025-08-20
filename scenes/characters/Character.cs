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
        // 检查对象是否仍然有效
        if (!IsInstanceValid(this) || this.agent == null || speechBubble == null || !IsInstanceValid(speechBubble))
        {
            return;
        }
        
        if (this.agent.AgentId == agent.AgentId)
        {
			GD.Print("OnPolicyNegotiated: " + agent.AgentId + " " + opinion.status.ToString());
            Speak(opinion.status.ToString());
        }
    }

    public void Speak(string text)
	{
        // 检查speechBubble是否仍然有效
        if (speechBubble != null && IsInstanceValid(speechBubble))
        {
            speechBubble.Speak(text);
        }
	}

    public override void _ExitTree()
    {
        // 取消事件订阅，防止在对象销毁后仍然被调用
        if (GameManager.Instance?.CommitteeManager != null)
        {
            GameManager.Instance.CommitteeManager.PolicyNegotiated -= OnPolicyNegotiated;
        }
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
