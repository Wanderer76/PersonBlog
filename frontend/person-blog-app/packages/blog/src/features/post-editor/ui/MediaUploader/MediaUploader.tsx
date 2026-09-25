import { useState, useRef, ChangeEvent, useCallback, useEffect, useMemo } from 'react';
import { ALLOWED_MEDIA_TYPES, MAX_FILE_SIZE } from './MediaUploader.constants';
import './MediaUploader.css';

interface MediaUploaderProps {
  files: File[];
  onChange: (files: File[]) => void;
  existingFiles?: ExistingMediaFile[];
  onRemoveExisting?: (id: string) => void;
  error?: string;
}

interface ExistingMediaFile {
  id: string;
  name: string;
  url: string;
  contentType: string;
  length: number;
}

export const MediaUploader = ({
  files,
  onChange,
  existingFiles = [],
  onRemoveExisting,
  error
}: MediaUploaderProps) => {
  const inputRef = useRef<HTMLInputElement>(null);
  const [dragActive, setDragActive] = useState(false);
  const [previewIndex, setPreviewIndex] = useState<number | null>(null);
  const fileUrls = useMemo(() => files.map((file) => URL.createObjectURL(file)), [files]);

  useEffect(() => () => {
    fileUrls.forEach((url) => URL.revokeObjectURL(url));
  }, [fileUrls]);

  const handleFiles = (newFiles: File[]) => {
    const validFiles = newFiles.filter((file) => {
      if (file.size > MAX_FILE_SIZE) {
        alert(`Файл "${file.name}" превышает 20МБ`);
        return false;
      }
      if (!ALLOWED_MEDIA_TYPES.includes(file.type)) {
        alert(`Файл "${file.name}" имеет недопустимый формат`);
        return false;
      }
      return true;
    });

    onChange([...files, ...validFiles]);
  };

  const handleChange = (e: ChangeEvent<HTMLInputElement>) => {
    if (e.target.files) {
      handleFiles(Array.from(e.target.files));
    }
  };

  const handleDrag = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    if (e.type === 'dragenter' || e.type === 'dragover') {
      setDragActive(true);
    } else if (e.type === 'dragleave') {
      setDragActive(false);
    }
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setDragActive(false);
    if (e.dataTransfer.files) {
      handleFiles(Array.from(e.dataTransfer.files));
    }
  };

  const removeFile = (index: number) => {
    onChange(files.filter((_, i) => i !== index));
  };

  const openPreview = (index: number) => {
    setPreviewIndex(index);
  };

  const closePreview = useCallback(() => {
    setPreviewIndex(null);
  }, []);

  const navigatePreview = useCallback((direction: number) => {
    setPreviewIndex((prev) => {
      if (prev === null) return null;
      const newIndex = prev + direction;
      if (newIndex < 0) return files.length - 1;
      if (newIndex >= files.length) return 0;
      return newIndex;
    });
  }, [files.length]);

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (previewIndex === null) return;
      if (e.key === 'Escape') closePreview();
      else if (e.key === 'ArrowLeft') navigatePreview(-1);
      else if (e.key === 'ArrowRight') navigatePreview(1);
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [previewIndex, closePreview, navigatePreview]);

  const renderPreviewContent = (file: File, url: string) => {
    
    if (file.type.startsWith('image/')) {
      return <img src={url} alt={file.name} />;
    } else if (file.type.startsWith('video/')) {
      return <video src={url} controls autoPlay />;
    } else if (file.type.startsWith('audio/')) {
      return (
        <div style={{ padding: '40px', background: '#1a1a1a', borderRadius: '8px', minWidth: '400px' }}>
          <audio src={url} controls autoPlay style={{ width: '100%' }} />
        </div>
      );
    }
    return null;
  };

  return (
    <div className="media-uploader">
      <div
        className={`media-uploader-dropzone ${dragActive ? 'is-active' : ''} ${error ? 'has-error' : ''}`}
        onDragEnter={handleDrag}
        onDragLeave={handleDrag}
        onDragOver={handleDrag}
        onDrop={handleDrop}
        onClick={() => inputRef.current?.click()}
      >
        <input
          ref={inputRef}
          type="file"
          multiple
          accept={ALLOWED_MEDIA_TYPES.join(',')}
          onChange={handleChange}
          className="media-uploader-input"
        />
        <p>📁 Перетащите файлы сюда или кликните для выбора</p>
        <p className="media-uploader-hint">До 20МБ каждый (изображения, видео, аудио)</p>
      </div>

      {(existingFiles.length > 0 || files.length > 0) && (
        <div className="media-uploader-preview">
          {existingFiles.map((file) => (
            <div key={file.id} className="media-uploader-file">
              {file.contentType.startsWith('image/') ? (
                <img src={file.url} alt={file.name} />
              ) : file.contentType.startsWith('video/') ? (
                <video src={file.url} controls preload="metadata" />
              ) : (
                <div className="media-uploader-file-icon">🎵</div>
              )}
              <div className="media-uploader-file-overlay">
                <div className="media-uploader-file-name">{file.name}</div>
                <div className="media-uploader-file-size">
                  {(file.length / 1024 / 1024).toFixed(2)} МБ
                </div>
              </div>
              {onRemoveExisting && (
                <button
                  type="button"
                  onClick={() => onRemoveExisting(file.id)}
                  className="media-uploader-file-remove"
                  aria-label={`Удалить файл ${file.name}`}
                  title="Удалить файл"
                >
                  ×
                </button>
              )}
            </div>
          ))}
          {files.map((file, index) => (
            <div 
              key={index} 
              className="media-uploader-file"
              onClick={() => openPreview(index)}
            >
              {file.type.startsWith('image/') ? (
                <img src={fileUrls[index]} alt={file.name} />
              ) : file.type.startsWith('video/') ? (
                <video src={fileUrls[index]} />
              ) : (
                <div className="media-uploader-file-icon">🎵</div>
              )}
              <div className="media-uploader-file-overlay">
                <div className="media-uploader-file-name">{file.name}</div>
                <div className="media-uploader-file-size">
                  {(file.size / 1024 / 1024).toFixed(2)} МБ
                </div>
              </div>
              <button
                type="button"
                onClick={(e) => {
                  e.stopPropagation();
                  removeFile(index);
                }}
                className="media-uploader-file-remove"
                title="Удалить файл"
              >
                ×
              </button>
            </div>
          ))}
        </div>
      )}

      {error && <p className="media-uploader-error">{error}</p>}

      {/* Preview Modal */}
      {previewIndex !== null && files[previewIndex] && (
        <div className="media-preview-modal" onClick={closePreview}>
          <div className="media-preview-modal-content" onClick={(e) => e.stopPropagation()}>
            <button className="media-preview-modal-close" onClick={closePreview}>
              ×
            </button>
            
            {files.length > 1 && (
              <>
                <button 
                  className="media-preview-modal-nav media-preview-modal-prev"
                  onClick={() => navigatePreview(-1)}
                >
                  ‹
                </button>
                <button 
                  className="media-preview-modal-nav media-preview-modal-next"
                  onClick={() => navigatePreview(1)}
                >
                  ›
                </button>
                <div className="media-preview-modal-counter">
                  {previewIndex + 1} / {files.length}
                </div>
              </>
            )}
            
            {renderPreviewContent(files[previewIndex], fileUrls[previewIndex])}
          </div>
        </div>
      )}
    </div>
  );
};
