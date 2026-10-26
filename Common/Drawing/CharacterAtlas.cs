using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace Vodyanitsa.Common.Drawing;

public sealed class AtlasFrame
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public float AnchorX { get; set; }
    public float AnchorY { get; set; }
    public float Scale { get; set; } = 1f;
    public float ShoulderX { get; set; }
    public float ShoulderY { get; set; }
    public float HandX { get; set; }
    public float HandY { get; set; }
    public Rectangle Source => new(X, Y, Width, Height);
    public Vector2 Anchor => new(AnchorX, AnchorY);
}

public sealed class AtlasDefinition
{
    public AtlasFrame[] LandFrames { get; set; } = Array.Empty<AtlasFrame>();
    public AtlasFrame[] WaterFrames { get; set; } = Array.Empty<AtlasFrame>();
    public AtlasFrame Portrait { get; set; } = new();
    public string PortraitTexture { get; set; } = "Land";
    public AtlasFrame[] ArmFrames { get; set; } = Array.Empty<AtlasFrame>();
}

public static class CharacterAtlas
{
    public static AtlasDefinition? Data { get; private set; }
    public static Asset<Texture2D>? Land { get; private set; }
    public static Asset<Texture2D>? Water { get; private set; }
    public static Asset<Texture2D>? Arms { get; private set; }
    public static Asset<Texture2D>? PortraitTexture { get; private set; }

    internal static void Load(Mod mod)
    {
        Data = JsonSerializer.Deserialize<AtlasDefinition>(mod.GetFileBytes("Assets/Character/atlas.json"),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new InvalidDataException("Vodyanitsa atlas.json is empty.");
        if (Data.LandFrames.Length != 8 || Data.WaterFrames.Length != 8) throw new InvalidDataException("Vodyanitsa needs exactly eight land and eight water frames.");
        Land = ModContent.Request<Texture2D>("Vodyanitsa/Assets/Character/Land", AssetRequestMode.ImmediateLoad);
        Water = ModContent.Request<Texture2D>("Vodyanitsa/Assets/Character/Water", AssetRequestMode.ImmediateLoad);
        foreach (AtlasFrame frame in Data.LandFrames) Validate(frame, Land.Value, "land");
        foreach (AtlasFrame frame in Data.WaterFrames) Validate(frame, Water.Value, "water");
        PortraitTexture = Data.PortraitTexture == "Land" ? Land
            : ModContent.Request<Texture2D>("Vodyanitsa/Assets/Character/" + Data.PortraitTexture, AssetRequestMode.ImmediateLoad);
        Validate(Data.Portrait, PortraitTexture.Value, "portrait");
        if (Data.ArmFrames.Length > 0) {
            if (Data.ArmFrames.Length is not (2 or 4)) throw new InvalidDataException("Optional armFrames must contain two shared poses, or four land/water straight/bent poses.");
            Arms = ModContent.Request<Texture2D>("Vodyanitsa/Assets/Character/Arms", AssetRequestMode.ImmediateLoad);
            foreach (AtlasFrame frame in Data.ArmFrames) Validate(frame, Arms.Value, "arm");
        }
        // This tML build's rawimg converter preserves straight-alpha RGB, while
        // its PNG reader premultiplies. Native player drawing uses AlphaBlend.
        // Normalize only our rawimg-backed assets; PNG source files, icons and
        // other mods' textures are untouched. Reading raw bytes also avoids
        // Texture2D.GetData on the loader thread (not permitted by FNA).
        var normalized = new HashSet<Asset<Texture2D>>();
        NormalizeRawAlpha(mod, Land, "Land", normalized);
        NormalizeRawAlpha(mod, Water, "Water", normalized);
        NormalizeRawAlpha(mod, PortraitTexture, Data.PortraitTexture, normalized);
        if (Arms != null) NormalizeRawAlpha(mod, Arms, "Arms", normalized);
    }

    private static void NormalizeRawAlpha(Mod mod, Asset<Texture2D> asset, string name, HashSet<Asset<Texture2D>> normalized)
    {
        if (!normalized.Add(asset)) return;
        string path = "Assets/Character/" + name + ".rawimg";
        if (!mod.FileExists(path)) return; // PngReader has already normalized PNGs.
        using var reader = new BinaryReader(new MemoryStream(mod.GetFileBytes(path)));
        if (reader.ReadInt32() != 1) throw new InvalidDataException("Unsupported Vodyanitsa rawimg version.");
        int width = reader.ReadInt32();
        int height = reader.ReadInt32();
        Texture2D texture = asset.Value;
        if (width != texture.Width || height != texture.Height) throw new InvalidDataException("Vodyanitsa rawimg dimensions do not match the loaded texture.");
        int length = checked(width * height * 4);
        byte[] pixels = reader.ReadBytes(length);
        if (pixels.Length != length) throw new EndOfStreamException("Vodyanitsa rawimg pixels are truncated.");
        for (int i = 0; i < pixels.Length; i += 4) {
            int alpha = pixels[i + 3];
            pixels[i] = (byte)(pixels[i] * alpha / 255);
            pixels[i + 1] = (byte)(pixels[i + 1] * alpha / 255);
            pixels[i + 2] = (byte)(pixels[i + 2] * alpha / 255);
        }
        Main.RunOnMainThread(() => texture.SetData(pixels)).GetAwaiter().GetResult();
    }

    private static void Validate(AtlasFrame frame, Texture2D texture, string kind)
    {
        if (frame.Width <= 0 || frame.Height <= 0 || frame.X < 0 || frame.Y < 0 || frame.X + frame.Width > texture.Width || frame.Y + frame.Height > texture.Height
            || !float.IsFinite(frame.Scale) || frame.Scale <= 0 || frame.Scale > 2f
            || !float.IsFinite(frame.AnchorX) || !float.IsFinite(frame.AnchorY))
            throw new InvalidDataException($"Invalid Vodyanitsa {kind} source rectangle, anchor or scale.");
    }

    internal static void Unload() { Data = null; Land = null; Water = null; Arms = null; PortraitTexture = null; }
}
