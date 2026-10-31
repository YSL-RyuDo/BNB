using System.Collections.Generic;
using System.Globalization;

// Transport-independent authoritative health cache, also used before scene UI exists.
public sealed class CombatState
{
    public ulong MatchId { get; private set; }
    private readonly Dictionary<string, ulong> revisions = new Dictionary<string, ulong>();
    private readonly Dictionary<string, int> health = new Dictionary<string, int>();

    public bool Begin(string value)
    {
        if (!ulong.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out ulong match) ||
            match == 0 || match <= MatchId) return false;
        MatchId = match;
        revisions.Clear(); health.Clear();
        return true;
    }

    public void Reset() { MatchId = 0; revisions.Clear(); health.Clear(); }
    public bool TryGetHealth(string nickname, out int hp) { return health.TryGetValue(nickname, out hp); }

    public bool Apply(string packet, out string nickname, out int hp)
    {
        nickname = null; hp = 0;
        if (packet == null) return false;
        string[] fields = packet.Split('|');
        if (fields.Length != 6 || fields[0] != "DAMAGE" || fields[1].Length == 0 ||
            !int.TryParse(fields[2], NumberStyles.None, CultureInfo.InvariantCulture, out int damage) || damage <= 0 ||
            !int.TryParse(fields[3], NumberStyles.None, CultureInfo.InvariantCulture, out int remaining) ||
            !ulong.TryParse(fields[4], NumberStyles.None, CultureInfo.InvariantCulture, out ulong match) || match == 0 || match != MatchId ||
            !ulong.TryParse(fields[5], NumberStyles.None, CultureInfo.InvariantCulture, out ulong revision) || revision == 0)
            return false;
        if (revisions.TryGetValue(fields[1], out ulong previous) && revision <= previous) return false;
        nickname = fields[1]; hp = remaining;
        revisions[nickname] = revision; health[nickname] = hp;
        return true;
    }
}
