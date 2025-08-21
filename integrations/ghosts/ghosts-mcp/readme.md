# ghosts-mcp

This is an MCP server that exposes all of the tools available for the corresponding API. It is a proof of concept for AI-assisted range operations, specifically for the GHOSTS range. The server is designed to be used with RangerAI, which is an AI agent that can assist with various tasks related to range operations.

## Running this server

The mcp-server file runs on stdio, and is proxied to http.

So,

1. Start the server with `python proxy.py`. You do not have to run the mcp-server file directly.

2. The server will be available at [http://127.0.0.1:8000/](http://127.0.0.1:8000/) and the key url is [http://127.0.0.1:8000/sse](http://127.0.0.1:8000/sse).


## Build

docker build -t dustinupdyke/ghosts-mcp .
docker run -d -p 8000:8000 --name ghosts-mcp dustinupdyke/ghosts-mcp