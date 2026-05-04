import { useState } from 'react';
import type { KeyboardEvent } from 'react';
import styles from './CommandList.module.css';

interface Props {
  label: string;
  commands: string[];
  onChange: (commands: string[]) => void;
}

export function CommandList({ label, commands, onChange }: Props) {
  const [input, setInput] = useState('');

  function add() {
    const v = input.trim();
    if (!v) return;
    onChange([...commands, v]);
    setInput('');
  }

  function remove(idx: number) {
    onChange(commands.filter((_, i) => i !== idx));
  }

  function onKey(e: KeyboardEvent<HTMLInputElement>) {
    if (e.key === 'Enter') { e.preventDefault(); add(); }
  }

  return (
    <div className={styles.root}>
      <label className={styles.label}>{label}</label>
      <div className={styles.inputRow}>
        <input
          className={styles.input}
          value={input}
          onChange={e => setInput(e.target.value)}
          onKeyDown={onKey}
          placeholder="es. 1Fh 00 p"
        />
        <button type="button" className={styles.addBtn} onClick={add}>+</button>
      </div>
      <ul className={styles.list}>
        {commands.map((cmd, i) => (
          <li key={i} className={styles.item}>
            <code className={styles.code}>{cmd}</code>
            <button type="button" className={styles.removeBtn} onClick={() => remove(i)}>✕</button>
          </li>
        ))}
        {commands.length === 0 && <li className={styles.empty}>Nessun comando</li>}
      </ul>
    </div>
  );
}
