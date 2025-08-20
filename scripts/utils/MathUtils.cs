using Godot;
using System;
namespace Threshold.Core.Utils
{
public static class MathUtils
{
	public static float DegToRad(float degrees)
	{
		return degrees * (float)Math.PI / 180.0f;
	}
}
}