# =========================
# 1) Builder: ставим зависимости в venv
# =========================
FROM python:3.11-slim AS builder

# Системные зависимости (минимум, если вообще нужны)
# RUN apt-get update && apt-get install -y --no-install-recommends build-essential && \
#     rm -rf /var/lib/apt/lists/*

ENV VENV_PATH=/opt/venv
RUN python -m venv $VENV_PATH
ENV PATH="$VENV_PATH/bin:$PATH" \
    PIP_NO_CACHE_DIR=1 \
    PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1

WORKDIR /app

# Кладём requirements отдельно, чтобы кэшировалось
COPY TokenizerService/requirements.txt ./requirements.txt

# --- ВАЖНО: ставим CPU-версию torch, затем остальные пакеты ---
# Убери torch из requirements.txt (или оставь, но переопредели индекс только для torch)
RUN pip install --upgrade pip && \
    pip install --no-cache-dir --index-url https://download.pytorch.org/whl/cpu torch==2.7.1 && \
    pip install --no-cache-dir -r requirements.txt

# Скачиваем только нужные данные NLTK в отдельную папку
ENV NLTK_DATA=/usr/local/nltk_data
RUN python - <<'PY'
import nltk, os
os.makedirs("/usr/local/nltk_data", exist_ok=True)
# Скачай только то, что реально нужно. Если нужен и 'punkt_tab', оставь обе строки:
nltk.download('stopwords', download_dir='/usr/local/nltk_data')
nltk.download('punkt', download_dir='/usr/local/nltk_data')
nltk.download('punkt_tab', download_dir='/usr/local/nltk_data')  
PY

# =========================
# 2) Final: переносим только venv + код
# =========================
FROM python:3.11-slim AS runtime

ENV VENV_PATH=/opt/venv
ENV PATH="$VENV_PATH/bin:$PATH" \
    NLTK_DATA=/usr/local/nltk_data \
    PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1

WORKDIR /app

# Копируем только то, что нужно для рантайма
COPY --from=builder $VENV_PATH $VENV_PATH
COPY --from=builder /usr/local/nltk_data /usr/local/nltk_data

# Код приложения
COPY TokenizerService/ ./

EXPOSE 8000

# В проде лучше uvicorn, а не fastapi-cli
# Предполагается, что в main.py есть переменная `app`
CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8000"]
