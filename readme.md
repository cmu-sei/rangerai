# Ranger

Ranger is AI infrastructure that helps with automation and orchestration for cyber range training, experimentation, and exercise. It is typically used in conjunction with the [Crucible Framework](https://cmu-sei.github.io/crucible/).

![Ranger Logo](docs/assets/ranger-face-sm.png)

Ranger is available in two forms:

- A docker stack easily deployed via docker-compose.
- A VM appliance deployed to a hypervisor such as Proxmox.

## Overview and Motivation

In a cyber range (a simulated environment for cybersecurity training or exercises), many tasks – from setting up scenarios to driving simulated network traffic – are traditionally manual, static, or time-consuming. Ranger addresses these challenges by acting as an intelligent orchestrator within the range, using generative AI to plan, deploy, and adapt cyber exercise tasking in real-time.

### Problems in Cyber Range Operations

Traditional cyber exercises often suffer from a few key shortcomings. Exercise development is slow and monotonous, requiring significant scripting and automation expertise. Scenarios tend to be static – they simulate yesterday’s threats and do not adapt to trainees’ actions in real-time. This means training outcomes are limited, and scenarios must be manually administered by instructors to introduce changes. The result is bespoke exercises that are expensive to create and hard to reuse. Ranger is built to solve these issues by introducing an intelligent, dynamic layer of control across a wide array of use-cases.

### How Ranger Works

At its core, Ranger is not just a chatbot, but a modular suite of AI tools that operate within constrained environments. It combines the power of Large Language Models (LLMs) with a suite of workflows and tools that enable generative AI to act on your behalf. For example, an exercise developer can simply describe in natural language the kind of user activities, or threats they want in an exercise. Ranger will gather the proper details, propose a detailed exercise plan, and on approval, call the appropriate range APIs to bring that request to life (deploying virtual machines, launching GHOSTS NPC agents, injecting network traffic, etc.). Once the exercise is running, Ranger can continue to act as a “virtual director” – monitoring the range condition and participant actions, and dynamically triggering scenario events or adaptations (for instance, escalating the difficulty if the trainees neutralize a threat too quickly). All of this is done within closed networks and with open-source models, meaning it can operate in sensitive or air-gapped environments that lack Internet connectivity. 

## Quick Start

Head over to the [Quick Start Guide](https://cmu-sei.github.io/rangerai/) in the Ranger docs to get started.

## License

Copyright 2022 Carnegie Mellon University. See the LICENSE.md files for details.
