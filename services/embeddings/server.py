"""Local ONNX embeddings. One inference thread; no paid embedding service."""
import json
import os
from http.server import BaseHTTPRequestHandler, HTTPServer
from fastembed import TextEmbedding

MODEL = os.environ.get("EMBEDDING_MODEL", "BAAI/bge-small-en-v1.5")
model = TextEmbedding(model_name=MODEL, cache_dir="/models", threads=1)


class Handler(BaseHTTPRequestHandler):
    def respond(self, status, body):
        raw = json.dumps(body).encode()
        self.send_response(status)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(raw)))
        self.end_headers()
        self.wfile.write(raw)

    def do_GET(self):
        if self.path == "/health":
            self.respond(200, {"model": MODEL, "dimensions": 384})
        else:
            self.respond(404, {"error": "unknown endpoint"})

    def do_POST(self):
        if self.path != "/embed":
            return self.respond(404, {"error": "unknown endpoint"})
        try:
            size = int(self.headers.get("Content-Length", "0"))
            if size > 1000000:
                return self.respond(413, {"error": "request too large"})
            data = json.loads(self.rfile.read(size))
            texts = data["texts"]
            if not isinstance(texts, list) or not 1 <= len(texts) <= 64 or not all(isinstance(t, str) and t.strip() for t in texts):
                return self.respond(400, {"error": "texts must contain 1-64 nonempty strings"})
            fn = model.query_embed if data.get("kind") == "query" else model.passage_embed
            vectors = [v.tolist() for v in fn(texts, batch_size=16)]
            self.respond(200, {"model": MODEL, "dimensions": 384, "vectors": vectors})
        except (KeyError, ValueError, TypeError) as e:
            self.respond(400, {"error": str(e)})


HTTPServer(("0.0.0.0", 8080), Handler).serve_forever()
