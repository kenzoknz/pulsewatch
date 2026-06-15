import { useState, useEffect } from 'react';
import { getAdminStats, getAdminUsers, toggleUserActive } from '../api/pulsewatchApi';

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

export default function AdminPage() {
  const [stats, setStats] = useState(null);
  const [users, setUsers] = useState([]);
  const [loadingUsers, setLoadingUsers] = useState(true);
  const [togglingUserId, setTogglingUserId] = useState(null);
  const [error, setError] = useState('');

  useEffect(() => {
    const fetchUsersData = async () => {
      try {
        const [statsRes, usersRes] = await Promise.all([
          getAdminStats(),
          getAdminUsers(),
        ]);
        setStats(statsRes.data);
        setUsers(usersRes.data);
      } catch {
        setError('Failed to load admin data.');
      } finally {
        setLoadingUsers(false);
      }
    };
    fetchUsersData();
  }, []);

  const handleToggleActive = async (userId, currentIsActive) => {
    setTogglingUserId(userId);
    try {
      const res = await toggleUserActive(userId, !currentIsActive);
      setUsers(prev => prev.map(u => u.id === userId ? res.data : u));
    } catch (err) {
      alert(err.response?.data?.message || 'Failed to update user.');
    } finally {
      setTogglingUserId(null);
    }
  };

  if (loadingUsers && !stats) {
    return (
      <div className="page-loading">
        <div className="loading-spinner"></div>
        <p>Loading admin panel...</p>
      </div>
    );
  }

  if (error) {
    return <div className="error-message">{error}</div>;
  }

  return (
    <div className="admin-page">
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

      <div className="admin-section">
        <div className="admin-table-wrapper">
          <table className="admin-table">
            <thead>
              <tr>
                <th>Username</th>
                <th>Email</th>
                <th>Roles</th>
                <th>Websites</th>
                <th>Joined</th>
                <th>Status</th>
                <th>Action</th>
              </tr>
            </thead>
            <tbody>
              {users.map(user => (
                <tr key={user.id} className={!user.isActive ? 'row-inactive' : ''}>
                  <td className="td-username">{user.username}</td>
                  <td className="td-email">{user.email}</td>
                  <td>
                    {user.roles.map(role => (
                      <span key={role} className={`role-badge role-badge--${role.toLowerCase()}`}>
                        {role}
                      </span>
                    ))}
                  </td>
                  <td className="td-center">{user.totalWebsites}</td>
                  <td className="td-date">{new Date(user.createdAt).toLocaleDateString()}</td>
                  <td>
                    <span className={`status-badge ${user.isActive ? 'status-badge--active' : 'status-badge--inactive'}`}>
                      {user.isActive ? 'Active' : 'Inactive'}
                    </span>
                  </td>
                  <td>
                    {!user.roles.includes('Admin') && (
                      <button
                        id={`btn-toggle-user-${user.id}`}
                        className={`btn btn-sm ${user.isActive ? 'btn-danger' : 'btn-success'}`}
                        onClick={() => handleToggleActive(user.id, user.isActive)}
                        disabled={togglingUserId === user.id}
                      >
                        {togglingUserId === user.id
                          ? '...'
                          : user.isActive ? 'Deactivate' : 'Activate'}
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
