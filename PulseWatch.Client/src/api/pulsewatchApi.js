import axios from "axios";

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || "/api"
});

export const getWebsites = () => api.get("/websites");
export const getWebsite = (id) => api.get(`/websites/${id}`);
export const createWebsite = (data) => api.post("/websites", data);
export const updateWebsite = (id, data) => api.put(`/websites/${id}`, data);
export const deleteWebsite = (id) => api.delete(`/websites/${id}`);

export const getDashboardSummary = () => api.get("/dashboard/summary");
export const getWebsiteStats = (id) => api.get(`/websites/${id}/stats`);
export const getWebsiteChecks = (id) => api.get(`/websites/${id}/checks`);
export const getDowntimeEvents = (id) => api.get(`/websites/${id}/downtime-events`);

export default api;