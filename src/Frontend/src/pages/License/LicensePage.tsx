import { useRef, useState } from 'react';
import { useLicenseStatus, usePickupLicense, useImportLicense } from '../../hooks/useLicense';
import styles from './LicensePage.module.css';

const STATUS_LABEL: Record<string, string> = {
  Valid:         'Valida',
  ExpiringSoon:  'In scadenza',
  OfflineValid:  'Valida (offline)',
  Expired:       'Scaduta',
  Revoked:       'Revocata',
  Suspended:     'Sospesa',
  NotActivated:  'Non attivata',
  Unknown:       'Sconosciuta',
};

const STATUS_CLASS: Record<string, string> = {
  Valid:        'statusValid',
  ExpiringSoon: 'statusWarn',
  OfflineValid: 'statusValid',
  Expired:      'statusError',
  Revoked:      'statusError',
  Suspended:    'statusError',
  NotActivated: 'statusError',
  Unknown:      'statusError',
};

export function LicensePage() {
  const { data: lic, isLoading, error } = useLicenseStatus();
  const pickup = usePickupLicense();
  const importMut = useImportLicense();
  const fileRef = useRef<HTMLInputElement>(null);
  const [importMsg, setImportMsg] = useState<string | null>(null);
  const [pickupMsg, setPickupMsg] = useState<string | null>(null);

  function handleOpenBrowser() {
    if (lic?.webActivationUrl) window.open(lic.webActivationUrl, '_blank', 'noopener,noreferrer');
  }

  async function handlePickup() {
    setPickupMsg(null);
    const result = await pickup.mutateAsync();
    setPickupMsg(result.success ? 'Attivazione verificata con successo.' : (result.message || 'Attivazione non ancora completata.'));
  }

  async function handleImport(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    if (!file) return;
    setImportMsg(null);
    const result = await importMut.mutateAsync(file);
    setImportMsg(result.success ? 'Licenza importata con successo.' : (result.message || 'Importazione fallita.'));
    if (fileRef.current) fileRef.current.value = '';
  }

  if (isLoading) return <div className={styles.root}><p className={styles.loading}>Caricamento...</p></div>;
  if (error || !lic) return <div className={styles.root}><p className={styles.errMsg}>Impossibile raggiungere il server.</p></div>;

  const statusCls = styles[STATUS_CLASS[lic.status] ?? 'statusError'];
  const statusLabel = STATUS_LABEL[lic.status] ?? lic.status;

  return (
    <div className={styles.root}>
      <h2 className={styles.heading}>Licenza</h2>

      <div className={styles.card}>
        <div className={styles.cardRow}>
          <span className={styles.label}>Stato</span>
          <span className={`${styles.badge} ${statusCls}`}>{statusLabel}</span>
        </div>
        {lic.serialNumber && (
          <div className={styles.cardRow}>
            <span className={styles.label}>Numero seriale</span>
            <span className={styles.value}>{lic.serialNumber}</span>
          </div>
        )}
        {lic.tier && (
          <div className={styles.cardRow}>
            <span className={styles.label}>Tier</span>
            <span className={styles.value}>{lic.tier}</span>
          </div>
        )}
        {lic.expiresAt && (
          <div className={styles.cardRow}>
            <span className={styles.label}>Scadenza</span>
            <span className={styles.value}>{new Date(lic.expiresAt).toLocaleDateString('it-IT')}</span>
          </div>
        )}
        <div className={styles.cardRow}>
          <span className={styles.label}>Prodotto</span>
          <span className={styles.value}>{lic.productId}</span>
        </div>
        <div className={styles.cardRowHash}>
          <span className={styles.label}>Machine hash</span>
          <code className={styles.hash}>{lic.machineHash}</code>
        </div>
      </div>

      {!lic.isValid && (
        <div className={styles.card}>
          <h3 className={styles.sectionTitle}>Attivazione</h3>
          <p className={styles.hint}>
            Per attivare la licenza apri il browser, completa la registrazione e poi clicca su "Verifica".
          </p>

          <div className={styles.actions}>
            <button
              className={styles.btnPrimary}
              onClick={handleOpenBrowser}
              disabled={!lic.webActivationUrl}
            >
              Apri browser attivazione
            </button>
            <button
              className={styles.btnSecondary}
              onClick={handlePickup}
              disabled={pickup.isPending}
            >
              {pickup.isPending ? 'Verifica...' : 'Verifica attivazione'}
            </button>
          </div>
          {pickupMsg && (
            <p className={pickup.data?.success ? styles.successMsg : styles.errMsg}>{pickupMsg}</p>
          )}

          <div className={styles.divider} />

          <p className={styles.hint}>Oppure importa un file <code>.lic</code> per attivazione air-gapped:</p>
          <div className={styles.actions}>
            <input
              ref={fileRef}
              type="file"
              accept=".lic"
              className={styles.fileInput}
              onChange={handleImport}
              disabled={importMut.isPending}
            />
            {importMut.isPending && <span className={styles.hint}>Importazione in corso...</span>}
          </div>
          {importMsg && (
            <p className={importMut.data?.success ? styles.successMsg : styles.errMsg}>{importMsg}</p>
          )}
        </div>
      )}

      {lic.isValid && (
        <div className={styles.card}>
          <h3 className={styles.sectionTitle}>Rinnovo / sostituzione</h3>
          <p className={styles.hint}>Per rinnovare o sostituire la licenza importa un nuovo file <code>.lic</code>:</p>
          <div className={styles.actions}>
            <input
              ref={fileRef}
              type="file"
              accept=".lic"
              className={styles.fileInput}
              onChange={handleImport}
              disabled={importMut.isPending}
            />
          </div>
          {importMsg && (
            <p className={importMut.data?.success ? styles.successMsg : styles.errMsg}>{importMsg}</p>
          )}
        </div>
      )}
    </div>
  );
}
