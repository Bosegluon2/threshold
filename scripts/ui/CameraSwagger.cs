using Godot;
using System;

public partial class CameraSwagger : Camera3D
{
	Vector3 originalPosition;
	[Export]
	FastNoiseLite noiser;
	double time = 0;
	public override void _Ready()
	{
		originalPosition = this.Position;
	}

	public override void _Process(double delta)
	{
		time += delta;
		// 获取鼠标在屏幕上的位置
		Vector2 mousePos = GetViewport().GetMousePosition();
		// 获取屏幕中心
		Vector2 screenCenter = GetViewport().GetVisibleRect().Size / 2.0f;
		// 鼠标偏移（以屏幕中心为原点，缩放以适应场景）
		Vector2 offset = (mousePos - screenCenter) * 0.00005f; // 调整缩放系数以适应实际需求

		// 鼠标位置转换为xy平面上的偏移
		Vector3 mouseOffset = new Vector3(offset.X, -offset.Y, 0);

		// 生成轻微噪波晃动
		float noiseStrength = 0.01f;
		Vector3 noise = new Vector3(noiser.GetNoise3D((float)time, 0.0f, 0.0f), noiser.GetNoise3D((float)time + 10000.0f, 0.0f, 0.0f), noiser.GetNoise3D((float)time + 20000.0f, 0.0f, 0.0f)) * noiseStrength;

		// 位置 = 鼠标位置转换为xz + 原本位置 + 噪波轻微晃动
		this.Position = originalPosition + mouseOffset + noise;
		
	}
}
