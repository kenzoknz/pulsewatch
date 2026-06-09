import React, { useState, useEffect } from 'react';

export default function WebsiteForm({ initialData, onSubmit, onCancel, isSubmitting }) {
  const [formData, setFormData] = useState({
    name: '',
    url: '',
    checkIntervalSeconds: 300,
    isActive: true,
  });

  const [errors, setErrors] = useState({});

  useEffect(() => {
    if (initialData) {
      setFormData(initialData);
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
      setErrors(prev => ({
        ...prev,
        [name]: '',
      }));
    }
  };

  const handleSubmit = (e) => {
    e.preventDefault();
    if (validateForm()) {
      onSubmit({
        ...formData,
        url: normalizeUrl(formData.url),
      });
    }
  };

  return (
    <form onSubmit={handleSubmit} className="form-panel">
      <h3 className="form-modal-title">
        {initialData?.id ? 'Edit Website' : 'Add Website'}
      </h3>

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

      <div className="form-group">
        <label htmlFor="checkInterval">Check Interval (seconds)</label>
        <input
          type="number"
          id="checkInterval"
          name="checkIntervalSeconds"
          value={formData.checkIntervalSeconds}
          onChange={handleChange}
          min={60}
          max={86400}
        />
        {errors.checkIntervalSeconds && (
          <div className="form-error">{errors.checkIntervalSeconds}</div>
        )}
        <small className="text-muted" style={{ marginTop: '4px', display: 'block' }}>
          Between 60 and 86400 seconds
        </small>
      </div>

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
