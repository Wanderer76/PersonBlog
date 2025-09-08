import os
from fastapi import FastAPI, HTTPException, UploadFile
from pydantic import BaseModel
import torch
from genresSearch.predict import DEVICE, GenreNet, predict_genre_tta_from_bytes
from tokenizer.service import prepare_elastic_keywords

def load_model(checkpoint_path: str):
    if not os.path.exists(checkpoint_path):
        raise FileNotFoundError(f"Чекпоинт не найден: {checkpoint_path}")

    checkpoint = torch.load(checkpoint_path, map_location=DEVICE, weights_only=True)
    model = GenreNet(n_classes=len(checkpoint["classes"])).to(DEVICE)
    model.load_state_dict(checkpoint["model"])
    model.eval()

    classes = checkpoint["classes"]
    cfg = checkpoint["cfg"]
    global_mean = cfg.get("GLOBAL_MEAN", None)
    global_std = cfg.get("GLOBAL_STD", None)

    return model, classes, global_mean, global_std
app = FastAPI()
MODEL, CLASSES, GLOBAL_MEAN, GLOBAL_STD = load_model('./genre_cnn_best_acc.pt')
# checkpoint = torch.load("./genre_cnn_best_acc.pt", map_location=DEVICE, weights_only=True)
# model = GenreNet(n_classes=len(checkpoint["classes"])).to(DEVICE)
# model.load_state_dict(checkpoint["model"])
# classes = checkpoint["classes"]


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