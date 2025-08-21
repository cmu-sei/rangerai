from fastmcp import FastMCP
import logging

from tools.machines import MachineService as machine_service
from tools.npcs import NpcService as npc_service
from tools.status import StatusService as status_service

logging.basicConfig(
    level=logging.DEBUG,
    format='%(asctime)s [%(levelname)s] %(message)s',
    handlers=[
        logging.FileHandler("logs/mcp_debug.log"),
        logging.StreamHandler()
    ]
)

logger = logging.getLogger(__name__)
mcp = FastMCP("GHOSTS API", description="GHOSTS AI MCP Server", version="0.1.0")

@mcp.tool()
async def list_machines(query: str) -> str:
    """Gets a list of all machines controlled by ghosts"""
    return machine_service.get(query)

@mcp.tool()
async def add_machines(query:str, machine_name: str, fqdn: str, domain: str, host: str, resolved_host: str, host_ip: str, ip_address: str, current_user_name: str) -> str:
    """Creates a new machine on the ghosts system"""

    logger.debug(f"Creating machine with data: {query}")
    
    if domain == None or domain == "":
        domain = ""
    if host == None or host == "":
        host = ""
    if resolved_host ==  None or resolved_host == "":
        resolved_host = ""
    if host_ip == None or host_ip == "":
        host_ip = ""
    if ip_address == None or ip_address == "":
        ip_address = ""
    if current_user_name == None or current_user_name == "":
        current_user_name = ""

    d = {
        "name": f"{machine_name}",
        "fqdn": f"{fqdn}",
        "domain": f"{domain}",
        "host": f"{host}",
        "resolvedHost": f"{resolved_host}",
        "hostIp": f"{host_ip}",
        "ipAddress": f"{ip_address}",
        "currentUsername": f"{current_user_name}",
    }

    logger.debug(f"Creating machine with data: {d}")

    try:
        return machine_service.create(d)
    except Exception as e:
        logger.error(f"Error creating machine: {e}")
        return f"Error creating machine: {e}"



@mcp.tool()
async def list_npcs(query: str) -> str:
    """Gets a list of all npcs controlled by ghosts"""
    return npc_service.get(query)

@mcp.tool()
async def command_npcs(npc_action: str, npc_name: str, reasoning: str) -> str:
    """Tells an npc to perform an action and why"""
    return npc_service.command(npc_action, npc_name, reasoning)

# @mcp.tool(description="Gets the status of the ghosts system, use this tool last when none of the other tools fit your needs")
# async def list_status(query: str) -> str:
#     """Gets the status of the ghosts system, use this tool last when none of the other tools fit your needs"""
#     return status_service.get(query)


if __name__ == "__main__":
    mcp.run(transport="sse", host="0.0.0.0", port=5000)
