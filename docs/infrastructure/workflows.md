# Workflows

Ranger integrates with workflow automation tools to perform multi-step operations, trigger external systems, and manage side effects beyond its core logic. Our current integration uses n8n, an open-source workflow engine designed for extensibility, visual editing, and self-hosted operation.

## Why n8n?

We selected n8n because it offers:

- Drag-and-drop visual workflow editor
- Hundreds of built-in integrations (HTTP, databases, messaging, cloud services)
- Secure, local deployment
- Trigger-based automation with fine-grained control

This makes it well-suited for complex cyber range workflows, such as sending alerts, recording outcomes, or orchestrating actions across multiple tools.

## How It Works

Ranger communicates with n8n in two primary ways:

- Direct HTTP Call: Ranger's API can call a webhook defined in n8n, passing parameters (e.g., { "user": "Alpha", "event": "trigger_ransomware" }).
- Chat-Triggered Execution: Through Open-WebUI’s Pipelines system, chat events (like a specific user prompt) can trigger predefined n8n workflows without modifying Ranger’s core logic.

## Use Cases

- Log exercise milestones (e.g., “Team Bravo achieved Objective X”)
- Send alerts when certain events occur
- Schedule post-exercise cleanup
- Log tool usage to external dashboards
- Dynamically configure virtual environments based on scenario state

## Security and Deployment

We deploy n8n in a separate container, typically on port 5678, and secure it behind internal access controls. Workflows triggered by Ranger require no public endpoint and are fully self-contained. Authentication and access restrictions are configurable per use case.

## Extending the System

While we use n8n today, Ranger’s architecture is not tied to it. Any HTTP-based automation system can be integrated similarly—Node-RED, Zapier (offline-compatible clone), or even custom Flask apps. Ranger simply needs a known endpoint and a JSON schema.