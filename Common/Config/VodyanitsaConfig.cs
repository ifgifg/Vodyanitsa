using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace Vodyanitsa.Common.Config;

public sealed class VodyanitsaConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ServerSide;

    [DefaultValue(true)]
    public bool Enabled { get; set; } = true;
}
