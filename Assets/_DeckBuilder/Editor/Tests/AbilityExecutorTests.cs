using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class AbilityExecutorTests
{
    [Test]
    public void Cast_WaitsForDelayBeforeContinuingSequence()
    {
        var delay = new AbilityDelayBehaviour();
        SetDelaySeconds(delay, 0.5f);
        var recorder = new RecordingBehaviour();
        var ability = CreateAbility(delay, recorder);
        var executor = new AbilityExecutor(ability, null, new FakeMovement(), null, new FakeDebuffService(), new AbilityStatProvider(), new FakeGlobalStats());
        int endCalls = 0;
        executor.On_EndCast += _ => endCalls++;

        executor.Cast(Vector3.zero, false);

        Assert.AreEqual(0, recorder.Started);
        executor.Update(0.25f);
        Assert.AreEqual(0, recorder.Started);

        executor.Update(0.3f);
        Assert.AreEqual(1, recorder.Started);
        Assert.AreEqual(1, recorder.Ended);
        Assert.AreEqual(1, endCalls);
    }

    [Test]
    public void Cast_WithBlockingBehaviour_DoesNotAutoEnd()
    {
        var blocking = new BlockingBehaviour();
        var ability = CreateAbility(blocking);
        var executor = new AbilityExecutor(ability, null, new FakeMovement(), null, new FakeDebuffService(), new AbilityStatProvider(), new FakeGlobalStats());
        int endCalls = 0;
        executor.On_EndCast += _ => endCalls++;

        executor.Cast(Vector3.zero, false);
        executor.Update(1f);

        Assert.IsTrue(executor.IsCasting);
        Assert.AreEqual(0, endCalls);

        executor.EndCast(Vector3.zero);
        Assert.AreEqual(1, endCalls);
        Assert.IsFalse(executor.IsCasting);
    }

    [Test]
    public void Cast_WithUpdatingBehaviour_RunsUpdatesUntilEnded()
    {
        var updating = new UpdatingBehaviour();
        var ability = CreateAbility(updating);
        var executor = new AbilityExecutor(ability, null, new FakeMovement(), null, new FakeDebuffService(), new AbilityStatProvider(), new FakeGlobalStats());

        executor.Cast(Vector3.zero, false);
        executor.Update(0.1f);
        executor.Update(0.2f);

        Assert.AreEqual(2, updating.UpdateCount);
        Assert.IsTrue(executor.IsCasting);

        executor.EndCast(Vector3.zero);
        Assert.IsFalse(executor.IsCasting);
    }

    private static Ability CreateAbility(params AbilityBehaviour[] behaviours)
    {
        var ability = ScriptableObject.CreateInstance<Ability>();
        SetPrivateField(ability, "behaviours", new List<AbilityBehaviour>(behaviours));
        SetPrivateField(ability, "rotatingCasterToCastDirection", false);
        SetPrivateField(ability, "stopMovementOnCast", false);
        SetPrivateField(ability, "baseStats", new List<AbilityStatEntry>
        {
            new AbilityStatEntry { Key = AbilityStatKey.Cooldown, Source = AbilityStatSource.Flat, Value = 1f, GlobalKey = GlobalStatKey.AttackPower }
        });
        return ability;
    }

    private static void SetDelaySeconds(AbilityDelayBehaviour delay, float seconds)
    {
        typeof(AbilityDelayBehaviour)
            .GetField("delaySeconds", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(delay, seconds);
    }

    private static void SetPrivateField<T>(Ability ability, string fieldName, T value)
    {
        var field = typeof(Ability).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(ability, value);
    }

    private sealed class RecordingBehaviour : AbilityBehaviour
    {
        public int Started;
        public int Ended;

        public override void OnCastStarted(AbilityCastContext context)
        {
            Started++;
        }

        public override void OnCastEnded(AbilityCastContext context, bool wasSuccessful)
        {
            Ended++;
        }
    }

    private sealed class BlockingBehaviour : AbilityBehaviour
    {
        public int Started;
        public override bool BlocksAbilityEnd => true;

        public override void OnCastStarted(AbilityCastContext context)
        {
            Started++;
        }
    }

    private sealed class UpdatingBehaviour : AbilityBehaviour
    {
        public int UpdateCount;
        public override bool RequiresUpdate => true;

        public override void OnCastUpdated(AbilityCastContext context, float deltaTime)
        {
            UpdateCount++;
        }
    }

    private sealed class FakeMovement : IAbilityMovement
    {
        public bool IsMoving { get; set; }
        public bool StopCalled { get; private set; }
        public bool Disabled { get; private set; }
        public Movement.DashData? LastDash { get; private set; }
        public Vector3 LastDashTarget { get; private set; }

        public void StopMovement()
        {
            StopCalled = true;
        }

        public void DisableMovement()
        {
            Disabled = true;
        }

        public void EnableMovement()
        {
            Disabled = false;
        }

        public void Dash(Movement.DashData dashData, Vector3 toward)
        {
            LastDash = dashData;
            LastDashTarget = toward;
        }
    }

    private sealed class FakeDebuffService : IAbilityDebuffService
    {
        public readonly List<ScriptableDebuff> Added = new List<ScriptableDebuff>();
        public readonly List<ScriptableDebuff> Removed = new List<ScriptableDebuff>();

        public void AddDebuff(ScriptableDebuff scriptableDebuff)
        {
            Added.Add(scriptableDebuff);
        }

        public void RemoveDebuff(ScriptableDebuff scriptableDebuff)
        {
            Removed.Add(scriptableDebuff);
        }
    }

    private sealed class FakeGlobalStats : IGlobalStatSource
    {
        private readonly Dictionary<GlobalStatKey, float> stats = new Dictionary<GlobalStatKey, float>();

        public Dictionary<GlobalStatKey, float> EvaluateGlobalStats() => stats;

        public Dictionary<GlobalStatKey, float> EvaluateGlobalStatsRaw() => stats;
    }
}
