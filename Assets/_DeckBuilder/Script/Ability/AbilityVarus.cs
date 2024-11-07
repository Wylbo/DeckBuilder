using UnityEngine;

[CreateAssetMenu(fileName = "AbilityVarus", menuName = FileName.Ability + "AbilityVarus", order = 0)]
public class AbilityVarus : AbilityChanneled, IAbilityRangeIndicator
{
    [SerializeField]
    private LinearProjectile projectile = null;

    [SerializeField]
    private float minDistance = 5;

    [SerializeField]
    private float maxDistance = 10;
    [SerializeField] private int minDamage;
    [SerializeField] private int maxDamage;
    [SerializeField] private RangeIndicator rangeIndicator;

    protected LinearProjectile launchedProjectile;
    private RangeIndicator spawnedRangedIndicator;

    protected override void DoCast(Vector3 worldPos)
    {
        SpawnIndicator(rangeIndicator);
        base.DoCast(worldPos);
    }

    public override void EndCast(Vector3 worldPos, bool isSucessful = true)
    {
        // force sucess because we launch a projectile even if not fully channeled
        isSucessful = true;
        base.EndCast(worldPos, isSucessful);

        PoolManager.Release(spawnedRangedIndicator.gameObject);
        spawnedRangedIndicator = null;

        LookAtCursorPosition();
        launchedProjectile = Caster.ProjectileLauncher.LaunchProjectile<LinearProjectile>(projectile);

        float distanceToTravel = Mathf.Lerp(minDistance, maxDistance, channeledRatio);
        launchedProjectile.SetLifeTime(distanceToTravel / launchedProjectile.MaxSpeed);

        Hitbox hitbox = launchedProjectile.GetComponent<Hitbox>();
        hitbox.SetDamage(Mathf.FloorToInt(Mathf.Lerp(minDamage, maxDamage, channeledRatio)));

        foreach (ScriptableDebuff scriptableDebuff in debuffsOnCast)
        {
            Caster.RemoveDebuff(scriptableDebuff);
        }
    }

    public void SpawnIndicator(RangeIndicator indicator)
    {
        spawnedRangedIndicator = PoolManager.Provide<RangeIndicator>(indicator.gameObject, Caster.transform.position + Vector3.up * -1, Quaternion.identity, Caster.transform);
        spawnedRangedIndicator.SetScale(minDistance * 2);
    }

    protected override void UpdateChanneling()
    {
        spawnedRangedIndicator.SetScale(Mathf.Lerp(minDistance, maxDistance, channeledRatio) * 2);
    }
}