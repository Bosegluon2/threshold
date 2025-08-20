using Godot;
using System;

public partial class CharacterButton : Button
{
	private bool _lastIsHovered = false;
	public string CharacterId = "";
	public override void _Ready()
	{
		_lastIsHovered = IsHovered();
	}
	public void SetCharacterId(string id)
	{
		CharacterId = id;
		Image characterImage = Image.LoadFromFile($"./data/image/characters/{CharacterId}/idle.png");
		ImageTexture imageTexture = new ImageTexture();
		imageTexture.SetImage(characterImage);
		GetNode<TextureRect>("Character").Texture = imageTexture;
	}
	public override void _Process(double delta)
	{
		bool currentIsHovered = IsHovered();
		if (currentIsHovered != _lastIsHovered)
		{
			var tween = CreateTween();
			tween.TweenProperty(this, "custom_minimum_size", currentIsHovered ? new Vector2(300, 100) : new Vector2(200, 100), 0.2f).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
			_lastIsHovered = currentIsHovered;
		}
	}
}
