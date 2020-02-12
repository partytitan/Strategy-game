﻿using UnityEngine;

public static class HexMetrics {

	public const float OuterToInner = 0.866025404f;

	public const float InnerToOuter = 1f / OuterToInner;

	public const float OuterRadius = 10f;

	public const float InnerRadius = OuterRadius * OuterToInner;

	public const float SolidFactor = 0.8f;

	public const float BlendFactor = 1f - SolidFactor;

	public const float ElevationStep = 3f;

	public const int TerracesPerSlope = 2;

	public const int TerraceSteps = TerracesPerSlope * 2 + 1;

	public const float HorizontalTerraceStepSize = 1f / TerraceSteps;

	public const float VerticalTerraceStepSize = 1f / (TerracesPerSlope + 1);

	public static Texture2D NoiseSource;

	public const float CellPerturbStrength = 4f;

	public const float NoiseScale = 0.003f;

	public const float ElevationPerturbStrength = 1.5f;

	public const int ChunkSizeX = 5, ChunkSizeZ = 5;

	public const float StreamBedElevationOffset = -1.75f;

	public const float WaterElevationOffset = -0.5f;

    public const float WaterFactor = 0.6f;

    public const float WaterBlendFactor = 1f - WaterFactor;

	private static readonly Vector3[] Corners = {
		new Vector3(0f, 0f, OuterRadius),
		new Vector3(InnerRadius, 0f, 0.5f * OuterRadius),
		new Vector3(InnerRadius, 0f, -0.5f * OuterRadius),
		new Vector3(0f, 0f, -OuterRadius),
		new Vector3(-InnerRadius, 0f, -0.5f * OuterRadius),
		new Vector3(-InnerRadius, 0f, 0.5f * OuterRadius),
		new Vector3(0f, 0f, OuterRadius)
	};
    public static Vector3 GetFirstCorner(HexDirection direction)
    {
        return Corners[(int)direction];
    }

    public static Vector3 GetSecondCorner(HexDirection direction)
    {
        return Corners[(int)direction + 1];
    }
	public static Vector3 GetFirstSolidCorner(HexDirection direction)
	{
		return Corners[(int)direction] * SolidFactor;
	}

	public static Vector3 GetSecondSolidCorner(HexDirection direction)
	{
		return Corners[(int)direction + 1] * SolidFactor;
	}

    public static Vector3 GetFirstWaterCorner(HexDirection direction)
    {
        return Corners[(int)direction] * WaterFactor;
    }

    public static Vector3 GetSecondWaterCorner(HexDirection direction)
    {
        return Corners[(int)direction + 1] * WaterFactor;
    }
	public static Vector3 GetBridge(HexDirection direction)
	{
		return (BlendFactor) * (Corners[(int)direction] + Corners[(int)direction + 1]);
	}

	public static Vector3 TerraceLerp(Vector3 a, Vector3 b, int step)
	{
		float h = step * HorizontalTerraceStepSize;
		a.x += (b.x - a.x) * h;
		a.z += (b.z - a.z) * h;
		float v = ((step + 1) / 2) * VerticalTerraceStepSize;
		a.y += (b.y - a.y) * v;
		return a;
	}
	public static Color TerraceLerp(Color a, Color b, int step)
	{
		float h = step * HorizontalTerraceStepSize;
		return Color.Lerp(a, b, h);
	}

	/// <summary>
	/// If the level difference is exactly one step, then we have a slope. And in all other cases we have a cliff.
	/// </summary>
	/// <param name="elevation1"></param>
	/// <param name="elevation2"></param>
	/// <returns></returns>
	public static HexEdgeType GetEdgeType(int elevation1, int elevation2)
	{
		if (elevation1 == elevation2)
		{
			return HexEdgeType.Flat;
		}
		int delta = elevation2 - elevation1;
		if (delta == 1 || delta == -1)
		{
			return HexEdgeType.Slope;
		}
		return HexEdgeType.Cliff;
	}

	public static Vector4 SampleNoise(Vector3 position)
	{
		return NoiseSource.GetPixelBilinear(
			position.x * NoiseScale,
			position.z * NoiseScale
		);
	}

	public static Vector3 GetSolidEdgeMiddle(HexDirection direction)
	{
		return
			(Corners[(int)direction] + Corners[(int)direction + 1]) *
			(0.5f * SolidFactor);
	}

	public static Vector3 Perturb(Vector3 position)
	{
		Vector4 sample = HexMetrics.SampleNoise(position);
		position.x += (sample.x * 2f - 1f) * HexMetrics.CellPerturbStrength;
		position.z += (sample.z * 2f - 1f) * HexMetrics.CellPerturbStrength;
		return position;
	}

    public static Vector3 GetWaterBridge(HexDirection direction)
    {
        return (Corners[(int)direction] + Corners[(int)direction + 1]) *
               WaterBlendFactor;
    }
}