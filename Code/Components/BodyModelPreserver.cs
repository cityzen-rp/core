/// <summary>
/// Preserves the body model set in the prefab after the Dresser component
/// overrides it with the player's s&box avatar/profile body.
/// Place this component on the same GameObject as the SkinnedModelRenderer
/// and the Dresser.
/// </summary>
public sealed class BodyModelPreserver : Component
{
	[RequireComponent] private SkinnedModelRenderer Renderer { get; set; }

	private Model _desiredModel;

	protected override void OnAwake()
	{
		_desiredModel = Renderer.Model;
	}

	protected override void OnStart()
	{
		_ = RestoreAfterDresser();
	}

	private async Task RestoreAfterDresser()
	{
		var dresser = Components.Get<Dresser>( FindMode.InSelf );
		if ( dresser.IsValid() )
		{
			await dresser.Apply();
		}

		if ( !this.IsValid() ) return;

		Renderer.Model = _desiredModel;
	}
}
