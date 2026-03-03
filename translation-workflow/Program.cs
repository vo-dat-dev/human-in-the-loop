// =============================================================
// Demo: Agents in Workflows
// Based on: https://learn.microsoft.com/en-us/agent-framework/workflows/agents-in-workflows
//
// This demo creates a sequential translation pipeline:
//   English input  →  French Agent  →  Spanish Agent  →  English Agent
//
// Instead of Azure Foundry, it uses GitHub Models (same approach
// as the ProverbsAgent project in this solution).
// =============================================================

using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OpenAI;

// ── 1. Setup client ────────────────────────────────────────────────────────────

// Read the GitHub token from user-secrets or environment variable.
// Run once: dotnet user-secrets set GitHubToken "<your-token>"
// Or:       gh auth token  |  set the output as GITHUB_TOKEN env var
string githubToken =
    Environment.GetEnvironmentVariable("GITHUB_TOKEN") ??
    GetUserSecret("GitHubToken") ??
    throw new InvalidOperationException(
        "GitHub token not found.\n" +
        "Set it with:  dotnet user-secrets set GitHubToken \"<token>\"\n" +
        "Or:           $env:GITHUB_TOKEN = (gh auth token)");

var openAiClient = new OpenAIClient(
    new System.ClientModel.ApiKeyCredential(githubToken),
    new OpenAIClientOptions
    {
        Endpoint = new Uri("https://models.inference.ai.azure.com")
    });

const string Model = "gpt-4o-mini";

// ── 2. Create translation agents ───────────────────────────────────────────────

Console.WriteLine("Creating translation agents...");

AIAgent frenchAgent  = CreateTranslationAgent("French",  openAiClient, Model);
AIAgent spanishAgent = CreateTranslationAgent("Spanish", openAiClient, Model);
AIAgent englishAgent = CreateTranslationAgent("English", openAiClient, Model);

Console.WriteLine("  ✓ French translator");
Console.WriteLine("  ✓ Spanish translator");
Console.WriteLine("  ✓ English translator");

// ── 3. Build the sequential workflow ──────────────────────────────────────────
//
//   Input → FrenchAgent → SpanishAgent → EnglishAgent → Output
//
// Each agent receives the previous agent's output as context.

// AgentWorkflowBuilder.BuildSequential wires the agents as a pipeline:
//   frenchAgent → spanishAgent → englishAgent
Workflow workflow = AgentWorkflowBuilder.BuildSequential(
    [frenchAgent, spanishAgent, englishAgent]);

Console.WriteLine("\nWorkflow: FrenchAgent → SpanishAgent → EnglishAgent");

// ── 4. Execute with streaming ──────────────────────────────────────────────────

string input = args.Length > 0 ? string.Join(" ", args) : "Hello World!";
Console.WriteLine($"\nInput: \"{input}\"");
Console.WriteLine(new string('─', 50));

await using StreamingRun run = await InProcessExecution.RunStreamingAsync(
    workflow,
    new ChatMessage(ChatRole.User, input));

// TurnToken triggers agents to start processing their cached messages.
await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

await foreach (WorkflowEvent evt in run.WatchStreamAsync().ConfigureAwait(false))
{
    if (evt is AgentResponseUpdateEvent update && !string.IsNullOrEmpty(update.Data))
    {
        Console.Write($"[{update.ExecutorId}] {update.Data}");
    }
    else if (evt is AgentResponseEvent response)
    {
        // Non-streaming fallback — print on a new line for readability
        Console.WriteLine($"\n[{response.ExecutorId}] {response.Data}");
    }
}

Console.WriteLine("\n" + new string('─', 50));
Console.WriteLine("Workflow complete.");

// ── Helper methods ─────────────────────────────────────────────────────────────

/// <summary>
/// Creates a <see cref="ChatClientAgent"/> configured to translate text into
/// <paramref name="targetLanguage"/>.
/// </summary>
static ChatClientAgent CreateTranslationAgent(
    string targetLanguage,
    OpenAIClient client,
    string model)
{
    var chatClient = client.GetChatClient(model).AsIChatClient();

    return new ChatClientAgent(
        chatClient,
        name: $"{targetLanguage}Translator",
        description: $"Translates text to {targetLanguage}.",
        instructions: $"You are a translation assistant. " +
                      $"Translate the provided text to {targetLanguage}. " +
                      $"Return ONLY the translated text, nothing else.");
}

/// <summary>
/// Reads a value from .NET user-secrets for this project.
/// </summary>
static string? GetUserSecret(string key)
{
    try
    {
        // User secrets are stored in %APPDATA%\Microsoft\UserSecrets\<id>\secrets.json
        var secretsId = "translation-workflow-12345678-1234-1234-1234-123456789abc";
        var secretsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Microsoft", "UserSecrets", secretsId, "secrets.json");

        if (!File.Exists(secretsPath))
            return null;

        var json = File.ReadAllText(secretsPath);
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty(key, out var val) ? val.GetString() : null;
    }
    catch
    {
        return null;
    }
}
