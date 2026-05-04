// ─── API DTOs — mirrors CommRouter.Interfaces.Dto ────────────────────────────

export interface ListenerDto {
  id: string;
  name: string;
  typeName: string;
  assemblyName: string;
  config: Record<string, string>;
  isListening: boolean;
}

export interface ReceiverDto {
  id: string;
  name: string;
  typeName: string;
  assemblyName: string;
  config: Record<string, string>;
}

export interface MatchDto {
  id: string;
  name: string;
  enabled: boolean;
  listenerId: string;
  receiverId: string;
  listenerCommands: string[];
  receiverCommands: string[];
}

export interface RouterStatusDto {
  isRunning: boolean;
  listenersCount: number;
  receiversCount: number;
  matchesCount: number;
}

export interface PluginTypeDto {
  typeName: string;
  assemblyName: string;
  displayName: string;
  configKeys: string[];
}

// ─── Request payloads ────────────────────────────────────────────────────────

export interface CreateListenerRequest {
  name: string;
  typeName: string;
  assemblyName: string;
  config: Record<string, string>;
}

export interface UpdateListenerRequest {
  name: string;
  config: Record<string, string>;
}

export interface CreateReceiverRequest {
  name: string;
  typeName: string;
  assemblyName: string;
  config: Record<string, string>;
}

export interface UpdateReceiverRequest {
  name: string;
  config: Record<string, string>;
}

export interface CreateMatchRequest {
  name: string;
  listenerId: string;
  receiverId: string;
  enabled: boolean;
  listenerCommands: string[];
  receiverCommands: string[];
}

export interface UpdateMatchRequest {
  name: string;
  enabled: boolean;
  listenerCommands: string[];
  receiverCommands: string[];
}

// ─── SignalR ──────────────────────────────────────────────────────────────────

export interface LogEntry {
  timestamp: string;
  level: 'info' | 'warning' | 'error' | 'debug';
  message: string;
}
