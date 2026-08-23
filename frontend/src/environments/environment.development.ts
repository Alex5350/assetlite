/**
 * Development environment.
 *
 * The SPA calls the API same-origin (/api/...) exactly as in production; the Angular dev
 * server proxies /api to the .NET backend on its fixed port (proxy.conf.json). This keeps
 * dev and production request paths identical and avoids cross-origin requests entirely.
 */
export const environment = {
  production: false,
  /** Same-origin API base; dev-server proxy forwards /api to http://localhost:5060. */
  apiUrl: '',
};
