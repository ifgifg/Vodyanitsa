using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;
using Vodyanitsa.Common.Players;

namespace Vodyanitsa.Common.Drawing;

internal readonly record struct CharacterPose(AtlasFrame Frame, Texture2D Texture, Vector2 Position, SpriteEffects Effects, float Rotation);

internal static class CharacterDrawing
{
    internal static bool Visible(PlayerDrawSet info) => VodyanitsaPlayer.Enabled && CharacterAtlas.Data != null
        && !info.hideEntirePlayer && !info.drawPlayer.dead && !info.drawPlayer.ghost && !info.drawPlayer.invis;

    internal static CharacterPose Pose(PlayerDrawSet info)
    {
        Player player = info.drawPlayer;
        var form = player.GetModPlayer<VodyanitsaPlayer>();
        bool water = form.IsMermaid;
        int frameIndex;
        if (water) frameIndex = (int)((Main.GameUpdateCount / 6 + (uint)player.whoAmI) % 8);
        else if (player.mount.Active || player.sitting.isSitting) frameIndex = 7;
        else if (player.velocity.Y < -0.25f) frameIndex = 5;
        else if (player.velocity.Y > 0.25f) frameIndex = 6;
        else if (Math.Abs(player.velocity.X) > 0.12f) frameIndex = 1 + Math.Clamp((player.legFrame.Y / Math.Max(1, player.legFrame.Height) - 7) * 4 / 13, 0, 3);
        else frameIndex = 0;
        AtlasFrame frame = water ? CharacterAtlas.Data!.WaterFrames[frameIndex] : CharacterAtlas.Data!.LandFrames[frameIndex];
        // Cropped artwork has an explicit real foot anchor; unlike vanilla's
        // 40x56 sheets it needs no transparent-bottom-padding compensation.
        Vector2 position = info.Position - Main.screenPosition + new Vector2(player.width / 2f,
            water ? player.height / 2f : player.gravDir > 0 ? player.height : 0f);
        return new(frame, (water ? CharacterAtlas.Water : CharacterAtlas.Land)!.Value, position, info.playerEffect, water ? form.SwimRotation : 0f);
    }

    internal static Vector2 MirroredAnchor(AtlasFrame frame, SpriteEffects effects)
        => new((effects & SpriteEffects.FlipHorizontally) != 0 ? frame.Width - frame.AnchorX : frame.AnchorX,
            (effects & SpriteEffects.FlipVertically) != 0 ? frame.Height - frame.AnchorY : frame.AnchorY);

    internal static Vector2 Offset(AtlasFrame frame, Vector2 local, SpriteEffects effects, float rotation)
    {
        Vector2 offset = (local - frame.Anchor) * frame.Scale;
        if ((effects & SpriteEffects.FlipHorizontally) != 0) offset.X = -offset.X;
        if ((effects & SpriteEffects.FlipVertically) != 0) offset.Y = -offset.Y;
        return offset.RotatedBy(rotation);
    }

    internal static void AlignUmbrellaHand(ref PlayerDrawSet info)
    {
        if (!Visible(info) || info.headOnlyRender || CharacterAtlas.Arms == null || !HeldItemPose.IsUmbrella(info.heldItem)
            || !HeldItemPose.ShouldPoseForHeldItem(info.drawPlayer, info.heldItem)) return;
        Texture2D weaponTexture = TextureAssets.Item[info.heldItem.type].Value;
        int armIndex = -1, weaponIndex = -1;
        for (int i = 0; i < info.DrawDataCache.Count; i++) {
            if (info.DrawDataCache[i].texture == CharacterAtlas.Arms.Value) armIndex = i;
            if (info.DrawDataCache[i].texture == weaponTexture) weaponIndex = i;
        }
        if (armIndex < 0 || weaponIndex < 0) return;
        DrawData arm = info.DrawDataCache[armIndex];
        DrawData weapon = info.DrawDataCache[weaponIndex];
        AtlasFrame? frame = null;
        foreach (AtlasFrame candidate in CharacterAtlas.Data!.ArmFrames)
            if (arm.sourceRect == candidate.Source) { frame = candidate; break; }
        if (frame == null) return;

        Rectangle source = weapon.sourceRect ?? weapon.texture.Bounds;
        // Native umbrella itemLocation is a texture corner, not its shaft.
        // Both vanilla umbrella sprites have the lower shaft at frame center,
        // four source pixels above the bottom. Transform this verified grip
        // through the actual native draw data after its final player transforms.
        Vector2 target = TexturePoint(weapon, new Vector2(source.Width / 2f, source.Height - 4f));
        Vector2 localHand = MirroredPoint(arm, new Vector2(frame.HandX, frame.HandY)) - arm.origin;
        Vector2 offset = localHand * arm.scale;
        Vector2 toGrip = target - arm.position;
        if (offset.LengthSquared() <= 1f || toGrip.LengthSquared() <= 1f) return;
        arm.scale *= MathHelper.Clamp(toGrip.Length() / offset.Length(), 0.5f, 1.6f);
        arm.rotation = toGrip.ToRotation() - (localHand * arm.scale).ToRotation();
        info.DrawDataCache[armIndex] = arm;
    }

