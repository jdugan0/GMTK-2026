using Godot;

public partial class Flashlight : PointLight2D
{
	[ExportGroup("Flicker")]
	[Export]
	private float flickerDuration = 0.45f;

	[Export]
	private float flickerIntervalMin = 0.02f;

	[Export]
	private float flickerIntervalMax = 0.07f;

	[Export]
	private float flickerDimEnergy = 0.15f;

	private float baseEnergy;
	private double flickerTimer;
	private double flickerStepTimer;
	private bool flickerDim;

	public override void _Ready()
	{
		baseEnergy = Energy;
		Bake();
	}

	public override void _Process(double delta)
	{
		if (flickerTimer <= 0)
			return;

		flickerTimer -= delta;
		flickerStepTimer -= delta;

		if (flickerTimer <= 0)
		{
			Energy = baseEnergy;
			return;
		}

		if (flickerStepTimer <= 0)
		{
			flickerDim = !flickerDim;
			flickerStepTimer = (float)GD.RandRange(flickerIntervalMin, flickerIntervalMax);
			// Fade the flicker out so the light settles back instead of cutting off.
			float strength = (float)(flickerTimer / flickerDuration);
			Energy = flickerDim
				? Mathf.Lerp(baseEnergy, baseEnergy * flickerDimEnergy, strength)
				: baseEnergy;
		}
	}

	public void Flicker()
	{
		baseEnergy = flickerTimer > 0 ? baseEnergy : Energy;
		flickerTimer = flickerDuration;
		flickerStepTimer = 0;
		flickerDim = false;
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
