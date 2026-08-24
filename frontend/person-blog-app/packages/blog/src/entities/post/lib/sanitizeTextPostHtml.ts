import DOMPurify from 'dompurify';

const RICH_TEXT_TAGS = [
  'p', 'br', 'strong', 'b', 'em', 'i', 's', 'strike', 'span',
  'h1', 'h2', 'h3', 'h4', 'h5', 'h6', 'ul', 'ol', 'li',
  'blockquote', 'pre', 'code', 'hr',
  'table', 'caption', 'colgroup', 'col', 'thead', 'tbody', 'tfoot',
  'tr', 'th', 'td',
];

const TABLE_SEPARATOR_CELL = /^:?-{3,}:?$/;

const splitTableCells = (value: string): string[] => {
  const source = value.trim().replace(/^\|/, '').replace(/\|$/, '');
  const cells: string[] = [];
  let cell = '';
  let escaped = false;

  for (const character of source) {
    if (escaped) {
      cell += character;
      escaped = false;
    } else if (character === '\\') {
      escaped = true;
    } else if (character === '|') {
      cells.push(cell.trim());
      cell = '';
    } else {
      cell += character;
    }
  }

  cells.push(cell.trim());
  return cells;
};

const getAlignment = (separator: string): 'left' | 'center' | 'right' => {
  const value = separator.trim();
  if (value.startsWith(':') && value.endsWith(':')) return 'center';
  if (value.endsWith(':')) return 'right';
  return 'left';
};

const createTable = (
  headerCells: string[],
  separatorCells: string[],
  rows: string[][],
): HTMLTableElement => {
  const alignments = separatorCells.map(getAlignment);
  const table = document.createElement('table');
  const tableHead = document.createElement('thead');
  const headerRow = document.createElement('tr');

  headerCells.forEach((content, cellIndex) => {
    const cell = document.createElement('th');
    cell.scope = 'col';
    cell.dataset.align = alignments[cellIndex];
    cell.innerHTML = content;
    headerRow.append(cell);
  });
  tableHead.append(headerRow);
  table.append(tableHead);

  const tableBody = document.createElement('tbody');
  rows.forEach((cells) => {
    const row = document.createElement('tr');
    cells.forEach((content, cellIndex) => {
      const cell = document.createElement('td');
      cell.dataset.align = alignments[cellIndex];
      cell.innerHTML = content;
      row.append(cell);
    });
    tableBody.append(row);
  });
  table.append(tableBody);

  return table;
};

const convertMultilineMarkdownTables = (root: DocumentFragment) => {
  Array.from(root.children)
    .filter((element): element is HTMLParagraphElement => element instanceof HTMLParagraphElement)
    .forEach((paragraph) => {
      const contentRoot = paragraph.childElementCount === 1 && paragraph.firstElementChild instanceof HTMLSpanElement
        ? paragraph.firstElementChild
        : paragraph;
      const lines = contentRoot.innerHTML
        .split(/<br\s*\/?\s*>/i)
        .map((line) => line.trim())
        .filter(Boolean);

      if (lines.length < 3) return;

      const headerCells = splitTableCells(lines[0]);
      const separatorCells = splitTableCells(lines[1].replace(/<[^>]+>/g, ''));
      const rows = lines.slice(2).map(splitTableCells);
      if (
        headerCells.length < 2 ||
        headerCells.length !== separatorCells.length ||
        !separatorCells.every((cell) => TABLE_SEPARATOR_CELL.test(cell.trim())) ||
        rows.some((cells) => cells.length !== headerCells.length)
      ) {
        return;
      }

      paragraph.before(createTable(headerCells, separatorCells, rows));
      paragraph.remove();
    });
};

const convertMarkdownTables = (root: DocumentFragment) => {
  convertMultilineMarkdownTables(root);
  const paragraphs = Array.from(root.children)
    .filter((element): element is HTMLParagraphElement => element instanceof HTMLParagraphElement);

  for (let index = 0; index < paragraphs.length - 1; index += 1) {
    const header = paragraphs[index];
    const separator = paragraphs[index + 1];
    if (header.parentNode !== root || separator.parentNode !== root) continue;

    const headerCells = splitTableCells(header.innerHTML);
    const separatorCells = splitTableCells(separator.textContent ?? '');
    if (
      headerCells.length < 2 ||
      headerCells.length !== separatorCells.length ||
      !separatorCells.every((cell) => TABLE_SEPARATOR_CELL.test(cell.trim()))
    ) {
      continue;
    }

    const rowParagraphs: HTMLParagraphElement[] = [];
    let sibling = separator.nextElementSibling;
    while (sibling instanceof HTMLParagraphElement) {
      const cells = splitTableCells(sibling.innerHTML);
      if (cells.length !== headerCells.length) break;
      rowParagraphs.push(sibling);
      sibling = sibling.nextElementSibling;
    }
    if (rowParagraphs.length === 0) continue;

    const table = createTable(
      headerCells,
      separatorCells,
      rowParagraphs.map((paragraph) => splitTableCells(paragraph.innerHTML)),
    );

    header.before(table);
    header.remove();
    separator.remove();
    rowParagraphs.forEach((paragraph) => paragraph.remove());
  }
};

const wrapTables = (root: DocumentFragment) => {
  root.querySelectorAll('table').forEach((table) => {
    if (table.parentElement?.hasAttribute('data-rich-text-table-wrapper')) return;

    const wrapper = document.createElement('div');
    wrapper.dataset.richTextTableWrapper = '';
    wrapper.tabIndex = 0;
    wrapper.role = 'region';
    wrapper.setAttribute(
      'aria-label',
      'Таблица публикации. Прокрутите по горизонтали, чтобы увидеть все столбцы.',
    );
    table.before(wrapper);
    wrapper.append(table);
  });
};

export const sanitizeTextPostHtml = (html: string): string => {
  const sanitizedHtml = DOMPurify.sanitize(html, {
    ALLOWED_TAGS: RICH_TEXT_TAGS,
    ALLOWED_ATTR: ['style', 'colspan', 'rowspan', 'colwidth', 'scope'],
    ALLOW_DATA_ATTR: false,
  });
  const template = document.createElement('template');
  template.innerHTML = sanitizedHtml;
  convertMarkdownTables(template.content);
  template.content.querySelectorAll<HTMLElement>('[style]').forEach((element) => {
    const color = element.style.color;
    element.removeAttribute('style');
    if (color) element.style.color = color;
  });
  wrapTables(template.content);
  return template.innerHTML;
};
