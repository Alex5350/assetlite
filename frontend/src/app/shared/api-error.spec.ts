import { HttpErrorResponse } from '@angular/common/http';
import { parseProblem } from './api-error';

/** Builds an HttpErrorResponse with a JSON problem body, like HttpClient delivers. */
function problemError(status: number, body: unknown): HttpErrorResponse {
  return new HttpErrorResponse({ status, error: body });
}

describe('parseProblem', () => {
  it('parses domain problems: title code, errors array with descriptions', () => {
    const problem = parseProblem(
      problemError(409, {
        title: 'Office.DuplicateCode',
        status: 409,
        detail: 'An office with this code already exists.',
        errors: [{ code: 'Office.DuplicateCode', description: 'An office with this code already exists.' }],
      }),
    );

    // The dotted error code reads poorly as a headline — the status fallback wins.
    expect(problem.title).toBe('Conflict');
    expect(problem.messages).toEqual(['An office with this code already exists.']);
    expect(problem.fieldErrors).toEqual({});
  });

  it('parses ValidationProblemDetails: errors dictionary into field errors', () => {
    const problem = parseProblem(
      problemError(400, {
        title: 'One or more validation errors occurred.',
        status: 400,
        errors: { Name: ['Name is required.'], PurchaseDate: ['Purchase date cannot be in the future.'] },
      }),
    );

    expect(problem.title).toBe('Invalid request');
    expect(problem.fieldErrors['Name']).toBe('Name is required.');
    expect(problem.fieldErrors['PurchaseDate']).toBe('Purchase date cannot be in the future.');
    expect(problem.messages).toContain('Name is required.');
  });

  it('falls back to the detail text when no errors extension is present', () => {
    const problem = parseProblem(problemError(404, { title: 'Not Found', status: 404, detail: 'Asset not found.' }));
    // A friendly server title is kept verbatim.
    expect(problem.title).toBe('Not Found');
    expect(problem.messages).toEqual(['Asset not found.']);
  });

  it('handles network-level failures without a JSON body', () => {
    const problem = parseProblem(new HttpErrorResponse({ status: 0, statusText: 'Unknown Error' }));
    expect(problem.title).toBe('Request failed (0)');
    expect(problem.messages.length).toBeGreaterThan(0);
  });

  it('handles non-HTTP thrown values', () => {
    const problem = parseProblem(new Error('boom'));
    expect(problem.title).toBe('Something went wrong');
    expect(problem.messages).toEqual(['boom']);
  });
});
