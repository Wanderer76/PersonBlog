import React, { useEffect, useRef, useState } from "react";
import styles from './CreatePostForm.module.css';
import API from "../../../scripts/apiMethod";
import { useNavigate } from "react-router-dom";
import { saveChunk } from "../../../serviceWorker/IndexedDB";
import {
  TitleInput,
  ThumbnailUpload,
  DescriptionTextarea,
  PrivacySelect,
  ActionButtons,
  CategoryMultiSelect
} from "./CommonComponents";

const CreatePostForm = function () {
  const [postForm, setPostForm] = useState({
    type: 1,
    title: "",
    description: "",
    video: null,
    thumbnail: null
  });
  const fileInputRef = useRef(null);
  const [uploadProgress, setUploadProgress] = useState(0);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [createModel, setCreateModel] = useState(null);
  const videoRef = useRef(null);
  const navigate = useNavigate();
  const CHUNK_SIZE = 10 * 1024 * 1024;

  function updateForm(event) {

    var target = event.target ?? event;
    var key = target.name;

    var value;
    if (target.files) {
      value = target.files[0];
    } else {
      value = target.value;
    }
    console.log(value)

    // Защита от undefined
    if (key) {
      setPostForm(prev => ({ ...prev, [key]: value }));
    }
  }

  useEffect(() => {
    API.get("/profile/api/Post/create")
      .then(response => setCreateModel(response.data));
  }, []);

  async function sendForm() {
    if (!postForm.title.trim()) {
      alert("Пожалуйста, добавьте название видео");
      return;
    }

    if (isSubmitting) return;
    setIsSubmitting(true);

    try {
      const url = "/profile/api/Post/create";
      const formData = new FormData();
      let postId = null;

      formData.append('type', postForm.type);
      formData.append('title', postForm.title);
      formData.append('description', postForm.description);
      if (postForm.thumbnail) {
        formData.append('thumbnail', postForm.thumbnail);
      }
      postForm.categories.forEach(id => {
        formData.append('categories[]', id);
      });



      const response = await API.post(url, formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
      });

      if (response.status === 200) {
        postId = response.data;
        if (postForm.video) {
          await uploadFile(postId);
        }
      }
      navigate('/profile');
    } catch (error) {
      console.error("Ошибка создания поста:", error);
      alert("Произошла ошибка при создании поста");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function uploadFile(postId) {
    const file = postForm.video;
    const totalChunks = Math.ceil(file.size / CHUNK_SIZE);

    const progressResp = await API.post("/profile/api/Post/uploadProgress", {
      postId: postId,
      totalChunkCount: totalChunks,
      totalSize: file.size
    });

    const progress = progressResp.data;
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
  }

  async function saveChunkAndNotifySW(chunk, chunkNumber, totalChunks, fileName,
    postId, fileExtension, totalSize, fileId) {
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
    await saveChunk(chunkId, chunk, meta);

    if (chunkNumber === totalChunks) {
      navigator.serviceWorker?.controller?.postMessage({
        type: 'UPLOAD_ALL_CHUNKS'
      });
    }
  }

  function handleFileSelect(input) {
    const file = input.target.files[0];
    if (!file) return;

    // Проверка размера файла (макс. 500MB)
    // if (file.size > 500 * 1024 * 1024) {
    //   alert("Файл слишком большой. Максимальный размер 500MB");
    //   return;
    // }

    const videoURL = URL.createObjectURL(file);
    if (videoRef.current) {
      videoRef.current.src = videoURL;
      videoRef.current.load();
    }
  }

  return (
    <div className={styles.modal}>
      <div className={styles.createPostForm}>
        <h1>Создать видео-пост</h1>

        <TitleInput
          value={postForm.title}
          onChange={updateForm}
          placeholder="Добавьте название вашего видео"
        />

        <ThumbnailUpload
          thumbnail={postForm.thumbnail}
          onChange={updateForm}
        />

        <div className={styles.formGroup}>
          <label>Видео</label>
          <div className={styles.uploadArea} onClick={() => fileInputRef.current?.click()}>
            <div className={styles.cameraIcon}>🎥</div>
            <h3>Выберите файл для загрузки</h3>
            <p>или перетащите видео файл</p>
            <input
              ref={fileInputRef}
              name='video'
              type="file"
              className={`${styles.videoInput} ${styles.fileInput}`}
              accept=".mp4,.mkv"
              hidden
              onChange={(e) => {
                updateForm(e);
                handleFileSelect(e);
              }}
            />
          </div>
        </div>

        <div className={styles.previewContainer}>
          <video className={styles.videoPreview} ref={videoRef} controls />
          <div className={styles.progressBar}>
            <div
              className={styles.progressFill}
              style={{ width: `${uploadProgress}%` }}
            />
          </div>
        </div>
        <DescriptionTextarea
          value={postForm.description}
          onChange={updateForm}
          placeholder="Добавьте описание к вашему видео"
        />

        <CategoryMultiSelect
          options={createModel?.categoryList || []}
          value={postForm.categories || []}
          onChange={updateForm}
        />

        <PrivacySelect
          options={createModel?.visibility || []}
          value={postForm.visibility}
          onChange={updateForm}
        />

        <ActionButtons
          onCancel={() => navigate('/profile')}
          onSubmit={sendForm}
          cancelText="Закрыть"
          submitText="Создать"
          isSubmitting={isSubmitting}
        />
      </div>
    </div>
  );
}

export default CreatePostForm;