using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Vodyanitsa.Common.Config;
using Vodyanitsa.Common.Drawing;
using Vodyanitsa.Common.Swimming;

namespace Vodyanitsa.Common.Players;

public sealed class VodyanitsaPlayer : ModPlayer
{
    private Vector2 velocityBeforeVanilla;
    private bool jumpSuppressed;
    private bool requestedJump;
    private int visualGrace;
    private bool hurtDuringUpdate;
    private Vector2 hurtVelocity;

    public static bool Enabled => ModContent.GetInstance<VodyanitsaConfig>().Enabled;
    public bool IsInOrdinaryWater { get; private set; }
    public bool IsMermaid => Enabled && !Player.dead && !Player.ghost && (IsInOrdinaryWater || visualGrace > 0);
    public float SwimRotation { get; private set; }

    public override void PreUpdate()
    {
        velocityBeforeVanilla = Player.velocity;
        hurtDuringUpdate = false;
        jumpSuppressed = false;
        requestedJump = Player.controlJump;
    }

    private bool CanSwim => Enabled && !Player.dead && !Player.ghost && !Player.CCed && !Player.frozen && !Player.webbed
        && !Player.stoned && !Player.tongued && !Player.mount.Active && !Player.pulley && Player.grapCount == 0;

    public override void SetControls()
    {
        requestedJump = Player.controlJump;
        if (CanSwim && SwimPhysics.IsOrdinaryWater(Player.wet, Player.lavaWet, Player.honeyWet, Player.shimmerWet)) {
            // Space is a swimming input, so vanilla must not spend wing/rocket
            // flight time or trigger an extra jump for the same held button.
            jumpSuppressed = requestedJump;
            Player.controlJump = false;
        }
    }

    public override void PostUpdateMiscEffects()
    {
        if (!Enabled || Player.dead || Player.ghost) return;
        if (WaterContact.TouchesOrdinaryWater(Player.Hitbox)) {
            Player.gills = true;
            Player.breath = Player.breathMax;
            Player.breathCD = 0;
            if (CanSwim) Player.ignoreWater = true;
        }
    }

    public override void PreUpdateMovement()
    {
        if (jumpSuppressed) Player.controlJump = requestedJump;
        bool water = Enabled && !Player.dead && !Player.ghost && SwimPhysics.IsOrdinaryWater(Player.wet, Player.lavaWet, Player.honeyWet, Player.shimmerWet);
        IsInOrdinaryWater = water;
        bool otherLiquid = Player.lavaWet || Player.honeyWet || Player.shimmerWet;
        visualGrace = water ? SwimPhysics.VisualGraceTicks : otherLiquid ? 0 : System.Math.Max(0, visualGrace - 1);
        if (!water || !CanSwim) return;

        Player.ignoreWater = true;
        Player.breath = Player.breathMax;
        Player.breathCD = 0;
        Vector2 input = SwimPhysics.GetInput(Player.controlLeft, Player.controlRight, Player.controlUp, Player.controlDown, requestedJump);
        Player.velocity = hurtDuringUpdate ? hurtVelocity : SwimPhysics.StepVelocity(velocityBeforeVanilla, input);
        Player.jump = 0;
        Player.fallStart = (int)(Player.position.Y / 16f);
        // No position or hitbox edits: vanilla DryCollision still resolves
        // solids, slopes and platforms immediately after this hook.
    }

    public override void PostHurt(Player.HurtInfo info)
    {
        // NPC/tile contact can hurt the player between PreUpdate and movement.
        // PostHurt runs after vanilla applies knockback; never overwrite that
        // same-frame impulse with the pre-update swimming velocity snapshot.
        if (Enabled && info.Knockback > 0f && info.HitDirection != 0) {
            hurtDuringUpdate = true;
            hurtVelocity = Player.velocity;
        }
    }

    public override void PostUpdate()
    {
        if (!Enabled) {
            IsInOrdinaryWater = false;
            visualGrace = 0;
            SwimRotation = 0f;
            return;
        }
        float target = 0f;
        if (IsInOrdinaryWater && Player.velocity.LengthSquared() > 0.05f && !HeldItemPose.ShouldPoseForHeldItem(Player, Player.lastVisualizedSelectedItem)) {
            target = Player.velocity.ToRotation();
            if (Player.direction < 0) target = MathHelper.WrapAngle(target - MathHelper.Pi);
        }
        SwimRotation = MathHelper.WrapAngle(SwimRotation + MathHelper.WrapAngle(target - SwimRotation) * 0.16f);
    }

    private void ResetFormState()
    {
        IsInOrdinaryWater = false;
        visualGrace = 0;
        SwimRotation = 0f;
    }

    public override void UpdateDead() => ResetFormState();
    public override void OnEnterWorld() => ResetFormState();
    public override void OnRespawn() => ResetFormState();

    public override void HideDrawLayers(PlayerDrawSet drawInfo)
    {
        if (!Enabled || CharacterAtlas.Data == null || Player.dead || Player.ghost || drawInfo.hideEntirePlayer) return;
        foreach (PlayerDrawLayer layer in PlayerDrawLayerLoader.Layers) {
            // Multiple-position vanilla layers have independent draw-order
            // slots. Keeping only HeldItem itself still hides its wrappers.
            PlayerDrawLayer source = layer is PlayerDrawLayerSlot slot ? slot.Layer : layer;
            if (source.Mod == Mod || KeepNativeLayers.Contains(source)) continue;
            layer.Hide();
        }
    }

    public override void TransformDrawData(ref PlayerDrawSet drawInfo) => CharacterDrawing.AlignUmbrellaHand(ref drawInfo);

    private static readonly HashSet<PlayerDrawLayer> KeepNativeLayers = new() {
        PlayerDrawLayers.HeldItem, PlayerDrawLayers.ProjectileOverArm,
        PlayerDrawLayers.MountBack, PlayerDrawLayers.MountFront, PlayerDrawLayers.Carpet,
        PlayerDrawLayers.PortableStool, PlayerDrawLayers.Pulley,
        PlayerDrawLayers.ElectrifiedDebuffBack, PlayerDrawLayers.ElectrifiedDebuffFront,
        PlayerDrawLayers.WebbedDebuffBack, PlayerDrawLayers.FrozenOrWebbedDebuff,
        PlayerDrawLayers.IceBarrier, PlayerDrawLayers.CaptureTheGem, PlayerDrawLayers.BeetleBuff
    };
}
