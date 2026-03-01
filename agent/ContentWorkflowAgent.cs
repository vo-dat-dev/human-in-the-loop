using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI;
using System.Runtime.CompilerServices;

/// <summary>
/// Demo: Workflow as Agent pattern
/// Pipeline: ResearchAgent → WriterAgent → ReviewerAgent
/// Each agent's output becomes the next one's input.
/// The whole pipeline is exposed as a single AIAgent via .AsAgent()
/// </summary>
public static class ContentWorkflowAgentFactory
{
    public static AIAgent Create(IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("ContentPipeline");

        var githubToken = configuration["GitHubToken"]
            ?? throw new InvalidOperationException("GitHubToken not found in configuration.");

        var openAiClient = new OpenAIClient(
            new System.ClientModel.ApiKeyCredential(githubToken),
            new OpenAIClientOptions
            {
                Endpoint = new Uri("https://models.inference.ai.azure.com")
            });

        IChatClient MakeClient() => openAiClient.GetChatClient("gpt-4o-mini").AsIChatClient();

        // ── Agent 1: Researcher ──────────────────────────────────────────────
        var researchAgent = new LoggingAgentWrapper(
            new ChatClientAgent(
                chatClient: MakeClient(),
                name: "ResearchAgent",
                instructions: """
                You are a research assistant. When given a topic, produce a concise research brief with:
                - 3 key facts
                - 2 current trends
                - 1 surprising insight 
                - Adds a [ResearchAgent ✓] tag at the top
                Format your output as a clear research brief for a writer. Keep it under 300 words.
                Always respond in the same language as the user's request.
                """,
                description: "Researches a topic and returns structured findings."
            ),
            logger
        );

        // ── Agent 2: Writer ──────────────────────────────────────────────────
        var writerAgent = new LoggingAgentWrapper(
            new ChatClientAgent(
                chatClient: MakeClient(),
                name: "WriterAgent",
                instructions: """
                You are a professional content writer. You receive a research brief.
                Write an engaging short article (200-250 words) with:
                - A catchy title
                - An engaging introduction
                - 2-3 body paragraphs
                - A clear conclusion
                - Adds a [WRITER AGENT ✓] tag at the top
                Make it accessible to a general audience.
                Always respond in the same language as the user's original request.
                """,
                description: "Writes engaging content based on research findings."
            ),
            logger
        );

        // ── Agent 3: Reviewer ────────────────────────────────────────────────
        var reviewerAgent = new LoggingAgentWrapper(
            new ChatClientAgent(
                chatClient: MakeClient(),
                name: "ReviewerAgent",
                instructions: """
                You are an expert editor. You receive a draft article.
                Review it and return a polished final version that:
                - Fixes any grammar or clarity issues
                - Strengthens the opening hook
                - Ensures a strong call-to-action in the conclusion
                - Adds a [REVIEWED ✓] tag at the top
                Return the full improved article.
                Always respond in the same language as the article you received.
                """,
                description: "Reviews the article and provides an improved final version."
            ),
            logger
        );

        // ── Build Sequential Workflow ─────────────────────────────────────────
        // ResearchAgent → WriterAgent → ReviewerAgent
        // Each agent's output is automatically forwarded as input to the next
        var workflow = AgentWorkflowBuilder.BuildSequential(
            [researchAgent, writerAgent, reviewerAgent]
        );

        // ── Expose the entire workflow as a single AIAgent ────────────────────
        // This is the "Workflow as Agent" pattern from Microsoft Agent Framework
        // https://learn.microsoft.com/en-us/agent-framework/workflows/as-agents
        return workflow.AsAgent(
            id: "content-pipeline",
            name: "ContentPipelineAgent",
            description: "A multi-agent workflow that researches, writes, and reviews content"
        );
    }
}

/// <summary>
/// Wraps an AIAgent to log when it starts and finishes processing,
/// so you can see which agent in the pipeline is currently running.
/// </summary>
public sealed class LoggingAgentWrapper(AIAgent innerAgent, ILogger logger) : DelegatingAIAgent(innerAgent)
{
    private static readonly string[] AgentEmojis = ["🔍", "✍️", "✅"];
    private static int _agentCounter = 0;
    private readonly string _emoji = AgentEmojis[(Interlocked.Increment(ref _agentCounter) - 1) % AgentEmojis.Length];

    public override async IAsyncEnumerable<AgentRunResponseUpdate> RunStreamingAsync(
        IEnumerable<ChatMessage> messages,
        AgentThread? thread = null,
        AgentRunOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var agentName = InnerAgent.Name ?? InnerAgent.Id;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        logger.LogInformation("▶️  [{Agent}] Starting...", agentName);

        int tokenCount = 0;
        await foreach (var update in InnerAgent.RunStreamingAsync(messages, thread, options, cancellationToken))
        {
            tokenCount++;
            yield return update;
        }

        stopwatch.Stop();
        logger.LogInformation("✅  [{Agent}] Done in {Elapsed}ms (~{Tokens} chunks streamed)",
            agentName, stopwatch.ElapsedMilliseconds, tokenCount);
    }
}
