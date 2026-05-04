import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { receiversApi } from '../services/apiService';
import type { CreateReceiverRequest, UpdateReceiverRequest } from '../types/api';

export const RECEIVERS_KEY = ['receivers'] as const;

export function useReceivers() {
  return useQuery({ queryKey: RECEIVERS_KEY, queryFn: receiversApi.getAll });
}

export function useCreateReceiver() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: CreateReceiverRequest) => receiversApi.create(body),
    onSuccess: () => qc.invalidateQueries({ queryKey: RECEIVERS_KEY }),
  });
}

export function useUpdateReceiver(id: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: UpdateReceiverRequest) => receiversApi.update(id, body),
    onSuccess: () => qc.invalidateQueries({ queryKey: RECEIVERS_KEY }),
  });
}

export function useDeleteReceiver() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => receiversApi.remove(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: RECEIVERS_KEY }),
  });
}
