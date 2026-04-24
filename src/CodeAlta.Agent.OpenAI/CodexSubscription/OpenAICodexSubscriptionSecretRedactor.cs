namespace CodeAlta.Agent.OpenAI.CodexSubscription;

internal static class OpenAICodexSubscriptionSecretRedactor
{
    public const string Redacted = "[REDACTED]";

    public static string Redact(string? text, OpenAICodexSubscriptionCredential? credential = null)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var redacted = text;
        if (credential is not null)
        {
            redacted = RedactValue(redacted, credential.AccessToken);
            redacted = RedactValue(redacted, credential.RefreshToken);
            redacted = RedactValue(redacted, credential.IdToken);
        }

        redacted = RedactBearerToken(redacted);
        return redacted;
    }

    public static string RedactValue(string text, string? secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
        {
            return text;
        }

        return text.Replace(secret, Redacted, StringComparison.Ordinal);
    }

    private static string RedactBearerToken(string text)
    {
        const string marker = "Bearer ";
        var start = text.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
        {
            return text;
        }

        var tokenStart = start + marker.Length;
        var tokenEnd = tokenStart;
        while (tokenEnd < text.Length && !char.IsWhiteSpace(text[tokenEnd]))
        {
            tokenEnd++;
        }

        return text[..tokenStart] + Redacted + text[tokenEnd..];
    }
}
