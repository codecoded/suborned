FROM python:3.12.11-slim
WORKDIR /app
COPY requirements.lock .
RUN pip install --no-cache-dir -r requirements.lock
COPY server.py .
CMD ["python", "-u", "server.py"]
