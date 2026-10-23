function asRecord(value: unknown): Record<string, unknown> | undefined {
  return value !== null && typeof value === 'object'
    ? value as Record<string, unknown>
    : undefined;
}

function messagesFrom(value: unknown): string[] {
  if (typeof value === 'string') {
    return value.trim() ? [value] : [];
  }

  if (Array.isArray(value)) {
    return value.flatMap(messagesFrom);
  }

  const record = asRecord(value);
  if (!record) {
    return [];
  }

  if (typeof record['message'] === 'string') {
    return messagesFrom(record['message']);
  }

  return Object.values(record).flatMap(messagesFrom);
}

export function getApiErrorMessage(error: unknown, fallback: string): string {
  const outer = asRecord(error);
  const body = asRecord(outer?.['error']) ?? outer;
  if (!body) {
    return fallback;
  }

  const fieldErrors = messagesFrom(body['errors']);
  if (fieldErrors.length > 0) {
    return [...new Set(fieldErrors)].join(' ');
  }

  const apiError = messagesFrom(body['error']);
  if (apiError.length > 0) {
    return [...new Set(apiError)].join(' ');
  }

  const message = messagesFrom(body['message'] ?? body['detail']);
  return message[0] ?? fallback;
}