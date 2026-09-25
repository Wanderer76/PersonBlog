import { useEditor, EditorContent } from '@tiptap/react';
import { Extension } from '@tiptap/core';
import StarterKit from '@tiptap/starter-kit';
import Color from '@tiptap/extension-color';
import Image from '@tiptap/extension-image';
import { TableKit } from '@tiptap/extension-table';
import { TextStyle } from '@tiptap/extension-text-style';
import { useCallback, useEffect, useMemo, useRef } from 'react';
import type { ChangeEvent } from 'react';
import './RichTextEditor.css';

export interface InlineImageUpload {
    referenceId: string;
    file: File;
}

export interface RichTextMedia {
    id: string;
    name: string;
    url: string;
    contentType: string;
}

interface RichTextEditorProps {
    value: string;
    onChange: (value: string) => void;
    media?: RichTextMedia[];
    onInlineImagesChange?: (images: InlineImageUpload[]) => void;
    placeholder?: string;
}

const MAX_IMAGE_SIZE = 20 * 1024 * 1024;
const ALLOWED_IMAGE_TYPES = ['image/jpeg', 'image/png', 'image/gif', 'image/webp'];

const InlineImage = Image.extend({
    addAttributes() {
        return {
            ...this.parent?.(),
            mediaId: {
                default: null,
                parseHTML: (element) => element.getAttribute('data-media-id'),
                renderHTML: (attributes) => attributes.mediaId
                    ? { 'data-media-id': attributes.mediaId }
                    : {},
            },
            mediaReference: {
                default: null,
                parseHTML: (element) => element.getAttribute('data-media-reference'),
                renderHTML: (attributes) => attributes.mediaReference
                    ? { 'data-media-reference': attributes.mediaReference }
                    : {},
            },
        };
    },
});

const hydrateInlineImages = (html: string, media: RichTextMedia[]): string => {
    if (!html || media.length === 0) return html;

    const template = document.createElement('template');
    template.innerHTML = html;
    const mediaById = new Map(media.map((item) => [item.id.toLowerCase(), item]));
    template.content.querySelectorAll<HTMLImageElement>('img[data-media-id]').forEach((image) => {
        const mediaId = image.dataset.mediaId?.toLowerCase();
        const item = mediaId ? mediaById.get(mediaId) : undefined;
        if (!item || !item.contentType.startsWith('image/')) return;

        image.src = item.url;
        if (!image.alt) image.alt = item.name;
    });
    return template.innerHTML;
};

const TableSlashCommand = Extension.create({
    name: 'tableSlashCommand',
    addKeyboardShortcuts() {
        return {
            Enter: () => {
                const { selection } = this.editor.state;
                const { $from } = selection;
                if (!selection.empty || !$from.parent.isTextblock) return false;

                const command = $from.parent
                    .textBetween(0, $from.parentOffset, ' ', ' ')
                    .trim()
                    .toLocaleLowerCase();
                if (command !== '/table' && command !== '/таблица') return false;

                return this.editor
                    .chain()
                    .focus()
                    .deleteRange({ from: $from.start(), to: $from.pos })
                    .insertTable({ rows: 3, cols: 3, withHeaderRow: true })
                    .run();
            },
        };
    },
});