    private static Vector2 MirroredPoint(DrawData draw, Vector2 point)
    {
        Rectangle source = draw.sourceRect ?? draw.texture.Bounds;
        if ((draw.effect & SpriteEffects.FlipHorizontally) != 0) point.X = source.Width - point.X;
        if ((draw.effect & SpriteEffects.FlipVertically) != 0) point.Y = source.Height - point.Y;
        return point;
    }

    private static Vector2 TexturePoint(DrawData draw, Vector2 point)
        => draw.position + ((MirroredPoint(draw, point) - draw.origin) * draw.scale).RotatedBy(draw.rotation);
}

public sealed class VodyanitsaBodyLayer : PlayerDrawLayer
{
    public override Position GetDefaultPosition() => new Between(PlayerDrawLayers.Skin, PlayerDrawLayers.Leggings);
    public override bool IsHeadLayer => true;
    public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) => CharacterDrawing.Visible(drawInfo);

    protected override void Draw(ref PlayerDrawSet info)
    {
        if (info.headOnlyRender) {
            AtlasFrame face = CharacterAtlas.Data!.Portrait;
            var player = info.drawPlayer;
            Vector2 position = info.Position - Main.screenPosition + new Vector2(player.width / 2f, 10f) + player.headPosition;
            info.DrawDataCache.Add(new DrawData(CharacterAtlas.PortraitTexture!.Value, position, face.Source, info.colorArmorHead,
                player.headRotation, CharacterDrawing.MirroredAnchor(face, info.playerEffect), face.Scale, info.playerEffect));
            return;
        }
        CharacterPose pose = CharacterDrawing.Pose(info);
        info.DrawDataCache.Add(new DrawData(pose.Texture, pose.Position, pose.Frame.Source, info.colorArmorBody,
            pose.Rotation, CharacterDrawing.MirroredAnchor(pose.Frame, pose.Effects), pose.Frame.Scale, pose.Effects));
    }
}

public sealed class VodyanitsaArmLayer : PlayerDrawLayer
{
    public override Position GetDefaultPosition() => new Between(PlayerDrawLayers.ArmOverItem, PlayerDrawLayers.HandOnAcc);
    public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) => !drawInfo.headOnlyRender && CharacterDrawing.Visible(drawInfo) && CharacterAtlas.Arms != null;

    protected override void Draw(ref PlayerDrawSet info)
    {
        Player player = info.drawPlayer;
        CharacterPose pose = CharacterDrawing.Pose(info);
        bool usingItem = HeldItemPose.ShouldPoseForHeldItem(player, info.heldItem);
        int armIndex = usingItem ? 1 : 0;
        if (CharacterAtlas.Data!.ArmFrames.Length == 4 && player.GetModPlayer<VodyanitsaPlayer>().IsMermaid) armIndex += 2;
        AtlasFrame arm = CharacterAtlas.Data.ArmFrames[armIndex];
        Vector2 shoulder = pose.Position + CharacterDrawing.Offset(pose.Frame, new Vector2(pose.Frame.ShoulderX, pose.Frame.ShoulderY), pose.Effects, pose.Rotation);
        float armRotation = pose.Rotation;
        float armScale = arm.Scale;
        if (!usingItem && player.GetModPlayer<VodyanitsaPlayer>().IsMermaid)
            armRotation += player.direction * player.gravDir * MathHelper.Pi / 3f;
        if (usingItem) {
            armRotation = player.compositeFrontArm.enabled ? player.compositeFrontArm.rotation : player.itemRotation - player.direction * MathHelper.PiOver2;
            if (player.gravDir < 0) armRotation = -armRotation;
            if (!HeldItemPose.IsUmbrella(info.heldItem) && (arm.HandX != 0 || arm.HandY != 0)) {
                Vector2 localHand = CharacterDrawing.Offset(arm, new Vector2(arm.HandX, arm.HandY), pose.Effects, 0f);
                // Use the draw set's final native hand location, including
                // mount/pose adjustments made after the basic player copy.
                Vector2 itemHandle = info.ItemLocation - Main.screenPosition;
                Vector2 toHandle = itemHandle - shoulder;
                if (localHand.LengthSquared() > 1f && toHandle.LengthSquared() is > 4f and < 4096f) {
                    armRotation = toHandle.ToRotation() - localHand.ToRotation();
                    // A fixed rotated limb does not reach changing native grip
                    // distances. Mild, bounded pose scaling closes that gap
                    // without moving the weapon or changing its use/collision.
                    armScale *= MathHelper.Clamp(toHandle.Length() / localHand.Length(), 0.5f, 1.6f);
                }
            }
        }
        info.DrawDataCache.Add(new DrawData(CharacterAtlas.Arms!.Value, shoulder, arm.Source, info.colorArmorBody,
            armRotation, CharacterDrawing.MirroredAnchor(arm, pose.Effects), armScale, pose.Effects));
    }
}
