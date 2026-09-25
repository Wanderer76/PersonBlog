# Background video upload tests

Run `node --test tests/background-upload.test.mjs` from the blog package with Playwright available.
For a shared installation, set `PLAYWRIGHT_MODULE` to its absolute `index.mjs` path.
Set `PLAYWRIGHT_CHANNEL=msedge` to use installed Edge instead of Playwright's Chromium.

The tests use a fresh browser context, real Service Worker and IndexedDB, and a local HTTP stub for the multipart API. No application account or production storage is used.

The form waits for post creation and the worker's IndexedDB transaction, then navigates to the profile. It does not wait for PUT requests or completion. A queued file is immutable; retries use its original file and server session. Successful completion frees the local file. Deleting a post first stops its upload.

Service worker execution is browser-managed. Closing the browser is not a guarantee of continued transfer. Saved uploads resume when the application is reopened, reconnects, or becomes visible, with credentials for the original account. Files require sufficient IndexedDB storage; an enqueue failure is shown in the form. Authentication tokens are kept in worker memory, not persisted alongside files.
