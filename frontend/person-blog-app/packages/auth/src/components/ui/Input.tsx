// src/components/ui/Input.tsx
import { forwardRef, type InputHTMLAttributes } from 'react';
import styles from './Input.module.css';

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
    label?: string;
    error?: string;
    hint?: string;
}

export const Input = forwardRef<HTMLInputElement, InputProps>(({ label, error, hint, className = '', ...props }, ref) => {
        return (
            <div className={`${styles.field} ${className}`}>
                {label && (
                    <label className={styles.label} htmlFor={props.id}>
                        {label}
                    </label>
                )}
                <input
                    ref={ref}
                    className={`${styles.input} ${error ? styles.error : ''}`}
                    {...props}
                />
                {error && <span className={styles.errorMessage}>{error}</span>}
                {hint && !error && <span className={styles.hint}>{hint}</span>}
            </div>
        );
    }
);
Input.displayName = 'Input';