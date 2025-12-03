using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.Events;

public class AbilityExecutorTests
{
    // These tests cover the execution sequencing rules of AbilityExecutor:
    // - Delay behaviours should pause the start chain until their delay expires.
    // - Blocking behaviours should keep the executor in a casting state until it is manually ended.
    // - Updating behaviours should receive OnCastUpdated calls every Update while casting.
    [Test]
    public void Cast_WaitsForDelayBeforeContinuingSequence()
    {
        var delay = new AbilityDelayBehaviour();
        SetDelaySeconds(delay, 0.5f);
        var recorder = new RecordingBehaviour();
        var ability = CreateAbility(delay, recorder);
        var executor = new AbilityExecutor(ability, null, new FakeMovement(), new FakeDebuffService(), new AbilityStatProvider());
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
        var executor = new AbilityExecutor(ability, null, new FakeMovement(), new FakeDebuffService(), new AbilityStatProvider());
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
        var executor = new AbilityExecutor(ability, null, new FakeMovement(), new FakeDebuffService(), new AbilityStatProvider());

        executor.Cast(Vector3.zero, false);
        executor.Update(0.1f);
        executor.Update(0.2f);

        Assert.AreEqual(2, updating.UpdateCount);
        Assert.IsTrue(executor.IsCasting);

        executor.EndCast(Vector3.zero);
        Assert.IsFalse(executor.IsCasting);
    }

    [Test]
    public void AutoTargeting_SelectsClosestTargetAndUpdatesContext()
    {
        var behaviour = new AbilityAutoTargetingBehaviour();
        SetPrivateField(behaviour, "autoTargetingRange", 3f);
        SetPrivateField(behaviour, "targetableLayerMask", LayerMask.GetMask("Default"));

        var ability = CreateAbility(behaviour);
        var caster = CreateTestCaster();
        var (context, _) = CreateCastContextWithExecutor(ability, new Dictionary<AbilityStatKey, float>(), caster: caster, targetPoint: Vector3.zero);

        var near = CreateTargetable(new Vector3(0.5f, 0f, 0f), 50);
        var far = CreateTargetable(new Vector3(2.5f, 0f, 0f), 50);

        behaviour.OnCastStarted(context);

        Assert.AreSame(near, context.Target);
        Assert.AreEqual(near.transform.position, context.TargetPoint);
        Assert.AreEqual(near.transform.position, context.AimPoint);

        Cleanup(near.gameObject, far.gameObject, caster.gameObject, ability);
    }

    [Test]
    public void DamageTargetBehaviour_AppliesRoundedDamageToCharacterHealth()
    {
        var behaviour = new AbilityDamageTargetBehaviour();
        var ability = CreateAbilityWithStats(new Dictionary<AbilityStatKey, float>
        {
            { AbilityStatKey.Damage, 25f }
        }, behaviour);

        var target = CreateTargetable(Vector3.zero, 100);
        var (context, _) = CreateCastContextWithExecutor(ability, new Dictionary<AbilityStatKey, float>
        {
            { AbilityStatKey.Damage, 25f }
        });
        context.Target = target;

        behaviour.OnCastStarted(context);

        Assert.AreEqual(75, target.Character.Health.Value);
        Cleanup(target.gameObject, ability);
    }

    [Test]
    public void DashBehaviour_UsesStatOverridesForDistanceAndSpeed()
    {
        var movement = new FakeMovement();
        var behaviour = new AbilityDashBehaviour();
        var stats = new Dictionary<AbilityStatKey, float>
        {
            { AbilityStatKey.DashDistance, 8f },
            { AbilityStatKey.DashSpeed, 12f }
        };

        var ability = CreateAbilityWithStats(stats, behaviour);
        var (context, _) = CreateCastContextWithExecutor(ability, stats, movement: movement, targetPoint: new Vector3(5f, 0f, 5f));

        behaviour.OnCastStarted(context);

        Assert.IsTrue(movement.LastDash.HasValue);
        Assert.AreEqual(8f, movement.LastDash.Value.dashDistance);
        Assert.AreEqual(12f, movement.LastDash.Value.dashSpeed);
        Assert.AreEqual(new Vector3(5f, 0f, 5f), movement.LastDashTarget);
        Cleanup(context.Caster?.gameObject, ability);
    }

