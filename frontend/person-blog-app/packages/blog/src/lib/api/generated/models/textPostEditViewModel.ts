/**
 * Generated-compatible model for editing text posts.
 */
import type { PostVisibility } from './postVisibility';
import type { TextPostMediaViewModel } from './textPostMediaViewModel';

export interface TextPostEditViewModel {
  id: string;
  title: string;
  text?: string | null;
  visibility: PostVisibility;
  media: TextPostMediaViewModel[];
}
