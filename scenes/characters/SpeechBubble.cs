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
		
		// 检查label是否仍然有效
		if (label != null && IsInstanceValid(label))
		{
			label.Text = text;
			Visible = true;
			
			// 检查对象是否仍然有效，如果无效则提前返回
			if (!IsInstanceValid(this))
			{
				return;
			}
			
			await ToSignal(GetTree().CreateTimer(duration), "timeout");
			
			// 再次检查对象是否仍然有效
			if (IsInstanceValid(this))
			{
				Visible = false;
			}
		}
	}
}
