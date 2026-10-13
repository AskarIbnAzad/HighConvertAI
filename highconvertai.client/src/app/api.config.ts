// API base host configuration. Can be overridden by setting window.API_BASE or window.__env.API_BASE
declare global {
    interface Window { __env?: any; API_BASE?: string }
}

export const API_BASE: string =
    ((window.__env && window.__env.API_BASE) || window.API_BASE || 'http://localhost:5157').replace(/\/$/, '');

export default API_BASE;
