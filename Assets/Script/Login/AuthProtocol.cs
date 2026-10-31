using System;

// Shared by both scene authentication paths; deliberately rejects the old secret-bearing response.
public static class AuthProtocol
{
    public static string Command(string message)
    {
        if (message == null) return string.Empty;
        int separator = message.IndexOf('|');
        return (separator < 0 ? message : message.Substring(0, separator)).TrimEnd('\r', '\n');
    }

    public static bool TryParseLoginSuccess(string message, out string userId, out string nickname)
    {
        userId = nickname = null;
        const string prefix = "LOGIN_SUCCESS|";
        if (message == null || !message.StartsWith(prefix, StringComparison.Ordinal)) return false;
        string payload = message.Substring(prefix.Length).TrimEnd('\r', '\n');
        string[] fields = payload.Split(',');
        if (fields.Length != 2 || !ValidField(fields[0]) || !ValidField(fields[1])) return false;
        userId = fields[0];
        nickname = fields[1];
        return true;
    }

    private static bool ValidField(string value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        foreach (char c in value) if (char.IsControl(c) || c == '|') return false;
        return true;
    }
}
