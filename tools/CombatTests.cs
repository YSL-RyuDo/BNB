using System;

public static class CombatTests
{
    private static int passed;
    private static void Check(bool ok, string name)
    {
        if (!ok) throw new Exception(name);
        Console.WriteLine("PASS " + name); passed++;
    }
    public static void Main()
    {
        var state = new CombatState();
        Check(!state.Apply("DAMAGE|player|10|90|1|1", out _, out _), "no match rejects damage");
        Check(state.Begin("1") && !state.Begin("1"), "one scene transition per match");
        Check(state.Apply("DAMAGE|player|10|90|1|1", out string nick, out int hp) && nick == "player" && hp == 90,
            "authoritative health applied");
        Check(!state.Apply("DAMAGE|player|10|90|1|1", out _, out _), "duplicate ignored");
        Check(state.Apply("DAMAGE|player|10|70|1|3", out _, out _) &&
            !state.Apply("DAMAGE|player|10|80|1|2", out _, out _) && state.TryGetHealth("player", out hp) && hp == 70,
            "reordered delivery cannot restore old health");
        Check(state.Apply("DAMAGE|other|10|90|1|2", out _, out _), "revisions tracked per player");
        Check(state.TryGetHealth("other", out hp) && hp == 90, "damage cached before scene UI exists");
        Check(state.Apply("DAMAGE|player|100|0|1|4", out _, out _) && state.TryGetHealth("player", out hp) && hp == 0,
            "lethal health comes from server");
        foreach (string malformed in new[] {
            "DAMAGE|player|10", "PLAYER_HIT|player|10", "DAMAGE|player|-10|100|1|9",
            "DAMAGE|player|10|-1|1|9", "DAMAGE||10|10|1|9", "DAMAGE|player|10|20|1|0",
            "DAMAGE|player|10|20|0|9", "DAMAGE|player|10|20|2|9", "DAMAGE|player|10|20|1|9|extra",
            "DAMAGE|player|10|999999999999999|1|9", "DAMAGE|player|10|20|1|18446744073709551616", null
        }) if (state.Apply(malformed, out _, out _)) throw new Exception("malformed accepted");
        Check(true, "12 malformed or legacy packets rejected");
        Check(state.Begin("2") && !state.TryGetHealth("player", out _) && !state.Begin("1") &&
            !state.Apply("DAMAGE|player|10|80|1|100", out _, out _), "new match clears state and rejects replay");
        state.Reset();
        Check(state.MatchId == 0 && state.Begin("1"), "new connection resets match sequence");
        Console.WriteLine($"{passed} groups passed");
    }
}