export const RichTextEditor = ({
    value,
    onChange,
    media = [],
    onInlineImagesChange,
    placeholder,
}: RichTextEditorProps) => {
    const imageInputRef = useRef<HTMLInputElement>(null);
    const pendingImagesRef = useRef<Map<string, InlineImageUpload & { url: string }>>(new Map());
    const hydratedValue = useMemo(() => hydrateInlineImages(value, media), [value, media]);

    const syncPendingImages = useCallback((currentEditor: NonNullable<ReturnType<typeof useEditor>>) => {
        const usedReferences = new Set<string>();
        currentEditor.state.doc.descendants((node) => {
            if (node.type.name === 'image' && typeof node.attrs.mediaReference === 'string') {
                usedReferences.add(node.attrs.mediaReference);
            }
        });

        pendingImagesRef.current.forEach((image, referenceId) => {
            if (usedReferences.has(referenceId)) return;
            URL.revokeObjectURL(image.url);
            pendingImagesRef.current.delete(referenceId);
        });
        onInlineImagesChange?.(Array.from(pendingImagesRef.current.values())
            .map(({ referenceId, file }) => ({ referenceId, file })));
    }, [onInlineImagesChange]);

    const editor = useEditor({
        extensions: [
            StarterKit,
            Color.configure({ types: ['textStyle'] }),
            TextStyle,
            InlineImage.configure({
                allowBase64: false,
                inline: false,
            }),
            TableKit.configure({
                table: {
                    resizable: true,
                    lastColumnResizable: true,
                    allowTableNodeSelection: true,
                },
            }),
            TableSlashCommand,
        ],
        content: hydratedValue,
        onUpdate: ({ editor }) => {
            const html = editor.getHTML();
            onChange(html);
            syncPendingImages(editor);
        },
        editorProps: {
            attributes: {
                class: 'prose prose-sm sm:prose lg:prose-lg xl:prose-xl focus:outline-none',
                'aria-label': placeholder ?? 'Редактор текста',
            },
        },
        immediatelyRender: false,
    });

    // Sync content when value prop changes
    useEffect(() => {
        if (editor && hydratedValue !== editor.getHTML()) {
            editor.commands.setContent(hydratedValue, { emitUpdate: false });
        }
    }, [hydratedValue, editor]);

    useEffect(() => () => {
        pendingImagesRef.current.forEach((image) => URL.revokeObjectURL(image.url));
        pendingImagesRef.current.clear();
    }, []);

    const addColor = useCallback((color: string) => {
        editor?.chain().focus().setColor(color).run();
    }, [editor]);

    const toggleBold = useCallback(() => {
        editor?.chain().focus().toggleBold().run();
    }, [editor]);

    const toggleItalic = useCallback(() => {
        editor?.chain().focus().toggleItalic().run();
    }, [editor]);

    const insertTable = useCallback(() => {
        editor?.chain().focus().insertTable({ rows: 3, cols: 3, withHeaderRow: true }).run();
    }, [editor]);

    const insertImages = useCallback((event: ChangeEvent<HTMLInputElement>) => {
        if (!editor || !event.target.files) return;

        Array.from(event.target.files).forEach((file) => {
            if (!ALLOWED_IMAGE_TYPES.includes(file.type)) {
                window.alert(`Формат файла «${file.name}» не поддерживается`);
                return;
            }
            if (file.size > MAX_IMAGE_SIZE) {
                window.alert(`Файл «${file.name}» превышает 20 МБ`);
                return;
            }

            const referenceId = crypto.randomUUID();
            const url = URL.createObjectURL(file);
            pendingImagesRef.current.set(referenceId, { referenceId, file, url });
            editor.chain().focus().insertContent({
                type: 'image',
                attrs: {
                    src: url,
                    alt: file.name,
                    title: file.name,
                    mediaReference: referenceId,
                },
            }).run();
        });

        event.target.value = '';
        syncPendingImages(editor);
    }, [editor, syncPendingImages]);

    if (!editor) {
        return null;
    }

    return (
        <div className="rich-text-editor">
            <div className="rich-text-editor-toolbar">
                <button
                    type="button"
                    onClick={toggleBold}
                    className={editor.isActive('bold') ? 'is-active' : ''}
                    title="Жирный"
                >
                    <strong>B</strong>
                </button>
                <button
                    type="button"
                    onClick={toggleItalic}
                    className={editor.isActive('italic') ? 'is-active' : ''}
                    title="Курсив"
                >
                    <em>I</em>
                </button>
                <input
                    type="color"
                    onChange={(e) => addColor(e.target.value)}
                    title="Цвет текста"
                    value={editor.getAttributes('textStyle').color || '#000000'}
                />
                <span className="rich-text-editor-toolbar-separator" aria-hidden="true" />
                <input
                    ref={imageInputRef}
                    className="rich-text-editor-image-input"
                    type="file"
                    accept={ALLOWED_IMAGE_TYPES.join(',')}
                    multiple
                    tabIndex={-1}
                    onChange={insertImages}
                />
                <button type="button" onClick={() => imageInputRef.current?.click()} title="Вставить изображение в позицию курсора">
                    Изображение
                </button>
                {editor.isActive('image') && (
                    <button
                        type="button"
                        className="is-danger"
                        onClick={() => editor.chain().focus().deleteSelection().run()}
                        title="Удалить выбранное изображение"
                    >
                        Удалить изображение
                    </button>
                )}
                <button type="button" onClick={insertTable} title="Вставить таблицу 3 на 3">
                    Таблица
                </button>
                {editor.isActive('table') && (
                    <div className="rich-text-editor-table-controls" role="group" aria-label="Редактирование таблицы">
                        <button type="button" onClick={() => editor.chain().focus().addRowAfter().run()} title="Добавить строку">
                            + строка
                        </button>
                        <button type="button" onClick={() => editor.chain().focus().deleteRow().run()} title="Удалить строку">
                            − строка
                        </button>
                        <button type="button" onClick={() => editor.chain().focus().addColumnAfter().run()} title="Добавить столбец">
                            + столбец
                        </button>
                        <button type="button" onClick={() => editor.chain().focus().deleteColumn().run()} title="Удалить столбец">
                            − столбец
                        </button>
                        <button
                            type="button"
                            className={editor.isActive('tableHeader') ? 'is-active' : ''}
                            onClick={() => editor.chain().focus().toggleHeaderRow().run()}
                            title="Включить или выключить строку заголовков"
                        >
                            Заголовок
                        </button>
                        <button
                            type="button"
                            className="is-danger"
                            onClick={() => editor.chain().focus().deleteTable().run()}
                            title="Удалить таблицу"
                        >
                            Удалить таблицу
                        </button>
                    </div>
                )}
            </div>
            <p className="rich-text-editor-hint">Изображения вставляются в позицию курсора. Таблицу также можно добавить командой <code>/table</code> или <code>/таблица</code>.</p>
            <EditorContent editor={editor} className="rich-text-editor-content" />
        </div>
    );
};
