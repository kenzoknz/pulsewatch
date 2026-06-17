import { useState, useEffect, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  RiNotification3Line,
  RiCheckDoubleLine,
  RiDeleteBin7Line,
  RiErrorWarningLine,
  RiCheckboxCircleLine,
} from 'react-icons/ri';
import useNotifications from '../hooks/useNotifications';
import './NotificationCenter.css';

export default function NotificationCenter() {
  const [isOpen, setIsOpen] = useState(false);
  const dropdownRef = useRef(null);
  const navigate = useNavigate();

  const {
    notifications,
    unreadCount,
    loading,
    loadNotifications,
    markAsRead,
    markAllRead,
    clearAll,
  } = useNotifications();

  useEffect(() => {
    if (isOpen) loadNotifications();
  }, [isOpen, loadNotifications]);

  // Close dropdown on outside click
  useEffect(() => {
    function handleClickOutside(e) {
      if (dropdownRef.current && !dropdownRef.current.contains(e.target)) {
        setIsOpen(false);
      }
    }
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const handleNotificationClick = async (n) => {
    if (!n.isRead) await markAsRead(n.id);
    setIsOpen(false);
    navigate(`/websites/${n.websiteId}`);
  };

  const handleClearAll = async () => {
    if (window.confirm('Clear all notifications?')) {
      await clearAll();
    }
  };

  const formatTimeAgo = (dateStr) => {
    const seconds = Math.floor((Date.now() - new Date(dateStr).getTime()) / 1000);
    if (seconds < 60) return 'Just now';
    const minutes = Math.floor(seconds / 60);
    if (minutes < 60) return `${minutes}m ago`;
    const hours = Math.floor(minutes / 60);
    if (hours < 24) return `${hours}h ago`;
    const days = Math.floor(hours / 24);
    if (days < 30) return `${days}d ago`;
    const months = Math.floor(days / 30);
    return `${months}mo ago`;
  };

  const isDown = (title) => title?.toLowerCase().includes('down');

  return (
    <div className="nc-container" ref={dropdownRef}>
      <button
        className="btn-refresh nc-bell"
        onClick={() => setIsOpen(!isOpen)}
        title="Notifications"
        id="notification-bell"
      >
        <RiNotification3Line size={18} />
        {unreadCount > 0 && (
          <span className="nc-badge">{unreadCount > 99 ? '99+' : unreadCount}</span>
        )}
      </button>

      {isOpen && (
        <div className="nc-dropdown">
          <div className="nc-header">
            <h4>Notifications</h4>
            <div className="nc-header-actions">
              {unreadCount > 0 && (
                <button
                  className="nc-action-btn"
                  onClick={markAllRead}
                  title="Mark all as read"
                >
                  <RiCheckDoubleLine size={16} />
                </button>
              )}
              {notifications.length > 0 && (
                <button
                  className="nc-action-btn nc-action-btn--danger"
                  onClick={handleClearAll}
                  title="Clear all"
                >
                  <RiDeleteBin7Line size={16} />
                </button>
              )}
            </div>
          </div>

          <div className="nc-list">
            {loading && notifications.length === 0 ? (
              <div className="nc-empty">Loading...</div>
            ) : notifications.length === 0 ? (
              <div className="nc-empty">No notifications</div>
            ) : (
              notifications.map((n) => (
                <div
                  key={n.id}
                  className={`nc-item ${n.isRead ? 'nc-item--read' : 'nc-item--unread'}`}
                  onClick={() => handleNotificationClick(n)}
                >
                  <div className={`nc-item-icon ${isDown(n.title) ? 'nc-icon--down' : 'nc-icon--up'}`}>
                    {isDown(n.title)
                      ? <RiErrorWarningLine size={18} />
                      : <RiCheckboxCircleLine size={18} />
                    }
                  </div>
                  <div className="nc-item-body">
                    <div className="nc-item-title">{n.title}</div>
                    <div className="nc-item-message">{n.message}</div>
                    <div className="nc-item-time">{formatTimeAgo(n.createdAt)}</div>
                  </div>
                  {!n.isRead && <div className="nc-item-dot" />}
                </div>
              ))
            )}
          </div>
        </div>
      )}
    </div>
  );
}
