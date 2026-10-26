using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace Vodyanitsa.Common.Swimming;

public static class WaterContact
{
    // CheckDrowning runs before vanilla refreshes wet flags. Reading the liquid
    // tiles directly also protects a player who enters water with zero breath.
    // Unlike Collision.WetCollision this does not alter global collision flags.
    public static bool TouchesOrdinaryWater(Rectangle hitbox)
    {
        if (Main.maxTilesX <= 0 || Main.maxTilesY <= 0) return false;
        int left = Math.Clamp(hitbox.Left / 16, 0, Main.maxTilesX - 1);
        int right = Math.Clamp((hitbox.Right - 1) / 16, 0, Main.maxTilesX - 1);
        int top = Math.Clamp(hitbox.Top / 16, 0, Main.maxTilesY - 1);
        int bottom = Math.Clamp((hitbox.Bottom - 1) / 16, 0, Main.maxTilesY - 1);
        bool water = false;
        for (int x = left; x <= right; x++) {
            for (int y = top; y <= bottom; y++) {
                Tile tile = Main.tile[x, y];
                if (tile.LiquidAmount == 0) continue;
                float surface = (y + 1) * 16f - tile.LiquidAmount * (16f / 255f);
                if (hitbox.Bottom <= surface || hitbox.Top >= (y + 1) * 16) continue;
                if (tile.LiquidType != LiquidID.Water) return false;
                water = true;
            }
        }
        return water;
    }
}
