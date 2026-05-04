import * as signalR from '@microsoft/signalr';
import type { LogEntry } from '../types/api';

let connection: signalR.HubConnection | null = null;

export function getHubConnection(): signalR.HubConnection {
  if (!connection) {
    connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/router')
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();
  }
  return connection;
}

export async function startHub(): Promise<void> {
  const conn = getHubConnection();
  if (conn.state === signalR.HubConnectionState.Disconnected) {
    await conn.start();
  }
}

export async function stopHub(): Promise<void> {
  const conn = getHubConnection();
  if (conn.state !== signalR.HubConnectionState.Disconnected) {
    await conn.stop();
  }
}

export function onStateChanged(callback: () => void): () => void {
  const conn = getHubConnection();
  conn.on('StateChanged', callback);
  return () => conn.off('StateChanged', callback);
}

export function onLogEntry(callback: (entry: LogEntry) => void): () => void {
  const conn = getHubConnection();
  conn.on('LogEntry', callback);
  return () => conn.off('LogEntry', callback);
}
