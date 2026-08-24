import DOMPurify from 'dompurify';

const RICH_TEXT_TAGS = [
  'p', 'br', 'strong', 'b', 'em', 'i', 's', 'strike', 'span',
  'h1', 'h2', 'h3', 'h4', 'h5', 'h6', 'ul', 'ol', 'li',
  'blockquote', 'pre', 'code', 'hr',
];

export const sanitizeTextPostHtml = (html: string): string => {
  const sanitizedHtml = DOMPurify.sanitize(html, {
    ALLOWED_TAGS: RICH_TEXT_TAGS,
    ALLOWED_ATTR: ['style'],
    ALLOW_DATA_ATTR: false,
  });
  const template = document.createElement('template');
  template.innerHTML = sanitizedHtml;
  template.content.querySelectorAll<HTMLElement>('[style]').forEach((element) => {
    const color = element.style.color;
    element.removeAttribute('style');
    if (color) element.style.color = color;
  });
  return template.innerHTML;
};
