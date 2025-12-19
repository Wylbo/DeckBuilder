using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = nameof(Ability), menuName = FileName.AbilityFolder + nameof(Ability), order = 0)]
public class Ability : ScriptableObject
{
    #region Fields
    [SerializeField] private Sprite icon;
    [SerializeField, TextArea] private string tooltip;
    [SerializeField] private bool rotatingCasterToCastDirection = true;
    [SerializeField] private bool stopMovementOnCast = false;
    [Tooltip("if false, the cooldown will start at the end of the cast")]
    [SerializeField] private bool startCooldownOnCast = true;
    [SerializeField] private List<ScriptableDebuff> debuffsOnCast = new List<ScriptableDebuff>();
    [SerializeField] private List<ScriptableDebuff> debuffsOnEndCast = new List<ScriptableDebuff>();
    [SerializeField] private GTagSet tagSet = new GTagSet();
    [SerializeField] private List<AbilityStatEntry> baseStats = new List<AbilityStatEntry>();
    [SerializeReference] private List<AbilityBehaviour> behaviours = new List<AbilityBehaviour>();
    [Header("Animation")]
    [SerializeField] private AnimationData animationData = null;
    #endregion

    #region Private Members
    #endregion

    #region Getters
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
    #endregion

    #region Unity Message Methods
    private void OnValidate()
    {
        EnsureCollectionsInitialized();
    }
    #endregion

    #region Public Methods
    public float GetBaseStatValue(AbilityStatKey key)
    {
        if (baseStats == null)
        {
            return 0f;
        }

        foreach (AbilityStatEntry stat in baseStats)
        {
            if (stat.Key == key)
            {
                if (stat.Source == AbilityStatSource.Flat)
                {
                    return stat.Value;
                }

                // Ratio/copy values depend on caster globals; treat as zero here.
                return 0f;
            }
        }

        return 0f;
    }
    #endregion

    #region Private Methods
    private void EnsureCollectionsInitialized()
    {
        if (debuffsOnCast == null)
        {
            debuffsOnCast = new List<ScriptableDebuff>();
        }

        if (debuffsOnEndCast == null)
        {
            debuffsOnEndCast = new List<ScriptableDebuff>();
        }

        if (baseStats == null)
        {
            baseStats = new List<AbilityStatEntry>();
        }

        if (behaviours == null)
        {
            behaviours = new List<AbilityBehaviour>();
        }

        if (tagSet == null)
        {
            tagSet = new GTagSet();
        }
    }
    #endregion
}
