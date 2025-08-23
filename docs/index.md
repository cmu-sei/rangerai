# ![](assets/ranger-face.png) Meet Ranger


???+ info "Note"
    Ranger is under active development, and the documentation is a work in progress. We welcome contributions and feedback to improve it. 😍


Ranger is AI infrastructure for cyber range work. It helps you automate the boring stuff, orchestrate the complicated stuff, and build capabilities that are more realistic, scale more easily, and provide increased value.

Whether you're running a training range, building a scenario, or experimenting with AI-enabled operations, Ranger acts as your range co-pilot: Planning, deploying, and dynamically adapting different areas of the exercise in real time using an open-source (OSS) and modular generative AI stack.

???+ tip "Run Ranger on the Crucible Framework"
    ![Crucible Logo](assets/crucible-icon-c-alpha.svg)
    Ranger runs alongside the SEI's **Crucible Framework**, a modular system for creating and managing virtual environments for training and exercises. Find more on [Crucible's source code](https://github.com/cmu-sei/crucible) and [Crucible Docs](https://cmu-sei.github.io/crucible/).

    Ranger is also designed to drive GHOSTS NPC agents in simulating all manner of user behavior in your range. Learn more about [GHOSTS](https://github.com/cmu-sei/GHOSTS/).

## Why We Built Ranger

Cyber ranges are awesome, but let’s face it, they can be difficult to manage. Scenario setup is tedious. Injecting new events takes a human. Exercises feel like plays stuck in rehearsal: Scripted, brittle, and already a bit outdated the moment they run.

**Ranger hopes to fix that.** It’s an intelligent layer that sits on top of your range tooling and makes things actually dynamic. It looks to:

- Drive live injects based on trainee behavior
- Adapt to how teams are performing
- Automate red/blue/gray actions in real time
- Scale without instructors watching every move

## What Ranger Actually Does

Ranger is more than a chatbot. Here’s how it works in practice:

1. **You describe what you want.** Natural language, no scripting required. "I want ghosts agents to set off a phishing campaign after the team resets the firewall." Cool, noted.
2. **Ranger asks questions.** If your description is unclear to Ranger, it asks follow up questions, checks what’s possible, and proposes a course of action.
3. **It acts on your behalf.** It actually executes commands: Launches NPC activity through GHOSTS, connects services, and injects network events.

All of this works without references to cloud services and can utilize **open-weight models**, so you can use Ranger in secure or air-gapped environments.

## Why Use Ranger?

Here’s what you get when you bring Ranger into the mix:

- **Faster scenario creation** — From hours of YAML to a few lines of conversation
- **Live, adaptive exercises** — Trainees act, Ranger responds
- **Realistic content** — Fake news, social media, email traffic, user behavior, and more
- **No vendor lock-in** — Runs locally, supports open models, speaks API
- **Lowers the barrier to entry** — You shouldn't need to be a range ninja or an LLM whisperer
