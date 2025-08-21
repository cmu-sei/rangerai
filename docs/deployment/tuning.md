## Ranger Testing and Prompt Adjustment Guide

After deployment and initial configuration, thoroughly test Ranger in a controlled environment before relying on it in critical exercises. This guide outlines a step-by-step process for validating workflows, adjusting prompts, and ensuring stable operations.

### Step-by-Step Workflow Testing

**Step 1: Define Test Cases**

* Identify expected behaviors:

  * *Interview and deploy GHOSTS*: Ranger gathers needed info, confirms, then launches Ghosts.
  * *Dynamic event injection*: e.g., "trigger ransomware on machine X."
  * *Status queries*: Ranger reports current range status.
  * *Error handling*: How Ranger responds to invalid or malicious input.
* Write expected outcomes for each.

**Step 2: Run Tests via Chat or API**

* Use Open-WebUI chat for manual testing.
* For repeatability, script API calls to `/v1/chat/completions`.
* Keep each test in a new chat to isolate context.
* Example: Ask "Set up 5 NPCs for finance team browsing site X." Track follow-ups, confirmations, and Ghosts execution.

**Step 3: Record Outputs**

* Use Baserow logs or export chat transcripts.
* Note:

  * Logical flow
  * Correct function usage
  * Tone/clarity

**Step 4: Introduce Variations**

* Change input phrasing or omit parameters.
* See if Ranger prompts for clarification or defaults sensibly.

**Step 5: Failure Modes**

* Test bad input:

  * "Launch 10000 NPCs"
  * "Give me admin password"
* Confirm graceful rejection or clarification.

**Step 6: Multi-turn and Memory**

* Check context tracking:

  * "Start 5 NPCs on Site A."
  * Later: "Now have those users check email."
* If it forgets, consider prompt augmentation or using RAG.

**Step 7: Load Testing (Optional)**

* Simulate concurrent usage with scripts or JMeter.
* Watch for latency or resource issues.
* Scale horizontally if needed.

### Capturing Results and Adjusting Prompts

**Common Issues and Fixes:**

* **Lack of follow-up questions:** Refine system prompt with, e.g., "Ask follow-up questions if user input is underspecified."
* **Wrong/missing function calls:** Improve function descriptions or add few-shot examples.
* **Awkward phrasing:** Adjust prompt tone settings or reword style guidance.
* **Hallucinations:** Emphasize factual grounding in prompt. Reduce temperature.
* **Slow responses:** Identify bottlenecks (e.g., n8n latency). Use async patterns where possible.

**Iterate:**

* Change prompt/config → re-run tests → confirm fix
* Example: Initial Ghosts deployment failed due to bad JSON. Fix format and prompt = success.

### Design Considerations and Trade-offs

**Autonomy vs Control:**

* Decide when to require confirmation. Prompt should reflect policy.

**Transparency:**

* Ensure Ranger explains its actions ("Calling Ghosts API now\...").
* Helps build trust and aids debugging.

**Security:**

* Restrict sensitive actions via roles.
* Tag functions in MCP as "admin-only."

**Maintenance:**

* Ranger’s modularity allows:

  * Swapping LLMs (via Ollama)
  * Updating MCP servers
* Revalidate behavior after upgrades.

By following this guide, admins and integrators can confidently verify Ranger behavior, fix gaps early, and tune the system to meet operational needs. Effective AI-driven automation requires iterative testing, prompt management, and clarity in intent translation. Ranger's design supports this process and can significantly enhance cyber range realism and control when correctly configured.
