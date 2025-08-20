using Godot;
using System;
using System.Formats.Tar;
using Threshold.Core.Data;
using static Godot.RichTextLabel;
using Threshold.Core;

public partial class HintPanel : PopupPanel
{
	public string hintIndex = "";
	public string hintType = "";
	private string hintText = "";
	private string test = "";
	private Place place;
	private Label titleLabel;
	private Label subtitleLabel;
	private Label contentLabel;
	private TextureRect icon;
	private TextureRect iconRes;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		iconRes = (TextureRect)GetNode<TextureRect>("M/Content/Resources/Icon").Duplicate();
		GetNode<TextureRect>("M/Content/Resources/Icon").QueueFree();
		titleLabel = GetNode<Label>("M/Content/H/Title");
		subtitleLabel = GetNode<Label>("M/Content/H/Subtitle");
		contentLabel = GetNode<Label>("M/Content/Content");
		icon = GetNode<TextureRect>("M/Icon");
	}
	public void SetHint(string id){
		place = GameManager.Instance.Library.GetPlace(id);
		hintIndex = place.Id;
		hintType = place.Type;
		UpdateHint();
	}
	public void UpdateHint(){

		titleLabel.Text = place.Name.ToUpper();
		subtitleLabel.Text = place.Type.ToUpper();
		contentLabel.Text = place.Description;
		foreach (Node node in GetNode<HBoxContainer>("M/Content/Resources").GetChildren()){
			node.QueueFree();
		}

		
	}
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
