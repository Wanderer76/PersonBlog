import type { ComponentPropsWithoutRef, ReactNode } from 'react';
import { Sidebar } from '@/widgets/sidebar';
import styles from './PageShell.module.css';

interface PageShellProps extends ComponentPropsWithoutRef<'div'> {
  children: ReactNode;
  contentClassName?: string;
}

export const PageShell = ({ children, className = '', contentClassName = '', ...props }: PageShellProps) => (
  <div className={`${styles.shell} ${className}`.trim()} {...props}>
    <Sidebar />
    <main className={`${styles.content} ${contentClassName}`.trim()}>{children}</main>
  </div>
);
