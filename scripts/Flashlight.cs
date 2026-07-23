using Godot;

public partial class Flashlight : PointLight2D
{
	public override void _Ready()
	{
		Bake();
	}
    public override void _Process(double delta)
    {
        // Bake();
    }


	private async void Bake()
	{
		var viewport = GetNode<SubViewport>("SubViewport");

		// Render the shader for one frame, then snapshot it into the light texture.
		viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
		await ToSignal(RenderingServer.Singleton, RenderingServerInstance.SignalName.FramePostDraw);
		if (!IsInstanceValid(this))
			return;

		Texture = ImageTexture.CreateFromImage(viewport.GetTexture().GetImage());
	}
}
