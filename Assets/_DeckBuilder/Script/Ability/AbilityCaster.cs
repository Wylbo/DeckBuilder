using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

[System.Serializable]
public struct NetworkSpellSlotState : INetworkSerializable, System.IEquatable<NetworkSpellSlotState>
{
    public FixedString64Bytes AbilityId;
    public float CooldownEndTime;
    public float CooldownDuration;
    public bool IsCasting;

    public NetworkSpellSlotState(string abilityId, float cooldownEndTime, float cooldownDuration, bool isCasting)
    {
        AbilityId = new FixedString64Bytes(string.IsNullOrEmpty(abilityId) ? string.Empty : abilityId);
        CooldownEndTime = cooldownEndTime;
        CooldownDuration = cooldownDuration;
        IsCasting = isCasting;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref AbilityId);
        serializer.SerializeValue(ref CooldownEndTime);
        serializer.SerializeValue(ref CooldownDuration);
        serializer.SerializeValue(ref IsCasting);
    }

    public bool Equals(NetworkSpellSlotState other)
    {
        return AbilityId.Equals(other.AbilityId)
               && Mathf.Approximately(CooldownEndTime, other.CooldownEndTime)
               && Mathf.Approximately(CooldownDuration, other.CooldownDuration)
               && IsCasting == other.IsCasting;
    }

    public static NetworkSpellSlotState Empty => new NetworkSpellSlotState(string.Empty, 0f, 0f, false);
}

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
    private NetworkList<NetworkSpellSlotState> slotStates;
    #endregion

    #region Private Members
    private const int InvalidSlotIndex = -1;
    private bool slotEventsRegistered = false;
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
    private void Awake()
    {
        InitializeSlotStateList();
    }

    private void OnEnable()
    {
        InitializeAbilities();
        RegisterSpellSlotEvents();
        EnsureSlotStateListSize();
        SyncAllSlotStates();
    }

    private void OnDisable()
    {
        UnregisterSpellSlotEvents();
        DisableAllAbilities();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        EnsureSlotStateListSize();
        SyncAllSlotStates();
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
        if (spellSlots != null)
        {
            foreach (SpellSlot spellSlot in spellSlots)
            {
                spellSlot?.Disable();
            }
        }

        SyncAllSlotStates();
    }

    public void AssignAbilityToSlot(int index, Ability ability)
    {
        if (!IsSlotIndexValid(index))
        {
            return;
        }

        spellSlots[index].SetAbility(ability, this);
        SyncSlotState(spellSlots[index], index, false);
    }

    public void AssignDodgeAbility(Ability ability)
    {
        dodgeSpellSlot.SetAbility(ability, this);
        SyncSlotState(dodgeSpellSlot, InvalidSlotIndex, true);
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

    public bool TryGetSlotState(int slotIndex, bool isDodgeSlot, out NetworkSpellSlotState state)
    {
        state = NetworkSpellSlotState.Empty;
        if (slotStates == null)
        {
            return false;
        }

        int stateIndex = GetStateIndex(slotIndex, isDodgeSlot);
        if (stateIndex < 0 || stateIndex >= slotStates.Count)
        {
            return false;
        }

        state = slotStates[stateIndex];
        return true;
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

    private void RegisterSpellSlotEvents()
    {
        if (slotEventsRegistered)
        {
            return;
        }

        AttachSlotEvents(dodgeSpellSlot);

        if (spellSlots != null)
        {
            foreach (SpellSlot spellSlot in spellSlots)
            {
                AttachSlotEvents(spellSlot);
            }
        }

        slotEventsRegistered = true;
    }

    private void UnregisterSpellSlotEvents()
    {
        if (!slotEventsRegistered)
        {
            return;
        }

        DetachSlotEvents(dodgeSpellSlot);

        if (spellSlots != null)
        {
            foreach (SpellSlot spellSlot in spellSlots)
            {
                DetachSlotEvents(spellSlot);
            }
        }

        slotEventsRegistered = false;
    }

    private void AttachSlotEvents(SpellSlot slot)
    {
        if (slot == null)
        {
            return;
        }

        slot.OnAbilityChanged -= HandleSlotAbilityChanged;
        slot.OnAbilityChanged += HandleSlotAbilityChanged;
        slot.OnCooldownStarted -= HandleSlotCooldownStarted;
        slot.OnCooldownStarted += HandleSlotCooldownStarted;
        slot.OnCooldownEnded -= HandleSlotCooldownEnded;
        slot.OnCooldownEnded += HandleSlotCooldownEnded;
        slot.OnCastStateChanged -= HandleSlotCastingChanged;
        slot.OnCastStateChanged += HandleSlotCastingChanged;
    }

    private void DetachSlotEvents(SpellSlot slot)
    {
        if (slot == null)
        {
            return;
        }

        slot.OnAbilityChanged -= HandleSlotAbilityChanged;
        slot.OnCooldownStarted -= HandleSlotCooldownStarted;
        slot.OnCooldownEnded -= HandleSlotCooldownEnded;
        slot.OnCastStateChanged -= HandleSlotCastingChanged;
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

    private void InitializeSlotStateList()
    {
        if (slotStates == null)
        {
            slotStates = new NetworkList<NetworkSpellSlotState>();
        }
    }

    private void EnsureSlotStateListSize()
    {
        InitializeSlotStateList();
        if (!CanWriteNetworkState())
        {
            return;
        }

        int targetCount = GetTotalSlotCount();
        if (slotStates.Count != targetCount)
        {
            slotStates.Clear();
            for (int i = 0; i < targetCount; i++)
            {
                slotStates.Add(NetworkSpellSlotState.Empty);
            }
        }
    }

    private void SyncAllSlotStates()
    {
        if (!CanWriteNetworkState())
        {
            return;
        }

        EnsureSlotStateListSize();

        if (spellSlots != null)
        {
            for (int i = 0; i < spellSlots.Length; i++)
            {
                SyncSlotState(spellSlots[i], i, false);
            }
        }

        SyncSlotState(dodgeSpellSlot, InvalidSlotIndex, true);
    }

    private void SyncSlotState(SpellSlot slot, int slotIndex, bool isDodgeSlot)
    {
        if (!CanWriteNetworkState())
        {
            return;
        }

        int stateIndex = GetStateIndex(slotIndex, isDodgeSlot);
        if (stateIndex < 0 || slotStates == null || stateIndex >= slotStates.Count)
        {
            return;
        }

        NetworkSpellSlotState state = slotStates[stateIndex];
        state.AbilityId = new FixedString64Bytes(GetAbilityId(slot != null ? slot.Ability : null));

        bool onCooldown = slot != null && slot.cooldown != null && slot.cooldown.IsRunning;
        state.CooldownDuration = slot != null && slot.cooldown != null ? slot.cooldown.TotalTime : 0f;
        state.CooldownEndTime = onCooldown ? ResolveServerTime() + slot.cooldown.Remaining : 0f;

        IAbilityExecutor executor = slot != null ? slot.Executor : null;
        state.IsCasting = executor != null && executor.IsCasting;

        slotStates[stateIndex] = state;
    }

    private void HandleSlotAbilityChanged(SpellSlot slot)
    {
        if (!CanWriteNetworkState())
        {
            return;
        }

        EnsureSlotStateListSize();
        if (!TryResolveSlotIndex(slot, out int slotIndex, out bool isDodgeSlot))
        {
            return;
        }

        int stateIndex = GetStateIndex(slotIndex, isDodgeSlot);
        if (stateIndex < 0 || stateIndex >= slotStates.Count)
        {
            return;
        }

        NetworkSpellSlotState state = slotStates[stateIndex];
        state.AbilityId = new FixedString64Bytes(GetAbilityId(slot.Ability));
        slotStates[stateIndex] = state;
    }

    private void HandleSlotCooldownStarted(SpellSlot slot, float duration)
    {
        if (!CanWriteNetworkState())
        {
            return;
        }

        EnsureSlotStateListSize();
        if (!TryResolveSlotIndex(slot, out int slotIndex, out bool isDodgeSlot))
        {
            return;
        }

        int stateIndex = GetStateIndex(slotIndex, isDodgeSlot);
        if (stateIndex < 0 || stateIndex >= slotStates.Count)
        {
            return;
        }

        NetworkSpellSlotState state = slotStates[stateIndex];
        state.CooldownDuration = duration;
        state.CooldownEndTime = ResolveServerTime() + duration;
        slotStates[stateIndex] = state;
    }

    private void HandleSlotCooldownEnded(SpellSlot slot)
    {
        if (!CanWriteNetworkState())
        {
            return;
        }

        EnsureSlotStateListSize();
        if (!TryResolveSlotIndex(slot, out int slotIndex, out bool isDodgeSlot))
        {
            return;
        }

        int stateIndex = GetStateIndex(slotIndex, isDodgeSlot);
        if (stateIndex < 0 || stateIndex >= slotStates.Count)
        {
            return;
        }

        NetworkSpellSlotState state = slotStates[stateIndex];
        state.CooldownDuration = 0f;
        state.CooldownEndTime = 0f;
        slotStates[stateIndex] = state;
    }

    private void HandleSlotCastingChanged(SpellSlot slot, bool isCasting)
    {
        if (!CanWriteNetworkState())
        {
            return;
        }

        EnsureSlotStateListSize();
        if (!TryResolveSlotIndex(slot, out int slotIndex, out bool isDodgeSlot))
        {
            return;
        }

        int stateIndex = GetStateIndex(slotIndex, isDodgeSlot);
        if (stateIndex < 0 || stateIndex >= slotStates.Count)
        {
            return;
        }

        NetworkSpellSlotState state = slotStates[stateIndex];
        state.IsCasting = isCasting;
        slotStates[stateIndex] = state;
    }

    private bool TryResolveSlotIndex(SpellSlot slot, out int slotIndex, out bool isDodgeSlot)
    {
        if (slot != null && ReferenceEquals(slot, dodgeSpellSlot))
        {
            slotIndex = InvalidSlotIndex;
            isDodgeSlot = true;
            return true;
        }

        if (spellSlots != null)
        {
            for (int i = 0; i < spellSlots.Length; i++)
            {
                if (ReferenceEquals(slot, spellSlots[i]))
                {
                    slotIndex = i;
                    isDodgeSlot = false;
                    return true;
                }
            }
        }

        slotIndex = InvalidSlotIndex;
        isDodgeSlot = false;
        return false;
    }

    private int GetStateIndex(int slotIndex, bool isDodgeSlot)
    {
        if (isDodgeSlot)
        {
            return spellSlots != null ? spellSlots.Length : 0;
        }

        if (!IsSlotIndexValid(slotIndex))
        {
            return -1;
        }

        return slotIndex;
    }

    private int GetTotalSlotCount()
    {
        int slotCount = spellSlots != null ? spellSlots.Length : 0;
        if (dodgeSpellSlot != null)
        {
            slotCount += 1;
        }

        return slotCount;
    }

    private bool CanWriteNetworkState()
    {
        return !IsSpawned || IsServer;
    }

    private float ResolveServerTime()
    {
        if (NetworkManager != null)
        {
            return (float)NetworkManager.ServerTime.Time;
        }

        return Time.time;
    }

    private string GetAbilityId(Ability ability)
    {
        return ability != null ? ability.name : string.Empty;
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
