using System;
using System.Globalization;

public sealed class WorldPacket
{
    public string Command, Nickname;
    public ulong Id, Sequence;
    public int Weapon;
    public float[] Position, Rotation;
    public float Length;
}

public static class WorldProtocol
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;
    private static bool Id(string text, out ulong value) => ulong.TryParse(text, NumberStyles.None, Culture, out value);
    private static bool Number(string text, out float value) => float.TryParse(text, NumberStyles.Float, Culture, out value) && !float.IsNaN(value) && !float.IsInfinity(value);
    private static bool Vector(string text, int count, out float[] values)
    {
        values = new float[count]; string[] parts = text.Split(',');
        if (parts.Length != count) return false;
        for (int i = 0; i < count; i++) if (!Number(parts[i], out values[i]) || Math.Abs(values[i]) > 1000) return false;
        return true;
    }
    public static bool Parse(string line, ulong match, out WorldPacket packet)
    {
        packet = null; if (line == null || match == 0) return false;
        string[] f = line.Split('|');
        if (f.Length < 2 || !Id(f[1], out ulong incoming) || incoming != match) return false;
        var p = new WorldPacket { Command = f[0] };
        switch (f[0])
        {
            case "COMBAT_BEGIN": if (f.Length != 2) return false; break;
            case "PLAYER_STATE":
                if (f.Length != 5 || f[2].Length == 0 || !Vector(f[3], 2, out p.Position) || !Id(f[4], out p.Sequence)) return false;
                p.Nickname = f[2]; break;
            case "WEAPON_SPAWN":
                if (f.Length != 8 || !Id(f[2], out p.Id) || p.Id == 0 || f[3].Length == 0 ||
                    !int.TryParse(f[4], NumberStyles.None, Culture, out p.Weapon) || p.Weapon < 0 || p.Weapon > 6 ||
                    !Vector(f[5], 3, out p.Position) || !Vector(f[6], 2, out p.Rotation) || !Number(f[7], out p.Length) || p.Length < 0 || p.Length > 15) return false;
                p.Nickname = f[3]; break;
            case "WEAPON_STATE":
                if (f.Length != 5 || !Id(f[2], out p.Id) || p.Id == 0 || !Vector(f[3], 3, out p.Position) || !Vector(f[4], 2, out p.Rotation)) return false;
                break;
            case "WEAPON_REMOVE": if (f.Length != 3 || !Id(f[2], out p.Id) || p.Id == 0) return false; break;
            case "TILE_REMOVE":
                if (f.Length != 3 || !Vector(f[2], 2, out p.Position) || p.Position[0] < 0 || p.Position[0] > 14 || p.Position[1] < 0 || p.Position[1] > 12 ||
                    p.Position[0] != Math.Floor(p.Position[0]) || p.Position[1] != Math.Floor(p.Position[1])) return false;
                break;
            default: return false;
        }
        packet = p; return true;
    }
    public static string Move(ulong match, ulong sequence, float x, float z)
    {
        if (match == 0 || sequence == 0 || float.IsNaN(x) || float.IsNaN(z) || float.IsInfinity(x) || float.IsInfinity(z) || x*x+z*z > 1.00001f) return null;
        return $"MOVE_INPUT|{match}|{sequence}|{x.ToString("R",Culture)},{z.ToString("R",Culture)}\n";
    }
    public static string Attack(ulong match, ulong sequence, float yaw)
    {
        if (match == 0 || sequence == 0 || float.IsNaN(yaw) || float.IsInfinity(yaw) || yaw < 0 || yaw >= 360) return null;
        return $"ATTACK|{match}|{sequence}|{yaw.ToString("R",Culture)}\n";
    }
}
