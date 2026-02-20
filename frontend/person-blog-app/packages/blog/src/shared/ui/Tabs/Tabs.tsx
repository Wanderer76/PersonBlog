import { ReactNode } from 'react';
import './Tabs.css';

interface TabItem {
  id: string;
  label: string;
}

interface TabsProps {
  activeTab: string;
  onChange: (tabId: string) => void;
  items: TabItem[];
  rightAction?: ReactNode;
}

export const Tabs = ({ activeTab, onChange, items, rightAction }: TabsProps) => (
  <div className="tabsContainer">
    <div className="tabButtons">
      {items.map(tab => (
        <button
          key={tab.id}
          className={`tabButton ${activeTab === tab.id ? 'active' : ''}`}
          onClick={() => onChange(tab.id)}
        >
          {tab.label}
        </button>
      ))}
    </div>
    {rightAction && <div className="tabsAction">{rightAction}</div>}
  </div>
);