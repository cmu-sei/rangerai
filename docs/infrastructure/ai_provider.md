# Ranger's AI Provider (or model engine)

Ranger’s ability to understand natural language and respond intelligently is powered by it's **AI Provider** — it's LLM backend.

By default, Ranger uses [Ollama](https://ollama.com) for this role — an open-source model server that can run large language models (LLMs) locally. But the AI Provider is not tied to Ollama: it’s designed to support **any compatible LLM backend**, including OpenAI, Claude, open-weight models via vLLM, or even internal fine-tuned APIs.

## Why Use Ollama?

Ollama was chosen as the default for several reasons:

- **Local-first:** It runs entirely on-prem without internet.
- **Supports many models:** LLaMA, Mistral, Phi, and others.
- **Hardware optimized:** Runs efficiently on CPUs, Apple Silicon, or GPUs.
- **Simple interface:** Exposes a REST API (default: port `11434`) that Ranger can talk to.
- **Air-gapped friendly:** No reliance on cloud APIs or license tokens.

It’s fast, flexible, and easy to switch models or versions depending on your hardware or needs.

???+ info "Note"
    We are always looking to improve Ranger's AI capabilities. If you have a preferred LLM backend or model that you think would enhance Ranger, please open an issue.

## How the AI Provider Works

When a user sends a message — whether via chat UI, n8n, or CLI — the flow looks like this:

1. **Prompt comes in** from Open-WebUI (or another frontend).
2. **Ranger intercepts it** via its API, instead of letting the frontend talk to Ollama directly.
3. Ranger evaluates the prompt:
   - If it’s a **language-only** query (e.g. “Can you generate a python 101 module for data science?”), it forwards it to Ollama and returns the answer.
   - If it’s an **actionable** command (e.g. “Start 3 NPC agents”), Ranger interprets and executes it using the appropriate GHOSTS workflow.
4. Ranger returns the final result to the user.

This **proxy architecture** ensures all requests pass through Ranger, allowing it to mediate, enrich, or act on them intelligently.

---

## Swapping Out the Brain

Ranger isn’t married to Ollama. It can talk to:

- Any **OpenAI-compatible API** (e.g. OpenAI, Azure, Groq)
- Claude (via a proxy or plugin)
- Local APIs you host yourself
- Ollama, with any model you install

To switch models in Ollama:

```bash
ollama list
ollama pull mistral:latest
ollama run mistral
```

Or use Open-WebUI’s model selector if running interactively.

You can even point Ranger at different model endpoints using environment variables:

```bash
LLM_API_BASE=http://ollama:11434
LLM_MODEL=llama2
```

## Isolation by Design

A key design decision: LLM execution happens outside Ranger.

Ranger never loads models into its own process. Instead, it treats the LLM like an external compute resource — asking questions, getting completions, and deciding what to do next.

This gives you:

   - Fault isolation (LLM crashes don’t affect Ranger logic)
   - Better performance (no model bloat inside the API)
   - Deployment flexibility (you can swap the model backend at any time)

## TL;DR

Ranger’s AI Provider is the pluggable AI layer that handles language understanding. By default it uses Ollama, but it’s backend-agnostic. This keeps Ranger flexible, lightweight, and adaptable to your environment — whether you’re using open models, fine-tunes, or proprietary APIs.

---

Want to plug in your own brain? Just point Ranger at it.