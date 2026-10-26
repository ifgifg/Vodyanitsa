using Terraria;
using Terraria.ID;

namespace Vodyanitsa.Common.Drawing;

public static class HeldItemPose
{
    public static bool IsUmbrella(Item item) => item.type is ItemID.Umbrella or ItemID.TragicUmbrella;

    /// <summary>Matches the native held-item visibility gates used for arm posing.</summary>
    public static bool ShouldPoseForHeldItem(Player player, Item item)
    {
        if (player.dead || player.ghost || player.frozen || player.JustDroppedAnItem) return false;
        // Held projectiles have their own native draw path, including weapons
        // whose item intentionally sets noUseGraphic.
        if (player.heldProj >= 0) return true;
        if (item.IsAir || item.noUseGraphic) return false;
        bool usingItem = player.itemAnimation > 0 && item.useStyle != 0;
        bool holdingIdle = item.holdStyle != 0 && !player.pulley && player.CanVisuallyHoldItem(item);
        if (!usingItem && !holdingIdle) return false;

        int visualType = item.type;
        if (player.UsingBiomeTorches) {
            if (visualType == ItemID.Torch) visualType = player.BiomeTorchHoldStyle(visualType);
            else if (visualType == ItemID.Campfire) visualType = player.BiomeCampfireHoldStyle(visualType);
        }
        bool waterTorch = visualType >= 0 && visualType < ItemID.Sets.WaterTorches.Length && ItemID.Sets.WaterTorches[visualType];
        if (player.wet && item.noWet && !waterTorch) return false;
        if (player.happyFunTorchTime && player.HeldItem.createTile == TileID.Torches && player.itemAnimation == 0) return false;
        return true;
    }
}
