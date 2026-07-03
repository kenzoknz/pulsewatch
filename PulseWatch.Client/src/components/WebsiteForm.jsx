import React, { useState, useEffect } from 'react';
import { RiSettings3Line, RiArrowUpSLine, RiArrowDownSLine } from 'react-icons/ri';

const INTERVAL_PRESETS = [
  { label: '1 minute', value: 60 },
  { label: '5 minutes', value: 300 },
  { label: '15 minutes', value: 900 },
  { label: '30 minutes', value: 1800 },
  { label: '1 hour', value: 3600 },
];

/** Parse JSON string of headers into [{key,value}] array for editing */
function parseHeaders(json) {
  if (!json) return [];
  try {
    const obj = JSON.parse(json);
    return Object.entries(obj).map(([key, value]) => ({ key, value }));
  } catch {
    return [];
  }
}

/** Serialize [{key,value}] array to JSON string (only non-empty pairs) */
function serializeHeaders(rows) {
  const obj = {};
  rows.forEach(({ key, value }) => {
    if (key.trim()) obj[key.trim()] = value;
  });
  return Object.keys(obj).length > 0 ? JSON.stringify(obj) : null;
}

export default function WebsiteForm({ initialData, onSubmit, onCancel, isSubmitting }) {
  const [formData, setFormData] = useState({
    name: '',
    url: '',
    checkIntervalSeconds: 300,
    isActive: true,
    isPublic: false,
    httpMethod: 'GET',
    customHeadersJson: null,
    responseBodyKeyword: '',
  });

  const [errors, setErrors] = useState({});
  const [advancedOpen, setAdvancedOpen] = useState(false);
  const [headerRows, setHeaderRows] = useState([{ key: '', value: '' }]);

  useEffect(() => {
    if (initialData) {
      setFormData({
        name: initialData.name || '',
        url: initialData.url || '',
        checkIntervalSeconds: initialData.checkIntervalSeconds || 300,
        isActive: initialData.isActive !== undefined ? initialData.isActive : true,
        isPublic: initialData.isPublic || false,
        httpMethod: initialData.httpMethod || 'GET',
        customHeadersJson: initialData.customHeadersJson || null,
        responseBodyKeyword: initialData.responseBodyKeyword || '',
      });
      const parsed = parseHeaders(initialData.customHeadersJson);
      setHeaderRows(parsed.length > 0 ? parsed : [{ key: '', value: '' }]);
      // Auto-open advanced if non-default values present
      if (initialData.httpMethod !== 'GET' || initialData.customHeadersJson || initialData.responseBodyKeyword || initialData.isPublic) {
        setAdvancedOpen(true);
      }
    }
  }, [initialData]);

  const normalizeUrl = (value) => {
    const trimmed = value.trim();
    if (!trimmed) return '';
    return /^https?:\/\//i.test(trimmed) ? trimmed : `https://${trimmed}`;
  };

  const validateForm = () => {
    const newErrors = {};
    if (!formData.name || formData.name.trim().length === 0) {
      newErrors.name = 'Name is required';
    } else if (formData.name.length > 200) {
      newErrors.name = 'Name must be 200 characters or less';
    }
    if (!formData.url || formData.url.trim().length === 0) {
      newErrors.url = 'URL is required';
    } else {
      try {
        const normalizedUrl = normalizeUrl(formData.url);
        const parsedUrl = new URL(normalizedUrl);
        if (!parsedUrl.hostname.includes('.')) {
          newErrors.url = 'Please enter a valid domain, e.g. google.com';
        }
      } catch {
        newErrors.url = 'Please enter a valid URL or domain, e.g. google.com';
      }
    }
    const interval = parseInt(formData.checkIntervalSeconds, 10);
    if (!interval || interval < 60 || interval > 86400) {
      newErrors.checkIntervalSeconds = 'Interval must be between 60 and 86400 seconds';
    }
    // Validate keyword is not needed when method is HEAD
    if (formData.httpMethod === 'HEAD' && formData.responseBodyKeyword?.trim()) {
      newErrors.responseBodyKeyword = 'Keyword assertion does not work with HEAD method (no response body)';
    }
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleChange = (e) => {
    const { name, value, type, checked } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: type === 'checkbox' ? checked : value,
    }));
    if (errors[name]) {
      setErrors(prev => ({ ...prev, [name]: '' }));
    }
  };

  const handleHeaderChange = (index, field, value) => {
    setHeaderRows(prev => {
      const updated = [...prev];
      updated[index] = { ...updated[index], [field]: value };
      return updated;
    });
  };

  const addHeaderRow = () => {
    if (headerRows.length >= 10) return;
    setHeaderRows(prev => [...prev, { key: '', value: '' }]);
  };

  const removeHeaderRow = (index) => {
    setHeaderRows(prev => prev.filter((_, i) => i !== index));
  };

  const handleSubmit = (e) => {
    e.preventDefault();
    if (validateForm()) {
      const payload = {
        ...formData,
        url: normalizeUrl(formData.url),
        customHeadersJson: serializeHeaders(headerRows),
        responseBodyKeyword: formData.responseBodyKeyword?.trim() || null,
      };
      onSubmit(payload);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="form-panel">
      <h3 className="form-modal-title">
        {initialData?.id ? 'Edit Website' : 'Add Website'}
      </h3>

      {/* Name */}
      <div className="form-group">
        <label htmlFor="name">Website Name</label>
        <input
          type="text"
          id="name"
          name="name"
          value={formData.name}
          onChange={handleChange}
          placeholder="e.g., My API"
          maxLength={200}
        />
        {errors.name && <div className="form-error">{errors.name}</div>}
      </div>

      {/* URL */}
      <div className="form-group">
        <label htmlFor="url">URL</label>
        <input
          type="text"
          id="url"
          name="url"
          value={formData.url}
          onChange={handleChange}
          placeholder="e.g., google.com, x.com, dut.udn.vn"
        />
        {errors.url && <div className="form-error">{errors.url}</div>}
      </div>

      {/* Check Interval */}
      <div className="form-group">
        <label htmlFor="checkInterval">Check Interval</label>
        <select
          id="checkInterval"
          name="checkIntervalSeconds"
          value={formData.checkIntervalSeconds}
          onChange={handleChange}
        >
          {INTERVAL_PRESETS.map(p => (
            <option key={p.value} value={p.value}>{p.label}</option>
          ))}
          <option value="custom">Custom…</option>
        </select>
        {/* Show number input if value doesn't match any preset */}
        {!INTERVAL_PRESETS.some(p => p.value === Number(formData.checkIntervalSeconds)) && (
          <input
            type="number"
            name="checkIntervalSeconds"
            value={formData.checkIntervalSeconds}
            onChange={handleChange}
            min={60}
            max={86400}
            style={{ marginTop: '8px' }}
          />
        )}
        {errors.checkIntervalSeconds && (
          <div className="form-error">{errors.checkIntervalSeconds}</div>
        )}
        <small className="text-muted" style={{ marginTop: '4px', display: 'block' }}>
          Between 60 and 86400 seconds
        </small>
      </div>

      {/* Active toggle (edit only) */}
      {initialData?.id && (
        <div className="form-group">
          <label htmlFor="isActive" style={{ display: 'flex', alignItems: 'center', gap: '8px', cursor: 'pointer', marginBottom: 0 }}>
            <input
              type="checkbox"
              id="isActive"
              name="isActive"
              checked={formData.isActive}
              onChange={handleChange}
            />
            <span>Active</span>
          </label>
        </div>
      )}

      {/* ── Advanced Options ── */}
      <div className="advanced-section">
        <button
          type="button"
          className="advanced-toggle"
          onClick={() => setAdvancedOpen(o => !o)}
          aria-expanded={advancedOpen}
        >
          <span style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
            <RiSettings3Line size={16} /> Advanced Options
          </span>
          <span className="advanced-toggle-arrow">
            {advancedOpen ? <RiArrowUpSLine size={16} /> : <RiArrowDownSLine size={16} />}
          </span>
        </button>

        {advancedOpen && (
          <div className="advanced-body">
            {/* HTTP Method */}
            <div className="form-group">
              <label htmlFor="httpMethod">HTTP Method</label>
              <select
                id="httpMethod"
                name="httpMethod"
                value={formData.httpMethod}
                onChange={handleChange}
              >
                <option value="GET">GET – Standard check (recommended)</option>
                <option value="HEAD">HEAD – Faster, no response body</option>
                <option value="POST">POST – For APIs that require POST</option>
              </select>
              <small className="text-muted" style={{ marginTop: '4px', display: 'block' }}>
                HEAD method is fastest but cannot use keyword assertion.
              </small>
            </div>

            {/* Response Keyword Assertion */}
            <div className="form-group">
              <label htmlFor="responseBodyKeyword">Response Keyword Assertion</label>
              <input
                type="text"
                id="responseBodyKeyword"
                name="responseBodyKeyword"
                value={formData.responseBodyKeyword || ''}
                onChange={handleChange}
                placeholder='e.g., "status":"ok" or "healthy"'
                maxLength={500}
                disabled={formData.httpMethod === 'HEAD'}
              />
              {errors.responseBodyKeyword && <div className="form-error">{errors.responseBodyKeyword}</div>}
              <small className="text-muted" style={{ marginTop: '4px', display: 'block' }}>
                If set, the site will be marked <strong>DOWN</strong> even on HTTP 200 if this keyword is not found in the response body.
                {formData.httpMethod === 'HEAD' && ' (Disabled — HEAD requests have no response body.)'}
              </small>
            </div>

            {/* Custom Headers */}
            <div className="form-group">
              <label>Custom Request Headers</label>
              {headerRows.map((row, i) => (
                <div key={i} style={{ display: 'flex', gap: '8px', marginBottom: '8px', alignItems: 'center' }}>
                  <input
                    type="text"
                    value={row.key}
                    onChange={e => handleHeaderChange(i, 'key', e.target.value)}
                    placeholder="Header name"
                    style={{ flex: '1' }}
                    maxLength={100}
                  />
                  <input
                    type="text"
                    value={row.value}
                    onChange={e => handleHeaderChange(i, 'value', e.target.value)}
                    placeholder="Value"
                    style={{ flex: '2' }}
                    maxLength={256}
                  />
                  <button
                    type="button"
                    onClick={() => removeHeaderRow(i)}
                    style={{ background: 'rgba(239,68,68,0.1)', border: '1px solid rgba(239,68,68,0.2)', color: '#ef4444', borderRadius: '6px', padding: '6px 10px', cursor: 'pointer', fontSize: '14px' }}
                    title="Remove header"
                  >✕</button>
                </div>
              ))}
              {headerRows.length < 10 && (
                <button type="button" className="btn btn-secondary btn-sm" onClick={addHeaderRow} style={{ marginTop: '4px' }}>
                  + Add Header
                </button>
              )}
              <small className="text-muted" style={{ marginTop: '6px', display: 'block' }}>
                Add custom headers like <code>Authorization</code>, <code>X-API-Key</code>, or <code>User-Agent</code>. Max 10 headers.
              </small>
            </div>

            {/* Make Public */}
            <div className="form-group">
              <label
                htmlFor="isPublic"
                style={{ display: 'flex', alignItems: 'flex-start', gap: '10px', cursor: 'pointer', marginBottom: 0 }}
              >
                <input
                  type="checkbox"
                  id="isPublic"
                  name="isPublic"
                  checked={formData.isPublic}
                  onChange={handleChange}
                  style={{ marginTop: '3px', flexShrink: 0 }}
                />
                <div>
                  <span style={{ color: 'var(--text-primary)', fontWeight: 600 }}>Make Public Status Page</span>
                  <small className="text-muted" style={{ display: 'block', marginTop: '2px' }}>
                    Enables a publicly accessible status page at <code>/status/{initialData?.id || '{id}'}</code> and an embeddable SVG badge.
                    No login required for viewers.
                  </small>
                </div>
              </label>
            </div>
          </div>
        )}
      </div>

      <div className="form-actions">
        <button type="button" className="btn btn-secondary" onClick={onCancel} disabled={isSubmitting}>
          Cancel
        </button>
        <button type="submit" className="btn btn-primary" disabled={isSubmitting}>
          {isSubmitting ? 'Saving...' : initialData?.id ? 'Save Changes' : 'Create Website'}
        </button>
      </div>
    </form>
  );
}
