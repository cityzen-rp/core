using Sandbox.Rendering;
using Sandbox.Utility;

public class VapeWeapon : BaseWeapon
{
    [Property] SoundEvent VapeSmoke { get; set; }
    [Property] SoundEvent VapeSmokeExhale { get; set; }

    bool isSmoking = false;

    TimeSince timeSinceStartedDragging = 0f;
    float draggingTime = 0;
    TimeSince timeSinceStartedSmoking = 0f;

    public override void OnControl( Player player )
	{
		base.OnControl( player );

        if ( Input.Down( "attack2" ) )
        {
            if (isSmoking)
                return;
            
            WeaponModel?.Renderer?.Set("b_smoking", true);
            player.Controller.Renderer.Set("b_smoking", true);
            GameObject?.PlaySound( VapeSmoke );
            isSmoking = true;
            timeSinceStartedDragging = 0f;
        }

        if ( Input.Released( "attack2" ) )
        {
            WeaponModel?.Renderer?.Set("b_smoking", false);
            player.Controller.Renderer.Set("b_smoking", false);
            player.smokeMouth.GetComponent<ParticleSphereEmitter>(true).Enabled = true;
            timeSinceStartedSmoking = 0;
            draggingTime = timeSinceStartedDragging;
            GameObject?.StopAllSounds();
            GameObject?.PlaySound( VapeSmokeExhale );
            isSmoking = false;
        }

        if (timeSinceStartedSmoking > draggingTime)
        {
            // player.smokeMouth.Enabled = false;
            player.smokeMouth.GetComponent<ParticleSphereEmitter>(true).Enabled = false;
        }

        var effect = player.smokeMouth.GetComponent<ParticleEffect>(true);
        effect.InitialVelocity = player.EyeTransform.Rotation.Forward * 500f;
    }

    public override void DrawHud( HudPainter painter, Vector2 crosshair )
	{
		// nothing!
	}
}