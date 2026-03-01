import { CopilotKit } from "@copilotkit/react-core";
import "@copilotkit/react-ui/styles.css";

export default function WorkflowLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    // Override the root layout's CopilotKit to use the content_pipeline agent
    <CopilotKit runtimeUrl="/api/copilotkit" agent="content_pipeline">
      {children}
    </CopilotKit>
  );
}
