using Godot;
using System;

public partial class Fader : CanvasLayer
{
	public ColorRect modulator;
	private Color _color = new Color(0, 0, 0, 1);
	private static Fader _instance;
	public static Fader Instance => _instance;
	public override void _EnterTree()
	{
		_instance = this; // Ensure the instance is set
		modulator = GetNode<ColorRect>("Modulator");
		modulator.Color = new Color(0, 0, 0, 1); // Set initial color to black with full opacity
		modulator.Visible = true; // Ensure the modulator is visible
		FadeIn(5.0f);
		// Initialization code can go here if needed
	}
	public void FadeIn(float duration,Callable callback = new Callable(),params Variant[] args)
	{
		GD.Print("FadeIn");
		modulator.Modulate = _color; // Start with black
		modulator.Visible = true; // Ensure the modulator is visible
		var tween = GetTree().CreateTween();
		tween.TweenProperty(modulator, "modulate", new Color(0, 0, 0, 0), duration).SetTrans(Tween.TransitionType.Linear).SetEase(Tween.EaseType.InOut);
		tween.Finished += () =>
		{
			GD.Print("FadeIn finished");
			 modulator.Visible = false; // Hide the modulator after fading out
			if(!callback.Equals(new Callable()))
				callback.Call(args);
		};
	}
	public void FadeOut(float duration,Callable callback = new Callable(), params Variant[] args)
	{
		GD.Print("FadeOut");
		modulator.Modulate = new Color(0, 0, 0, 0); // Start with transparent
		modulator.Visible = true; // Ensure the modulator is visible
		var tween = GetTree().CreateTween();
		tween.TweenProperty(modulator, "modulate", new Color(0, 0, 0, 1), duration).SetTrans(Tween.TransitionType.Linear).SetEase(Tween.EaseType.InOut);
		tween.Finished += () =>
		{
			// modulator.Visible = false; // Hide the modulator after fading out
			GD.Print("FadeOut finished");
			if (!callback.Equals(new Callable()))
				callback.Call(args);

		};
	}

	/// <summary>
	/// Sets the color of the fader and updates the modulator's color.
	/// This method is better called when the modulator is invisible.
	/// </summary>
	/// <param name="color">The new color to apply to the fader.</param>
	public void SetColor(Color color)
	{
		_color = color;
		modulator.Modulate = _color;
	}
}
