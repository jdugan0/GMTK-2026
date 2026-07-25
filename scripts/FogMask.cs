using Godot;

public partial class FogMask : ColorRect
{
    private ShaderMaterial fogMaterial;
    private PointLight2D flashlight;
    private bool textureBound;

    public override void _Ready()
    {
        fogMaterial = Material as ShaderMaterial;
    }

    public override void _Process(double delta)
    {
        if (fogMaterial == null)
            return;

        if (flashlight == null)
        {
            var player = GetTree().GetFirstNodeInGroup("player") as Node2D;
            flashlight = player?.GetNodeOrNull<PointLight2D>("Flashlight");
            if (flashlight == null)
                return;
        }

        if (!textureBound)
        {
            if (flashlight.Texture == null)
                return;
            fogMaterial.SetShaderParameter("light_texture", flashlight.Texture);
            textureBound = true;
        }

        Vector2 viewport = GetViewport().GetVisibleRect().Size;
        Vector2 extent = flashlight.Texture.GetSize() * flashlight.TextureScale;

        Transform2D uvToScreen = new Transform2D(
            new Vector2(viewport.X, 0f),
            new Vector2(0f, viewport.Y),
            Vector2.Zero
        );
        Transform2D lightToUv =
            new Transform2D(
                new Vector2(1f / extent.X, 0f),
                new Vector2(0f, 1f / extent.Y),
                new Vector2(0.5f, 0.5f)
            ) * new Transform2D(Vector2.Right, Vector2.Down, -flashlight.Offset);

        Transform2D screenToLightUv =
            lightToUv
            * flashlight.GlobalTransform.AffineInverse()
            * GetViewport().CanvasTransform.AffineInverse()
            * uvToScreen;

        fogMaterial.SetShaderParameter(
            "screen_to_light_uv_x",
            new Vector3(screenToLightUv.X.X, screenToLightUv.Y.X, screenToLightUv.Origin.X)
        );
        fogMaterial.SetShaderParameter(
            "screen_to_light_uv_y",
            new Vector3(screenToLightUv.X.Y, screenToLightUv.Y.Y, screenToLightUv.Origin.Y)
        );
    }
}
