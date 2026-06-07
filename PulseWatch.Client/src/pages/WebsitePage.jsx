import React, { useState, useEffect } from 'react';
import { useSearchParams } from 'react-router-dom';
import {
  getWebsites,
  createWebsite,
  updateWebsite,
  deleteWebsite,
} from '../api/pulsewatchApi';
import WebsiteTable from '../components/WebsiteTable';
import WebsiteForm from '../components/WebsiteForm';
import { RiSearchLine, RiAddLine, RiAlertLine, RiRefreshLine } from 'react-icons/ri';

export default function WebsitePage({ onViewDetail, onRefresh }) {
  const [searchParams, setSearchParams] = useSearchParams();
  const [websites, setWebsites] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [searchTerm, setSearchTerm] = useState(searchParams.get('q') || '');
  const [filterStatus, setFilterStatus] = useState(searchParams.get('status') || 'all');
  const [showForm, setShowForm] = useState(false);
  const [editingWebsite, setEditingWebsite] = useState(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    fetchWebsites();
  }, []);

  useEffect(() => {
    const nextParams = {};

    if (searchTerm.trim()) {
      nextParams.q = searchTerm.trim();
    }

    if (filterStatus !== 'all') {
      nextParams.status = filterStatus;
    }

    setSearchParams(nextParams, { replace: true });
  }, [searchTerm, filterStatus, setSearchParams]);

  const fetchWebsites = async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await getWebsites();
      setWebsites(response.data || []);
    } catch (err) {
      setError('Failed to load websites. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const handleAddClick = () => {
    setEditingWebsite(null);
    setShowForm(true);
  };

  const handleEditClick = (website) => {
    setEditingWebsite(website);
    setShowForm(true);
  };

  const handleFormCancel = () => {
    setShowForm(false);
    setEditingWebsite(null);
  };

  const handleFormSubmit = async (formData) => {
    try {
      setIsSubmitting(true);
      if (editingWebsite?.id) {
        await updateWebsite(editingWebsite.id, formData);
      } else {
        await createWebsite(formData);
      }
      handleFormCancel();
      fetchWebsites();
      onRefresh();
    } catch (err) {
      alert('Failed to save website. Please try again.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleDelete = async (websiteId) => {
    try {
      await deleteWebsite(websiteId);
      fetchWebsites();
      onRefresh();
    } catch (err) {
      alert('Failed to delete website. Please try again.');
    }
  };

  // Filter and search logic
  const filteredWebsites = websites.filter(site => {
    const matchesSearch =
      site.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
      site.url.toLowerCase().includes(searchTerm.toLowerCase());

    const matchesFilter =
      filterStatus === 'all' ||
      (filterStatus === 'active' && site.isActive) ||
      (filterStatus === 'inactive' && !site.isActive);

    return matchesSearch && matchesFilter;
  });

  if (loading && websites.length === 0) {
    return (
      <div className="loading-state">
        <div className="loading-spinner" />
        <p className="loading-state-description">Loading websites...</p>
      </div>
    );
  }

  if (error && websites.length === 0) {
    return (
      <div className="error-state">
        <div className="error-state-icon" style={{ fontSize: '2rem', color: 'var(--warning)' }}>
          <RiAlertLine />
        </div>
        <h3 className="error-state-title">Failed to load websites</h3>
        <p className="error-state-description">{error}</p>
        <button className="btn btn-primary" onClick={fetchWebsites}>
          <RiRefreshLine style={{ marginRight: '6px' }} /> Try Again
        </button>
      </div>
    );
  }

  return (
    <div>
      <div className="website-list-header">
        <h1 className="website-list-title">Monitored Websites</h1>
      </div>

      {/* Toolbar */}
      <div className="toolbar">
        <div className="toolbar-search">
          <span className="search-icon"><RiSearchLine size={16} /></span>
          <input
            type="text"
            placeholder="Search by name or URL..."
            value={searchTerm}
            onChange={e => setSearchTerm(e.target.value)}
          />
        </div>

        <div className="toolbar-filters">
          <select
            value={filterStatus}
            onChange={e => setFilterStatus(e.target.value)}
            style={{ maxWidth: '200px' }}
          >
            <option value="all">All Websites</option>
            <option value="active">Active Only</option>
            <option value="inactive">Inactive Only</option>
          </select>
        </div>

        <div className="toolbar-actions">
          <button className="btn btn-primary" onClick={handleAddClick}>
            <RiAddLine style={{ marginRight: '6px' }} /> Add Website
          </button>
        </div>
      </div>

      {/* Show form if needed */}
      {showForm && (
        <div style={{ marginBottom: '20px' }}>
          <WebsiteForm
            initialData={editingWebsite}
            onSubmit={handleFormSubmit}
            onCancel={handleFormCancel}
            isSubmitting={isSubmitting}
          />
        </div>
      )}

      {/* Results info */}
      {filteredWebsites.length > 0 && (
        <p className="text-muted" style={{ fontSize: '12px', marginBottom: '16px' }}>
          Showing {filteredWebsites.length} of {websites.length} websites
        </p>
      )}

      {/* Website Table */}
      <WebsiteTable
        websites={filteredWebsites}
        onView={onViewDetail}
        onEdit={handleEditClick}
        onDelete={handleDelete}
      />
    </div>
  );
}
