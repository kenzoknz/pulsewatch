import { useState, useEffect, useCallback, useRef } from 'react';
import * as signalR from '@microsoft/signalr';
import {
  getNotifications,
  getUnreadCount,
  markNotificationAsRead,
  markAllNotificationsAsRead,
  clearAllNotifications,
} from '../api/pulsewatchApi';
import { getCookie } from '../utils/cookies';

const POLL_INTERVAL = 60000;
const HUB_URL = import.meta.env.VITE_API_BASE_URL
  ? new URL('/hubs/notifications', import.meta.env.VITE_API_BASE_URL.replace('/api', '')).href
  : '/hubs/notifications';

export default function useNotifications() {
  const [notifications, setNotifications] = useState([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [loading, setLoading] = useState(false);
  const connectionRef = useRef(null);
  const pollTimerRef = useRef(null);

  const fetchUnreadCount = useCallback(async () => {
    try {
      const res = await getUnreadCount();
      setUnreadCount(res.data.count);
    } catch (err) {
      console.error('Failed to fetch unread count', err);
    }
  }, []);

  const loadNotifications = useCallback(async () => {
    setLoading(true);
    try {
      const res = await getNotifications(1, 15);
      setNotifications(res.data.list);
    } catch (err) {
      console.error('Failed to load notifications', err);
    } finally {
      setLoading(false);
    }
  }, []);

  const markAsRead = useCallback(async (id) => {
    try {
      await markNotificationAsRead(id);
      setNotifications(prev =>
        prev.map(n => (n.id === id ? { ...n, isRead: true } : n))
      );
      setUnreadCount(prev => Math.max(0, prev - 1));
    } catch (err) {
      console.error('Failed to mark notification as read', err);
    }
  }, []);

  const markAllRead = useCallback(async () => {
    try {
      await markAllNotificationsAsRead();
      setNotifications(prev => prev.map(n => ({ ...n, isRead: true })));
      setUnreadCount(0);
    } catch (err) {
      console.error('Failed to mark all as read', err);
    }
  }, []);

  const clearAll = useCallback(async () => {
    try {
      await clearAllNotifications();
      setNotifications([]);
      setUnreadCount(0);
    } catch (err) {
      console.error('Failed to clear all notifications', err);
    }
  }, []);

  // SignalR connection
  useEffect(() => {
    const token = getCookie('accessToken');
    if (!token) return;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL, { accessTokenFactory: () => getCookie('accessToken') })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    connection.on('ReceiveNotification', (notification) => {
      setNotifications(prev => [notification, ...prev].slice(0, 15));
      setUnreadCount(prev => prev + 1);
    });

    connection.onreconnected(() => {
      fetchUnreadCount();
    });

    connection.start().catch(err => {
      console.error('SignalR connection failed, falling back to polling', err);
    });

    connectionRef.current = connection;

    return () => {
      connection.stop();
      connectionRef.current = null;
    };
  }, [fetchUnreadCount]);

  // Polling fallback
  useEffect(() => {
    fetchUnreadCount();
    pollTimerRef.current = setInterval(fetchUnreadCount, POLL_INTERVAL);
    return () => clearInterval(pollTimerRef.current);
  }, [fetchUnreadCount]);

  return {
    notifications,
    unreadCount,
    loading,
    loadNotifications,
    fetchUnreadCount,
    markAsRead,
    markAllRead,
    clearAll,
  };
}
