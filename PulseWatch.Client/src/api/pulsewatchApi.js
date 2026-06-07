import axios from "axios";

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || "/api"
});

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
export const runWebsiteCheck = (id) => api.post(`/websites/${id}/checks/run`);
export const getDowntimeEvents = (id) => api.get(`/websites/${id}/downtime-events`);

export default api;