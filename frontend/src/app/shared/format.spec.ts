import { dash, formatDate, formatMoney } from './format';

describe('formatMoney', () => {
  it('formats an amount with grouped thousands and two decimals', () => {
    expect(formatMoney(1234.5)).toBe('$1,234.50');
    expect(formatMoney(0)).toBe('$0.00');
  });

  it('renders an em dash for null, undefined and non-finite amounts', () => {
    expect(formatMoney(null)).toBe('—');
    expect(formatMoney(undefined)).toBe('—');
    expect(formatMoney(Number.NaN)).toBe('—');
    expect(formatMoney(Number.POSITIVE_INFINITY)).toBe('—');
  });
});

describe('formatDate', () => {
  it('formats a plain YYYY-MM-DD date as a medium local date', () => {
    // Local-midnight parsing keeps the calendar day stable across time zones.
    expect(formatDate('2026-08-22')).toBe('Aug 22, 2026');
  });

  it('formats ISO date-time strings', () => {
    expect(formatDate('2026-08-22T10:00:00Z')).toBe('Aug 22, 2026');
  });

  it('renders an em dash for null, empty and unparseable values', () => {
    expect(formatDate(null)).toBe('—');
    expect(formatDate(undefined)).toBe('—');
    expect(formatDate('')).toBe('—');
    expect(formatDate('not-a-date')).toBe('—');
  });
});

describe('dash', () => {
  it('passes non-empty values through', () => {
    expect(dash('Laptops')).toBe('Laptops');
  });

  it('trims surrounding whitespace before deciding emptiness', () => {
    expect(dash('  spaced  ')).toBe('spaced');
  });

  it('renders an em dash for null, undefined and blank strings', () => {
    expect(dash(null)).toBe('—');
    expect(dash(undefined)).toBe('—');
    expect(dash('')).toBe('—');
    expect(dash('   ')).toBe('—');
  });
});
