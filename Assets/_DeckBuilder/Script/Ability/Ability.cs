using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = nameof(Ability), menuName = FileName.AbilityFolder + nameof(Ability), order = 0)]
public class Ability : ScriptableObject
{
    [SerializeField] private Sprite icon;
    [SerializeField, TextArea] private string tooltip;
    [SerializeField] private bool rotatingCasterToCastDirection = true;
    [SerializeField] private bool stopMovementOnCast = false;
    [Tooltip("if false, the cooldown will start at the end of the cast")]
    [SerializeField] private bool startCooldownOnCast = true;
    [SerializeField] private List<ScriptableDebuff> debuffsOnCast;
    [SerializeField] private List<ScriptableDebuff> debuffsOnEndCast;
    [SerializeField] private GTagSet tagSet = new GTagSet();
    [SerializeField] private List<AbilityStatEntry> baseStats = new List<AbilityStatEntry>();
    [SerializeReference]
    private List<AbilityBehaviour> behaviours = new List<AbilityBehaviour>();
    [Header("Animation")]
    [SerializeField] private AnimationData animationData = null;

    public Sprite Icon => icon;
    public string Tooltip => tooltip;
    public bool RotatingCasterToCastDirection => rotatingCasterToCastDirection;
    public bool StopMovementOnCast => stopMovementOnCast;
    public bool StartCooldownOnCast => startCooldownOnCast;
    public GTagSet TagSet => tagSet;
    public IReadOnlyList<AbilityStatEntry> BaseStats => baseStats;
    public IReadOnlyList<AbilityBehaviour> Behaviours => behaviours;
    public IReadOnlyList<ScriptableDebuff> DebuffsOnCast => debuffsOnCast;
    public IReadOnlyList<ScriptableDebuff> DebuffsOnEndCast => debuffsOnEndCast;
    public AnimationData AnimationData => animationData;

    public float GetBaseStatValue(AbilityStatKey key)
    {
        if (baseStats == null)
            return 0f;

        foreach (var stat in baseStats)
        {
            if (stat.Key == key)
            {
                if (stat.Source == AbilityStatSource.Flat)
                    return stat.Value;

                // Ratio/copy values depend on caster globals; treat as zero here.
                return 0f;
            }
        }
        return 0f;
    }
}
