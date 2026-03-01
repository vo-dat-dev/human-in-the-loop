import { HttpAgent } from "@ag-ui/client";
import {
  CopilotRuntime,
  ExperimentalEmptyAdapter,
  copilotRuntimeNextJSAppRouterEndpoint,
} from "@copilotkit/runtime";
import { NextRequest } from "next/server";

// 1. You can use any service adapter here for multi-agent support. We use
//    the empty adapter since we're only using one agent.
const serviceAdapter = new ExperimentalEmptyAdapter();

// 2. Create the CopilotRuntime instance and utilize the Microsoft Agent Framework
// AG-UI integration to setup the connection.
const agentUrl = process.env.AGENT_URL ?? "http://localhost:8000";
const runtime = new CopilotRuntime({
  agents: {
    // Proverbs agent - human-in-the-loop demo
    my_agent: new HttpAgent({ url: `${agentUrl}/` }),
    // Content pipeline workflow: ResearchAgent → WriterAgent → ReviewerAgent
    // Demonstrates the "Workflow as Agent" pattern
    content_pipeline: new HttpAgent({ url: `${agentUrl}/workflow` }),
  },
});

// 3. Build a Next.js API route that handles the CopilotKit runtime requests.
export const POST = async (req: NextRequest) => {
  const { handleRequest } = copilotRuntimeNextJSAppRouterEndpoint({
    runtime,
    serviceAdapter,
    endpoint: "/api/copilotkit",
  });

  return handleRequest(req);
};
