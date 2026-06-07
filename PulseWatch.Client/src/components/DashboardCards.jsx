import React from 'react';
import { RiGlobalLine, RiCheckboxCircleLine, RiCloseCircleLine, RiFlashlightLine, RiAlertLine } from 'react-icons/ri';

const iconMap = {
  websites: RiGlobalLine,
  online: RiCheckboxCircleLine,
  offline: RiCloseCircleLine,
  'response-time': RiFlashlightLine,
  downtime: RiAlertLine,
};

export default function DashboardCards({ summary }) {
  if (!summary) {
    return null;
  }

  const cards = [
    {
      id: 'websites',
      label: 'Total Websites',
      value: summary.totalWebsites || 0,
      description: 'Being monitored',
    },
    {
      id: 'online',
      label: 'Online',
      value: summary.onlineWebsites || 0,
      description: 'Operational',
    },
    {
      id: 'offline',
      label: 'Offline',
      value: summary.offlineWebsites || 0,
      description: 'Down or unreachable',
    },
    {
      id: 'response-time',
      label: 'Avg Response Time',
      value: summary.averageResponseTimeMs ? `${Math.round(summary.averageResponseTimeMs)} ms` : '0 ms',
      description: 'Across all checks',
    },
    {
      id: 'downtime',
      label: 'Downtime Events',
      value: summary.totalDowntimeEvents || 0,
      description: 'Total recorded',
    },
  ];

  return (
    <div className="grid grid-auto">
      {cards.map(card => {
        const Icon = iconMap[card.id];
        return (
          <div key={card.id} className="metric-card">
            <div className="metric-icon">{Icon && <Icon size={22} />}</div>
            <div className="metric-label">{card.label}</div>
            <div className="metric-value">{card.value}</div>
            <div className="metric-description">{card.description}</div>
          </div>
        );
      })}
    </div>
  );
}
