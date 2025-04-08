import React, { useRef, useState } from "react";
import './CreatePostForm.css';
import { JwtTokenService } from "../../scripts/TokenStrorage";
import API from "../../scripts/apiMethod";

export function CreatePostForm(props) {

    const [postForm, setPostForm] = useState({ type: 1, title: "", description: "", video: null });
    const [uploadProgress, setUploadProgress] = useState(0);
    const [maxProgress, setMaxProgress] = useState(0);
    const [showProgress, setShowProgress] = useState(false);
    const videoRef = useRef(null);

    const CHUNK_SIZE = 10 * 1024 * 1024;

    function updateForm(event) {
        const key = event.target.name;
        const value = key === 'video' ? event.target.files[0] : event.target.value;
        setPostForm((prev) => ({
            ...prev,
            [key]: value
        }))
    }

    async function sendForm() {
        const url = "/profile/api/Post/create";
        let formData = new FormData();
        var postId = null;
        Object.keys(postForm).forEach((key) => {

            if (key === "video") {
                // if (postForm.video !== null && postForm.video.size < CHUNK_SIZE) {
                //     formData.append(key, postForm[key])
                // }
            }
            else
                formData.append(key, postForm[key])
        });

        await API.post(url, formData, {
            headers: {
                ['Content-Type']: 'multipart/form-data'
            }
        }
        ).then(response => {
            if (response.status === 200) {
                console.log(response.data)
                postId = response.data;
            }
        })

        if (postForm.video !== null) {
            setShowProgress(true);
            await uploadFile(postId);
        }
        props.onHandleClose();
        window.location.reload()
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

        try {
            const response = await API.post('/profile/api/Post/uploadChunk', formData,
                {
                    headers: {
                        'Content-Type': 'multipart/form-data'
                    }
                });
            console.log(response);
            if (response.status !== 200) throw new Error('Upload failed');
        } catch (error) {
            console.error('Error uploading chunk:', error);
        }
    }


    return (
        <>
            {/* <div className="modal">
                <div className="createPostForm">
                    <p>Создать пост</p>
                    <p>Название</p>
                    <input className="modalContent" placeholder="Название" name="title" onChange={updateForm} />
                    <p>Описание</p>
                    <input className="modalContent" placeholder="Название" name="description" onChange={updateForm} />
                    <p>Видео</p>
                    <input className="modalContent" type="file" accept=".mp4,.mkv" name="video" onChange={updateForm} />
                    <br />
                    {showProgress && <progress value={uploadProgress} max={maxProgress}></progress>}
                    {showProgress && <br />}
                    <button onClick={props.onHandleClose}>Закрыть</button>
                    <button onClick={sendForm}>Создать</button>
                </div>
            </div> */}
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

                    <div className="uploadArea" onClick={() => document.querySelector('.fileInput').click()}>
                        <div className="cameraIcon">🎥</div>
                        <h3>Выберите файл для загрузки</h3>
                        <p>или перетащите видео файл</p>
                        <input
                            type="file"
                            className="fileInput"
                            accept=".mp4,.mkv"
                            hidden
                            onChange={(e) => {
                                updateForm(e);
                                handleFileSelect(e);
                            }}
                        />
                    </div>

                    <div className="previewContainer">
                        <video className="videoPreview"  ref={videoRef} controls />
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
                            <select name="privacy" onChange={updateForm}>
                                <option>Публичный</option>
                                <option>Ссылочный</option>
                                <option>Приватный</option>
                            </select>
                            <span>🔒</span>
                        </div>
                    </div>

                    <div className="actionButtons">
                        <button className="btn btnSecondary" onClick={props.onHandleClose}>
                            Закрыть
                        </button>
                        <button className="btn btnPrimary" onClick={sendForm}>
                        Создать
                        </button>
                    </div>
                </div>
            </div>
        </>
    );

    function handleFileSelect(input) {
        const file = input.target.files[0];
        if (file) {
            // Показать прелоадер
            // document.getElementById('preloader').style.display = 'block';
            
            // Предпросмотр видео
            const videoPreview = document.getElementsByClassName('videoPreview');
            const videoURL = URL.createObjectURL(file);
            if (videoRef.current) {
                videoRef.current.src = videoURL;
                // videoRef.current.style.display = 'block';
                videoRef.current.load();
              }
          

            // Запустить загрузку на сервер
            // document.getElementById('<%= btnUpload.ClientID %>').click();
        }
    }

    // Обработчик завершения загрузки
    function uploadComplete() {
        document.getElementById('preloader').style.display = 'none';
    }
}