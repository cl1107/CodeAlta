namespace CodeAlta.App;

internal sealed record DeleteSessionResult(
    IReadOnlyList<string> DeletedSessionIds,
    bool DeletedFromSessionStore);
