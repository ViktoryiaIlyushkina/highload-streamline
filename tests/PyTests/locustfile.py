import uuid
from locust import HttpUser, task, between

class ApiTrafficSim(HttpUser):
    # Пауза между запросами одного "пользователя" (от 0.01 до 0.1 сек для высокого RPS)
    wait_time = between(0.01, 0.1)

    @task(5)
    def post_data(self):
        """Тест эндпоинта записи (API -> RabbitMQ)"""
        record_id = str(uuid.uuid4())
        payload = {
            "id": record_id,
            "payload": f"Highload test payload {record_id}",
            "createdAt": "2026-02-24T15:00:00Z"
        }
        # Отправляем POST запрос
        self.client.post("/data", json=payload, name="/data [POST]")