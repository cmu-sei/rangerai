import requests
import logging
from config import API_BASE_URL
from infrastructure.format_npc import json_to_csv

logger = logging.getLogger(__name__)

class StatusService:
    BASE_URL = API_BASE_URL.replace("api", "test")

    @staticmethod
    def get(query: str) -> str:
        logger.debug(f"Test query: {query} on {StatusService.BASE_URL}")

        response = requests.get(
            StatusService.BASE_URL,
            headers={'accept': 'application/json'}
        )
        
        return response.json()
