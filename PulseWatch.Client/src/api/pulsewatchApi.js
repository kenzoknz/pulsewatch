import axios from "axios";

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || "/api"
});

// Request interceptor to attach Authorization header
api.interceptors.request.use((config) => {
  const token = localStorage.getItem("accessToken");
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
}, (error) => {
  return Promise.reject(error);
});

// Response interceptor to handle 401 Unauthorized
api.interceptors.response.use((response) => {
  return response;
}, (error) => {
  if (error.response && error.response.status === 401) {
    localStorage.removeItem("accessToken");
    localStorage.removeItem("tokenExpiresAt");
    
    // Dispatch custom event to notify AuthContext to clear state
    window.dispatchEvent(new CustomEvent("auth:unauthorized"));
    
    if (!window.location.pathname.startsWith("/login")) {
      window.location.href = "/login";
    }
  }
  return Promise.reject(error);
});

// Auth API
export const authApi = {
  register: (data) => api.post("/auth/register", data),
  login: (data) => api.post("/auth/login", data),
  getMe: () => api.get("/auth/me")
};

// Websites
export const getWebsites = (params) => api.get("/websites", { params });
export const getWebsite = (id) => api.get(`/websites/${id}`);
export const createWebsite = (data) => api.post("/websites", data);
export const bulkCreateWebsites = (data) => api.post("/websites/bulk", data);
export const updateWebsite = (id, data) => api.put(`/websites/${id}`, data);
export const deleteWebsite = (id) => api.delete(`/websites/${id}`);

// Bulk actions
export const bulkCheckWebsites = (websiteIds) => api.post("/websites/bulk-check", { websiteIds });
export const bulkDeleteWebsites = (websiteIds) => api.post("/websites/bulk-delete", { websiteIds });
export const checkAllWebsites = () => api.post("/websites/check-all");
export const deleteAllWebsites = () => api.post("/websites/delete-all");

// Dashboard & Stats
export const getDashboardSummary = () => api.get("/dashboard/summary");
export const getWebsiteStats = (id) => api.get(`/websites/${id}/stats`);
export const getWebsiteChecks = (id, page = 1, pageSize = 20) =>
  api.get(`/websites/${id}/checks`, {
    params: { page, pageSize },
  });
export const runWebsiteCheck = (id) => api.post(`/websites/${id}/check`);
export const getDowntimeEvents = (id) => api.get(`/websites/${id}/downtime-events`);
export const runDeepCheck = (id) => api.post(`/websites/${id}/deep-check`);

export default api;