import { useEditor, EditorContent } from '@tiptap/react';
import StarterKit from '@tiptap/starter-kit';
import Color from '@tiptap/extension-color';
import { TextStyle } from '@tiptap/extension-text-style';
import { useCallback, useEffect } from 'react';
import './RichTextEditor.css';

interface RichTextEditorProps {
    value: string;
    onChange: (value: string) => void;
    placeholder?: string;
}

export const RichTextEditor = ({ value, onChange, placeholder }: RichTextEditorProps) => {
    const editor = useEditor({
        extensions: [
            StarterKit,
            Color.configure({ types: ['textStyle'] }),
            TextStyle,
        ],
        content: value,
        onUpdate: ({ editor }) => {
            const html = editor.getHTML();
            onChange(html);
        },
        editorProps: {
            attributes: {
                class: 'prose prose-sm sm:prose lg:prose-lg xl:prose-xl focus:outline-none',
            },
        },
        immediatelyRender: false,
    });

    // Sync content when value prop changes
    useEffect(() => {
        if (editor && value !== editor.getHTML()) {
            editor.commands.setContent(value);
        }
    }, [value, editor]);

    const addColor = useCallback((color: string) => {
        editor?.chain().focus().setColor(color).run();
    }, [editor]);

    const toggleBold = useCallback(() => {
        editor?.chain().focus().toggleBold().run();
    }, [editor]);

    const toggleItalic = useCallback(() => {
        editor?.chain().focus().toggleItalic().run();
    }, [editor]);

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
            </div>
            <EditorContent editor={editor} className="rich-text-editor-content" />
        </div>
    );
};