    [Test]
    public void ChannelBehaviour_CompletesAfterDurationAndReenablesMovement()
    {
        var movement = new FakeMovement();
        var behaviour = new AbilityChannelBehaviour();
        var stats = new Dictionary<AbilityStatKey, float>
        {
            { AbilityStatKey.ChannelDuration, 1f }
        };
        var caster = CreateTestCaster();
        var ability = CreateAbilityWithStats(stats, behaviour);
        var (context, executor) = CreateCastContextWithExecutor(ability, stats, caster: caster, movement: movement, targetPoint: Vector3.one, isHeld: true);

        behaviour.OnCastStarted(context);
        Assert.IsTrue(movement.Disabled);

        behaviour.OnCastUpdated(context, 0.6f);
        Assert.Greater(context.ChannelRatio, 0f);

        behaviour.OnCastUpdated(context, 0.5f);
        Assert.IsTrue(executor.EndCastCalled);
        Assert.IsTrue(executor.LastEndSuccess);
        Assert.AreEqual(context.TargetPoint, executor.LastEndPosition);

        behaviour.OnCastEnded(context, true);
        Assert.IsFalse(movement.Disabled);
        Cleanup(caster.gameObject, ability);
    }

    [Test]
    public void ChannelProjectileCountBehaviour_AddsProjectilesBasedOnChannelRatio()
    {
        var behaviour = new AbilityChannelProjectileCountBehaviour();
        SetPrivateField(behaviour, "minAdditionalProjectiles", 1);
        SetPrivateField(behaviour, "maxAdditionalProjectiles", 3);

        var stats = new Dictionary<AbilityStatKey, float>
        {
            { AbilityStatKey.ProjectileCount, 2f }
        };
        var ability = CreateAbilityWithStats(stats, behaviour);
        var (context, _) = CreateCastContextWithExecutor(ability, stats);
        context.ChannelRatio = 0.5f;

        behaviour.OnCastEnded(context, true);

        Assert.IsTrue(context.TryGetSharedStatOverride(AbilityStatKey.ProjectileCount, out float finalCount));
        Assert.AreEqual(4f, finalCount);
        Cleanup(ability);
    }

    [Test]
    public void ProjectileLaunchBehaviour_LaunchesConfiguredCountAtTargetPoint()
    {
        var projectilePrefab = CreateProjectilePrefab<TrackingProjectile>("LaunchProjectile");
        TrackingProjectile.SpawnPositions.Clear();
        var behaviour = new AbilityProjectileLaunchBehaviour();
        SetPrivateField(behaviour, "projectile", projectilePrefab);
        SetPrivateField(behaviour, "launchPosition", AbilityProjectileLaunchBehaviour.LaunchPosition.TargetPoint);

        var stats = new Dictionary<AbilityStatKey, float>
        {
            { AbilityStatKey.ProjectileCount, 3f },
            { AbilityStatKey.ProjectileSpreadAngle, 0f },
            { AbilityStatKey.ProjectileMaxSpreadAngle, 0f }
        };

        var caster = CreateTestCaster();
        var launcher = caster.GetComponent<ProjectileLauncher>();
        var ability = CreateAbilityWithStats(stats, behaviour);
        var (context, _) = CreateCastContextWithExecutor(ability, stats, caster: caster, movement: new FakeMovement(), projectileLauncher: launcher, targetPoint: new Vector3(10f, 0f, 0f));

        behaviour.OnCastStarted(context);

        Assert.NotNull(context.LastLaunchedProjectiles);
        Assert.AreEqual(3, context.LastLaunchedProjectiles.Length);
        foreach (var pos in TrackingProjectile.SpawnPositions)
        {
            Assert.AreEqual(new Vector3(10f, 0f, 0f), pos);
        }

        CleanupSpawnedProjectiles();
        Cleanup(projectilePrefab.gameObject, caster.gameObject, ability);
    }

