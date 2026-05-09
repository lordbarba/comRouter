import { Outlet, NavLink, type NavLinkRenderProps } from 'react-router-dom';
import { useQueryClient } from '@tanstack/react-query';
import { useSignalR } from './hooks/useSignalR';
import { LogPanel } from './components/LogPanel';
import { LISTENERS_KEY } from './hooks/useListeners';
import { RECEIVERS_KEY } from './hooks/useReceivers';
import { MATCHES_KEY } from './hooks/useMatches';
import { ROUTER_STATUS_KEY } from './hooks/useRouterStatus';
import styles from './App.module.css';

export function App() {
  const qc = useQueryClient();

  function onStateChange() {
    qc.invalidateQueries({ queryKey: LISTENERS_KEY });
    qc.invalidateQueries({ queryKey: RECEIVERS_KEY });
    qc.invalidateQueries({ queryKey: MATCHES_KEY });
    qc.invalidateQueries({ queryKey: ROUTER_STATUS_KEY });
  }

  const { connected, logs, clearLogs } = useSignalR(onStateChange);

  return (
    <div className={styles.layout}>
      <nav className={styles.sidebar}>
        <div className={styles.brand}>
          <span className={styles.brandIcon}>⇄</span>
          <span className={styles.brandName}>ComRouter</span>
        </div>
        <ul className={styles.navList}>
          <li>
            <NavLink to="/" end className={({ isActive }: NavLinkRenderProps) => `${styles.navItem} ${isActive ? styles.navActive : ''}`}>
              Matrice
            </NavLink>
          </li>
          <li>
            <NavLink to="/listeners" className={({ isActive }: NavLinkRenderProps) => `${styles.navItem} ${isActive ? styles.navActive : ''}`}>
              Listeners
            </NavLink>
          </li>
          <li>
            <NavLink to="/receivers" className={({ isActive }: NavLinkRenderProps) => `${styles.navItem} ${isActive ? styles.navActive : ''}`}>
              Receivers
            </NavLink>
          </li>
          <li>
            <NavLink to="/license" className={({ isActive }: NavLinkRenderProps) => `${styles.navItem} ${isActive ? styles.navActive : ''}`}>
              Licenza
            </NavLink>
          </li>
        </ul>
        <div className={styles.sidebarFooter}>
          <span className={`${styles.hubStatus} ${connected ? styles.hubConnected : styles.hubDisconnected}`}>
            {connected ? '● Hub' : '○ Hub'}
          </span>
        </div>
      </nav>

      <main className={styles.main}>
        <div className={styles.content}>
          <Outlet />
        </div>
        <div className={styles.logArea}>
          <LogPanel logs={logs} connected={connected} onClear={clearLogs} />
        </div>
      </main>
    </div>
  );
}

