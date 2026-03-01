using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OpenAI;

/// <summary>
/// Demo: Workflow as Agent pattern
/// Pipeline: ResearchAgent → WriterAgent → ReviewerAgent
/// Each agent's output becomes the next one's input.
/// The whole pipeline is exposed as a single AIAgent via .AsAgent()
/// </summary>
public static class ContentWorkflowAgentFactory
{
    public static AIAgent Create(IConfiguration configuration)
    {
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
        var researchAgent = new ChatClientAgent(
            chatClient: MakeClient(),
            name: "ResearchAgent",
            instructions: """
            You are a research assistant. When given a topic, produce a concise research brief with:
            - 3 key facts
            - 2 current trends
            - 1 surprising insight
            Format your output as a clear research brief for a writer. Keep it under 300 words.
            """,
            description: "Researches a topic and returns structured findings."
        );

        // ── Agent 2: Writer ──────────────────────────────────────────────────
        var writerAgent = new ChatClientAgent(
            chatClient: MakeClient(),
            name: "WriterAgent",
            instructions: """
            You are a professional content writer. You receive a research brief.
            Write an engaging short article (200-250 words) with:
            - A catchy title
            - An engaging introduction
            - 2-3 body paragraphs
            - A clear conclusion
            Make it accessible to a general audience.
            """,
            description: "Writes engaging content based on research findings."
        );

        // ── Agent 3: Reviewer ────────────────────────────────────────────────
        var reviewerAgent = new ChatClientAgent(
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
            """,
            description: "Reviews the article and provides an improved final version."
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