    [Test]
    public void ChargedProjectileReleaseBehaviour_SetsLifetimeAndDamageFromChannel()
    {
        var projectilePrefab = CreateProjectilePrefab<TrackingLinearProjectile>("ChargedProjectile");
        TrackingLinearProjectile.Spawned.Clear();
        SetPrivateField(projectilePrefab, "maxSpeed", 10f);
        var behaviour = new AbilityChargedProjectileReleaseBehaviour();
        SetPrivateField(behaviour, "projectile", projectilePrefab);
        SetPrivateField(behaviour, "minDistance", 4f);
        SetPrivateField(behaviour, "maxDistance", 8f);
        SetPrivateField(behaviour, "minDamage", 10);
        SetPrivateField(behaviour, "maxDamage", 30);

        var stats = new Dictionary<AbilityStatKey, float>
        {
            { AbilityStatKey.ProjectileCount, 2f },
            { AbilityStatKey.ProjectileSpreadAngle, 0f },
            { AbilityStatKey.ProjectileMaxSpreadAngle, 0f }
        };

        var caster = CreateTestCaster();
        var launcher = caster.GetComponent<ProjectileLauncher>();
        var ability = CreateAbilityWithStats(stats, behaviour);
        var (context, executor) = CreateCastContextWithExecutor(ability, stats, caster: caster, projectileLauncher: launcher);
        context.ChannelRatio = 0.75f;
        context.SetAimPoint(new Vector3(1f, 0f, 0f));

        behaviour.OnCastEnded(context, true);

        Assert.NotNull(context.LastLaunchedProjectiles);
        Assert.AreEqual(2, context.LastLaunchedProjectiles.Length);
        float expectedLifeTime = 7f / 10f;
        foreach (var proj in TrackingLinearProjectile.Spawned)
        {
            float life = (float)GetPrivateField(proj, "lifeTime");
            Assert.AreEqual(expectedLifeTime, life, 0.001f);

            var hitbox = proj.GetComponent<Hitbox>();
            Assert.NotNull(hitbox);
            Assert.AreEqual(25, hitbox.CreateDamageInstance().Damage);
        }

        Assert.AreEqual(new Vector3(1f, 0f, 0f), executor.LastLookAtPosition);

        CleanupSpawnedProjectiles();
        Cleanup(projectilePrefab.gameObject, caster.gameObject, ability);
    }

    [Test]
    public void PerpendicularVolleyBehaviour_SpawnsAcrossRightVector()
    {
        var projectilePrefab = CreateProjectilePrefab<TrackingProjectile>("VolleyProjectile");
        TrackingProjectile.SpawnPositions.Clear();
        var behaviour = new AbilityPerpendicularProjectileVolleyBehaviour();
        SetPrivateField(behaviour, "projectile", projectilePrefab);

        var stats = new Dictionary<AbilityStatKey, float>
        {
            { AbilityStatKey.ProjectileCount, 3f },
            { AbilityStatKey.VolleySpacing, 2f },
            { AbilityStatKey.VolleyVerticalOffset, 0.5f }
        };

        var caster = CreateTestCaster();
        caster.transform.rotation = Quaternion.identity;
        var launcher = caster.GetComponent<ProjectileLauncher>();
        var ability = CreateAbilityWithStats(stats, behaviour);
        var (context, _) = CreateCastContextWithExecutor(ability, stats, caster: caster, projectileLauncher: launcher);
        context.ChannelRatio = 1f;

        behaviour.OnCastEnded(context, true);

        Assert.AreEqual(3, TrackingProjectile.SpawnPositions.Count);
        Assert.Less(Vector3.Distance(new Vector3(-2f, 0.5f, 0f), TrackingProjectile.SpawnPositions[0]), 0.001f);
        Assert.Less(Vector3.Distance(new Vector3(0f, 0.5f, 0f), TrackingProjectile.SpawnPositions[1]), 0.001f);
        Assert.Less(Vector3.Distance(new Vector3(2f, 0.5f, 0f), TrackingProjectile.SpawnPositions[2]), 0.001f);

        CleanupSpawnedProjectiles();
        Cleanup(projectilePrefab.gameObject, caster.gameObject, ability);
    }

    [Test]
    public void RangeIndicatorBehaviour_ShowsDuringChannelAndHidesOnEnd()
    {
        var rangePrefab = CreateRangeIndicatorPrefab("RangeIndicator");
        var behaviour = new AbilityRangeIndicatorBehaviour();
        SetPrivateField(behaviour, "rangeIndicator", rangePrefab);
        SetPrivateField(behaviour, "minDistance", 2f);
        SetPrivateField(behaviour, "maxDistance", 6f);

        var caster = CreateTestCaster();
        var ability = CreateAbility(behaviour);
        var (context, _) = CreateCastContextWithExecutor(ability, new Dictionary<AbilityStatKey, float>(), caster: caster);

        behaviour.OnCastStarted(context);
        var indicator = Object.FindFirstObjectByType<RangeIndicator>();
        Assert.NotNull(indicator);
        Assert.AreEqual(new Vector3(4f, 1f, 4f), indicator.transform.localScale);

        context.ChannelRatio = 0.5f;
        behaviour.OnCastUpdated(context, 0.1f);
        Assert.AreEqual(new Vector3(8f, 1f, 8f), indicator.transform.localScale);

        behaviour.OnCastEnded(context, true);
        Assert.IsFalse(indicator.gameObject.activeSelf);

        Cleanup(rangePrefab.gameObject, caster.gameObject, ability, indicator.gameObject);
    }

