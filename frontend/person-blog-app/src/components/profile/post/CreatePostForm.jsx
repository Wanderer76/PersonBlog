import React, { useEffect, useRef, useState } from "react";
import './CreatePostForm.css';
import API from "../../../scripts/apiMethod";
import { useNavigate } from "react-router-dom";
import { saveChunk, deleteChunk } from "../../../serviceWorker/IndexedDB";
import { getAccessToken, JwtTokenService } from "../../../scripts/TokenStrorage";

const CreatePostForm = function () {

    const [postForm, setPostForm] = useState({ type: 1, title: null, description: null, video: null });
    const [uploadProgress, setUploadProgress] = useState(0);
    const videoRef = useRef(null);
    const navigate = useNavigate();
    const CHUNK_SIZE = 10 * 1024 * 1024;
    const isCreateDisabled = useRef(false);
    const [createModel, setCreateModel] = useState(null);

    function updateForm(event) {
        const key = event.target.name;
        const value = (key === 'video' || key === 'thumbnail') ? event.target.files[0] : event.target.value;
        setPostForm((prev) => ({
            ...prev,
            [key]: value
        }));
    }

    useEffect(() => {
        const url = "/profile/api/Post/create";
        API.get(url).then(response => {
            setCreateModel(response.data);
        });
    }, []);

    async function sendForm() {
        const url = "/profile/api/Post/create";
        let formData = new FormData();
        var postId = null;

        Object.keys(postForm).forEach((key) => {
            if (key !== "video") {
                formData.append(key, postForm[key]);
            }
        });


        await API.post(url, formData, {
            headers: {
                ['Content-Type']: 'multipart/form-data'
            }
        }).then(response => {
            if (response.status === 200) {
                console.log(response.data);
                postId = response.data;
            }
        });

        if (postForm.video !== null && postId !== null) {
            await uploadFile(postId);
        }
        navigate('/profile');
    }

    async function uploadFile(postId) {
        const file = postForm.video;
        const totalChunks = Math.ceil(file.size / CHUNK_SIZE);

        // Получаем fileId от сервера
        const progressResp = await API.post("/profile/api/Post/uploadProgress", {
            postId: postId,
            totalChunkCount: totalChunks,
            totalSize: file.size
        });

        const progress = progressResp.data;
        console.log("Upload progress:", progress);

        let currentChunk = progress.lastUploadChunkNumber ?? 0;

        setUploadProgress(Math.round(currentChunk / totalChunks * 100));

        while (currentChunk < totalChunks) {
            const start = currentChunk * CHUNK_SIZE;
            const end = Math.min(start + CHUNK_SIZE, file.size);
            const chunk = file.slice(start, end);

            await saveChunkAndNotifySW(
                chunk,
                currentChunk + 1,
                totalChunks,
                file.name,
                postId,
                '.mp4',
                file.size,
                progress.fileId
            );

            currentChunk++;
            setUploadProgress(Math.round(currentChunk / totalChunks * 100));
        }
        alert('Видео успешно загружено!');
    }

    async function saveChunkAndNotifySW(chunk, chunkNumber, totalChunks, fileName, postId, fileExtension, totalSize, fileId) {
        const meta = {
            chunkNumber,
            totalChunks,
            fileName,
            fileExtension,
            postId,
            totalSize,
            duration: videoRef.current?.duration ?? 0,
            contentType: "video/mp4",
            fileId
        };

        const chunkId = `${fileId}_${chunkNumber}`;

        // ➡ сохраняем chunk в IndexedDB
        await saveChunk(chunkId, chunk, meta);
        if (meta.chunkNumber == totalChunks) {
            navigator.serviceWorker?.controller?.postMessage({
                type: 'UPLOAD_ALL_CHUNKS'
            });
        }

        // // ➡ посылаем команду SW загрузить его
        // if (navigator.serviceWorker?.controller) {
        //     navigator.serviceWorker.controller.postMessage({
        //         type: 'UPLOAD_CHUNK',
        //         payload: { chunkId }
        //     });
        // }
    }

    function handleFileSelect(input) {
        const file = input.target.files[0];
        if (file) {
            const videoURL = URL.createObjectURL(file);
            if (videoRef.current) {
                videoRef.current.src = videoURL;
                videoRef.current.load();
            }
        }
    }

    return (
        <>
            <div className="modal">
                <div className="createPostForm">
                    <h1>Создать видео-пост</h1>

                    <div className="formGroup">
                        <label>Название</label>
                        <input
                            className="modalContent"
                            type="text"
                            placeholder="Добавьте название вашего видео"
                            name="title"
                            onChange={updateForm}
                        />
                    </div>
                    <div className="formGroup">
                        <label>Превью (миниатюра)</label>
                        <div className="uploadThumbnail" onClick={() => document.querySelector('.thumbnailInput').click()}>
                            {postForm.thumbnail ? (
                                <img
                                    src={URL.createObjectURL(postForm.thumbnail)}
                                    alt="Превью"
                                    className="thumbnailPreview"
                                />
                            ) : (
                                <>
                                    <span>📷</span>
                                    <p>Выберите изображение</p>
                                </>
                            )}
                        </div>
                        <input
                            name="thumbnail"
                            type="file"
                            className="thumbnailInput fileInput"
                            accept="image/*"
                            hidden
                            onChange={updateForm}
                        />
                    </div>
                    <div className="uploadArea" onClick={() => document.querySelector('.videoInput').click()}>
                        <div className="cameraIcon">🎥</div>
                        <h3>Выберите файл для загрузки</h3>
                        <p>или перетащите видео файл</p>
                        <input
                            name='video'
                            type="file"
                            className="videoInput fileInput"
                            accept=".mp4,.mkv"
                            hidden
                            onChange={(e) => {
                                updateForm(e);
                                handleFileSelect(e);
                            }}
                        />
                    </div>

                    <div className="previewContainer">
                        <video className="videoPreview" ref={videoRef} controls />
                        <div className="progressBar">
                            <div
                                className="progressFill"
                                style={{ width: `${uploadProgress}%` }}
                            />
                        </div>
                    </div>

                    <div className="formGroup">
                        <label>Описание</label>
                        <textarea
                            className="modalContent description"
                            rows="4"
                            placeholder="Добавьте описание к вашему видео"
                            name="description"
                            onChange={updateForm}
                        />
                    </div>

                    <div className="formGroup">
                        <label>Настройки приватности</label>
                        <div className="privacySettings">
                            <select name="visibility" defaultValue={createModel?.visibility?.[0]?.value} onChange={updateForm}>
                                {createModel?.visibility?.map((v) => {
                                    return <option key={v.value} value={v.value}>{v.text}</option>
                                })}
                            </select>
                            <span>🔒</span>
                        </div>
                    </div>

                    <div className="actionButtons">
                        <button className="btn btnSecondary" onClick={() => navigate('/profile')}>
                            Закрыть
                        </button>
                        <button className="btn btnPrimary" disabled={isCreateDisabled.current} onClick={() => {
                            isCreateDisabled.current = true;
                            sendForm();
                            isCreateDisabled.current = false;
                        }}>
                            Создать
                        </button>
                    </div>
                </div>
            </div>
        </>
    );
}

export default CreatePostForm;
