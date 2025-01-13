import React, { useState } from "react";
import './CreatePostForm.css';
import { JwtTokenService } from "../../scripts/TokenStrorage";
import API from "../../scripts/apiMethod";

export function CreatePostForm(props) {

    const [postForm, setPostForm] = useState({ type: 1, title: "", text: "", video: null });

    function updateForm(event) {
        const key = event.target.name;
        const value = key == 'video' ? event.target.files[0] : event.target.value;
        setPostForm((prev) => ({
            ...prev,
            [key]: value
        }))
    }

    function sendForm() {
        const url = "http://localhost:7892/profile/Blog/post/create";
        let formData = new FormData();
        Object.keys(postForm).forEach((key) => {
            console.log(`${key} - ${postForm[key]}`);
            formData.append(key, postForm[key])
        });

        API.post(url, formData, {
            headers: {
                'Authorization': JwtTokenService.getFormatedTokenForHeader()
            }
        }).then(response => {
            if (response.status === 200) {
                props.onHandleClose();
            }
        })
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
                    <button onClick={props.onHandleClose}>Закрыть</button>
                    <button onClick={sendForm}>Создать</button>
                </div>
            </div>
        </>
    );
}