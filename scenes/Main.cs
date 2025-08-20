using Godot;
using System;

public partial class Main : Node3D
{
	// Called when the node enters the scene tree for the first time.\
	public override void _EnterTree()
	{
		GD.Print("Main enter tree");
		Fader.Instance.SetColor(new Color(0, 0, 0, 1));
		Fader.Instance.FadeIn(3.0f);
	}
	public override void _Ready()
	{
		GD.Print("Main ready");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		
	}
}
