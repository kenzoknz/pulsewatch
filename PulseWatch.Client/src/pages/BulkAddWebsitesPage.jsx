import React, { useMemo, useState } from 'react';
import { bulkCreateWebsites } from '../api/pulsewatchApi';
import { RiAlertLine, RiCheckboxCircleLine, RiCloseCircleLine, RiUploadCloudLine } from 'react-icons/ri';

export default function BulkAddWebsitesPage({ onCompleted }) {
  const [rawUrls, setRawUrls] = useState('');
  const [defaultCheckIntervalMinutes, setDefaultCheckIntervalMinutes] = useState(5);
  const [nameStrategy, setNameStrategy] = useState('auto');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [result, setResult] = useState(null);
  const [error, setError] = useState(null);

  const preview = useMemo(() => {
    const lines = rawUrls.split('\n');
    const trimmed = lines.map(url => url.trim());
    const urls = trimmed.filter(Boolean);
    const emptyLines = trimmed.length - urls.length;
    const seen = new Set();
    let duplicates = 0;

    urls.forEach(url => {
      const key = url.toLowerCase();
      if (seen.has(key)) {
        duplicates += 1;
      } else {
        seen.add(key);
      }
    });

    return {
      totalLines: lines.length,
      urls,
      urlCount: urls.length,
      emptyLines,
      duplicates,
      uniqueCount: seen.size,
    };
  }, [rawUrls]);

  const handleSubmit = async (event) => {
    event.preventDefault();

    if (preview.urls.length === 0) {
      setError('Please enter at least one URL.');
      setResult(null);
      return;
    }

    try {
      setIsSubmitting(true);
      setError(null);
      setResult(null);

      const response = await bulkCreateWebsites({
        urls: preview.urls,
        defaultCheckIntervalMinutes: Number(defaultCheckIntervalMinutes),
        nameStrategy,
      });

      setResult(response.data);
      onCompleted?.();
    } catch (err) {
      setError('Failed to bulk add websites. Please try again.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div>
      <div className="website-list-header">
        <h1 className="website-list-title">Bulk Add Websites</h1>
        <p className="text-muted">
          Paste one URL per line to add multiple websites at once.
        </p>
      </div>

      <form className="bulk-add-card" onSubmit={handleSubmit}>
        <div className="form-group">
          <label htmlFor="bulk-urls">Website URLs</label>
          <textarea
            id="bulk-urls"
            className="bulk-add-textarea"
            value={rawUrls}
            onChange={event => setRawUrls(event.target.value)}
            placeholder={'https://google.com\nhttps://github.com\nexample.com'}
            rows={10}
          />
        </div>

        <div className="bulk-add-options">
          <div className="form-group">
            <label htmlFor="bulk-interval">Default check interval minutes</label>
            <input
              id="bulk-interval"
              type="number"
              min="1"
              max="1440"
              value={defaultCheckIntervalMinutes}
              onChange={event => setDefaultCheckIntervalMinutes(event.target.value)}
            />
          </div>

          <div className="form-group">
            <label htmlFor="bulk-name-strategy">Name strategy</label>
            <select
              id="bulk-name-strategy"
              value={nameStrategy}
              onChange={event => setNameStrategy(event.target.value)}
            >
              <option value="auto">Auto: page title, fallback to domain</option>
              <option value="title">Page title</option>
              <option value="url">Domain / URL</option>
            </select>
          </div>
        </div>

        <div className="bulk-add-preview">
          <div>
            <span className="bulk-add-preview-value">{preview.urlCount}</span>
            <span className="bulk-add-preview-label">URLs</span>
          </div>
          <div>
            <span className="bulk-add-preview-value">{preview.uniqueCount}</span>
            <span className="bulk-add-preview-label">Unique</span>
          </div>
          <div>
            <span className="bulk-add-preview-value">{preview.duplicates}</span>
            <span className="bulk-add-preview-label">Duplicates</span>
          </div>
          <div>
            <span className="bulk-add-preview-value">{preview.emptyLines}</span>
            <span className="bulk-add-preview-label">Empty lines</span>
          </div>
        </div>

        {error && (
          <div className="alert alert-error">
            <RiAlertLine />
            <span>{error}</span>
          </div>
        )}

        <button
          className="btn btn-primary"
          type="submit"
          disabled={isSubmitting}
        >
          <RiUploadCloudLine style={{ marginRight: '6px' }} />
          {isSubmitting ? 'Adding Websites...' : 'Add Websites'}
        </button>
      </form>

      {result && (
        <div className="bulk-add-result">
          <h2>Bulk Add Result</h2>

          <div className="bulk-add-summary">
            <div>
              <span className="bulk-add-preview-value">{result.summary?.total ?? 0}</span>
              <span className="bulk-add-preview-label">Total</span>
            </div>
            <div>
              <span className="bulk-add-preview-value success">{result.summary?.created ?? 0}</span>
              <span className="bulk-add-preview-label">Created</span>
            </div>
            <div>
              <span className="bulk-add-preview-value warning">{result.summary?.skipped ?? 0}</span>
              <span className="bulk-add-preview-label">Skipped</span>
            </div>
            <div>
              <span className="bulk-add-preview-value danger">{result.summary?.failed ?? 0}</span>
              <span className="bulk-add-preview-label">Failed</span>
            </div>
          </div>

          {result.created?.length > 0 && (
            <div className="bulk-add-result-section">
              <h3><RiCheckboxCircleLine /> Created</h3>
              <ul>
                {result.created.map(site => (
                  <li key={site.id}>
                    <strong>{site.name}</strong> — {site.url}
                  </li>
                ))}
              </ul>
            </div>
          )}

          {result.skipped?.length > 0 && (
            <div className="bulk-add-result-section">
              <h3><RiAlertLine /> Skipped</h3>
              <ul>
                {result.skipped.map((item, index) => (
                  <li key={`${item.url}-${index}`}>
                    <strong>{item.url}</strong> — {item.reason}
                  </li>
                ))}
              </ul>
            </div>
          )}

          {result.failed?.length > 0 && (
            <div className="bulk-add-result-section">
              <h3><RiCloseCircleLine /> Failed</h3>
              <ul>
                {result.failed.map((item, index) => (
                  <li key={`${item.url}-${index}`}>
                    <strong>{item.url}</strong> — {item.reason}
                  </li>
                ))}
              </ul>
            </div>
          )}
        </div>
      )}
    </div>
  );
}