    private static Ability CreateAbility(params AbilityBehaviour[] behaviours)
    {
        var ability = ScriptableObject.CreateInstance<Ability>();
        SetAbilityField(ability, "behaviours", new List<AbilityBehaviour>(behaviours));
        SetAbilityField(ability, "rotatingCasterToCastDirection", false);
        SetAbilityField(ability, "stopMovementOnCast", false);
        SetAbilityField(ability, "baseStats", new List<AbilityStatEntry>
        {
            new AbilityStatEntry { Key = AbilityStatKey.Cooldown, Value = 1f }
        });
        return ability;
    }

    private static Ability CreateAbilityWithStats(Dictionary<AbilityStatKey, float> baseStats, params AbilityBehaviour[] behaviours)
    {
        var ability = ScriptableObject.CreateInstance<Ability>();
        SetAbilityField(ability, "behaviours", new List<AbilityBehaviour>(behaviours));
        SetAbilityField(ability, "rotatingCasterToCastDirection", false);
        SetAbilityField(ability, "stopMovementOnCast", false);
        var statEntries = new List<AbilityStatEntry>();
        foreach (var kvp in baseStats)
        {
            statEntries.Add(new AbilityStatEntry { Key = kvp.Key, Value = kvp.Value });
        }

        // Always include a cooldown so SpellSlot/executors have a value.
        if (!baseStats.ContainsKey(AbilityStatKey.Cooldown))
            statEntries.Add(new AbilityStatEntry { Key = AbilityStatKey.Cooldown, Value = 1f });

        SetAbilityField(ability, "baseStats", statEntries);
        return ability;
    }

    private static void SetDelaySeconds(AbilityDelayBehaviour delay, float seconds)
    {
        typeof(AbilityDelayBehaviour)
            .GetField("delaySeconds", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(delay, seconds);
    }

    private static void SetAbilityField<T>(Ability ability, string fieldName, T value)
    {
        var field = typeof(Ability).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(ability, value);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var type = target.GetType();
        while (type != null)
        {
            var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(target, value);
                return;
            }

            type = type.BaseType;
        }
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

    private static (AbilityCastContext context, StubExecutor executor) CreateCastContextWithExecutor(
        Ability ability,
        Dictionary<AbilityStatKey, float> stats,
        AbilityCaster caster = null,
        IAbilityMovement movement = null,
        ProjectileLauncher projectileLauncher = null,
        IAbilityDebuffService debuffService = null,
        Vector3? targetPoint = null,
        bool isHeld = false)
    {
        caster ??= CreateTestCaster();
        projectileLauncher ??= caster.GetComponent<ProjectileLauncher>();
        var executor = new StubExecutor(ability, stats, caster, movement, projectileLauncher, debuffService);
        var behaviourContext = new AbilityBehaviourContext(
            ability,
            caster,
            movement,
            projectileLauncher,
            caster.ModifierManager,
            executor,
            debuffService,
            new AbilityStatProvider());
        var context = new AbilityCastContext(behaviourContext, targetPoint ?? Vector3.zero, isHeld);
        return (context, executor);
    }

    private static TestAbilityCaster CreateTestCaster()
    {
        var go = new GameObject("TestCaster");
        var launcher = go.AddComponent<ProjectileLauncher>();
        var caster = go.AddComponent<TestAbilityCaster>();
        SetPrivateField(caster, "projectileLauncher", launcher);
        return caster;
    }

    private static Targetable CreateTargetable(Vector3 position, int maxHealth)
    {
        var go = new GameObject("Targetable");
        go.transform.position = position;
        var collider = go.AddComponent<SphereCollider>();
        var targetable = go.AddComponent<Targetable>();
        var character = go.AddComponent<TestCharacter>();
        var health = go.AddComponent<Health>();
        SetPrivateField(health, "maxHealth", maxHealth);
        health.Initialize();

        SetPrivateField(character, "health", health);
        SetPrivateField(targetable, "targetableCollider", collider);
        SetPrivateField(targetable, "character", character);
        return targetable;
    }

    private static T CreateProjectilePrefab<T>(string name) where T : Projectile
    {
        var go = new GameObject(name);
        var projectile = go.AddComponent<T>();
        go.AddComponent<Hitbox>();
        return projectile;
    }

    private static RangeIndicator CreateRangeIndicatorPrefab(string name)
    {
        var go = new GameObject(name);
        return go.AddComponent<RangeIndicator>();
    }

    private static void CleanupSpawnedProjectiles()
    {
        foreach (var projectile in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))
        {
            Object.DestroyImmediate(projectile.gameObject);
        }
    }

