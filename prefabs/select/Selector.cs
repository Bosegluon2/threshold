
using Godot;
using System;

public partial class Selector : Node3D
{
	public Vector3 TargetPosition { get; set; }
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		Position=Position.Lerp(TargetPosition, (float)delta*10.0f);
	}
}
