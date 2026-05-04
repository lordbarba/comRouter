import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { matchesApi } from '../services/apiService';
import type { CreateMatchRequest, UpdateMatchRequest } from '../types/api';

export const MATCHES_KEY = ['matches'] as const;

export function useMatches() {
  return useQuery({ queryKey: MATCHES_KEY, queryFn: matchesApi.getAll });
}

export function useCreateMatch() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: CreateMatchRequest) => matchesApi.create(body),
    onSuccess: () => qc.invalidateQueries({ queryKey: MATCHES_KEY }),
  });
}

export function useUpdateMatch(id: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: UpdateMatchRequest) => matchesApi.update(id, body),
    onSuccess: () => qc.invalidateQueries({ queryKey: MATCHES_KEY }),
  });
}

export function useDeleteMatch() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => matchesApi.remove(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: MATCHES_KEY }),
  });
}
