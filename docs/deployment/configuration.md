# Configuring and Operating Ranger

Once deployment is successful, the next step is understanding how to configure Ranger for your specific exercises and operate it during live runs. This section covers:

* Connecting additional range APIs via MCP
* Orchestrating GHOSTS through Ranger
* Tuning prompts and logging
* Using interfaces (chat, CLI, workflows)

## Connecting Range APIs via MCP

Ranger uses the Model Context Protocol (MCP) to integrate with external services. Each service (e.g., VM orchestration, scoring) is wrapped in an MCP server.

**Steps to add an MCP integration:**

1. **Deploy or Enable the MCP Server:**

   * Use an existing implementation or generate one with tools like FastMCP.
   * Base it on the service’s OpenAPI (Swagger) spec.

2. **Register the MCP Server:**

   * Set environment variables (e.g., `MCP_GHOSTS_ADDR=ws://ghosts-mcp:8000`).
   * Repeat for each service (e.g., `MCP_SERVICEX_ADDR=http://servicex-mcp:port`).

3. **Update Tool Context:**

   * Extend Ranger’s system prompt to describe new tools.
   * Include function names and descriptions.

4. **Test the Integration:**

   * Use a natural prompt (e.g., "create a new VM") and check if Ranger calls the MCP function.
   * If it fails, refine tool descriptions or debug connectivity.

> **Design Tip:** Use intuitive aliases for functions. Check open-source MCP definitions for inspiration.

## GHOSTS Orchestration

Ranger simplifies GHOSTS control. Key workflows include:

* **Initial Setup:** Ensure NPC agents connect to the GHOSTS server. Pre-load timeline/scripts.
* **Start NPCs:** Example prompt: "Start 10 NPCs in finance segment doing office work."
* **Monitor NPCs:** Ask: "How many NPCs are active and what are they doing?"
* **Adjust in Real-Time:** Prompts like "Increase browsing NPCs to 20 over the next hour" are translated to API calls.

The GHOSTS MCP server supports SSE/WebSocket for live command streaming and async updates.

## Prompt Management and Logging

### System Prompts

* Define Ranger’s role, available tools, and policy rules.
* Keep it concise but authoritative.
* Optionally include few-shot examples for behavior.

### User Guidance

* Provide a cheat sheet for phrasing requests.
* Encourage specificity in prompts.

### Logging

* Log all interactions to a database (e.g., Baserow).
* Review logs for:

  * Misunderstandings
  * Hallucinations
  * Performance
  * User feedback (e.g., from Open-WebUI ratings)

### Prompt Adjustments

* Refine system prompt for recurring issues.
* Add retry/correction logic in the core.
* Consider fine-tuning or model switching.
* Update MCP tool schemas for clarity.

## Interfaces

### Web Chat (Open-WebUI)

* Primary interface for most users.
* Supports multi-chat, permissions, and logs for AAR.

### Command-Line (CLI)

* Use curl or scripts to call Ranger's OpenAI-compatible API:

```bash
curl http://localhost:5067/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{"model": "Ranger", "messages": [{"role": "user", "content": "Summarize current range status."}]}'
```

### n8n Workflows

* Trigger Ranger via HTTP webhooks or schedules.
* Automate summaries, monitoring, or downstream actions.

### Direct API Integration

* Use Ranger as a backend in custom apps.
* Follows OpenAI API structure, so most SDKs work with just a base URL change.

---

This configuration and operations layer turns Ranger from a chatbot into a range-integrated orchestrator. Whether you're working via chat, script, or dashboard, the same AI logic applies—making it flexible and operational in any training environment.
