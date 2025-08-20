using Godot;
using System;

public partial class SpeechBubble : Node3D
{
	Label label;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		label = GetNode<Label>("Sprite3D/SubViewport/Control/Label");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	public async void Speak(string text,float duration=3.0f)
	{
		GD.Print("Speak: " + text);
		label.Text = text;
		Visible = true;
		await ToSignal(GetTree().CreateTimer(duration), "timeout");
		Visible = false;
	}
}
