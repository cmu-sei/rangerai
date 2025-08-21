import requests
import logging
from infrastructure.formatters import json_to_csv_string
from config import API_BASE_URL

logger = logging.getLogger(__name__)

class MachineService:
    BASE_URL = API_BASE_URL + '/machines'

    @staticmethod
    def get(query: str) -> str:
        logger.debug(f"machines MCP query: {query} on {MachineService.BASE_URL}")
        response = requests.get(
            MachineService.BASE_URL,
            headers={'accept': 'application/json'}
        )
        o = response.json()

        if not o:
            return "No machine data available."

        o = json_to_csv_string(response.json())
        logger.debug(f"Machines: {o}")
        return o
        
    
    @staticmethod
    def create(machine_data: dict) -> dict:
        logger.debug(f"Creating machine with data: {machine_data} on {MachineService.BASE_URL}")
        response = requests.post(
            MachineService.BASE_URL,
            headers={
                'accept': '*/*',
                'Content-Type': 'application/json-patch+json'
            },
            json=machine_data
        )
        response.raise_for_status()
        return response.json()