/**
 * Public API client — no Authorization header attached.
 * Used for unauthenticated public status page and badge endpoints.
 */
import axios from "axios";

const publicApi = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || "/api",
});

// GET /api/public/websites/{id}/status
export const getPublicWebsiteStatus = (id) =>
  publicApi.get(`/public/websites/${id}/status`);

// Returns a fully qualified URL string for embedding the SVG badge
export const getPublicBadgeUrl = (id) => {
  const base = import.meta.env.VITE_API_BASE_URL || "/api";
  return `${base}/public/websites/${id}/badge`;
};

export default publicApi;
