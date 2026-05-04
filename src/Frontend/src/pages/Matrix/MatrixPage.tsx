import { useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { useListeners } from '../../hooks/useListeners';
import { useReceivers } from '../../hooks/useReceivers';
import { useMatches, useCreateMatch, useDeleteMatch, useUpdateMatch } from '../../hooks/useMatches';
import { useRouterStatus, useRouterStart, useRouterStop } from '../../hooks/useRouterStatus';
import { Modal } from '../../components/Modal';
import { CommandList } from '../../components/CommandList';
import type { MatchDto } from '../../types/api';
import styles from './MatrixPage.module.css';

// ─── Match cell dialog ────────────────────────────────────────────────────────

interface CellMatch {
  listenerId: string;
  receiverId: string;
  existing: MatchDto | undefined;
}

function MatchModal({ cell, onClose }: { cell: CellMatch; onClose: () => void }) {
  const { data: listeners = [] } = useListeners();
  const { data: receivers = [] } = useReceivers();
  const createMatch = useCreateMatch();
  const updateMatch = useUpdateMatch(cell.existing?.id ?? '');
  const deleteMatch = useDeleteMatch();

  const listener = listeners.find(l => l.id === cell.listenerId);
  const receiver = receivers.find(r => r.id === cell.receiverId);

  const [name, setName] = useState(cell.existing?.name ?? `${listener?.name} → ${receiver?.name}`);
  const [enabled, setEnabled] = useState(cell.existing?.enabled ?? true);
  const [listenerCmds, setListenerCmds] = useState<string[]>(cell.existing?.listenerCommands ?? []);
  const [receiverCmds, setReceiverCmds] = useState<string[]>(cell.existing?.receiverCommands ?? []);

  async function save() {
    if (cell.existing) {
      await updateMatch.mutateAsync({ name, enabled, listenerCommands: listenerCmds, receiverCommands: receiverCmds });
    } else {
      await createMatch.mutateAsync({
        name, enabled,
        listenerId: cell.listenerId,
        receiverId: cell.receiverId,
        listenerCommands: listenerCmds,
        receiverCommands: receiverCmds,
      });
    }
    onClose();
  }

  async function remove() {
    if (cell.existing) {
      await deleteMatch.mutateAsync(cell.existing.id);
    }
    onClose();
  }

  const busy = createMatch.isPending || updateMatch.isPending || deleteMatch.isPending;

  return (
    <Modal
      title={cell.existing ? 'Modifica Match' : 'Nuovo Match'}
      onClose={onClose}
      footer={
        <>
          {cell.existing && (
            <button className={styles.btnDanger} onClick={remove} disabled={busy}>Elimina</button>
          )}
          <button className={styles.btnSecondary} onClick={onClose} disabled={busy}>Annulla</button>
          <button className={styles.btnPrimary} onClick={save} disabled={busy}>Salva</button>
        </>
      }
    >
      <div className={styles.formField}>
        <label>Nome</label>
        <input value={name} onChange={e => setName(e.target.value)} />
      </div>
      <div className={styles.formField}>
        <label>
          <input type="checkbox" checked={enabled} onChange={e => setEnabled(e.target.checked)} />
          {' '}Abilitato
        </label>
      </div>
      <div className={styles.cmdColumns}>
        <CommandList label={`Comandi Listener (${listener?.name})`} commands={listenerCmds} onChange={setListenerCmds} />
        <CommandList label={`Comandi Receiver (${receiver?.name})`} commands={receiverCmds} onChange={setReceiverCmds} />
      </div>
    </Modal>
  );
}

// ─── Main component ───────────────────────────────────────────────────────────

export function MatrixPage() {
  const { data: listeners = [], isLoading: loadL } = useListeners();
  const { data: receivers = [], isLoading: loadR } = useReceivers();
  const { data: matches = [] } = useMatches();
  const { data: status } = useRouterStatus();
  const startRouter = useRouterStart();
  const stopRouter = useRouterStop();

  const [cell, setCell] = useState<CellMatch | null>(null);

  const qc = useQueryClient();

  function findMatch(listenerId: string, receiverId: string) {
    return matches.find(m => m.listenerId === listenerId && m.receiverId === receiverId);
  }

  if (loadL || loadR) return <div className={styles.loading}>Caricamento…</div>;

  return (
    <div className={styles.root}>
      <div className={styles.toolbar}>
        <h2 className={styles.heading}>Matrice di Routing</h2>
        <div className={styles.routerStatus}>
          <span className={status?.isRunning ? styles.running : styles.stopped}>
            {status?.isRunning ? '● Running' : '○ Stopped'}
          </span>
          {status?.isRunning
            ? <button className={styles.btnDanger} onClick={() => stopRouter.mutate()} disabled={stopRouter.isPending}>Stop</button>
            : <button className={styles.btnSuccess} onClick={() => startRouter.mutate()} disabled={startRouter.isPending}>Start</button>
          }
        </div>
      </div>

      {listeners.length === 0 || receivers.length === 0
        ? (
          <div className={styles.empty}>
            Aggiungi almeno un Listener e un Receiver per configurare la matrice.
          </div>
        )
        : (
          <div className={styles.tableWrapper}>
            <table className={styles.matrix}>
              <thead>
                <tr>
                  <th className={styles.cornerCell}>Listener \ Receiver</th>
                  {receivers.map(r => (
                    <th key={r.id} className={styles.colHeader}>{r.name}<br /><small>{r.typeName.split('.').pop()}</small></th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {listeners.map(l => (
                  <tr key={l.id}>
                    <td className={styles.rowHeader}>
                      <span>{l.name}</span>
                      <small>{l.typeName.split('.').pop()}</small>
                      <span className={l.isListening ? styles.listenDot : styles.listenDotOff} title={l.isListening ? 'In ascolto' : 'Fermo'}>●</span>
                    </td>
                    {receivers.map(r => {
                      const m = findMatch(l.id, r.id);
                      return (
                        <td
                          key={r.id}
                          className={`${styles.cell} ${m ? (m.enabled ? styles.cellEnabled : styles.cellDisabled) : styles.cellEmpty}`}
                          onDoubleClick={() => setCell({ listenerId: l.id, receiverId: r.id, existing: m })}
                          title={m ? `${m.name} — doppio click per modificare` : 'Doppio click per creare match'}
                        >
                          {m ? <span className={styles.matchName}>{m.name}</span> : <span className={styles.plus}>+</span>}
                        </td>
                      );
                    })}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )
      }

      {cell && <MatchModal cell={cell} onClose={() => { setCell(null); qc.invalidateQueries({ queryKey: ['matches'] }); }} />}
    </div>
  );
}
