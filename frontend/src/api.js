const API_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5000';

export async function apiRequest(path, options = {}) {
  const token = localStorage.getItem('token');
  const headers = new Headers(options.headers ?? {});

  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }

  if (options.body && !(options.body instanceof FormData)) {
    headers.set('Content-Type', 'application/json');
  }

  const response = await fetch(`${API_URL}${path}`, {
    ...options,
    headers,
    body: options.body instanceof FormData ? options.body : JSON.stringify(options.body),
  });

  if (response.status === 401) {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    throw new Error('Oturum suresi doldu. Lutfen tekrar giris yapin.');
  }

  if (!response.ok) {
    let message = 'Islem tamamlanamadi.';
    try {
      const error = await response.json();
      message = error.message ?? message;
    } catch {
      message = response.statusText || message;
    }
    throw new Error(message);
  }

  if (response.status === 204) {
    return null;
  }

  return response.json();
}

export { API_URL };
