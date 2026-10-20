// Turns "accidentsLast3Years" into "Accidents last 3 years"
export function prettyKey(key: string): string {
  const words = key
    .replace(/([a-z])([A-Z0-9])/g, '$1 $2')
    .replace(/([0-9])([A-Za-z])/g, '$1 $2')
    .toLowerCase();
  return words.charAt(0).toUpperCase() + words.slice(1);
}

// Pulls a readable message out of an API error
export function messagesFrom(err: any): string {
  if (err.status === 0) {
    return 'We could not reach the server. Please check that the API is running.';
  }
  const body = err.error;
  if (body?.error) {
    return body.error;
  }
  if (body?.errors) {
    return (Object.values(body.errors) as string[][]).flat().join(' ');
  }
  return 'Something went wrong. Please try again.';
}