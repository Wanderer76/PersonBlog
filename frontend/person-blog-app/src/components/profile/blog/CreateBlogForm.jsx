import React, { useRef, useState } from "react";
import '../post/CreatePostForm.css';
import API from "../../../scripts/apiMethod";
import { useNavigate } from "react-router-dom";

const CreateBlogForm = function () {
    const [blogForm, setBlogForm] = useState({ 
        title: null, 
        description: null, 
        photoUrl: null 
    });
    const [uploadProgress, setUploadProgress] = useState(0);
    const [imagePreview, setImagePreview] = useState(null);
    const navigate = useNavigate();
    const isCreateDisabled = useRef(false);

    function updateForm(event) {
        const key = event.target.name;
        const value = key === 'photoUrl' ? event.target.files[0] : event.target.value;
        setBlogForm((prev) => ({
            ...prev,
            [key]: value
        }));
    }

    function handleImageSelect(input) {
        const file = input.target.files[0];
        if (file) {
            const imageURL = URL.createObjectURL(file);
            setImagePreview(imageURL);
        }
    }

    async function sendForm() {
        const url = "/profile/api/Blog/create";
        let formData = new FormData();

        // Append all form fields
        Object.keys(blogForm).forEach((key) => {
            formData.append(key, blogForm[key]);
        });

        try {
            const response = await API.post(url, formData, {
                headers: {
                    'Content-Type': 'multipart/form-data'
                },
                onUploadProgress: (progressEvent) => {
                    const progress = Math.round(
                        (progressEvent.loaded * 100) / progressEvent.total
                    );
                    setUploadProgress(progress);
                }
            });

            if (response.status === 200) {
                alert('Блог успешно создан!');
                navigate('/profile');
            }
        } catch (error) {
            console.error("Error creating blog:", error);
            alert('Произошла ошибка при создании блога');
        }
    }

    return (
        <div className="modal">
            <div className="createPostForm">
                <h1>Создать блог</h1>

                <div className="formGroup">
                    <label>Название</label>
                    <input
                        className="modalContent"
                        type="text"
                        placeholder="Добавьте название вашего блога"
                        name="title"
                        onChange={updateForm}
                        required
                    />
                </div>

                <div className="uploadArea" onClick={() => document.querySelector('.fileInput').click()}>
                    <div className="cameraIcon">📷</div>
                    <h3>Выберите обложку для блога</h3>
                    <p>или перетащите изображение</p>
                    <input
                        name='photoUrl'
                        type="file"
                        className="fileInput"
                        accept="image/*"
                        hidden
                        onChange={(e) => {
                            updateForm(e);
                            handleImageSelect(e);
                        }}
                    />
                </div>

                {imagePreview && (
                    <div className="previewContainer">
                        <img 
                            src={imagePreview} 
                            alt="Предпросмотр обложки" 
                            className="videoPreview"
                            style={{ objectFit: 'cover' }}
                        />
                        <div className="progressBar">
                            <div
                                className="progressFill"
                                style={{ width: `${uploadProgress}%` }}
                            />
                        </div>
                    </div>
                )}

                <div className="formGroup">
                    <label>Описание</label>
                    <textarea
                        className="modalContent description"
                        rows="4"
                        placeholder="Добавьте описание к вашему блогу"
                        name="description"
                        onChange={updateForm}
                    />
                </div>

                <div className="actionButtons">
                    <button className="btn btnSecondary" onClick={() => navigate('/profile')}>
                        Закрыть
                    </button>
                    <button 
                        className="btn btnPrimary" 
                        disabled={isCreateDisabled.current || !blogForm.title}
                        onClick={() => {
                            isCreateDisabled.current = true;
                            sendForm();
                            isCreateDisabled.current = false;
                        }}
                    >
                        Создать
                    </button>
                </div>
            </div>
        </div>
    );
}

export default CreateBlogForm;