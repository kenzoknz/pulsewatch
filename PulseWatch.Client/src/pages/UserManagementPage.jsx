import { useState, useEffect, useRef } from 'react';
import {
  getAdminStats,
  getAdminUsers,
  toggleUserActive,
  createAdminUser,
  updateAdminUser,
  deleteAdminUser,
} from '../api/pulsewatchApi';
import {
  RiUserAddLine,
  RiEditLine,
  RiDeleteBinLine,
  RiShieldUserLine,
  RiUserLine,
  RiCheckLine,
  RiCloseLine,
  RiEyeLine,
  RiEyeOffLine,
  RiSearch2Line,
  RiRefreshLine,
} from 'react-icons/ri';

const ROLES = ['User', 'Admin'];

const formatDate = (dateString) => {
  if (!dateString) return '—';
  try {
    return new Intl.DateTimeFormat('vi-VN', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    }).format(new Date(dateString));
  } catch {
    return '—';
  }
};

// ─── Modal backdrop ───────────────────────────────────────────────────────────
function Modal({ title, onClose, children }) {
  // Close on Escape key
  useEffect(() => {
    const handler = (e) => { if (e.key === 'Escape') onClose(); };
    window.addEventListener('keydown', handler);
    return () => window.removeEventListener('keydown', handler);
  }, [onClose]);

  return (
    <div
      className="um-modal-overlay"
      onClick={(e) => { if (e.target === e.currentTarget) onClose(); }}
    >
      <div className="um-modal" role="dialog" aria-modal="true">
        <div className="um-modal-header">
          <h3>{title}</h3>
          <button className="um-modal-close" onClick={onClose} aria-label="Close"><RiCloseLine size={20} /></button>
        </div>
        {children}
      </div>
    </div>
  );
}

// ─── Password field with toggle ───────────────────────────────────────────────
function PasswordField({ id, value, onChange, placeholder = 'Password', required }) {
  const [visible, setVisible] = useState(false);
  return (
    <div className="um-input-icon-wrapper">
      <input
        id={id}
        type={visible ? 'text' : 'password'}
        className="um-input"
        value={value}
        onChange={onChange}
        placeholder={placeholder}
        required={required}
        autoComplete="new-password"
      />
      <button
        type="button"
        className="um-input-icon-btn"
        onClick={() => setVisible(v => !v)}
        tabIndex={-1}
        aria-label={visible ? 'Hide password' : 'Show password'}
      >
        {visible ? <RiEyeOffLine size={16} /> : <RiEyeLine size={16} />}
      </button>
    </div>
  );
}

