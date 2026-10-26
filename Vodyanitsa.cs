using Terraria;
using Terraria.ModLoader;
using Vodyanitsa.Common.Drawing;

namespace Vodyanitsa;

public sealed class Vodyanitsa : Mod
{
    public override void Load()
    {
        if (!Main.dedServ) CharacterAtlas.Load(this);
    }

    public override void Unload() => CharacterAtlas.Unload();
}
