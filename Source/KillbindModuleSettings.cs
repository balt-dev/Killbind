using Celeste.Mod;

namespace Celeste.Mod.Killbind;

public class KillbindModuleSettings : EverestModuleSettings {
    [DefaultButtonBinding(Buttons.Back, Keys.Tab)]
    public ButtonBinding YourButtonBinding { get; set; }

}