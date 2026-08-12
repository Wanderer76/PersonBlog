import { KeyboardEvent, ReactNode, useRef } from 'react';
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

export const Tabs = ({ activeTab, onChange, items, rightAction }: TabsProps) => {
  const tabRefs = useRef<Record<string, HTMLButtonElement | null>>({});

  const handleKeyDown = (event: KeyboardEvent<HTMLButtonElement>, currentIndex: number) => {
    let nextIndex: number | null = null;
    if (event.key === 'ArrowRight') nextIndex = (currentIndex + 1) % items.length;
    if (event.key === 'ArrowLeft') nextIndex = (currentIndex - 1 + items.length) % items.length;
    if (event.key === 'Home') nextIndex = 0;
    if (event.key === 'End') nextIndex = items.length - 1;
    if (nextIndex === null) return;

    event.preventDefault();
    const nextTab = items[nextIndex];
    if (!nextTab) return;
    onChange(nextTab.id);
    tabRefs.current[nextTab.id]?.focus();
  };

  return (
    <div className="tabsContainer">
      <div className="tabButtons" role="tablist" aria-label="Разделы профиля">
        {items.map((tab, index) => (
          <button
            key={tab.id}
            ref={node => { tabRefs.current[tab.id] = node; }}
            type="button"
            role="tab"
            aria-selected={activeTab === tab.id}
            tabIndex={activeTab === tab.id ? 0 : -1}
            className={`tabButton ${activeTab === tab.id ? 'active' : ''}`}
            onClick={() => onChange(tab.id)}
            onKeyDown={event => handleKeyDown(event, index)}
          >
            {tab.label}
          </button>
        ))}
      </div>
      {rightAction && <div className="tabsAction">{rightAction}</div>}
    </div>
  );
};
