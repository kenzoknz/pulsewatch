import React, { useState, useEffect, useCallback } from 'react';
import { useSearchParams } from 'react-router-dom';
import {
  getWebsites,
  createWebsite,
  updateWebsite,
  deleteWebsite,
  runWebsiteCheck,
  bulkCheckWebsites,
  bulkDeleteWebsites,
  checkAllWebsites,
  deleteAllWebsites,
} from '../api/pulsewatchApi';
import WebsiteTable from '../components/WebsiteTable';
import WebsiteForm from '../components/WebsiteForm';
import {
  RiSearchLine,
  RiAddLine,
  RiAlertLine,
  RiRefreshLine,
  RiDeleteBinLine,
  RiPulseLine,
  RiCheckboxCircleLine,
  RiCloseLine,
} from 'react-icons/ri';

import { useAuth } from '../contexts/AuthContext';

export default function WebsitePage({ onViewDetail, onRefresh }) {
  const { user } = useAuth();
  const isAdmin = user?.roles?.includes('Admin');
  
  const [searchParams, setSearchParams] = useSearchParams();
  const [websites, setWebsites] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [searchTerm, setSearchTerm] = useState(searchParams.get('q') || '');
  const [filterStatus, setFilterStatus] = useState(searchParams.get('status') || 'all');
  const [showForm, setShowForm] = useState(false);
  const [editingWebsite, setEditingWebsite] = useState(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [checkingWebsiteId, setCheckingWebsiteId] = useState(null);

  // Pagination state
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [totalItems, setTotalItems] = useState(0);
  const [totalPages, setTotalPages] = useState(0);

  // Selection state
  const [selectedIds, setSelectedIds] = useState([]);

  // Bulk action state
  const [bulkActionLoading, setBulkActionLoading] = useState(false);
  const [bulkActionMessage, setBulkActionMessage] = useState(null);

  const fetchWebsites = useCallback(async (currentPage, currentPageSize, search) => {
    try {
      setLoading(true);
      setError(null);
      const params = { page: currentPage, pageSize: currentPageSize };
      if (search && search.trim()) {
        params.search = search.trim();
      }
      const response = await getWebsites(params);
      const data = response.data;
      setWebsites(data.items || []);
      setTotalItems(data.totalItems || 0);
      setTotalPages(data.totalPages || 0);
      setPage(data.page || 1);
    } catch (err) {
      setError('Failed to load websites. Please try again.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchWebsites(page, pageSize, searchTerm);
  }, [page, pageSize]);

  // Debounced search
  useEffect(() => {
    const timer = setTimeout(() => {
      setPage(1);
      fetchWebsites(1, pageSize, searchTerm);
    }, 400);
    return () => clearTimeout(timer);
  }, [searchTerm]);

  useEffect(() => {
    const nextParams = {};
    if (searchTerm.trim()) nextParams.q = searchTerm.trim();
    if (filterStatus !== 'all') nextParams.status = filterStatus;
    setSearchParams(nextParams, { replace: true });
  }, [searchTerm, filterStatus, setSearchParams]);

  // Clear selection when page data changes
  useEffect(() => {
    setSelectedIds([]);
  }, [websites]);

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
      fetchWebsites(page, pageSize, searchTerm);
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
      // If this was the last item on the page and we're not on page 1, go back a page
      if (websites.length === 1 && page > 1) {
        setPage(page - 1);
      } else {
        fetchWebsites(page, pageSize, searchTerm);
      }
      onRefresh();
    } catch (err) {
      alert('Failed to delete website. Please try again.');
    }
  };

  const handleRunCheck = async (websiteId) => {
    try {
      setCheckingWebsiteId(websiteId);
      await runWebsiteCheck(websiteId);
      await fetchWebsites(page, pageSize, searchTerm);
      onRefresh();
    } catch (err) {
      alert('Failed to run uptime check. Please try again.');
    } finally {
      setCheckingWebsiteId(null);
    }
  };

  // Selection handlers
  const handleToggleSelect = (id) => {
    setSelectedIds(prev =>
      prev.includes(id) ? prev.filter(x => x !== id) : [...prev, id]
    );
  };

  const handleToggleSelectAll = (checked) => {
    if (checked) {
      setSelectedIds(websites.map(s => s.id));
    } else {
      setSelectedIds([]);
    }
  };

  // Bulk actions
  const handleBulkCheck = async () => {
    if (selectedIds.length === 0) return;
    try {
      setBulkActionLoading(true);
      setBulkActionMessage(null);
      const res = await bulkCheckWebsites(selectedIds);
      const data = res.data;
      setBulkActionMessage({
        type: 'success',
        text: `Checked ${data.success} successfully, ${data.failed} failed${data.skipped ? `, ${data.skipped} skipped` : ''}.`,
      });
      setSelectedIds([]);
      await fetchWebsites(page, pageSize, searchTerm);
      onRefresh();
    } catch (err) {
      setBulkActionMessage({ type: 'error', text: 'Failed to check selected websites.' });
    } finally {
      setBulkActionLoading(false);
    }
  };

  const handleBulkDelete = async () => {
    if (selectedIds.length === 0) return;
    if (!confirm(`Delete ${selectedIds.length} selected website(s)? This cannot be undone.`)) return;
    try {
      setBulkActionLoading(true);
      setBulkActionMessage(null);
      await bulkDeleteWebsites(selectedIds);
      setBulkActionMessage({ type: 'success', text: `Deleted ${selectedIds.length} website(s).` });
      setSelectedIds([]);
      // Adjust page if needed
      const remainingOnPage = totalItems - selectedIds.length;
      const newTotalPages = Math.ceil(remainingOnPage / pageSize) || 1;
      if (page > newTotalPages) {
        setPage(newTotalPages);
      } else {
        fetchWebsites(page, pageSize, searchTerm);
      }
      onRefresh();
    } catch (err) {
      setBulkActionMessage({ type: 'error', text: 'Failed to delete selected websites.' });
    } finally {
      setBulkActionLoading(false);
    }
  };

  const handleCheckAll = async () => {
    if (totalItems > 50 && !confirm(`You are about to check ALL ${totalItems} websites. This may take a while. Continue?`)) return;
    try {
      setBulkActionLoading(true);
      setBulkActionMessage(null);
      const res = await checkAllWebsites();
      const data = res.data;
      setBulkActionMessage({
        type: 'success',
        text: `Checked all: ${data.success} success, ${data.failed} failed${data.skipped ? `, ${data.skipped} skipped` : ''}.`,
      });
      setSelectedIds([]);
      await fetchWebsites(page, pageSize, searchTerm);
      onRefresh();
    } catch (err) {
      setBulkActionMessage({ type: 'error', text: 'Failed to check all websites.' });
    } finally {
      setBulkActionLoading(false);
    }
  };

  const handleDeleteAll = async () => {
    if (!confirm(`⚠️ WARNING: You are about to DELETE ALL ${totalItems} websites and all related data. This action is IRREVERSIBLE. Are you sure?`)) return;
    if (!confirm('Final confirmation: Delete ALL websites?')) return;
    try {
      setBulkActionLoading(true);
      setBulkActionMessage(null);
      const res = await deleteAllWebsites();
      setBulkActionMessage({ type: 'success', text: `Deleted all: ${res.data.deletedCount} website(s).` });
      setSelectedIds([]);
      setPage(1);
      await fetchWebsites(1, pageSize, searchTerm);
      onRefresh();
    } catch (err) {
      setBulkActionMessage({ type: 'error', text: 'Failed to delete all websites.' });
    } finally {
      setBulkActionLoading(false);
    }
  };

  // Filter displayed websites by status (client-side on current page)
  const filteredWebsites = websites.filter(site => {
    if (filterStatus === 'all') return true;
    if (filterStatus === 'active') return site.isActive;
    if (filterStatus === 'inactive') return !site.isActive;
    return true;
  });

  const handlePageSizeChange = (newSize) => {
    setPageSize(newSize);
    setPage(1);
  };

  // Compute effective page info
  const effectiveTotalPages = totalPages || 1;
  const hasPreviousPage = page > 1;
  const hasNextPage = page < effectiveTotalPages;

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
        <button className="btn btn-primary" onClick={() => fetchWebsites(page, pageSize, searchTerm)}>
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

      {/* Bulk Action Toolbar */}
      {selectedIds.length > 0 && (
        <div className="bulk-action-toolbar">
          <div className="bulk-action-info">
            <RiCheckboxCircleLine size={16} />
            <span>{selectedIds.length} selected</span>
          </div>
          <div className="bulk-action-buttons">
            <button
              className="btn btn-primary btn-sm"
              onClick={handleBulkCheck}
              disabled={bulkActionLoading}
            >
              <RiPulseLine size={14} style={{ marginRight: '4px' }} />
              {bulkActionLoading ? 'Checking...' : 'Check selected'}
            </button>
            <button
              className="btn btn-danger btn-sm"
              onClick={handleBulkDelete}
              disabled={bulkActionLoading}
            >
              <RiDeleteBinLine size={14} style={{ marginRight: '4px' }} />
              Delete selected
            </button>
            <button
              className="btn btn-ghost btn-sm"
              onClick={() => setSelectedIds([])}
              disabled={bulkActionLoading}
            >
              <RiCloseLine size={14} style={{ marginRight: '4px' }} />
              Clear
            </button>
          </div>
        </div>
      )}

      {/* Global Bulk Actions */}
      <div className="toolbar" style={{ marginBottom: '16px', padding: '8px 12px' }}>
        <div className="toolbar-actions" style={{ gap: '8px' }}>
          <button
            className="btn btn-ghost btn-sm"
            onClick={handleCheckAll}
            disabled={bulkActionLoading || totalItems === 0}
            title="Check all websites in database"
          >
            <RiPulseLine size={14} style={{ marginRight: '4px' }} />
            Check all websites
          </button>
          <button
            className="btn btn-danger btn-sm"
            onClick={handleDeleteAll}
            disabled={bulkActionLoading || totalItems === 0}
            title="Delete all websites in database"
          >
            <RiDeleteBinLine size={14} style={{ marginRight: '4px' }} />
            Delete all websites
          </button>
        </div>
      </div>

      {/* Bulk Action Message */}
      {bulkActionMessage && (
        <div className={`alert ${bulkActionMessage.type === 'error' ? 'alert-error' : 'alert-success'}`}>
          <span>{bulkActionMessage.text}</span>
          <button className="btn btn-ghost btn-sm" onClick={() => setBulkActionMessage(null)}>
            <RiCloseLine size={14} />
          </button>
        </div>
      )}

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
      <p className="text-muted" style={{ fontSize: '12px', marginBottom: '16px' }}>
        {totalItems > 0
          ? `Showing ${(page - 1) * pageSize + 1}–${Math.min(page * pageSize, totalItems)} of ${totalItems} websites`
          : 'No websites found'}
      </p>

      {/* Website Table */}
      <WebsiteTable
        websites={filteredWebsites}
        isAdmin={isAdmin}
        onView={onViewDetail}
        onEdit={handleEditClick}
        onDelete={handleDelete}
        onRunCheck={handleRunCheck}
        checkingWebsiteId={checkingWebsiteId}
        selectedIds={selectedIds}
        onToggleSelect={handleToggleSelect}
        onToggleSelectAll={handleToggleSelectAll}
      />

      {/* Pagination Controls */}
      {totalPages > 0 && (
        <div className="pagination-controls">
          <button
            className="btn btn-ghost btn-sm"
            onClick={() => setPage(page - 1)}
            disabled={!hasPreviousPage || loading}
          >
            Previous
          </button>
          <span className="pagination-info">
            Page {page} of {effectiveTotalPages}
          </span>
          <button
            className="btn btn-ghost btn-sm"
            onClick={() => setPage(page + 1)}
            disabled={!hasNextPage || loading}
          >
            Next
          </button>
          <select
            value={pageSize}
            onChange={e => handlePageSizeChange(Number(e.target.value))}
            className="pagination-size-select"
          >
            {[5, 10, 20, 50].map(size => (
              <option key={size} value={size}>{size} / page</option>
            ))}
          </select>
        </div>
      )}
    </div>
  );
}