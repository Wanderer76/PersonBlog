import io
import os
import torch
import torchaudio
from torch import nn
import numpy as np
from torchvision.models import efficientnet_b0

SAMPLE_RATE = 22050
DURATION = 10.0
NUM_SAMPLES = int(SAMPLE_RATE * DURATION)
N_MELS = 128
N_FFT = 1024
HOP_LENGTH = 512

DEVICE = "cuda" if torch.cuda.is_available() else "cpu"

mel_spec = torchaudio.transforms.MelSpectrogram(
    sample_rate=SAMPLE_RATE, n_fft=N_FFT, hop_length=HOP_LENGTH, n_mels=N_MELS
)
amp_to_db = torchaudio.transforms.AmplitudeToDB()

def load_wav(path: bytes):
    buffer = io.BytesIO(path)
    wav, sr = torchaudio.load(buffer)
    if sr != SAMPLE_RATE:
        wav = torchaudio.functional.resample(wav, sr, SAMPLE_RATE)
    if wav.shape[0] > 1:
        wav = wav.mean(dim=0, keepdim=True)
    return wav

def to_logmel(wav):
    with torch.no_grad():
        S = mel_spec(wav)
        S = amp_to_db(S)
    return S

class GenreNet(nn.Module):
    def __init__(self, n_classes):
        super().__init__()
        self.backbone = efficientnet_b0()
        self.backbone.features[0][0] = nn.Conv2d(1, 32, kernel_size=3, stride=2, padding=1, bias=False)
        self.backbone.classifier[1] = nn.Linear(self.backbone.classifier[1].in_features, n_classes)

    def forward(self, x):
        return self.backbone(x)

def predict_genre_tta(model : GenreNet, classes, audio_path: bytes, n_segments=5, global_mean=None, global_std=None):
    print('fgdef')
    model.eval()
    with torch.no_grad():
        wav_full = load_wav(audio_path)

        if wav_full.shape[1] < NUM_SAMPLES:
            wav_full = torch.nn.functional.pad(wav_full, (0, NUM_SAMPLES - wav_full.shape[1]))

        probs_list = []
        step = max(1, (wav_full.shape[1] - NUM_SAMPLES) // max(1, n_segments-1)) if wav_full.shape[1] > NUM_SAMPLES else 1

        for i in range(n_segments):
            start = min(i * step, wav_full.shape[1] - NUM_SAMPLES)
            segment = wav_full[:, start:start+NUM_SAMPLES]
            S = to_logmel(segment)

            if global_mean is not None and global_std is not None:
                S = (S - global_mean) / global_std
            else:
                mu = S.mean()
                sigma = S.std().clamp(min=1e-5)
                S = (S - mu) / sigma

            S = S.unsqueeze(0).to(DEVICE)
            logits = model(S)
            probs = torch.softmax(logits, dim=1)
            probs_list.append(probs.cpu())

        avg_probs = torch.mean(torch.stack(probs_list), dim=0)
        pred_idx = avg_probs.argmax().item()
        confidence = avg_probs[0, pred_idx].flatten()[0].item()
        return classes[pred_idx], confidence, avg_probs[0].numpy()

def predict_genre_tta_from_bytes(model, classes, audio_bytes, n_segments=5, global_mean=None, global_std=None):
    """Предсказывает жанр из байтов аудиофайла"""
    model.eval()
    with torch.no_grad():
        wav_full = load_wav(audio_bytes)
        if wav_full.shape[1] < NUM_SAMPLES:
            wav_full = torch.nn.functional.pad(wav_full, (0, NUM_SAMPLES - wav_full.shape[1]))

        probs_list = []
        step = max(1, (wav_full.shape[1] - NUM_SAMPLES) // max(1, n_segments-1)) if wav_full.shape[1] > NUM_SAMPLES else 1

        for i in range(n_segments):
            start = min(i * step, wav_full.shape[1] - NUM_SAMPLES)
            segment = wav_full[:, start:start+NUM_SAMPLES]
            S = to_logmel(segment)

            if global_mean is not None and global_std is not None:
                S = (S - global_mean) / global_std
            else:
                mu = S.mean()
                sigma = S.std().clamp(min=1e-5)
                S = (S - mu) / sigma

            S = S.unsqueeze(0).to(DEVICE)
            logits = model(S)
            probs = torch.softmax(logits, dim=1)
            probs_list.append(probs.cpu())

        avg_probs = torch.mean(torch.stack(probs_list), dim=0)
        pred_idx = avg_probs.argmax().item()
        confidence = avg_probs[0, pred_idx].item()
        top_probs, top_idxs = torch.topk(avg_probs, k=3,dim=1)
        print('top_probs ',top_probs)
        print('top_idxs ',top_idxs)

        result = {
            "predicted_genre": classes[pred_idx],
            "confidence": round(confidence, 3),
            "top_3": [
                {
                    "genre": classes[top_idxs[0, i].item()],
                    "probability": round(top_probs[0, i].item(), 3)
                }
                for i in range(top_probs.size(1)) 
            ],
            "all_probabilities": {
                cls: round(avg_probs[0, i].item(), 3)
                for i, cls in enumerate(classes)
            }
        }
        return result