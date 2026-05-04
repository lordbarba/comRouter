import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { routerApi } from '../services/apiService';

export const ROUTER_STATUS_KEY = ['router-status'] as const;

export function useRouterStatus() {
  return useQuery({
    queryKey: ROUTER_STATUS_KEY,
    queryFn: routerApi.getStatus,
    refetchInterval: 5000,
  });
}

export function useRouterStart() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: routerApi.start,
    onSuccess: () => qc.invalidateQueries({ queryKey: ROUTER_STATUS_KEY }),
  });
}

export function useRouterStop() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: routerApi.stop,
    onSuccess: () => qc.invalidateQueries({ queryKey: ROUTER_STATUS_KEY }),
  });
}
