import React, { useState } from "react";
import './CreatePostForm.css';
import { JwtTokenService } from "../../scripts/TokenStrorage";
import API from "../../scripts/apiMethod";

export function CreatePostForm(props) {

    const [postForm, setPostForm] = useState({ type: 1, title: "", text: "", video: null });
    const [uploadProgress, setUploadProgress] = useState(0);
    const [maxProgress, setMaxProgress] = useState(0);
    const [showProgress, setShowProgress] = useState(false);

    const CHUNK_SIZE = 10 * 1024 * 1024; // 5MB. You may want to lower this if you anticipate poor connections

    function updateForm(event) {
        const key = event.target.name;
        const value = key === 'video' ? event.target.files[0] : event.target.value;
        setPostForm((prev) => ({
            ...prev,
            [key]: value
        }))
    }

    async function sendForm() {
        const url = "/profile/Blog/post/create";
        let formData = new FormData();
        var postId = null;
        Object.keys(postForm).forEach((key) => {

            if (key === "video") {
                if (postForm.video !== null && postForm.video.size < CHUNK_SIZE) {
                    formData.append(key, postForm[key])
                }
            }
            else
                formData.append(key, postForm[key])
        });

        await API.post(url, formData, {
            headers: {
                'Authorization': JwtTokenService.getFormatedTokenForHeader()
            }
        }).then(response => {
            if (response.status === 200) {
                console.log(response.data)
                postId = response.data;
            }
        })

        if (postForm.video !== null && postForm.video.size > CHUNK_SIZE) {
            setShowProgress(true);
            await uploadFile(postId);
        }
        props.onHandleClose();
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
            const response = await API.post('/profile/post/uploadChunk', formData,
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
                    <p>Создать пост</p>
                    <p>Название</p>
                    <input className="modalContent" placeholder="Название" name="title" onChange={updateForm} />
                    <p>Описание</p>
                    <input className="modalContent" placeholder="Название" name="text" onChange={updateForm} />
                    <p>Видео</p>
                    <input className="modalContent" type="file" accept=".mp4,.mkv" name="video" onChange={updateForm} />
                    <br />
                    {showProgress && <progress value={uploadProgress} max={maxProgress}></progress>}
                    {showProgress && <br />}
                    <button onClick={props.onHandleClose}>Закрыть</button>
                    <button onClick={sendForm}>Создать</button>
                </div>
            </div>
        </>
    );
}