export interface SelectOption<T> {
  label: string;
  value: T;
}

/**
 * Builds unique dropdown options from a list of items for a given key.
 * Reuse across any grid instead of writing a bespoke buildXOptions() each time.
 * Filters out null/undefined values so callers always get a clean, non-nullable option list.
 */
export function buildSelectOptions<T, K extends keyof T>(
  items: T[],
  key: K,
): SelectOption<NonNullable<T[K]>>[] {
  const unique = [...new Set(items.map((item) => item[key]))].filter(
    (value): value is NonNullable<T[K]> => value !== null && value !== undefined,
  );

  return unique.map((value) => ({ label: String(value), value }));
}
