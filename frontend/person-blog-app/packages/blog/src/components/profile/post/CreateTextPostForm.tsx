import { useState, useEffect } from 'react';
import type { ChangeEvent, FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button } from '@/shared/ui/Button/Button';
import { PrivacySelect, TitleInput } from './CommonComponents';
import { PostVisibility } from '@/lib/api/generated/models';
import type { PostApiTextPostCreateTextPostBody, PostVisibilitySelectItem } from '@/lib/api/generated/models';
import { getTextPost } from '@/lib/api/generated/text-post/text-post';
import { ALLOWED_MEDIA_TYPES, MAX_FILE_SIZE, MediaUploader } from '@/features/post-management/components/MediaUploader/MediaUploader';
import { RichTextEditor } from '@/features/post-management/components/RichTextEditor/RichTextEditor';
import './CreateTextPostForm.css';

interface FormErrors {
    title?: string;
    text?: string;
    media?: string;
    general?: string;
}

type TextPostFormData = Omit<PostApiTextPostCreateTextPostBody, 'Media'> & { Media?: File[] };

const CreateTextPostForm = () => {
    const navigate = useNavigate();
    const [formData, setFormData] = useState<TextPostFormData>({
        Title: 'e',
        Text: '',
        Media: [],
        Visibility: PostVisibility.NUMBER_0,
    });

    const [errors, setErrors] = useState<FormErrors>({});
    const [visibilities, setVisivilities] = useState<PostVisibilitySelectItem[]>([]);
    const client = getTextPost();


    const handleChange = <K extends keyof TextPostFormData>(
        field: K,
        value: TextPostFormData[K]
    ) => {
        setFormData((prev) => ({ ...prev, [field]: value }));
        if (errors[field as keyof FormErrors]) {
            setErrors((prev) => ({ ...prev, [field]: undefined }));
        }
    };

    const handleSubmit = async (e: FormEvent) => {
        e.preventDefault();

        const validationErrors = validateForm(formData);
        setErrors(validationErrors);

        if (hasErrors(validationErrors)) {
            return;
        }

        // const submitData = new FormData();
        // submitData.append('Title', formData.Title);

        // if (formData.Text?.trim()) {
        //   submitData.append('Text', formData.Text);
        // }

        // submitData.append('Visibility', formData.Visibility!.toString());

        // formData.Media?.forEach((file) => {
        //   submitData.append('Media', file);
        // });

        var result = await client.postApiTextPostCreateTextPost(formData)
        if (result.status == 200) {
            alert('success')
        }
        // mutate(submitData, {
        //   onSuccess: (data) => {
        //     navigate(`/post/${data.id}`);
        //   },
        // });
    };


    useEffect(() => {
        client.getApiTextPostCreate().then(val => {
            if (val.status === 200) {
                setVisivilities(val.data.visibility!)
            }
        });
    }, []);

    if(visibilities.length == 0)
        return <></>;

    return (
        <form className="create-text-post-form" onSubmit={handleSubmit}>
            <TitleInput
                value={formData.Title}
                onChange={(value: any) => handleChange('Title', value.target.value)}
                placeholder="Заголовок"
            />

            <div className="create-text-post-form__section">
                <label className="create-text-post-form__label">Текст</label>
                <RichTextEditor
                    value={formData.Text!}
                    onChange={(value) => handleChange('Text', value)}
                    placeholder="Напишите что-нибудь..."
                />
                {errors.text && !errors.media && (
                    <p className="create-text-post-form__error">{errors.text}</p>
                )}
            </div>

            <div className="create-text-post-form__section">
                <label className="create-text-post-form__label">Медиа</label>
                <MediaUploader
                    files={formData.Media ?? []}
                    onChange={(files) => handleChange('Media', files)}
                    error={errors.media}
                />
            </div>

            <div className="create-text-post-form__section">
                <label className="create-text-post-form__label">Видимость</label>
                <PrivacySelect
                    options={visibilities}
                    value={visibilities[0]}
                    onChange={(e: ChangeEvent<HTMLSelectElement>) => {
                        const { value } = e.target;
                        return handleChange('Visibility', value);
                    }}
                />
            </div>

            {/* {(apiError || errors.general) && (
        <p className="create-text-post-form__error create-text-post-form__error--general">
          {errors.general || apiError?.message}
        </p>
      )} */}

            <div className="create-text-post-form__actions">
                <Button
                    type="button"
                    variant="secondary"
                    onClick={() => navigate(-1)}
                >
                    Закрыть
                </Button>
                <Button type="submit">
                    {'Создать'}
                </Button>
            </div>
        </form>
    );
};

export const validateForm = (data: TextPostFormData): FormErrors => {
    const errors: FormErrors = {};

    // Title is always required
    if (!data.Title.trim()) {
        errors.title = 'Заголовок обязателен';
    }

    // Either text or media is required
    const hasText = data.Text?.trim().length ?? 0 > 0;
    const hasMedia = data.Media?.length ?? 0 > 0;

    if (!hasText && !hasMedia) {
        errors.text = 'Необходимо заполнить текст или добавить медиа';
        errors.media = 'Необходимо заполнить текст или добавить медиа';
    }

    // Validate media files
    if (hasMedia) {
        for (const file of data.Media!) {
            if (file.size > MAX_FILE_SIZE) {
                errors.media = `Файл "${file.name}" превышает 20МБ`;
                break;
            }
            if (!ALLOWED_MEDIA_TYPES.includes(file.type)) {
                errors.media = `Файл "${file.name}" имеет недопустимый формат`;
                break;
            }
        }
    }

    return errors;
};

export const hasErrors = (errors: FormErrors): boolean => {
    return Object.values(errors).some((error) => error !== undefined);
};


export default CreateTextPostForm;


