from fastapi import FastAPI
from pydantic import BaseModel
from tokenizer.service import prepare_elastic_keywords


app = FastAPI()


class TokenizeRequest(BaseModel):
    text:str

@app.post('/tokenize')
def tokenize_text(req:TokenizeRequest):
    keywords = prepare_elastic_keywords(req.text)
    return {
        'tokens':keywords
    }
