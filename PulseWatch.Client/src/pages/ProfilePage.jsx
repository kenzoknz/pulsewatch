import { useState, useEffect } from 'react';
import { getProfile, updateProfile, changePassword, updateEmailSettings } from '../api/pulsewatchApi';
import { useAuth } from '../contexts/AuthContext';
import { RiMailLine } from 'react-icons/ri';

export default function ProfilePage() {
  const { refreshUser } = useAuth();
  const [profile, setProfile] = useState(null);
  const [loading, setLoading] = useState(true);

  const [profileForm, setProfileForm] = useState({ username: '' });
  const [profileMsg, setProfileMsg] = useState({ text: '', isError: false });
  const [profileSaving, setProfileSaving] = useState(false);

  const [pwForm, setPwForm] = useState({ currentPassword: '', newPassword: '', confirmPassword: '' });
  const [pwMsg, setPwMsg] = useState({ text: '', isError: false });
  const [pwSaving, setPwSaving] = useState(false);

  const [emailNotificationsEnabled, setEmailNotificationsEnabled] = useState(true);
  const [emailMsg, setEmailMsg] = useState({ text: '', isError: false });

  useEffect(() => {
    const fetchProfile = async () => {
      try {
        const res = await getProfile();
        setProfile(res.data);
        setProfileForm({ username: res.data.username });
        setEmailNotificationsEnabled(res.data.emailNotificationsEnabled);
      } catch {
        setProfileMsg({ text: 'Failed to load profile.', isError: true });
      } finally {
        setLoading(false);
      }
    };
    fetchProfile();
  }, []);

  const handleProfileSubmit = async (e) => {
    e.preventDefault();
    setProfileSaving(true);
    setProfileMsg({ text: '', isError: false });

    try {
      const res = await updateProfile({ username: profileForm.username });
      setProfile(res.data);
      setProfileMsg({ text: 'Profile updated successfully.', isError: false });
      await refreshUser();
    } catch (err) {
      const msg = err.response?.data?.message || 'Update failed.';
      setProfileMsg({ text: msg, isError: true });
    } finally {
      setProfileSaving(false);
    }
  };

  const handlePasswordSubmit = async (e) => {
    e.preventDefault();
    setPwSaving(true);
    setPwMsg({ text: '', isError: false });

    if (pwForm.newPassword !== pwForm.confirmPassword) {
      setPwMsg({ text: 'Passwords do not match.', isError: true });
      setPwSaving(false);
      return;
    }

    try {
      await changePassword({
        currentPassword: pwForm.currentPassword,
        newPassword: pwForm.newPassword,
        confirmPassword: pwForm.confirmPassword,
      });
      setPwMsg({ text: 'Password changed successfully.', isError: false });
      setPwForm({ currentPassword: '', newPassword: '', confirmPassword: '' });
    } catch (err) {
      const msg = err.response?.data?.message || 'Password change failed.';
      setPwMsg({ text: msg, isError: true });
    } finally {
      setPwSaving(false);
    }
  };

  const handleEmailSettingsSave = async () => {
    try {
      await updateEmailSettings({ emailNotificationsEnabled });
      setEmailMsg({ text: 'Email settings updated successfully.', isError: false });
    } catch {
      setEmailMsg({ text: 'Failed to update email settings. Please try again.', isError: true });
    }
  };

  if (loading) {
    return (
      <div className="page-loading">
        <div className="loading-spinner"></div>
        <p>Loading profile...</p>
      </div>
    );
  }

  return (
    <div className="profile-page">
      <div className="profile-header">
        <div className="profile-avatar-large">
          {profile?.username?.charAt(0).toUpperCase()}
        </div>
        <div className="profile-header-info">
          <h1>{profile?.username}</h1>
          <span className="profile-email">{profile?.email}</span>
          <div className="profile-meta">
            <span>Member since {new Date(profile?.createdAt).toLocaleDateString()}</span>
            <span className="meta-dot">·</span>
            <span>{profile?.totalWebsites} websites monitored</span>
          </div>
        </div>
      </div>

      <div className="profile-sections">
        <div className="profile-card">
          <h2>Account Information</h2>
          <form onSubmit={handleProfileSubmit} className="profile-form">
            <div className="form-group">
              <label htmlFor="profile-email">Email</label>
              <input
                id="profile-email"
                type="text"
                value={profile?.email || ''}
                disabled
                className="input-disabled"
              />
              <span className="input-hint">Email cannot be changed.</span>
            </div>

            <div className="form-group">
              <label htmlFor="profile-username">Username</label>
              <input
                id="profile-username"
                type="text"
                value={profileForm.username}
                onChange={(e) => setProfileForm({ username: e.target.value })}
                required
                minLength={3}
              />
            </div>

            {profileMsg.text && (
              <div className={`form-message ${profileMsg.isError ? 'form-message--error' : 'form-message--success'}`}>
                {profileMsg.text}
              </div>
            )}

            <button
              id="btn-save-profile"
              type="submit"
              className="btn btn-primary"
              disabled={profileSaving}
            >
              {profileSaving ? 'Saving...' : 'Save Changes'}
            </button>
          </form>
        </div>

        <div className="profile-card">
          <h2>Change Password</h2>
          <form onSubmit={handlePasswordSubmit} className="profile-form">
            <div className="form-group">
              <label htmlFor="current-password">Current Password</label>
              <input
                id="current-password"
                type="password"
                value={pwForm.currentPassword}
                onChange={(e) => setPwForm(prev => ({ ...prev, currentPassword: e.target.value }))}
                required
              />
            </div>

            <div className="form-group">
              <label htmlFor="new-password">New Password</label>
              <input
                id="new-password"
                type="password"
                value={pwForm.newPassword}
                onChange={(e) => setPwForm(prev => ({ ...prev, newPassword: e.target.value }))}
                required
                minLength={8}
              />
            </div>

            <div className="form-group">
              <label htmlFor="confirm-new-password">Confirm New Password</label>
              <input
                id="confirm-new-password"
                type="password"
                value={pwForm.confirmPassword}
                onChange={(e) => setPwForm(prev => ({ ...prev, confirmPassword: e.target.value }))}
                required
              />
            </div>

            {pwMsg.text && (
              <div className={`form-message ${pwMsg.isError ? 'form-message--error' : 'form-message--success'}`}>
                {pwMsg.text}
              </div>
            )}

            <button
              id="btn-change-password"
              type="submit"
              className="btn btn-primary"
              disabled={pwSaving}
            >
              {pwSaving ? 'Changing...' : 'Change Password'}
            </button>
          </form>
        </div>

        <div className="profile-card">
          <h2><RiMailLine style={{ marginRight: '8px', verticalAlign: 'middle' }} />Email Notifications</h2>
          <div className="profile-form">
            <div className="form-group form-group-toggle">
              <label htmlFor="email-enabled">Enable Email Notifications</label>
              <label className="toggle-switch">
                <input
                  id="email-enabled"
                  type="checkbox"
                  checked={emailNotificationsEnabled}
                  onChange={(e) => setEmailNotificationsEnabled(e.target.checked)}
                />
                <span className="toggle-slider"></span>
              </label>
              <span className="input-hint">
                {emailNotificationsEnabled
                  ? 'You will receive email alerts when websites go down or come back online.'
                  : 'All email alerts are currently disabled.'}
              </span>
            </div>

            {emailMsg.text && (
              <div className={`form-message ${emailMsg.isError ? 'form-message--error' : 'form-message--success'}`}>
                {emailMsg.text}
              </div>
            )}

            <button className="btn btn-primary" onClick={handleEmailSettingsSave}>
              Save Email Settings
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
