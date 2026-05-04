import type { LogEntry } from '../types/api';
import styles from './LogPanel.module.css';

interface Props {
  logs: LogEntry[];
  connected: boolean;
  onClear: () => void;
}

export function LogPanel({ logs, connected, onClear }: Props) {
  return (
    <div className={styles.root}>
      <div className={styles.toolbar}>
        <span className={styles.title}>Log</span>
        <span className={`${styles.status} ${connected ? styles.connected : styles.disconnected}`}>
          {connected ? '● Connesso' : '○ Disconnesso'}
        </span>
        <button className={styles.clearBtn} onClick={onClear}>Pulisci</button>
      </div>
      <div className={styles.list}>
        {logs.length === 0 && <span className={styles.empty}>Nessun messaggio</span>}
        {logs.map((entry, i) => (
          <div key={i} className={`${styles.entry} ${styles[entry.level]}`}>
            <span className={styles.time}>{new Date(entry.timestamp).toLocaleTimeString()}</span>
            <span className={styles.msg}>{entry.message}</span>
          </div>
        ))}
      </div>
    </div>
  );
}
