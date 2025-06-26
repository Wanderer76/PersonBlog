import re
from collections import defaultdict
from rake_nltk import Rake
from nltk.corpus import stopwords as nltk_stopwords
from nltk.stem.snowball import SnowballStemmer

# Настройки
stemmer = SnowballStemmer("russian")
custom_stopwords = set(nltk_stopwords.words("russian"))
punctuations = '''!"#$%&'()*+,-./:;<=>?@[\$$^_`{|}~—«»'''

def prepare_elastic_keywords(text: str, top_n=15) -> list[dict[str,int]]:
    """Извлечение ключевых слов с весами, в формате для Elasticsearch"""
    # Очистка текста от знаков препинания и приведение к нижнему регистру
    text = clean_text(text)
    # Если текст пуст, вернуть пустой список
    if not text:
        return []
    
    # Инициализация RAKE с русскими стоп-словами и пунктуацией
    rake = Rake(stopwords=custom_stopwords, punctuations=punctuations)
    rake.extract_keywords_from_text(text)
    keywords_with_scores = rake.get_ranked_phrases_with_scores()
    
    # Подсчёт весов для отдельных слов
    word_scores = defaultdict(float)
    
    for score, phrase in keywords_with_scores:
        words = phrase.split()
        valid_words = []
        
        for word in words:
            if len(word) <= 2 or word in custom_stopwords:
                continue
            stemmed = stemmer.stem(word)
            valid_words.append(stemmed)
        
        if not valid_words:
            continue
        
        score_per_word = score / len(valid_words)
        for word in valid_words:
            word_scores[word] += score_per_word
    
    # Сортировка и формирование результата
    sorted_keywords = sorted(word_scores.items(), key=lambda x: x[1], reverse=True)
    
    return [{"word": word, "score": round(score, 2)} for word, score in sorted_keywords[:top_n]]

import re

def clean_text(text: str) -> str:
    """Очистка текста: удаление ссылок, знаков препинания, приведение к нижнему регистру"""
    # Удаление URL-ссылок (http, https, www)
    text = re.sub(r'http\S+|www\S+|https\S+', '', text, flags=re.MULTILINE)
    
    # Удаление email-адресов
    text = re.sub(r'\S+@\S+', '', text)
    
    # Удаление знаков препинания (кроме пробелов и букв/цифр)
    text = re.sub(r"[^\w\s]", " ", text.lower())
    
    # Удаление лишних пробелов
    text = re.sub(r"\s+", " ", text).strip()
    
    return text