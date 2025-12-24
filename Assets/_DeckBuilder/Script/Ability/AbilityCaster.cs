using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(ProjectileLauncher))]
public class AbilityCaster : NetworkBehaviour
{
    #region Fields
    [SerializeField] private SpellSlot dodgeSpellSlot;
    [SerializeField] private SpellSlot[] spellSlots = new SpellSlot[4];
    [SerializeField] private ProjectileLauncher projectileLauncher = null;
    [SerializeField] private DebuffUpdater debuffUpdater = null;
    [SerializeField] private StatsModifierManager modifierManager;
    [SerializeField] private GlobalStatSource globalStatSource;
    #endregion

    #region Private Members
    private const int InvalidSlotIndex = -1;
    #endregion

    #region Getters
    public SpellSlot[] SpellSlots => spellSlots;
    public SpellSlot DodgeSpellSlot => dodgeSpellSlot;
    public ProjectileLauncher ProjectileLauncher => projectileLauncher;
    public IAbilityDebuffService DebuffService => debuffUpdater;
    public StatsModifierManager ModifierManager => modifierManager;
    public IGlobalStatSource GlobalStatSource => globalStatSource;
    #endregion

    #region Unity Message Methods
    private void OnEnable()
    {
        InitializeAbilities();
    }

    private void OnDisable()
    {
        DisableAllAbilities();
    }

    private void Reset()
    {
        projectileLauncher = GetComponent<ProjectileLauncher>();
        if (globalStatSource == null)
        {
            globalStatSource = GetComponent<GlobalStatSource>();
        }
    }

    private void Update()
    {
        UpdateSpellSlotsCooldowns();
    }
    #endregion

    #region Public Methods
    public void AddDebuff(ScriptableDebuff scriptableDebuff)
    {
        if (DebuffService == null || scriptableDebuff == null)
        {
            return;
        }

        DebuffService.AddDebuff(scriptableDebuff);
    }

    public void RemoveDebuff(ScriptableDebuff scriptableDebuff)
    {
        if (DebuffService == null || scriptableDebuff == null)
        {
            return;
        }

        DebuffService.RemoveDebuff(scriptableDebuff);
    }

    public void DisableAllAbilities()
    {
        dodgeSpellSlot?.Disable();
        if (spellSlots == null)
        {
            return;
        }

        foreach (SpellSlot spellSlot in spellSlots)
        {
            spellSlot?.Disable();
        }
    }

    public void AssignAbilityToSlot(int index, Ability ability)
    {
        if (!IsSlotIndexValid(index))
        {
            return;
        }

        spellSlots[index].SetAbility(ability, this);
    }

    public void AssignDodgeAbility(Ability ability)
    {
        dodgeSpellSlot.SetAbility(ability, this);
    }

    public bool Cast(int index, Vector3 worldPos)
    {
        return RequestCastSlot(index, worldPos, false, true);
    }

    public bool StartHold(int index, Vector3 worldPos)
    {
        return RequestCastSlot(index, worldPos, false, true);
    }

    public bool CastDodge(Vector3 worldPos)
    {
        return RequestCastSlot(InvalidSlotIndex, worldPos, true, true);
    }

