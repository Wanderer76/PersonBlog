import { useEditor, EditorContent } from '@tiptap/react';
import { Extension } from '@tiptap/core';
import StarterKit from '@tiptap/starter-kit';
import Color from '@tiptap/extension-color';
import { TableKit } from '@tiptap/extension-table';
import { TextStyle } from '@tiptap/extension-text-style';
import { useCallback, useEffect } from 'react';
import './RichTextEditor.css';

interface RichTextEditorProps {
    value: string;
    onChange: (value: string) => void;
    placeholder?: string;
}

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

export const RichTextEditor = ({ value, onChange, placeholder }: RichTextEditorProps) => {
    const editor = useEditor({
        extensions: [
            StarterKit,
            Color.configure({ types: ['textStyle'] }),
            TextStyle,
            TableKit.configure({
                table: {
                    resizable: true,
                    lastColumnResizable: true,
                    allowTableNodeSelection: true,
                },
            }),
            TableSlashCommand,
        ],
        content: value,
        onUpdate: ({ editor }) => {
            const html = editor.getHTML();
            onChange(html);
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

    const insertTable = useCallback(() => {
        editor?.chain().focus().insertTable({ rows: 3, cols: 3, withHeaderRow: true }).run();
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
                <span className="rich-text-editor-toolbar-separator" aria-hidden="true" />
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
            <p className="rich-text-editor-hint">Таблицу также можно вставить командой <code>/table</code> или <code>/таблица</code>.</p>
            <EditorContent editor={editor} className="rich-text-editor-content" />
        </div>
    );
};
