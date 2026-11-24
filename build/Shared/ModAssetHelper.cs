using System;
using System.Collections.Generic;
using System.IO;
using Godot;

namespace Sts2Mods.Shared;

/// <summary>
/// Loads raw PNG textures from Godot res:// (PCK) or filesystem paths without requiring Godot Editor .ctex imports.
/// </summary>
public static class ModAssetHelper
{
    private static readonly Dictionary<string, Texture2D> Cache = new(StringComparer.Ordinal);
    private static readonly object SyncLock = new();

    public static Texture2D? LoadTexture(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        lock (SyncLock)
        {
            if (Cache.TryGetValue(path, out var cached))
                return cached;

            byte[]? bytes = ReadBytes(path);
            if (bytes == null || bytes.Length == 0)
                return null;

            var image = new Image();
            var err = image.LoadPngFromBuffer(bytes);
            if (err != Error.Ok)
                return null;

            var texture = ImageTexture.CreateFromImage(image);
            Cache[path] = texture;
            return texture;
        }
    }

    private static byte[]? ReadBytes(string path)
    {
        if (path.StartsWith("res://", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("user://", StringComparison.OrdinalIgnoreCase))
        {
            if (!Godot.FileAccess.FileExists(path))
                return null;
            return Godot.FileAccess.GetFileAsBytes(path);
        }

        return File.Exists(path) ? File.ReadAllBytes(path) : null;
    }
}
