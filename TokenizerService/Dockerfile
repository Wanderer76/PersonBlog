# Используем официальный slim Python-образ
FROM python:3.11-slim

# Устанавливаем рабочую директорию внутри контейнера
WORKDIR /app

# Копируем зависимости отдельно, чтобы использовать кэш при билде
COPY TokenizerService/requirements.txt ./requirements.txt

# Устанавливаем pip-зависимости
RUN pip install  --upgrade pip \
    && pip install -r requirements.txt
RUN python -c "import nltk; nltk.download('stopwords'); nltk.download('punkt_tab')"
# Копируем всё содержимое TokenizerService внутрь контейнера
COPY TokenizerService/ .

# Указываем порт, на котором будет работать uvicorn
EXPOSE 8000

# Запускаем FastAPI приложение через uvicorn
CMD ["fastapi", "run", "main.py",  "--port", "8000"]