// ─── Create / Edit form modal ─────────────────────────────────────────────────
function UserFormModal({ editUser, onClose, onSave }) {
  const isEdit = !!editUser;
  const [form, setForm] = useState({
    username: editUser?.username ?? '',
    email: editUser?.email ?? '',
    password: '',
    role: editUser?.roles?.[0] ?? 'User',
    isActive: editUser?.isActive ?? true,
  });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const firstRef = useRef(null);

  useEffect(() => { firstRef.current?.focus(); }, []);

  const set = (field) => (e) =>
    setForm(f => ({ ...f, [field]: e.target.type === 'checkbox' ? e.target.checked : e.target.value }));

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setSaving(true);
    try {
      let result;
      if (isEdit) {
        const payload = {
          username: form.username || undefined,
          email: form.email || undefined,
          isActive: form.isActive,
          role: form.role || undefined,
        };
        if (form.password) payload.password = form.password;
        result = await updateAdminUser(editUser.id, payload);
      } else {
        result = await createAdminUser({
          username: form.username,
          email: form.email,
          password: form.password,
          role: form.role,
          isActive: form.isActive,
        });
      }
      onSave(result.data, isEdit);
    } catch (err) {
      const msg = err.response?.data?.message || (isEdit ? 'Failed to update user.' : 'Failed to create user.');
      const errs = err.response?.data?.errors;
      setError(errs ? `${msg}: ${errs.join(', ')}` : msg);
    } finally {
      setSaving(false);
    }
  };

  return (
    <Modal title={isEdit ? 'Edit User' : 'Create New User'} onClose={onClose}>
      <form className="um-form" onSubmit={handleSubmit} noValidate>
        {error && <div className="um-form-error">{error}</div>}

        <div className="um-form-row">
          <div className="um-form-group">
            <label htmlFor="um-username">Username</label>
            <input
              ref={firstRef}
              id="um-username"
              className="um-input"
              type="text"
              value={form.username}
              onChange={set('username')}
              placeholder="john_doe"
              required={!isEdit}
            />
          </div>
          <div className="um-form-group">
            <label htmlFor="um-email">Email</label>
            <input
              id="um-email"
              className="um-input"
              type="email"
              value={form.email}
              onChange={set('email')}
              placeholder="john@example.com"
              required={!isEdit}
            />
          </div>
        </div>

        <div className="um-form-row">
          <div className="um-form-group">
            <label htmlFor="um-password">
              {isEdit ? 'New Password' : 'Password'}
              {isEdit && <span className="um-label-hint"> (leave blank to keep)</span>}
            </label>
            <PasswordField
              id="um-password"
              value={form.password}
              onChange={set('password')}
              placeholder={isEdit ? 'Leave blank to keep' : 'Min. 8 characters'}
              required={!isEdit}
            />
          </div>
          <div className="um-form-group">
            <label htmlFor="um-role">Role</label>
            <select
              id="um-role"
              className="um-input um-select"
              value={form.role}
              onChange={set('role')}
              disabled={editUser?.roles?.includes('Admin')}
            >
              {ROLES.map(r => <option key={r} value={r}>{r}</option>)}
            </select>
            {editUser?.roles?.includes('Admin') && (
              <span className="um-label-hint">Admin role cannot be changed</span>
            )}
          </div>
        </div>

        <div className="um-form-group um-toggle-group">
          <label className="um-toggle-label" htmlFor="um-is-active">
            <div className="um-toggle-text">
              <span>Active Status</span>
              <span className="um-label-hint">Inactive users cannot log in</span>
            </div>
            <div className={`um-toggle ${form.isActive ? 'um-toggle--on' : ''}`}>
              <input
                id="um-is-active"
                type="checkbox"
                checked={form.isActive}
                onChange={set('isActive')}
                disabled={editUser?.roles?.includes('Admin')}
              />
              <span className="um-toggle-thumb" />
            </div>
          </label>
        </div>

        <div className="um-form-actions">
          <button type="button" className="btn btn-ghost" onClick={onClose} disabled={saving}>
            Cancel
          </button>
          <button type="submit" className="btn btn-primary" id="um-submit-btn" disabled={saving}>
            {saving ? 'Saving…' : isEdit ? 'Save Changes' : 'Create User'}
          </button>
        </div>
      </form>
    </Modal>
  );
}

// ─── Confirm delete dialog ────────────────────────────────────────────────────
function DeleteConfirmModal({ user, onClose, onConfirm }) {
  const [deleting, setDeleting] = useState(false);
  const [error, setError] = useState('');

  const handleDelete = async () => {
    setDeleting(true);
    setError('');
    try {
      await onConfirm(user.id);
      onClose();
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to delete user.');
      setDeleting(false);
    }
  };

  return (
    <Modal title="Delete User" onClose={onClose}>
      <div className="um-confirm-body">
        <div className="um-confirm-icon um-confirm-icon--danger">
          <RiDeleteBinLine size={28} />
        </div>
        <p className="um-confirm-text">
          Are you sure you want to permanently delete{' '}
          <strong>{user.username}</strong>?
          <br />
          <span className="um-label-hint">This action cannot be undone.</span>
        </p>
        {error && <div className="um-form-error">{error}</div>}
        <div className="um-form-actions">
          <button className="btn btn-ghost" onClick={onClose} disabled={deleting}>Cancel</button>
          <button
            id={`um-delete-confirm-${user.id}`}
            className="btn btn-danger"
            onClick={handleDelete}
            disabled={deleting}
          >
            {deleting ? 'Deleting…' : 'Delete'}
          </button>
        </div>
      </div>
    </Modal>
  );
}

