import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { licenseApi } from '../services/apiService';

export const LICENSE_STATUS_KEY = ['license', 'status'];

export function useLicenseStatus() {
  return useQuery({
    queryKey: LICENSE_STATUS_KEY,
    queryFn: licenseApi.getStatus,
    retry: false,
  });
}

export function usePickupLicense() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: licenseApi.pickup,
    onSuccess: () => qc.invalidateQueries({ queryKey: LICENSE_STATUS_KEY }),
  });
}

export function useImportLicense() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (file: File) => licenseApi.importLic(file),
    onSuccess: () => qc.invalidateQueries({ queryKey: LICENSE_STATUS_KEY }),
  });
}
