using Godot;
using Threshold.Core.Data;
using System;

public partial class SelectProxy : StaticBody3D
{
	public enum SelectProxyType
	{
		Descovered,
		Selected,
		Undiscovered,
		Unreachable
	}
	// Called when the node enters the scene tree for the first time.
	public SelectProxyType Type = SelectProxyType.Undiscovered;
	public Place Place;
	public bool IsSelected = false;
	public bool FirstSelect = false;
	public override void _Ready()
	{
		// var Banner = GetNode<MeshInstance3D>("Banner");
		// var BannerMaterial = (ShaderMaterial)Banner.Mesh.SurfaceGetMaterial(0);
		// BannerMaterial.SetShaderParameter("line_color", new Color(1, 0, 0));
		// BannerMaterial.SetShaderParameter("line_width", 0.05f);
		// BannerMaterial.SetShaderParameter("line_spacing", 0.25f);
		// BannerMaterial.SetShaderParameter("speed", 0.2f);
		// BannerMaterial.SetShaderParameter("alpha", 0.5f);
	}
	
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
