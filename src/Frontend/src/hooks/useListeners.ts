import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { listenersApi } from '../services/apiService';
import type { CreateListenerRequest, UpdateListenerRequest } from '../types/api';

export const LISTENERS_KEY = ['listeners'] as const;

export function useListeners() {
  return useQuery({ queryKey: LISTENERS_KEY, queryFn: listenersApi.getAll });
}

export function useCreateListener() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: CreateListenerRequest) => listenersApi.create(body),
    onSuccess: () => qc.invalidateQueries({ queryKey: LISTENERS_KEY }),
  });
}

export function useUpdateListener(id: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: UpdateListenerRequest) => listenersApi.update(id, body),
    onSuccess: () => qc.invalidateQueries({ queryKey: LISTENERS_KEY }),
  });
}

export function useDeleteListener() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => listenersApi.remove(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: LISTENERS_KEY }),
  });
}
