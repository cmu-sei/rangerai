# Model Context Protocol (MCP)

Ranger uses the Model Context Protocol (MCP) to bridge the gap between natural language processing and real-world tool execution. MCP enables Ranger to safely and flexibly invoke external APIs, tools, and services by standardizing how actions are defined, described, and executed from within an AI-driven system.

## What Is MCP?

MCP is a protocol for connecting language models to tools. Originally introduced by Anthropic in 2024, MCP defines a standardized way for an LLM to understand what functions are available, what arguments they require, and how to invoke them safely.

In Ranger, MCP acts as the intermediary between the AI and the cyber range’s various services (e.g., GHOSTS, range deployment tools, n8n workflows, etc.).

## Why MCP?

- **Modularity**: You can integrate any tool or service by wrapping it in an MCP-compliant server.
- **Interoperability**: The LLM doesn’t need to know anything about the service’s internal API – it only sees a generic function description.
- **Safety**: Tools expose only explicitly defined functions, with input validation, descriptions, and roles.
- **Future-proofing**: As services evolve, their MCP wrapper can be updated without modifying Ranger’s core logic or LLM prompts.

## How It Works

1. **Tool Description**: Each tool is exposed through an MCP server that describes available functions, including:
   - Name
   - Description
   - Parameters (with types and descriptions)
   - Optional role-based access

2. **LLM Planning**: Ranger’s LLM receives this tool metadata as part of the system prompt or RAG. When a user gives an instruction, the LLM can select a function and generate a structured function call.

3. **Execution Flow**:
   - Ranger parses the LLM’s response to check for a function call.
   - The request is forwarded to the appropriate MCP server.
   - The MCP server executes the function and returns the result.
   - Ranger relays the result back to the user via chat or logs.

## Example

User says:
> “Set up 10 NPCs browsing news sites on subnet 10.0.0.0/24”

Ranger:
- Uses MCP metadata to identify a `deploy_npcs` function from the Ghosts MCP server.
- Sends a JSON call like:
  ```json
  {
    "function": "deploy_npcs",
    "parameters": {
      "count": 10,
      "activity": "news_browsing",
      "subnet": "10.0.0.0/24"
    }
  }
  ```
- Receives confirmation or result, and informs the user.

## Adding a New MCP Service

1. Define your service’s actions in a JSON schema or code.
2. Wrap the service in an MCP server (we provide templates).
3. Deploy the server and register its URL with Ranger.
4. Optionally, update the Ranger system prompt with a summary of the new tool’s capabilities.

## Security Considerations

- Each MCP function can include access controls (e.g., admin-only).
- Ranger should verify the user’s role or session before invoking sensitive functions.
- Always validate inputs on the MCP server side.

## Deployment Notes

- MCP servers are typically small FastAPI or Flask apps (we use <strong>[FastMCP](https://gofastmcp.com/getting-started/welcome)</strong>).
- They can run as containers and communicate over internal networks.
- Ranger maintains a registry of available MCP services and queries them as needed.

MCP enables Ranger to function like an intelligent operator—reading user intent, deciding which tools to use, and taking action—without hardcoding tool-specific logic into the LLM or application core.