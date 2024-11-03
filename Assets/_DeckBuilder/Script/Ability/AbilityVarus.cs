using UnityEngine;

[CreateAssetMenu(fileName = "AbilityVarus", menuName = FileName.Ability + "AbilityVarus", order = 0)]
public class AbilityVarus : AbilityChanneled
{
    [SerializeField]
    private LinearProjectile projectile = null;

    [SerializeField]
    private float minDistance = 5;

    [SerializeField]
    private float maxDistance = 10;

    protected LinearProjectile launchedProjectile;

    public override void EndCast(Vector3 worldPos, bool isSucessful = true)
    {
        // force sucess because we launch a projectile even if not fully channeled
        isSucessful = true;
        base.EndCast(worldPos, isSucessful);

        launchedProjectile = Caster.ProjectileLauncher.LaunchProjectile<LinearProjectile>(projectile);

        float distanceToTravel = Mathf.Lerp(minDistance, maxDistance, channeledRatio);
        Debug.Log($"distance to travel: {distanceToTravel} | chaneled ratio: {channeledRatio}");
        launchedProjectile.SetLifeTime(distanceToTravel / launchedProjectile.MaxSpeed);
    }
}