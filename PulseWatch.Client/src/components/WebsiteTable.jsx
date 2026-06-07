import React from 'react';
import { RiEyeLine, RiEditLine, RiDeleteBinLine, RiGlobalLine } from 'react-icons/ri';

const formatDate = (dateString) => {
  if (!dateString) return '—';
  try {
    const date = new Date(dateString);
    return new Intl.DateTimeFormat('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    }).format(date);
  } catch {
    return '—';
  }
};

export default function WebsiteTable({ websites, onView, onEdit, onDelete }) {
  if (!websites || websites.length === 0) {
    return (
      <div className="empty-state">
        <div className="empty-state-icon"><RiGlobalLine size={48} /></div>
        <h3 className="empty-state-title">No websites yet</h3>
        <p className="empty-state-description">
          Start monitoring your first endpoint to see uptime data here.
        </p>
      </div>
    );
  }

  return (
    <div className="table-wrap">
      <table>
        <thead>
          <tr>
            <th>Status</th>
            <th>Name</th>
            <th>URL</th>
            <th>Interval</th>
            <th>Created</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          {websites.map(site => (
            <tr
              key={site.id}
              className={site.isActive ? 'check-row-online' : 'check-row-offline'}
            >
              <td data-label="Status">
                <span className={`status-badge ${site.isActive ? 'status-online' : 'status-offline'}`}>
                  <span className="status-dot" />
                  {site.isActive ? 'Active' : 'Inactive'}
                </span>
              </td>
              <td data-label="Name">{site.name}</td>
              <td data-label="URL">
                <a
                  href={site.url}
                  target="_blank"
                  rel="noopener noreferrer"
                  style={{ fontSize: '12px', wordBreak: 'break-all' }}
                >
                  {site.url}
                </a>
              </td>
              <td data-label="Interval">{site.checkIntervalMinutes} min</td>
              <td data-label="Created">{formatDate(site.createdAt)}</td>
              <td data-label="Actions">
                <div className="flex gap-2" style={{ justifyContent: 'flex-end' }}>
                  <button
                    className="btn btn-ghost btn-sm"
                    onClick={() => onView(site.id)}
                    title="View details"
                  >
                    <RiEyeLine size={16} />
                  </button>
                  <button
                    className="btn btn-ghost btn-sm"
                    onClick={() => onEdit(site)}
                    title="Edit"
                  >
                    <RiEditLine size={16} />
                  </button>
                  <button
                    className="btn btn-ghost btn-sm"
                    onClick={() => {
                      if (confirm(`Delete "${site.name}"? This cannot be undone.`)) {
                        onDelete(site.id);
                      }
                    }}
                    title="Delete"
                  >
                    <RiDeleteBinLine size={16} />
                  </button>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
