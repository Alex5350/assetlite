/**
 * Development environment.
 *
 * The .NET API is expected at a fixed port (http://localhost:5060) and is
 * CORS-enabled for the dev server, so no proxy configuration or Aspire
 * service-discovery override is needed: we simply call the API directly.
 */
export const environment = {
  production: false,
  /** API base URL — fixed dev port of the .NET backend. */
  apiUrl: 'http://localhost:5060',
};
