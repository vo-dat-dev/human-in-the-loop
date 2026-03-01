"use client";

import { CopilotKitCSSProperties, CopilotSidebar } from "@copilotkit/react-ui";
import Link from "next/link";
import { useState } from "react";

export default function WorkflowPage() {
  const [themeColor] = useState("#0ea5e9");

  return (
    <main
      style={
        { "--copilot-kit-primary-color": themeColor } as CopilotKitCSSProperties
      }
    >
      <CopilotSidebar
        disableSystemMessage={true}
        clickOutsideToClose={false}
        defaultOpen={true}
        labels={{
          title: "Content Pipeline",
          initial:
            "👋 Hi! I'm a multi-agent content pipeline.\n\nGive me any topic and I'll:\n1. 🔍 **Research** it (ResearchAgent)\n2. ✍️ **Write** an article (WriterAgent)\n3. ✅ **Review & polish** it (ReviewerAgent)\n\nTry: *\"Write an article about quantum computing\"*",
        }}
        suggestions={[
          {
            title: "Tech Article",
            message: "Write an article about the future of quantum computing.",
          },
          {
            title: "Science Article",
            message:
              "Write an article about recent breakthroughs in fusion energy.",
          },
          {
            title: "Business Article",
            message:
              "Write an article about how AI is transforming software development.",
          },
          {
            title: "Health Article",
            message:
              "Write an article about the benefits of intermittent fasting.",
          },
        ]}
      >
        <div
          style={{ backgroundColor: themeColor }}
          className="h-screen flex flex-col justify-center items-center gap-6 transition-colors duration-300 p-8"
        >
          {/* Header */}
          <div className="text-center text-white">
            <h1 className="text-4xl font-bold mb-2">Content Pipeline</h1>
            <p className="text-lg opacity-80">Workflow as Agent Demo</p>
          </div>

          {/* Pipeline visualization */}
          <div className="flex items-center gap-3 bg-white/20 backdrop-blur rounded-2xl p-6">
            {[
              {
                icon: "🔍",
                name: "ResearchAgent",
                desc: "Gathers key facts & trends",
              },
              { icon: "✍️", name: "WriterAgent", desc: "Drafts the article" },
              { icon: "✅", name: "ReviewerAgent", desc: "Reviews & polishes" },
            ].map((agent, i) => (
              <div key={agent.name} className="flex items-center gap-3">
                <div className="flex flex-col items-center bg-white/30 rounded-xl p-4 w-40 text-white text-center">
                  <span className="text-3xl mb-1">{agent.icon}</span>
                  <span className="font-semibold text-sm">{agent.name}</span>
                  <span className="text-xs opacity-75 mt-1">{agent.desc}</span>
                </div>
                {i < 2 && (
                  <span className="text-white text-2xl font-bold opacity-60">
                    →
                  </span>
                )}
              </div>
            ))}
          </div>

          {/* Info box */}
          <div className="bg-white/20 backdrop-blur rounded-xl p-4 max-w-md text-white text-sm text-center">
            <p className="font-semibold mb-1">🏗️ Workflow as Agent Pattern</p>
            <p className="opacity-80">
              Three specialized agents are chained into a single workflow using{" "}
              <code className="bg-white/20 px-1 rounded">
                AgentWorkflowBuilder.BuildSequential()
              </code>{" "}
              and exposed as one agent via{" "}
              <code className="bg-white/20 px-1 rounded">.AsAgent()</code>
            </p>
          </div>

          {/* Navigation */}
          <Link
            href="/"
            className="text-white/70 hover:text-white text-sm underline transition-colors"
          >
            ← Back to Proverbs Agent (Human-in-the-Loop)
          </Link>
        </div>
      </CopilotSidebar>
    </main>
  );
}
