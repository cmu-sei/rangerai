# Data Storage

Ranger uses structured data storage to manage logs, configurations, test results, and workflow history. While the current implementation uses [Baserow](https://baserow.io)—an open-source, self-hosted Airtable-style database—the system is flexible and can be adapted to any backend with a RESTful or SQL interface.

## Why Use Structured Storage?

In AI-driven infrastructure, it's not enough to observe behaviors—we need to log, review, and improve them. Data storage supports:

- **Prompt Iteration**: Capturing prompts and completions for analysis and tuning.
- **Workflow Tracking**: Logging function calls, responses, and system decisions over time.
- **Test Results**: Recording test case outcomes for reproducibility and regression checks.
- **Metadata and Configurations**: Storing knowledge index metadata, scenario templates, or environment-specific variables.

## Current Implementation: Baserow

We currently use Baserow as the backing store because it offers:

- **Web UI** for managing data without needing a full-blown admin interface
- **REST API** access for programmatic updates and queries
- **Self-hosted, offline operation**, supporting air-gapped deployments
- **Relational structure**: linked tables, rich field types, and user roles

By default, we define tables for:

- `Prompts`: each message sent to the LLM, with metadata
- `Completions`: associated responses, duration, tokens, etc.
- `FunctionCalls`: structured logs of MCP-triggered actions
- `Tests`: predefined test cases and results
- `Sessions`: session metadata, timestamps, user ID, etc.

## Integrating With Ranger

Ranger’s API includes logic to automatically:

- Write logs to Baserow during LLM interactions
- Record which MCP functions were invoked and with what arguments
- Associate conversation history with test runs

This data can be filtered, exported, or audited later—especially useful in exercises or evaluations.

## Alternative Backends

If Baserow isn’t suitable for your environment, alternatives include:

- **PostgreSQL** or **SQLite** for direct integration into existing DB infrastructure
- **Airtable** (if cloud access is allowed)
- **Custom REST API** wrappers over your own storage schema

Ranger does not assume a fixed schema—just that it can log JSON-structured records via HTTP.

## Summary

Data storage isn’t just an afterthought in Ranger—it’s part of the loop. Logs, prompts, completions, and test results give administrators and developers visibility into how the system behaves and evolves. Whether you use Baserow or another backend, maintaining this data gives you:

- Accountability
- Audit trails
- Insight for improvement
- Historical baselines for performance comparison

Use storage to observe, understand, and refine Ranger's real-world behavior.

