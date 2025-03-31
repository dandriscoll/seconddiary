import React, { useState, useEffect } from 'react';
import '../index.less';

interface EmailSettingsProps {
  token: string;
  userEmail?: string;
}

interface EmailSettingsData {
  id: string;
  userId: string;
  email: string;
  preferredTime: string;
  isEnabled: boolean;
  timeZone: string;
}

export const EmailSettings: React.FC<EmailSettingsProps> = ({ token, userEmail }) => {
  const [emailSettings, setEmailSettings] = useState<EmailSettingsData | null>(null);
  const [email, setEmail] = useState<string>(userEmail || '');
  const [preferredTime, setPreferredTime] = useState<string>('09:00');
  const [isEnabled, setIsEnabled] = useState<boolean>(true);
  const [timeZone, setTimeZone] = useState<string>(Intl.DateTimeFormat().resolvedOptions().timeZone);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [isSaving, setIsSaving] = useState<boolean>(false);
  const [isSendingTest, setIsSendingTest] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  useEffect(() => {
    if (token) {
      fetchEmailSettings();
    }
  }, [token]);

  const fetchEmailSettings = async () => {
    setIsLoading(true);
    setError(null);
    
    try {
      const response = await fetch('/api/emailSettings', {
        method: 'GET',
        headers: {
          'Authorization': `Bearer ${token.trim()}`,
          'Content-Type': 'application/json'
        }
      });

      if (response.ok) {
        const data = await response.json();
        setEmailSettings(data);
        
        // Convert ISO time format to HH:MM for input
        if (data) {
          setEmail(data.email || userEmail || '');
          
          // Convert time format from TimeSpan total seconds to HH:MM
          if (data.preferredTime) {
            const totalSeconds = typeof data.preferredTime === 'string' 
              ? parseInt(data.preferredTime.replace(/.*?(\d+).*/, '$1'), 10) 
              : data.preferredTime;
              
            const hours = Math.floor(totalSeconds / 3600).toString().padStart(2, '0');
            const minutes = Math.floor((totalSeconds % 3600) / 60).toString().padStart(2, '0');
            setPreferredTime(`${hours}:${minutes}`);
          } else {
            setPreferredTime('09:00');
          }
          
          setIsEnabled(data.isEnabled !== undefined ? data.isEnabled : true);
          setTimeZone(data.timeZone || Intl.DateTimeFormat().resolvedOptions().timeZone);
        }
      } else {
        // If 404, it means no settings exist yet, which is fine
        if (response.status !== 404) {
          setError('Failed to load email settings');
          console.error('Error fetching email settings:', response.statusText);
        }
      }
    } catch (err) {
      setError('Error loading email settings');
      console.error('Error:', err);
    } finally {
      setIsLoading(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSaving(true);
    setError(null);
    setSuccessMessage(null);

    try {
      const [hours, minutes] = preferredTime.split(':').map(Number);
      const formattedPreferredTime: string = `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:00.00000`;
      
      const settingsToSave: {
        id: string;
        preferredTime: string;
        isEnabled: boolean;
        timeZone: string;
      } = {
        id: emailSettings?.id || '',
        preferredTime: formattedPreferredTime,
        isEnabled: isEnabled,
        timeZone: timeZone
      };

      const response = await fetch('/api/emailSettings', {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token.trim()}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(settingsToSave)
      });

      if (response.ok) {
        const data = await response.json();
        setEmailSettings(data);
        setSuccessMessage('Email settings saved successfully!');
        
        // Clear success message after 5 seconds
        setTimeout(() => setSuccessMessage(null), 5000);
      } else {
        const errorText = await response.text();
        setError(`Failed to save email settings: ${errorText}`);
      }
    } catch (err) {
      setError('Error saving email settings');
      console.error('Error:', err);
    } finally {
      setIsSaving(false);
    }
  };

  const handleSendTestEmail = async () => {
    setIsSendingTest(true);
    setError(null);
    setSuccessMessage(null);

    try {
      const response = await fetch('/api/emailSettings/sendTestEmail', {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token.trim()}`,
          'Content-Type': 'application/json'
        }
      });

      if (response.ok) {
        const message = await response.text();
        setSuccessMessage(message);
        setTimeout(() => setSuccessMessage(null), 5000);
      } else {
        const errorText = await response.text();
        setError(`Failed to send test email: ${errorText}`);
      }
    } catch (err) {
      setError('Error sending test email');
      console.error('Error:', err);
    } finally {
      setIsSendingTest(false);
    }
  };

  // Get all available time zones
  const getTimeZones = (): string[] => {
    // This returns time zones in a more user-friendly format
    return Intl.supportedValuesOf('timeZone');
  };

  if (isLoading) {
    return <div className="loading">Loading email settings...</div>;
  }

  return (
    <div className="email-settings">
      <h2>Email Recommendation Settings</h2>
      <p className="description">
        Configure when you'd like to receive AI-generated recommendations based on your diary entries.
      </p>
      
      {error && <div className="error-message">{error}</div>}
      {successMessage && <div className="success-message">{successMessage}</div>}
      
      <form onSubmit={handleSubmit}>
        <div className="form-group">
          <label htmlFor="email">Email Address</label>
          <div className="email-display">{email || 'Loading email address...'}</div>
          <p className="hint">Your email address is automatically retrieved from your account.</p>
        </div>

        <div className="form-group">
          <label htmlFor="preferredTime">Preferred Time</label>
          <input
            type="time"
            id="preferredTime"
            value={preferredTime}
            onChange={(e) => setPreferredTime(e.target.value)}
            required
            className="time-input"
          />
          <p className="hint">Time when you'd like to receive your daily recommendation email.</p>
        </div>

        <div className="form-group">
          <label htmlFor="timeZone">Time Zone</label>
          <select
            id="timeZone"
            value={timeZone}
            onChange={(e) => setTimeZone(e.target.value)}
            required
            className="time-zone-select"
          >
            {getTimeZones().map((tz) => (
              <option key={tz} value={tz}>
                {tz.replace(/_/g, ' ')}
              </option>
            ))}
          </select>
          <p className="hint">Your local time zone for email delivery timing.</p>
        </div>

        <div className="form-group checkbox-group">
          <input
            type="checkbox"
            id="isEnabled"
            checked={isEnabled}
            onChange={(e) => setIsEnabled(e.target.checked)}
          />
          <label htmlFor="isEnabled">Enable daily recommendation emails</label>
        </div>

        <div className="button-group">
          <button 
            type="submit" 
            disabled={isSaving || !email}
            className="save-button full-width-button"
          >
            {isSaving ? 'Saving...' : (emailSettings ? 'Save Settings' : 'Enable Email')}
          </button>

          {emailSettings && (
            <button
              type="button"
              onClick={handleSendTestEmail}
              disabled={isSendingTest || !isEnabled}
              className="test-button full-width-button"
            >
              {isSendingTest ? 'Sending...' : 'Send Test Email'}
            </button>
          )}
        </div>
      </form>
    </div>
  );
};
