/**
 * Cookie utility functions for managing auth tokens.
 * Using cookies instead of localStorage for better security (HttpOnly via server, CSRF protection, etc.)
 */

export function setCookie(name, value, expiresAt) {
  let cookie = `${name}=${encodeURIComponent(value)}; path=/; SameSite=Lax`;
  if (expiresAt) {
    const date = new Date(expiresAt);
    cookie += `; expires=${date.toUTCString()}`;
  }
  document.cookie = cookie;
}

export function getCookie(name) {
  const match = document.cookie.match(new RegExp('(?:^|; )' + name + '=([^;]*)'));
  return match ? decodeURIComponent(match[1]) : null;
}

export function removeCookie(name) {
  document.cookie = `${name}=; path=/; expires=Thu, 01 Jan 1970 00:00:00 GMT; SameSite=Lax`;
}