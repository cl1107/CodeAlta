using CodeNoesis.CodexSdk;
using CodeNoesis.CodexSdk.V2;

var codexClient = await CodexClient.StartAsync(new ClientInfo
{
    Name = "CodeNoesis",
    Version = "1.0.0",
    Title = "CodeNoesis App"
});


var threadList = await codexClient.ThreadListAsync(new ThreadListParams()
{
    Cwd = @"C:\code\XenoAtom\XenoAtom.CommandLine"
});


foreach(var thread in threadList.Data)
{
    Console.WriteLine($"Thread: {thread.Id} - ModelProvider: {thread.ModelProvider}, CliVersion: {thread.CliVersion}, CreatedAt: {thread.CreatedAt} TurnsCount: {thread.Turns.Count}, Preview: {thread.Preview}, Source: {thread.Source}, GitInfo: {thread.GitInfo}");
}

