import type { PostApiTextPostCreateTextPostBody } from '@/lib/api/generated/models';
import { ALLOWED_MEDIA_TYPES, MAX_FILE_SIZE } from '@/features/post-management/components/MediaUploader/MediaUploader.constants';

export interface FormErrors {
  title?: string;
  text?: string;
  media?: string;
  general?: string;
}

export type TextPostFormData = Omit<PostApiTextPostCreateTextPostBody, 'Media'> & { Media?: File[] };

const hasVisibleText = (html?: string) => {
  if (!html) return false;
  const template = document.createElement('template');
  template.innerHTML = html;
  return Boolean(template.content.textContent?.trim());
};

export const validateForm = (data: TextPostFormData, hasExistingMedia = false): FormErrors => {
  const errors: FormErrors = {};

  const title = data.Title.trim();
  if (!title) errors.title = 'Заголовок обязателен';
  else if (title.length < 3) errors.title = 'Заголовок должен содержать минимум 3 символа';

  const hasText = hasVisibleText(data.Text);
  const hasMedia = hasExistingMedia || Boolean(data.Media?.length);
  if (!hasText && !hasMedia) {
    errors.text = 'Напишите текст или добавьте медиафайл';
    errors.media = 'Напишите текст или добавьте медиафайл';
  }

  for (const file of data.Media ?? []) {
    if (file.size > MAX_FILE_SIZE) {
      errors.media = `Файл «${file.name}» превышает 20 МБ`;
      break;
    }
    if (!ALLOWED_MEDIA_TYPES.includes(file.type)) {
      errors.media = `Формат файла «${file.name}» не поддерживается`;
      break;
    }
  }

  return errors;
};

export const hasErrors = (errors: FormErrors) => Object.values(errors).some(Boolean);
