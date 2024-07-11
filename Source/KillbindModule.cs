using System;

namespace Celeste.Mod.Killbind;

public class KillbindModule : EverestModule {
    public static KillbindModule Instance { get; private set; }

    public override Type SettingsType => typeof(KillbindModuleSettings);
    public static KillbindModuleSettings Settings => (KillbindModuleSettings) Instance._Settings;

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
    }

    public override void Unload() {
        // TODO: unapply any hooks applied in Load()
    }
}