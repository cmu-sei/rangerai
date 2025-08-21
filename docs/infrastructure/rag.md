## RAG and Vector Databases for Knowledge Retrieval

To extend Ranger’s intelligence with up-to-date, exercise-specific knowledge, we implement Retrieval-Augmented Generation (RAG) using a vector database—specifically, [Qdrant](https://github.com/qdrant/qdrant). This allows Ranger to overcome the context limitations of large language models by injecting precise, relevant information into the LLM’s prompt at runtime.

### Why Qdrant?

Qdrant is an open-source vector search engine optimized for high-performance similarity search on embedding vectors. It’s containerized, easy to deploy in air-gapped environments, and supports both REST and gRPC interfaces. We selected it for:

* Purpose-built similarity search
* Low latency, high throughput
* Simple API surface
* Full control over data (self-hosted, no external dependencies)

By default, we expose Qdrant on port `6333`. The Ranger API communicates with Qdrant to perform search queries and maintain the vector index.

### What Goes into Qdrant?

We store environment-specific data as embeddings:

* Network diagrams and topologies
* Standard Operating Procedures (SOPs)
* Scenario configurations
* Exercise briefs and reports

These documents are preprocessed into chunks, embedded into vector space (using the same embedding model used at runtime), and stored in Qdrant collections.

### How Retrieval Works

When a user asks something like:

> “Has Team Alpha achieved objective X in the last run?”
> “Show me the network topology for this exercise.”

Ranger embeds the user’s question, queries Qdrant for similar embeddings, and retrieves the most relevant document snippets. These are injected into the LLM prompt to inform the response.

This retrieval pipeline typically looks like:

1. **Embed** user question
2. **Query** Qdrant for top-N similar vectors
3. **Retrieve** text chunks from indexed documents
4. **Inject** retrieved text into system prompt context
5. **Generate** final response from the LLM

### Updating the Index

We keep Qdrant’s index updated with current exercise materials before each run. For example:

* Loading scenario briefs and attacker profiles
* Ingesting updated range topology and configurations

External integrators can automate this process or extend it to new data sources. For example, if your environment includes a proprietary CMDB or scoring engine, you could feed relevant documents or data extracts into Qdrant.

### Design Trade-Offs

Adding RAG adds complexity—data must be maintained, embedded, and indexed. But the benefit is substantial: Ranger becomes context-aware, responsive to dynamic scenarios, and capable of answering detailed operational queries using your environment’s ground truth.

In short, Qdrant gives Ranger memory.
