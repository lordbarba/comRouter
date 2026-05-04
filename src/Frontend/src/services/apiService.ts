import axios from 'axios';
import type {
  ListenerDto,
  ReceiverDto,
  MatchDto,
  RouterStatusDto,
  PluginTypeDto,
  CreateListenerRequest,
  UpdateListenerRequest,
  CreateReceiverRequest,
  UpdateReceiverRequest,
  CreateMatchRequest,
  UpdateMatchRequest,
} from '../types/api';

const api = axios.create({
  baseURL: '/api',
  headers: { 'Content-Type': 'application/json' },
});

// ─── Router ───────────────────────────────────────────────────────────────────

export const routerApi = {
  getStatus: () => api.get<RouterStatusDto>('/router/status').then(r => r.data),
  start: () => api.post('/router/start'),
  stop: () => api.post('/router/stop'),
};

// ─── Plugin types ─────────────────────────────────────────────────────────────

export const typesApi = {
  getListenerTypes: () => api.get<PluginTypeDto[]>('/types/listeners').then(r => r.data),
  getReceiverTypes: () => api.get<PluginTypeDto[]>('/types/receivers').then(r => r.data),
};

// ─── Listeners ────────────────────────────────────────────────────────────────

export const listenersApi = {
  getAll: () => api.get<ListenerDto[]>('/listeners').then(r => r.data),
  getById: (id: string) => api.get<ListenerDto>(`/listeners/${id}`).then(r => r.data),
  create: (body: CreateListenerRequest) => api.post<ListenerDto>('/listeners', body).then(r => r.data),
  update: (id: string, body: UpdateListenerRequest) => api.put<ListenerDto>(`/listeners/${id}`, body).then(r => r.data),
  remove: (id: string) => api.delete(`/listeners/${id}`),
};

// ─── Receivers ────────────────────────────────────────────────────────────────

export const receiversApi = {
  getAll: () => api.get<ReceiverDto[]>('/receivers').then(r => r.data),
  getById: (id: string) => api.get<ReceiverDto>(`/receivers/${id}`).then(r => r.data),
  create: (body: CreateReceiverRequest) => api.post<ReceiverDto>('/receivers', body).then(r => r.data),
  update: (id: string, body: UpdateReceiverRequest) => api.put<ReceiverDto>(`/receivers/${id}`, body).then(r => r.data),
  remove: (id: string) => api.delete(`/receivers/${id}`),
};

// ─── Matches ──────────────────────────────────────────────────────────────────

export const matchesApi = {
  getAll: () => api.get<MatchDto[]>('/matches').then(r => r.data),
  getById: (id: string) => api.get<MatchDto>(`/matches/${id}`).then(r => r.data),
  create: (body: CreateMatchRequest) => api.post<MatchDto>('/matches', body).then(r => r.data),
  update: (id: string, body: UpdateMatchRequest) => api.put<MatchDto>(`/matches/${id}`, body).then(r => r.data),
  remove: (id: string) => api.delete(`/matches/${id}`),
};
