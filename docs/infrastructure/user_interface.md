## User Interface

Ranger is most easily accessed through a web-based chat interface currently powered by [Open-WebUI](https://github.com/open-webui/open-webui). This self-hosted, offline-capable chat frontend offers a robust user experience out-of-the-box, providing multi-user support, access controls, and session management—ideal for cyber range environments with multiple users.

### Why Open-WebUI?

We chose Open-WebUI instead of building a custom frontend because it delivers:

* Multi-user accounts and role-based access
* Persistent chat sessions and history
* Response feedback and evaluation tracking
* Admin dashboard with conversation and user management
* Containerized deployment

All these features make it especially useful in exercise environments where several users interact with Ranger concurrently.

### Integration with Ranger

Open-WebUI is typically used to communicate directly with an LLM backend (e.g., Ollama). In our design, however, we route Open-WebUI’s traffic to the Ranger API instead. To enable this, Ranger mimics the OpenAI chat completions API, including:

* JSON request/response formats
* Streaming partial responses (for real-time feedback)
* Chat memory and multi-turn dialogue support

This allows Ranger to intercept messages, decide whether to answer using the LLM or to perform an action, and stream responses back to the frontend—all transparently to the user.

For example, when a user says:

> "Have Sally browse her financial sites to find out more about her investments"

Open-WebUI sends that prompt to Ranger’s API. Ranger processes it, determines tool usage (like telling GHOSTS NPCs to perform some activity), and streams back both status updates and final results—all while the user remains in a familiar chat UI.

### Admin Features

Open-WebUI includes a built-in admin panel:

* Manage user accounts
* View and export conversations
* Adjust model and system prompt settings
* Upload documents and templates (optional)

It also supports extensions like Pipelines—allowing Python scripts to be triggered on chat events that are outside the scope of Ranger for now, but which are possible.

### Flexibility and Future Compatibility

Ranger’s adherence to the OpenAI-compatible API schema ensures that it can work with other chat frontends as well. If Open-WebUI no longer suits your needs, swapping to another interface (like Chatbot UI or even a CLI wrapper) would require minimal changes.

For now, Open-WebUI strikes the right balance of usability, control, and extensibility—making it an ideal interface for Ranger deployments in cyber range environments.
