import { useState } from 'react';
import { useReceivers, useCreateReceiver, useUpdateReceiver, useDeleteReceiver } from '../../hooks/useReceivers';
import { usePluginTypes } from '../../hooks/usePluginTypes';
import { Modal } from '../../components/Modal';
import type { ReceiverDto, PluginTypeDto } from '../../types/api';
import styles from './ReceiversPage.module.css';

interface FormProps {
  existing?: ReceiverDto;
  receiverTypes: PluginTypeDto[];
  onClose: () => void;
}

function ReceiverForm({ existing, receiverTypes, onClose }: FormProps) {
  const [name, setName] = useState(existing?.name ?? '');
  const [selectedType, setSelectedType] = useState<PluginTypeDto | null>(
    existing ? (receiverTypes.find(t => t.typeName === existing.typeName) ?? null) : null
  );
  const [config, setConfig] = useState<Record<string, string>>(existing?.config ?? {});

  const create = useCreateReceiver();
  const update = useUpdateReceiver(existing?.id ?? '');

  function onTypeChange(typeName: string) {
    const t = receiverTypes.find(t => t.typeName === typeName) ?? null;
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
      title={existing ? 'Modifica Receiver' : 'Nuovo Receiver'}
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
        <input value={name} onChange={e => setName(e.target.value)} placeholder="es. Proiettore" />
      </div>
      {!existing && (
        <div className={styles.formField}>
          <label>Tipo</label>
          <select value={selectedType?.typeName ?? ''} onChange={e => onTypeChange(e.target.value)}>
            <option value="">— seleziona tipo —</option>
            {receiverTypes.map(t => (
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

export function ReceiversPage() {
  const { data: receivers = [], isLoading } = useReceivers();
  const { receiverTypes } = usePluginTypes();
  const deleteReceiver = useDeleteReceiver();
  const [editing, setEditing] = useState<ReceiverDto | undefined>(undefined);
  const [showForm, setShowForm] = useState(false);

  function openNew() { setEditing(undefined); setShowForm(true); }
  function openEdit(r: ReceiverDto) { setEditing(r); setShowForm(true); }
  function closeForm() { setShowForm(false); setEditing(undefined); }

  if (isLoading) return <div className={styles.loading}>Caricamento…</div>;

  return (
    <div className={styles.root}>
      <div className={styles.toolbar}>
        <h2 className={styles.heading}>Receivers</h2>
        <button className={styles.btnPrimary} onClick={openNew}>+ Aggiungi</button>
      </div>

      <table className={styles.table}>
        <thead>
          <tr>
            <th>Nome</th>
            <th>Tipo</th>
            <th>Configurazione</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {receivers.length === 0
            ? <tr><td colSpan={4} className={styles.empty}>Nessun receiver configurato</td></tr>
            : receivers.map(r => (
              <tr key={r.id}>
                <td className={styles.nameTd}>{r.name}</td>
                <td><span className={styles.badge}>{r.typeName.split('.').pop()}</span></td>
                <td className={styles.configTd}>
                  {Object.entries(r.config).map(([k, v]) => (
                    <span key={k} className={styles.configEntry}>{k}: <strong>{v}</strong></span>
                  ))}
                </td>
                <td className={styles.actions}>
                  <button className={styles.btnIcon} onClick={() => openEdit(r)}>✏️</button>
                  <button
                    className={styles.btnIconDanger}
                    onClick={() => deleteReceiver.mutate(r.id)}
                    disabled={deleteReceiver.isPending}
                  >🗑️</button>
                </td>
              </tr>
            ))
          }
        </tbody>
      </table>

      {showForm && (
        <ReceiverForm
          existing={editing}
          receiverTypes={receiverTypes}
          onClose={closeForm}
        />
      )}
    </div>
  );
}