    public void EndHold(int index, Vector3 worldPos)
    {
        RequestEndHold(index, worldPos, false);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void RequestCast_ServerRpc(int slotIndex, Vector3 worldPos, bool isDodgeSlot, bool isHeldRequest, RpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;
        TryProcessCast(slotIndex, worldPos, isDodgeSlot, isHeldRequest, senderClientId);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void RequestStartHold_ServerRpc(int slotIndex, Vector3 worldPos, bool isDodgeSlot, RpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;
        TryProcessCast(slotIndex, worldPos, isDodgeSlot, true, senderClientId);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void RequestEndHold_ServerRpc(int slotIndex, Vector3 worldPos, bool isDodgeSlot, RpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;
        TryProcessEndHold(slotIndex, worldPos, isDodgeSlot, senderClientId);
    }
    #endregion

    #region Private Methods
    private void InitializeAbilities()
    {
        if (dodgeSpellSlot != null)
        {
            dodgeSpellSlot.Initialize(this);
        }

        if (spellSlots == null)
        {
            return;
        }

        foreach (SpellSlot spellSlot in spellSlots)
        {
            spellSlot?.Initialize(this);
        }
    }

    private bool RequestCastSlot(int slotIndex, Vector3 worldPos, bool isDodgeSlot, bool isHeldRequest)
    {
        bool isServerInstance = IsServer || !IsSpawned;
        if (isServerInstance)
        {
            return TryProcessCast(slotIndex, worldPos, isDodgeSlot, isHeldRequest, ResolveServerClientId());
        }

        if (isHeldRequest)
        {
            RequestStartHold_ServerRpc(slotIndex, worldPos, isDodgeSlot);
        }
        else
        {
            RequestCast_ServerRpc(slotIndex, worldPos, isDodgeSlot, isHeldRequest);
        }

        return true;
    }

    private void RequestEndHold(int slotIndex, Vector3 worldPos, bool isDodgeSlot)
    {
        bool isServerInstance = IsServer || !IsSpawned;
        if (isServerInstance)
        {
            TryProcessEndHold(slotIndex, worldPos, isDodgeSlot, ResolveServerClientId());
            return;
        }

        RequestEndHold_ServerRpc(slotIndex, worldPos, isDodgeSlot);
    }

    private bool TryProcessCast(int slotIndex, Vector3 worldPos, bool isDodgeSlot, bool isHeldRequest, ulong senderClientId)
    {
        SpellSlot targetSlot = ResolveSpellSlot(slotIndex, isDodgeSlot);
        if (!ValidateCastRequest(targetSlot, slotIndex, isDodgeSlot, senderClientId))
        {
            return false;
        }

        targetSlot.Cast(this, worldPos, isHeldRequest);
        return true;
    }

    private void TryProcessEndHold(int slotIndex, Vector3 worldPos, bool isDodgeSlot, ulong senderClientId)
    {
        SpellSlot targetSlot = ResolveSpellSlot(slotIndex, isDodgeSlot);
        if (!ValidateEndHoldRequest(targetSlot, slotIndex, isDodgeSlot, senderClientId))
        {
            return;
        }

        targetSlot.EndHold(this, worldPos);
    }

    private bool ValidateCastRequest(SpellSlot spellSlot, int slotIndex, bool isDodgeSlot, ulong senderClientId)
    {
        if (!IsServer && IsSpawned)
        {
            return false;
        }

        if (!IsSenderAuthorized(senderClientId))
        {
            return false;
        }

        if (!IsSlotValidForRequest(spellSlot, slotIndex, isDodgeSlot))
        {
            return false;
        }

        if (spellSlot.Ability == null)
        {
            return false;
        }

        if (!spellSlot.CanCast)
        {
            return false;
        }

        if (!HasRequiredResources(spellSlot.Ability))
        {
            return false;
        }

        if (!AreTagsValid(spellSlot.Ability))
        {
            return false;
        }

        return true;
    }

    private bool ValidateEndHoldRequest(SpellSlot spellSlot, int slotIndex, bool isDodgeSlot, ulong senderClientId)
    {
        if (!IsServer && IsSpawned)
        {
            return false;
        }

        if (!IsSenderAuthorized(senderClientId))
        {
            return false;
        }

        if (!IsSlotValidForRequest(spellSlot, slotIndex, isDodgeSlot))
        {
            return false;
        }

        return true;
    }

    private bool IsSenderAuthorized(ulong senderClientId)
    {
        if (!IsSpawned || NetworkObject == null)
        {
            return true;
        }

        if (NetworkManager != null && senderClientId == NetworkManager.ServerClientId)
        {
            return true;
        }

        return NetworkObject.OwnerClientId == senderClientId;
    }

    private bool IsSlotValidForRequest(SpellSlot spellSlot, int slotIndex, bool isDodgeSlot)
    {
        if (isDodgeSlot)
        {
            return spellSlot != null;
        }

        return IsSlotIndexValid(slotIndex) && spellSlot != null;
    }

    private bool IsSlotIndexValid(int index)
    {
        return index >= 0 && spellSlots != null && index < spellSlots.Length;
    }

    private SpellSlot ResolveSpellSlot(int slotIndex, bool isDodgeSlot)
    {
        if (isDodgeSlot)
        {
            return dodgeSpellSlot;
        }

        if (!IsSlotIndexValid(slotIndex))
        {
            return null;
        }

        return spellSlots[slotIndex];
    }

    private ulong ResolveServerClientId()
    {
        if (NetworkManager != null)
        {
            return NetworkManager.ServerClientId;
        }

        return 0;
    }

    private bool HasRequiredResources(Ability ability)
    {
        return ability != null;
    }

    private bool AreTagsValid(Ability ability)
    {
        if (ability == null)
        {
            return false;
        }

        GTagSet tagSet = ability.TagSet;
        if (tagSet == null || tagSet.Tags == null)
        {
            return true;
        }

        foreach (GTag tag in tagSet.AsTags())
        {
            if (!GTagRegistry.Exists(tag))
            {
                Debug.LogWarning($"[{nameof(AbilityCaster)}] Ability {ability.name} has invalid tag {tag}.", this);
                return false;
            }
        }

        return true;
    }

    private void UpdateSpellSlotsCooldowns()
    {
        if (dodgeSpellSlot != null)
        {
            dodgeSpellSlot.UpdateCooldown(Time.deltaTime);
        }

        if (spellSlots == null)
        {
            return;
        }

        foreach (SpellSlot slot in spellSlots)
        {
            slot?.UpdateCooldown(Time.deltaTime);
        }
    }
    #endregion
}
