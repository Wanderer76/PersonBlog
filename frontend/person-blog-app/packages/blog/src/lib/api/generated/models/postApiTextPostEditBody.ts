/**
 * Generated-compatible request body for editing text posts.
 */
import type { PostVisibility } from './postVisibility';

export interface PostApiTextPostEditBody {
  Id: string;
  Title: string;
  Text?: string;
  Visibility: PostVisibility;
  Media?: Blob[];
  RemovedMediaIds?: string[];
}