    private static void Cleanup(params Object[] objects)
    {
        foreach (var obj in objects)
        {
            if (obj != null)
                Object.DestroyImmediate(obj);
        }
    }

    private static object GetPrivateField(object target, string fieldName)
    {
        var type = target.GetType();
        while (type != null)
        {
            var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
                return field.GetValue(target);

            type = type.BaseType;
        }

        return null;
    }

    private sealed class StubExecutor : IAbilityExecutor
    {
        private readonly Dictionary<AbilityStatKey, float> stats;

        public Ability Definition { get; }
        public AbilityCaster Caster { get; }
        public IAbilityMovement Movement { get; }
        public ProjectileLauncher ProjectileLauncher { get; }
        public AbilityModifierManager ModifierManager { get; }
        public IAbilityStatProvider StatProvider { get; }
        public IAbilityDebuffService DebuffService { get; }
        public float Cooldown => GetStat(AbilityStatKey.Cooldown);
        public bool IsCasting { get; private set; }
        public bool EndCastCalled { get; private set; }
        public bool LastEndSuccess { get; private set; }
        public Vector3 LastEndPosition { get; private set; }
        public Vector3 LastLookAtPosition { get; private set; }

        public event UnityAction<Ability> On_StartCast;
        public event UnityAction<bool> On_EndCast;

        public StubExecutor(
            Ability ability,
            Dictionary<AbilityStatKey, float> stats,
            AbilityCaster caster,
            IAbilityMovement movement,
            ProjectileLauncher projectileLauncher,
            IAbilityDebuffService debuffService)
        {
            Definition = ability;
            this.stats = stats ?? new Dictionary<AbilityStatKey, float>();
            Caster = caster;
            Movement = movement;
            ProjectileLauncher = projectileLauncher;
            DebuffService = debuffService;
            ModifierManager = caster != null ? caster.ModifierManager : null;
            StatProvider = new AbilityStatProvider();
        }

        public void Cast(Vector3 worldPos, bool isHeld)
        {
            IsCasting = true;
            On_StartCast?.Invoke(Definition);
        }

        public void EndHold(Vector3 worldPos) { }

        public void EndCast(Vector3 worldPos, bool isSuccessful = true)
        {
            IsCasting = false;
            EndCastCalled = true;
            LastEndSuccess = isSuccessful;
            LastEndPosition = worldPos;
            On_EndCast?.Invoke(isSuccessful);
        }

        public void Update(float deltaTime) { }

        public float GetStat(AbilityStatKey key)
        {
            return stats != null && stats.TryGetValue(key, out float value) ? value : 0f;
        }

        public void LookAtCastDirection(Vector3 worldPos)
        {
            LastLookAtPosition = worldPos;
        }

        public void Disable()
        {
            IsCasting = false;
        }
    }

    private class TestAbilityCaster : AbilityCaster
    {
        // Avoid the default slot initialization noise during tests.
        private void OnEnable() { }
        private void OnDisable() { }
    }

    private class TestCharacter : Character
    {
        // Avoid base OnEnable hooking into null components during tests.
        private void OnEnable() { }
        private void OnDisable() { }
    }

    private class TrackingProjectile : Projectile
    {
        public static readonly List<Vector3> SpawnPositions = new List<Vector3>();

        protected override void OnEnable()
        {
            base.OnEnable();
            SpawnPositions.Add(transform.position);
        }
    }

    private class TrackingLinearProjectile : LinearProjectile
    {
        public static readonly List<TrackingLinearProjectile> Spawned = new List<TrackingLinearProjectile>();

        protected override void OnEnable()
        {
            base.OnEnable();
            Spawned.Add(this);
        }
    }
}
