import { useState } from 'react';
import { useListeners, useCreateListener, useUpdateListener, useDeleteListener } from '../../hooks/useListeners';
import { usePluginTypes } from '../../hooks/usePluginTypes';
import { Modal } from '../../components/Modal';
import type { ListenerDto, PluginTypeDto } from '../../types/api';
import styles from './ListenersPage.module.css';

// ─── Form dialog ──────────────────────────────────────────────────────────────

interface FormProps {
  existing?: ListenerDto;
  listenerTypes: PluginTypeDto[];
  onClose: () => void;
}

function ListenerForm({ existing, listenerTypes, onClose }: FormProps) {
  const [name, setName] = useState(existing?.name ?? '');
  const [selectedType, setSelectedType] = useState<PluginTypeDto | null>(
    existing ? (listenerTypes.find(t => t.typeName === existing.typeName) ?? null) : null
  );
  const [config, setConfig] = useState<Record<string, string>>(existing?.config ?? {});

  const create = useCreateListener();
  const update = useUpdateListener(existing?.id ?? '');

  function onTypeChange(typeName: string) {
    const t = listenerTypes.find(t => t.typeName === typeName) ?? null;
    setSelectedType(t);
    if (t) {
      const def: Record<string, string> = {};
      t.configKeys.forEach(k => { def[k] = existing?.config[k] ?? ''; });
      setConfig(def);
    }
  }

  function setConfigKey(key: string, value: string) {
    setConfig(prev => ({ ...prev, [key]: value }));
  }

  async function save() {
    if (!selectedType) return;
    if (existing) {
      await update.mutateAsync({ name, config });
    } else {
      await create.mutateAsync({ name, typeName: selectedType.typeName, assemblyName: selectedType.assemblyName, config });
    }
    onClose();
  }

  const busy = create.isPending || update.isPending;

  return (
    <Modal
      title={existing ? 'Modifica Listener' : 'Nuovo Listener'}
      onClose={onClose}
      footer={
        <>
          <button className={styles.btnSecondary} onClick={onClose} disabled={busy}>Annulla</button>
          <button className={styles.btnPrimary} onClick={save} disabled={busy || !selectedType || !name.trim()}>Salva</button>
        </>
      }
    >
      <div className={styles.formField}>
        <label>Nome</label>
        <input value={name} onChange={e => setName(e.target.value)} placeholder="es. COM1 Listener" />
      </div>
      {!existing && (
        <div className={styles.formField}>
          <label>Tipo</label>
          <select value={selectedType?.typeName ?? ''} onChange={e => onTypeChange(e.target.value)}>
            <option value="">— seleziona tipo —</option>
            {listenerTypes.map(t => (
              <option key={t.typeName} value={t.typeName}>{t.displayName}</option>
            ))}
          </select>
        </div>
      )}
      {selectedType && selectedType.configKeys.length > 0 && (
        <div className={styles.configSection}>
          <span className={styles.configTitle}>Configurazione</span>
          {selectedType.configKeys.map(key => (
            <div key={key} className={styles.formField}>
              <label>{key}</label>
              <input value={config[key] ?? ''} onChange={e => setConfigKey(key, e.target.value)} />
            </div>
          ))}
        </div>
      )}
    </Modal>
  );
}

// ─── Main page ────────────────────────────────────────────────────────────────

export function ListenersPage() {
  const { data: listeners = [], isLoading } = useListeners();
  const { listenerTypes } = usePluginTypes();
  const deleteListener = useDeleteListener();
  const [editing, setEditing] = useState<ListenerDto | undefined>(undefined);
  const [showForm, setShowForm] = useState(false);

  function openNew() { setEditing(undefined); setShowForm(true); }
  function openEdit(l: ListenerDto) { setEditing(l); setShowForm(true); }
  function closeForm() { setShowForm(false); setEditing(undefined); }

  if (isLoading) return <div className={styles.loading}>Caricamento…</div>;

  return (
    <div className={styles.root}>
      <div className={styles.toolbar}>
        <h2 className={styles.heading}>Listeners</h2>
        <button className={styles.btnPrimary} onClick={openNew}>+ Aggiungi</button>
      </div>

      <table className={styles.table}>
        <thead>
          <tr>
            <th>Nome</th>
            <th>Tipo</th>
            <th>Stato</th>
            <th>Configurazione</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {listeners.length === 0
            ? <tr><td colSpan={5} className={styles.empty}>Nessun listener configurato</td></tr>
            : listeners.map(l => (
              <tr key={l.id}>
                <td className={styles.nameTd}>{l.name}</td>
                <td><span className={styles.badge}>{l.typeName.split('.').pop()}</span></td>
                <td>
                  <span className={l.isListening ? styles.statusOn : styles.statusOff}>
                    {l.isListening ? '● In ascolto' : '○ Fermo'}
                  </span>
                </td>
                <td className={styles.configTd}>
                  {Object.entries(l.config).map(([k, v]) => (
                    <span key={k} className={styles.configEntry}>{k}: <strong>{v}</strong></span>
                  ))}
                </td>
                <td className={styles.actions}>
                  <button className={styles.btnIcon} onClick={() => openEdit(l)}>✏️</button>
                  <button
                    className={styles.btnIconDanger}
                    onClick={() => deleteListener.mutate(l.id)}
                    disabled={deleteListener.isPending}
                  >🗑️</button>
                </td>
              </tr>
            ))
          }
        </tbody>
      </table>

      {showForm && (
        <ListenerForm
          existing={editing}
          listenerTypes={listenerTypes}
          onClose={closeForm}
        />
      )}
    </div>
  );
}
