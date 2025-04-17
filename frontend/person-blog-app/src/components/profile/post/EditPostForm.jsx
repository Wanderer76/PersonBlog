import React, { useState, useRef } from "react";
import './CreatePostForm.css';
import { JwtTokenService } from "../../../scripts/TokenStrorage";
import API from "../../../scripts/apiMethod";

export function EditPostForm(props) {

    const [postForm, setPostForm] = useState({ ...props.post, video: null });
    const [uploadProgress, setUploadProgress] = useState(0);
    const [maxProgress, setMaxProgress] = useState(0);
    const [showProgress, setShowProgress] = useState(false);
    const [preview, setPreview] = useState(props.post.previewId);

    const CHUNK_SIZE = 10 * 1024 * 1024;
    function updateForm(event) {
        const key = event.target.name;
        const value = (key === 'video' || key === 'previewId') ? event.target.files[0] : event.target.value;
        setPostForm((prev) => ({
            ...prev,
            [key]: value
        }))
        console.log(postForm);
    }

    async function sendForm() {
        const url = "/profile/api/Post/edit";
        let formData = new FormData();
        var postId = postForm.id;
        Object.keys(postForm).forEach((key) => {
            if (key !== "video") {
                // if (postForm.video !== null && postForm.video.size < CHUNK_SIZE) {
                //     formData.append(key, postForm[key])
                // }
                formData.append(key, postForm[key])

            }
        });

        await API.post(url, formData, {
            headers: {
                'Authorization': JwtTokenService.getFormatedTokenForHeader()
            }
        }).then(response => {
            if (response.status === 200) {
                console.log(response.data)
                postId = response.data.id;
            }
        })

        if (postForm.video !== null) {
            setShowProgress(true);
            console.log(postForm.video);

            await uploadFile(postId);
        }
        props.onHandleClose();
        // window.location.reload()
    }

    async function uploadFile(postId) {
        const file = postForm.video;

        const totalChunks = Math.ceil(file.size / CHUNK_SIZE);
        let currentChunk = 0;

        setUploadProgress(0);
        setMaxProgress(totalChunks);

        while (currentChunk < totalChunks) {
            const start = currentChunk * CHUNK_SIZE;
            const end = Math.min(start + CHUNK_SIZE, file.size);
            const chunk = file.slice(start, end);

            await uploadChunk(chunk, currentChunk + 1, totalChunks, file.name, postId, '.mp4', file.size);
            currentChunk++;
            setUploadProgress(currentChunk);
        }
        alert('Загрузка завершена');
    }

    async function uploadChunk(chunk, chunkNumber, totalChunks, fileName, postId, fileExtension, totalSize) {
        const formData = new FormData();
        formData.append('chunkNumber', chunkNumber);
        formData.append('totalChunkCount', totalChunks);
        formData.append('fileName', fileName);
        formData.append('fileExtension', fileExtension);
        formData.append('postId', postId);
        formData.append('totalSize', totalSize);
        formData.append('chunkData', chunk);
        console.log(postId);
        try {
            const response = await API.post('/profile/api/Post/uploadChunk', formData,
                {
                    headers: { 'Authorization': JwtTokenService.getFormatedTokenForHeader() }
                });
            console.log(response);
            if (response.status !== 200) throw new Error('Upload failed');
        } catch (error) {
            console.error('Error uploading chunk:', error);
        }
    }


    return (
        <>
            <div className="modal">
                <div className="createPostForm">
                    <h1>Редактировать видео-пост</h1> {/* Изменен заголовок */}
                    <div className="formGroup">
                        <label>Название</label>
                        <input
                            className="modalContent"
                            type="text"
                            placeholder="Добавьте название вашего видео"
                            name="title"
                            onChange={updateForm}
                            value={postForm.title}
                        />
                    </div>
                    {postForm.existingVideoUrl && !postForm.video && (
                        <div className="existing-video">
                            <video controls src={postForm.existingVideoUrl} />
                            <button
                                className="btn btnSecondary"
                                onClick={() => document.querySelector('.fileInput').click()}>
                                Заменить видео
                            </button>
                        </div>
                    )}
                    <div className="previewContainer">
                        <video className="videoPreview"  controls />
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
                            value={postForm.description}
                        />
                    </div>

                    <div className="formGroup">
                        <label>Настройки приватности</label>
                        <div className="privacySettings">
                            <select name="privacy" onChange={updateForm}>
                                <option>Публичный</option>
                                <option>Ссылочный</option>
                                <option>Приватный</option>
                            </select>
                            <span>🔒</span>
                        </div>
                    </div>
                    <div className="actionButtons">
                        <button className="btn btnPrimary" onClick={sendForm}>
                            Сохранить изменения {/* Изменен текст кнопки */}
                        </button>
                        <button className="btn btnSecondary" onClick={props.onHandleClose}>
                            Отмена {/* Изменен текст кнопки */}
                        </button>
                    </div>
                </div>
            </div>
        </>
    );
}