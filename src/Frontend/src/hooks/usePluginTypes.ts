import { useQuery } from '@tanstack/react-query';
import { typesApi } from '../services/apiService';

export function usePluginTypes() {
  const listeners = useQuery({ queryKey: ['types-listeners'], queryFn: typesApi.getListenerTypes });
  const receivers = useQuery({ queryKey: ['types-receivers'], queryFn: typesApi.getReceiverTypes });
  return { listenerTypes: listeners.data ?? [], receiverTypes: receivers.data ?? [] };
}
