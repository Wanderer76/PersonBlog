import os
from fastapi import FastAPI, HTTPException, UploadFile
from pydantic import BaseModel
import torch
from genresSearch.predict import load_model, predict_genre_tta_from_bytes
from tokenizer.service import prepare_elastic_keywords


app = FastAPI()
MODEL, CLASSES, GLOBAL_MEAN, GLOBAL_STD = load_model('./genre_cnn_best_acc_fma.pt')

class TokenizeRequest(BaseModel):
    text:str

@app.post('/tokenize')
def tokenize_text(req:TokenizeRequest):
    keywords = prepare_elastic_keywords(req.text)
    return {
        'tokens':keywords
    }

@app.post("/predict")
async def predict_genre(file: UploadFile):
    audio_bytes = await file.read()

    try:
        result = predict_genre_tta_from_bytes(
            MODEL, CLASSES, audio_bytes,
            n_segments=5,
            global_mean=GLOBAL_MEAN,
            global_std=GLOBAL_STD
        )
        return result
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Prediction error: {str(e)}")