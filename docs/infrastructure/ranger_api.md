# Ranger Core API

The **Ranger Core API** is the main service endpoint that handles incoming requests, coordinates system components, and integrates with a language model backend. It does **not** contain the AI model itself — rather, it serves as the central controller that routes messages, invokes tools, and returns results. It sits between client applications and the LLM, acting as a mediator. It can simply relay messages to the LLM for language understanding, or it can execute complex workflows by calling external tools and APIs while incorporating LLM output anywhere within the workflow.

---

## Role in the System

When a user interacts with Ranger (for example, via a chat interface like Open-WebUI), the message is sent to the **Ranger Core API**. This API determines whether the message is informational (just needs a model response) or operational (requires triggering a tool or action). For language understanding and response generation, the Core API uses an LLM service such as Ollama, OpenAI, or Claude.

If tool use is needed, the API executes a workflow, which might incorporate other functionalities such as RAG, **Model Context Protocol (MCP)** or otherwise to identify capabilities and call the right functions.

---

## Model Context Protocol (MCP)

**MCP** is an open standard introduced by Anthropic in late 2024 to simplify how LLM-enabled applications interact with external tools and APIs. Instead of hardwiring logic for every service, MCP provides a generic interface where:

- The Core API queries connected MCP servers to discover available functions
- The LLM receives these function definitions in its prompt
- The LLM can return a function call with arguments
- The Core API executes that call through the MCP server
- The result is returned to the user, optionally with additional reasoning

This flow — **Plan → Function Call → Execute → Respond** — is the core loop of Ranger's intelligent behavior.

---

## Adding New Tools

Each external system (e.g., GHOSTS, n8n, PCTE APIs) can be wrapped in its own **MCP server**. These servers expose available functions in a machine-readable format. The Ranger Core API doesn’t need to change to support new tools — it simply reads what each MCP server exposes.

This modularity makes the system extensible and future-proof. If you need Ranger to support a new service, you just write an MCP wrapper for it.

---

## Summary

- The **Ranger Core API** is the central orchestrator, not the AI itself
- It routes messages between chat front-ends, LLMs, and tool integrations
- It uses **MCP** to dynamically discover and call tools
- It enables intelligent, language-based control of complex range operations

