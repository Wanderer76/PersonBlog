// import { useState, useEffect } from 'react';
// import './PlaylistEditorModal.module.css';

// const PlaylistEditorModal = ({ isOpen, onClose, initialData, onSubmit }) => {
//   const [formData, setFormData] = useState({
//     title: '',
//     description: '',
//     cover: null,
//     isPrivate: false,
//     videos: [],
//     ...initialData
//   });

//   const [preview, setPreview] = useState(initialData?.coverUrl || null);
//   const [errors, setErrors] = useState({});

//   useEffect(() => {
//     if (initialData) {
//       setFormData(prev => ({ ...prev, ...initialData }));
//       if (initialData.coverUrl) setPreview(initialData.coverUrl);
//     }
//   }, [initialData]);

//   const handleFileUpload = (e) => {
//     const file = e.target.files[0];
//     if (file) {
//       setFormData(prev => ({ ...prev, cover: file }));
//       const reader = new FileReader();
//       reader.onloadend = () => setPreview(reader.result);
//       reader.readAsDataURL(file);
//     }
//   };

//   const validateForm = () => {
//     const newErrors = {};
//     if (!formData.title.trim()) newErrors.title = 'Название обязательно';
//     return newErrors;
//   };

//   const handleSubmit = (e) => {
//     e.preventDefault();
//     const validationErrors = validateForm();
//     if (Object.keys(validationErrors).length > 0) {
//       setErrors(validationErrors);
//       return;
//     }
//     onSubmit(formData);
//     onClose();
//   };

//   if (!isOpen) return null;

//   return (
//     <div className="modal-overlay">
//       <div className="modal-content">
//         <div className="modal-header">
//           <h2>{initialData ? 'Редактирование' : 'Создание'} плейлиста</h2>
//           <button className="modal-close-btn" onClick={onClose}>&times;</button>
//         </div>

//         <form onSubmit={handleSubmit} className="modal-form">
//           <div className="form-group">
//             <label className="cover-upload-label">
//               <input
//                 type="file"
//                 accept="image/*"
//                 onChange={handleFileUpload}
//                 className="hidden-input"
//               />
//               <div className="cover-preview">
//                 {preview ? (
//                   <img src={preview} alt="Обложка" />
//                 ) : (
//                   <div className="cover-placeholder">
//                     <span>+ Загрузить обложку</span>
//                   </div>
//                 )}
//               </div>
//             </label>
//           </div>

//           <div className="form-group">
//             <label>Название плейлиста *</label>
//             <input
//               type="text"
//               value={formData.title}
//               onChange={(e) => setFormData({ ...formData, title: e.target.value })}
//               className={errors.title ? 'has-error' : ''}
//             />
//             {errors.title && <span className="error-message">{errors.title}</span>}
//           </div>

//           <div className="form-group">
//             <label>Описание</label>
//             <textarea
//               value={formData.description}
//               onChange={(e) => setFormData({ ...formData, description: e.target.value })}
//               rows="3"
//             />
//           </div>

//           <div className="form-group checkbox-group">
//             <label>
//               <input
//                 type="checkbox"
//                 checked={formData.isPrivate}
//                 onChange={(e) => setFormData({ ...formData, isPrivate: e.target.checked })}
//               />
//               Приватный плейлист
//             </label>
//           </div>

//           <div className="modal-actions">
//             <button type="button" className="btn btn-secondary" onClick={onClose}>
//               Отмена
//             </button>
//             <button type="submit" className="btn btn-primary">
//               {initialData ? 'Сохранить изменения' : 'Создать плейлист'}
//             </button>
//           </div>
//         </form>
//       </div>
//     </div>
//   );
// };

// export default PlaylistEditorModal;