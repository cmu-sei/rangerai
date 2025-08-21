import requests
import logging
from config import API_BASE_URL
from infrastructure.format_npc import json_to_csv
#from infrastructure.filters import filter_csv_columns

logger = logging.getLogger(__name__)

class NpcService:
    BASE_URL = API_BASE_URL + '/npcs'

    @staticmethod
    def get(query: str) -> str:
        logger.debug(f"NPC MCP query: {query} on {NpcService.BASE_URL}")

        response = requests.get(
            NpcService.BASE_URL,
            headers={'accept': 'application/json'}
        )
        
        o = json_to_csv(response.json())
        logger.debug(f"npcs: {o}")
        return o
    
    @staticmethod
    def command(npc_action: str, npc_name: str, reasoning: str) -> None:
        logger.debug(f"NPC MCP command: {npc_action} {npc_name} on {NpcService.BASE_URL}/command")

        _ = requests.post(
            NpcService.BASE_URL + "/command",
            headers={
                'accept': 'application/json',
                'Content-Type': 'application/json'
            },
            json={
                "handler": npc_action,
                "action": npc_action,
                "scale": 1,
                "who": npc_name,
                "reasoning": reasoning,
                "sentiment": "",
                "original": ""
            }
        )
        logger.debug(f"NPC command sent: {npc_action} {npc_name}")