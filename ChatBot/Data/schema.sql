-- Chạy script này trên database "erp_rag" (hoặc database bạn khai báo trong ConnectionStrings:Postgres)
-- trước khi chạy ứng dụng lần đầu.

CREATE EXTENSION IF NOT EXISTS vector;

CREATE TABLE IF NOT EXISTS document_chunks (
    id UUID PRIMARY KEY,
    filename TEXT NOT NULL,
    chunk_index INT NOT NULL,
    content TEXT NOT NULL,
    embedding VECTOR(384) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Index HNSW cho cosine distance (dùng toán tử <=> khi query) — tăng tốc vector search khi dữ liệu lớn.
CREATE INDEX IF NOT EXISTS idx_document_chunks_embedding
    ON document_chunks USING hnsw (embedding vector_cosine_ops);

-- Index phụ để list/delete theo filename nhanh hơn.
CREATE INDEX IF NOT EXISTS idx_document_chunks_filename
    ON document_chunks (filename);
