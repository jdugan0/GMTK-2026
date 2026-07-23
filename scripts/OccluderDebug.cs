using System;
using Godot;

[Tool]
[GlobalClass]
public partial class OccluderDebug : Node2D
{
	private const int FlipH = 4096;
	private const int FlipV = 8192;
	private const int Transpose = 16384;
	private const int TransformBits = FlipH | FlipV | Transpose;

	private bool showOccluders = true;
	private Node searchRoot;

	[Export]
	public bool ShowOccluders
	{
		get => showOccluders;
		set
		{
			showOccluders = value;
			QueueRedraw();
		}
	}

	[Export]
	public Node SearchRoot
	{
		get => searchRoot;
		set
		{
			searchRoot = value;
			QueueRedraw();
		}
	}

	[Export] public bool IncludeTileMapLayers = true;
	[Export] public bool IncludeOccluderNodes = true;

	[Export(PropertyHint.Layers2DRender)] public uint ShadowItemCullMask = 0xFFFFFFFF;

	[Export] public Color FillColor = new(1f, 0.25f, 0.4f, 0.15f);
	[Export] public Color OutlineColor = new(1f, 0.35f, 0.5f, 0.9f);
	[Export(PropertyHint.Range, "0.5,16,0.5")] public float OutlineWidth = 3f;

	[Export] public bool ShowOriginMarker = true;
	[Export] public bool DrawAtRuntime = false;

	public override void _Process(double delta)
	{
		if (ShowOccluders && (Engine.IsEditorHint() || DrawAtRuntime))
			QueueRedraw();
	}

	public override void _Draw()
	{
		if (!ShowOccluders)
			return;
		if (!Engine.IsEditorHint() && !DrawAtRuntime)
			return;

		if (ShowOriginMarker)
		{
			DrawLine(new Vector2(-24f, 0f), new Vector2(24f, 0f), OutlineColor, OutlineWidth);
			DrawLine(new Vector2(0f, -24f), new Vector2(0f, 24f), OutlineColor, OutlineWidth);
		}

		DrawSubtree(SearchRoot ?? this, GlobalTransform.AffineInverse());
	}

	private void DrawSubtree(Node node, Transform2D inv)
	{
		if (IncludeTileMapLayers && node is TileMapLayer layer)
			DrawTileLayer(layer, inv);
		else if (IncludeOccluderNodes && node is LightOccluder2D occluder)
			DrawOccluderNode(occluder, inv);

		foreach (Node child in node.GetChildren())
			DrawSubtree(child, inv);
	}

	private void DrawTileLayer(TileMapLayer layer, Transform2D inv)
	{
		if (layer.TileSet == null)
			return;

		TileSet tileSet = layer.TileSet;
		int occlusionLayers = tileSet.GetOcclusionLayersCount();
		if (occlusionLayers == 0)
			return;

		Transform2D toLocal = inv * layer.GlobalTransform;

		foreach (Vector2I cell in layer.GetUsedCells())
		{
			int sourceId = layer.GetCellSourceId(cell);
			if (sourceId < 0 || tileSet.GetSource(sourceId) is not TileSetAtlasSource atlas)
				continue;

			int alternative = layer.GetCellAlternativeTile(cell);
			TileData data = atlas.GetTileData(layer.GetCellAtlasCoords(cell), alternative & ~TransformBits);
			if (data == null)
				continue;

			bool flipH = (alternative & FlipH) != 0;
			bool flipV = (alternative & FlipV) != 0;
			bool transpose = (alternative & Transpose) != 0;
			Vector2 cellOrigin = layer.MapToLocal(cell);

			for (int occlusionLayer = 0; occlusionLayer < occlusionLayers; occlusionLayer++)
			{
				if ((tileSet.GetOcclusionLayerLightMask(occlusionLayer) & ShadowItemCullMask) == 0)
					continue;

				int polygons = data.GetOccluderPolygonsCount(occlusionLayer);
				for (int i = 0; i < polygons; i++)
				{
					OccluderPolygon2D polygon = data.GetOccluderPolygon(occlusionLayer, i, flipH, flipV, transpose);
					DrawOccluderPolygon(polygon, toLocal, cellOrigin);
				}
			}
		}
	}

	private void DrawOccluderNode(LightOccluder2D node, Transform2D inv)
	{
		if (node.Occluder == null)
			return;
		if (((uint)node.OccluderLightMask & ShadowItemCullMask) == 0)
			return;

		DrawOccluderPolygon(node.Occluder, inv * node.GlobalTransform, Vector2.Zero);
	}

	private void DrawOccluderPolygon(OccluderPolygon2D polygon, Transform2D toLocal, Vector2 offset)
	{
		if (polygon == null)
			return;

		Vector2[] source = polygon.Polygon;
		if (source.Length < 2)
			return;

		var points = new Vector2[source.Length];
		for (int i = 0; i < source.Length; i++)
			points[i] = toLocal * (source[i] + offset);

		// DrawColoredPolygon ear-clips internally but errors on degenerate input, so pre-check.
		if (polygon.Closed && points.Length >= 3 && FillColor.A > 0f
			&& Geometry2D.TriangulatePolygon(points).Length >= 3)
		{
			DrawColoredPolygon(points, FillColor);
		}

		var outline = new Vector2[points.Length + (polygon.Closed ? 1 : 0)];
		Array.Copy(points, outline, points.Length);
		if (polygon.Closed)
			outline[^1] = points[0];

		DrawPolyline(outline, OutlineColor, OutlineWidth);
	}
}