// ─── Main Page ────────────────────────────────────────────────────────────────
export default function UserManagementPage() {
  const [stats, setStats] = useState(null);
  const [users, setUsers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [search, setSearch] = useState('');
  const [filterRole, setFilterRole] = useState('all');
  const [filterStatus, setFilterStatus] = useState('all');

  // Modal state
  const [showCreate, setShowCreate] = useState(false);
  const [editUser, setEditUser] = useState(null);
  const [deleteUser, setDeleteUser] = useState(null);
  const [togglingId, setTogglingId] = useState(null);

  const fetchData = async () => {
    setLoading(true);
    setError('');
    try {
      const [statsRes, usersRes] = await Promise.all([getAdminStats(), getAdminUsers()]);
      setStats(statsRes.data);
      setUsers(usersRes.data);
    } catch {
      setError('Failed to load data.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { fetchData(); }, []);

  // ── Handlers ──
  const handleSave = (savedUser, isEdit) => {
    setUsers(prev =>
      isEdit
        ? prev.map(u => u.id === savedUser.id ? savedUser : u)
        : [savedUser, ...prev]
    );
    setShowCreate(false);
    setEditUser(null);
  };

  const handleDelete = async (userId) => {
    await deleteAdminUser(userId);
    setUsers(prev => prev.filter(u => u.id !== userId));
  };

  const handleToggleActive = async (user) => {
    setTogglingId(user.id);
    try {
      const res = await toggleUserActive(user.id, !user.isActive);
      setUsers(prev => prev.map(u => u.id === user.id ? res.data : u));
    } catch (err) {
      alert(err.response?.data?.message || 'Failed to update user.');
    } finally {
      setTogglingId(null);
    }
  };

  // ── Filtering ──
  const filtered = users.filter(u => {
    const q = search.toLowerCase();
    const matchSearch =
      !q ||
      u.username.toLowerCase().includes(q) ||
      u.email.toLowerCase().includes(q);

    const matchRole =
      filterRole === 'all' ||
      u.roles.map(r => r.toLowerCase()).includes(filterRole.toLowerCase());

    const matchStatus =
      filterStatus === 'all' ||
      (filterStatus === 'active' && u.isActive) ||
      (filterStatus === 'inactive' && !u.isActive);

    return matchSearch && matchRole && matchStatus;
  });

  if (loading) {
    return (
      <div className="page-loading">
        <div className="loading-spinner" />
        <p>Loading users…</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="um-error-state">
        <p>{error}</p>
        <button className="btn btn-primary" onClick={fetchData}>Retry</button>
      </div>
    );
  }

  return (
    <>
      {/* ── Stats ── */}
      <div className="admin-stats-grid">
        <div className="admin-stat-card">
          <div className="stat-value">{stats?.totalUsers ?? 0}</div>
          <div className="stat-label">Total Users</div>
        </div>
        <div className="admin-stat-card">
          <div className="stat-value">{stats?.activeUsers ?? 0}</div>
          <div className="stat-label">Active Users</div>
        </div>
        <div className="admin-stat-card">
          <div className="stat-value">{stats?.totalWebsites ?? 0}</div>
          <div className="stat-label">Total Websites</div>
        </div>
        <div className="admin-stat-card">
          <div className="stat-value">{stats?.activeWebsites ?? 0}</div>
          <div className="stat-label">Active Websites</div>
        </div>
        <div className="admin-stat-card">
          <div className="stat-value">{stats?.totalUptimeChecksToday ?? 0}</div>
          <div className="stat-label">Checks Today</div>
        </div>
        <div className="admin-stat-card admin-stat-card--alert">
          <div className="stat-value">{stats?.totalDowntimeEventsOpen ?? 0}</div>
          <div className="stat-label">Open Downtime Events</div>
        </div>
      </div>

      {/* ── Toolbar ── */}
      <div className="um-toolbar">
        <div className="um-toolbar-left">
          <div className="um-search-wrapper">
            <RiSearch2Line className="um-search-icon" size={16} />
            <input
              id="um-search"
              className="um-input um-search-input"
              type="text"
              placeholder="Search by username or email…"
              value={search}
              onChange={e => setSearch(e.target.value)}
            />
          </div>

          <select
            id="um-filter-role"
            className="um-input um-select um-select--sm"
            value={filterRole}
            onChange={e => setFilterRole(e.target.value)}
          >
            <option value="all">All Roles</option>
            {ROLES.map(r => <option key={r} value={r}>{r}</option>)}
          </select>

          <select
            id="um-filter-status"
            className="um-input um-select um-select--sm"
            value={filterStatus}
            onChange={e => setFilterStatus(e.target.value)}
          >
            <option value="all">All Status</option>
            <option value="active">Active</option>
            <option value="inactive">Inactive</option>
          </select>

          <button
            className="btn btn-ghost btn-icon"
            onClick={fetchData}
            title="Refresh"
            id="um-refresh-btn"
          >
            <RiRefreshLine size={16} />
          </button>
        </div>

        <div className="um-toolbar-right">
          <span className="um-count-badge">{filtered.length} user{filtered.length !== 1 ? 's' : ''}</span>
          <button
            id="um-create-user-btn"
            className="btn btn-primary"
            onClick={() => setShowCreate(true)}
          >
            <RiUserAddLine size={16} />
            <span>New User</span>
          </button>
        </div>
      </div>

      {/* ── Table ── */}
      <div className="admin-section">
        <div className="admin-table-wrapper">
          {filtered.length === 0 ? (
            <div className="um-empty-state">
              <RiUserLine size={40} />
              <p>No users found</p>
            </div>
          ) : (
            <table className="admin-table">
              <thead>
                <tr>
                  <th>Username</th>
                  <th>Email</th>
                  <th>Role</th>
                  <th className="td-center">Websites</th>
                  <th>Joined</th>
                  <th>Status</th>
                  <th className="td-center">Actions</th>
                </tr>
              </thead>
              <tbody>
                {filtered.map(user => (
                  <tr key={user.id} className={!user.isActive ? 'row-inactive' : ''}>
                    <td className="td-username">
                      <div className="um-user-cell">
                        <div className="um-avatar">{user.username.charAt(0).toUpperCase()}</div>
                        <span>{user.username}</span>
                      </div>
                    </td>
                    <td className="td-email">{user.email}</td>
                    <td>
                      {user.roles.map(role => (
                        <span
                          key={role}
                          className={`role-badge role-badge--${role.toLowerCase()}`}
                        >
                          {role === 'Admin' ? <RiShieldUserLine size={11} /> : <RiUserLine size={11} />}
                          {role}
                        </span>
                      ))}
                    </td>
                    <td className="td-center">{user.totalWebsites}</td>
                    <td className="td-date">{formatDate(user.createdAt)}</td>
                    <td>
                      <span className={`status-badge ${user.isActive ? 'status-badge--active' : 'status-badge--inactive'}`}>
                        {user.isActive ? <RiCheckLine size={11} /> : <RiCloseLine size={11} />}
                        {user.isActive ? 'Active' : 'Inactive'}
                      </span>
                    </td>
                    <td>
                      <div className="um-actions">
                        {/* Toggle active – only non-admin */}
                        {!user.roles.includes('Admin') && (
                          <button
                            id={`um-toggle-${user.id}`}
                            className={`btn btn-sm ${user.isActive ? 'btn-warning' : 'btn-success'}`}
                            onClick={() => handleToggleActive(user)}
                            disabled={togglingId === user.id}
                            title={user.isActive ? 'Deactivate' : 'Activate'}
                          >
                            {togglingId === user.id ? '…' : user.isActive ? 'Deactivate' : 'Activate'}
                          </button>
                        )}

                        {/* Edit */}
                        <button
                          id={`um-edit-${user.id}`}
                          className="btn btn-sm btn-ghost btn-icon-only"
                          onClick={() => setEditUser(user)}
                          title="Edit user"
                        >
                          <RiEditLine size={15} />
                        </button>

                        {/* Delete – only non-admin */}
                        {!user.roles.includes('Admin') && (
                          <button
                            id={`um-delete-${user.id}`}
                            className="btn btn-sm btn-danger btn-icon-only"
                            onClick={() => setDeleteUser(user)}
                            title="Delete user"
                          >
                            <RiDeleteBinLine size={15} />
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </div>

      {/* ── Modals ── */}
      {(showCreate || editUser) && (
        <UserFormModal
          editUser={editUser}
          onClose={() => { setShowCreate(false); setEditUser(null); }}
          onSave={handleSave}
        />
      )}

      {deleteUser && (
        <DeleteConfirmModal
          user={deleteUser}
          onClose={() => setDeleteUser(null)}
          onConfirm={handleDelete}
        />
      )}
    </>
  );
}
