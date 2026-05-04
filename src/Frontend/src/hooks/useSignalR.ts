import { useEffect, useRef, useState } from 'react';
import { startHub, stopHub, onStateChanged, onLogEntry } from '../services/signalrService';
import type { LogEntry } from '../types/api';

const MAX_LOG_ENTRIES = 200;

export function useSignalR(onStateChange?: () => void) {
  const [connected, setConnected] = useState(false);
  const [logs, setLogs] = useState<LogEntry[]>([]);
  const cbRef = useRef(onStateChange);
  cbRef.current = onStateChange;

  useEffect(() => {
    let unsubState: (() => void) | undefined;
    let unsubLog: (() => void) | undefined;

    startHub()
      .then(() => {
        setConnected(true);
        unsubState = onStateChanged(() => cbRef.current?.());
        unsubLog = onLogEntry(entry => {
          setLogs(prev => {
            const next = [...prev, entry];
            return next.length > MAX_LOG_ENTRIES ? next.slice(-MAX_LOG_ENTRIES) : next;
          });
        });
      })
      .catch(console.error);

    return () => {
      unsubState?.();
      unsubLog?.();
      stopHub().catch(console.error);
      setConnected(false);
    };
  }, []);

  return { connected, logs, clearLogs: () => setLogs([]) };
}
