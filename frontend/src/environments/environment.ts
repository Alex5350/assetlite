/**
 * Production environment.
 *
 * The app is served behind the same origin as the API (e.g. via Aspire
 * orchestration or a reverse proxy), so API calls are same-origin relative.
 */
export const environment = {
  production: true,
  /** API base URL — same origin in production. */
  apiUrl: '/',
};
