using Celeste.Mod;
using Microsoft.Xna.Framework.Input;

namespace Celeste.Mod.Killbind;

public class KillbindModuleSettings : EverestModuleSettings {
    [DefaultButtonBinding(Buttons.Back, Keys.R)]
    public ButtonBinding Killbind { get; set; }

    public bool AlwaysDieFast {get; set;}

    public bool SkipAnimation {get; set;}

    public bool AllowWithGolden {get; set;}
}