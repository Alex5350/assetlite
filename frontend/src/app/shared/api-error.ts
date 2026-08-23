import { HttpErrorResponse } from '@angular/common/http';

/** One entry of the API's `errors` problem-details extension (ErrorDetail). */
interface ProblemErrorDetail {
  code: string;
  description: string;
}

/**
 * RFC 9457 problem payloads the AssetLite API emits:
 * - domain errors (404/409…): `{ title: "Asset.CannotAssignRetired", detail, errors: [{code, description}] }`
 * - validation errors (400): ASP.NET ValidationProblemDetails with `errors: { "Name": ["msg"] }`
 */
interface ProblemBody {
  title?: string;
  detail?: string;
  status?: number;
  errors?: unknown;
}

/** Uniform, UI-ready view of a failed HTTP call. */
export interface ApiProblem {
  /** Human-oriented headline: the problem title or a status fallback. */
  title: string;
  /** All human-readable messages (deduplicated, in server order). */
  messages: string[];
  /** Property-level validation messages keyed by field name (e.g. `Name`), when available. */
  fieldErrors: Record<string, string>;
}

const STATUS_TITLES: Record<number, string> = {
  400: 'Invalid request',
  404: 'Not found',
  409: 'Conflict',
  500: 'Server error',
};

/**
 * Parses an HTTP error into a display-friendly problem. Never throws — unknown
 * shapes fall back to a generic message so callers can render an error panel.
 */
export function parseProblem(err: unknown): ApiProblem {
  if (!(err instanceof HttpErrorResponse)) {
    return { title: 'Something went wrong', messages: [describe(err)], fieldErrors: {} };
  }

  const body = (err.error ?? null) as ProblemBody | null;
  const statusTitle = STATUS_TITLES[err.status] ?? `Request failed (${err.status})`;

  if (!body || typeof body !== 'object') {
    // Network-level failure or a non-JSON body (e.g. offline dev server).
    return { title: statusTitle, messages: [err.message || 'The request could not be completed.'], fieldErrors: {} };
  }

  const fieldErrors: Record<string, string> = {};
  const messages: string[] = [];

  if (Array.isArray(body.errors)) {
    // Domain problem: errors is [{code, description}].
    for (const entry of body.errors as ProblemErrorDetail[]) {
      if (entry && typeof entry.description === 'string') {
        messages.push(entry.description);
      }
    }
  } else if (body.errors && typeof body.errors === 'object') {
    // ValidationProblemDetails: errors is { Property: [messages] }.
    for (const [property, value] of Object.entries(body.errors as Record<string, unknown>)) {
      const joined = (Array.isArray(value) ? value : [value]).filter((m): m is string => typeof m === 'string').join(' ');
      if (joined) {
        fieldErrors[property] = joined;
        messages.push(joined);
      }
    }
  }

  if (messages.length === 0 && typeof body.detail === 'string' && body.detail.trim()) {
    messages.push(body.detail.trim());
  }

  // Titles like "Asset.CannotAssignRetired" read poorly verbatim — prefer the
  // message text and keep the code as a small annotation.
  const title =
    messages.length > 0 ? humanizeTitle(body.title, statusTitle) : typeof body.title === 'string' && !isErrorCode(body.title) ? body.title : statusTitle;

  return {
    title,
    messages: dedupe(messages).length > 0 ? dedupe(messages) : [title],
    fieldErrors,
  };
}

/** Short message for an unknown thrown value. */
function describe(err: unknown): string {
  return err instanceof Error ? err.message : 'Unexpected error.';
}

/** Domain error codes (e.g. "Office.DuplicateCode") — dotted, mostly-camel identifiers. */
function isErrorCode(title: string): boolean {
  return /^[A-Za-z][A-Za-z0-9]*(\.[A-Za-z][A-Za-z0-9]*)+$/.test(title);
}

/** Turns "One or more validation errors occurred." style titles into something friendlier. */
function humanizeTitle(title: string | undefined, fallback: string): string {
  if (!title || isErrorCode(title) || /validation errors? occurred/i.test(title)) {
    return fallback;
  }
  return title;
}

function dedupe(messages: string[]): string[] {
  return [...new Set(messages)];
}
