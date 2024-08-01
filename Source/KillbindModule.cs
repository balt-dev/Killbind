using System;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace Celeste.Mod.Killbind;

public class KillbindModule : EverestModule {
    public static KillbindModule Instance { get; private set; }

    public override Type SettingsType => typeof(KillbindModuleSettings);
    static KillbindModuleSettings Settings => (KillbindModuleSettings) Instance._Settings;

    private static ILHook hook_PlayerDeadBody_DeathRoutine;
    private static readonly MethodInfo m_DeathRoutineEnumerator
        = typeof(PlayerDeadBody).GetMethod("DeathRoutine", BindingFlags.NonPublic | BindingFlags.Instance).GetStateMachineTarget();

    public KillbindModule() {
        Instance = this;
#if DEBUG
        // debug builds use verbose logging
        Logger.SetLogLevel(nameof(KillbindModule), LogLevel.Verbose);
#else
        // release builds use info logging to reduce spam in log files
        Logger.SetLogLevel(nameof(KillbindModule), LogLevel.Info);
#endif
    }

    public override void Load() {
        // TODO: apply any hooks that should always be active
        On.Celeste.Player.Update += OnPlayerUpdate;
        hook_PlayerDeadBody_DeathRoutine = 
                new ILHook(m_DeathRoutineEnumerator, DeathRoutineQuickDie);
        On.Celeste.Pico8.Classic.player.update += OnPicoPlayerUpdate;
        On.Celeste.Pico8.Classic.restart_room += OnPicoRestartRoom;
    }

    private static void OnPicoPlayerUpdate(On.Celeste.Pico8.Classic.player.orig_update orig, Pico8.Classic.player self)
    {
        orig(self);
        if (!Settings.Killbind.Pressed) return;
        Settings.Killbind.ConsumePress();
        self.G.kill_player(self);
    }
    
    private static void OnPicoRestartRoom(On.Celeste.Pico8.Classic.orig_restart_room orig, Pico8.Classic self)
    {
        orig(self);
        if (Settings.SkipAnimation)
        {
            self.delay_restart = 1;
        }
    }


    private static void DeathRoutineQuickDie(ILContext il) {
        ILCursor cur = new(il);
        ILLabel branch = default;

        if (!cur.TryGotoNext(MoveType.After, 
            instr => instr.MatchCall<Vector2>("op_Inequality"),
            instr => instr.MatchBrfalse(out branch)
        )) {
            Logger.Log(LogLevel.Error, nameof(Killbind), $"IL@{cur.Next}: Hook failed to find vector comparison, quick death will not work.");
            return;
        };
        cur.EmitCall(((Delegate) AlwaysDieFast).Method);
        cur.EmitBrtrue(branch);

        if (!cur.TryGotoNext(MoveType.After, 
            instr => instr.MatchLdcR4(0.65f),
            instr => instr.MatchMul()
        )) {
            Logger.Log(LogLevel.Error, nameof(Killbind), $"IL@{cur.Next}: Hook failed to find death effect addition, animation skip will not work.");
            return;
        };

        cur.EmitCall(((Delegate) SkipAnimationMul).Method);
        cur.EmitMul();
    }

    private static bool AlwaysDieFast() { return Settings.AlwaysDieFast; }
    private static float SkipAnimationMul() { if (Settings.SkipAnimation) { return 0f; } else { return 1f;} }

    public override void Unload() {
        // TODO: unapply any hooks applied in Load()
        On.Celeste.Player.Update -= OnPlayerUpdate;
        hook_PlayerDeadBody_DeathRoutine.Dispose();
    }

    #nullable enable
    private static void OnPlayerUpdate(On.Celeste.Player.orig_Update orig, Player self) {
        if (Settings.Killbind.Pressed && self.CanRetry) {
            Settings.Killbind.ConsumePress();

            Strawberry? golden = self.Leader.Followers
                .AsEnumerable()
                .Where((follower) => {
                    return follower.Entity is Strawberry strawberry
                    && strawberry.Golden
                    && !strawberry.Winged;
                })
                .Select((follower) => follower.Entity as Strawberry)
                .FirstOrDefault();

            if (golden != null && !Settings.AllowWithGolden) {
                Level level = self.SceneAs<Level>();
				level.Particles.Emit(Strawberry.P_WingsBurst, 8, golden.Position, new Vector2(4f, 2f));
		        Audio.Play("event:/game/general/strawberry_laugh", golden.Position);
				level.Displacement.AddBurst(golden.Position, 0.6f, 4f, 28f, 0.2f);
            } else {
                self.Die(Vector2.Zero, evenIfInvincible: true);
            }
        }
        orig(self);
    }
}