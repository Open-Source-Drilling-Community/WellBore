# WellBore identity cutover

This runbook moves WellBore from the legacy NORCE workload identity to OSDC without changing public routes, record UUIDs, the SQLite filename, or the persistent volume. Do not execute it until the OSDC images have been built, tested, published, and pinned to reviewed immutable digests or `sha-*` tags.

## Identity map

| Concern | Legacy | OSDC |
| --- | --- | --- |
| Root namespace | `NORCE.Drilling.WellBore` | `OSDC.Drilling.WellBore` |
| WebPages package | `NORCE.Drilling.WellBore.WebPages` | `OSDC.Drilling.WellBore.WebPages` |
| Service image | `digiwells/norcedrillingwellboreservice` | `digiwells/osdcdrillingwellboreservice` |
| WebApp image | `digiwells/norcedrillingwellborewebappclient` | `digiwells/osdcdrillingwellborewebappclient` |
| Service Helm release | `norcedrillingwellboreservice` | `osdcdrillingwellboreservice` |
| WebApp Helm release | `norcedrillingwellborewebappclient` | `osdcdrillingwellborewebappclient` |
| Service Deployment/Service | `norcedrillingwellboreservice` | `osdcwellboreservice` |
| WebApp Deployment/Service | `norcedrillingwellborewebappclient` | `osdcwellborewebappclient` |
| PVC | `wellbore-claim` | unchanged |
| Database | `/home/WellBore.db` | unchanged |
| Public paths | `/WellBore/api`, `/WellBore/webapp` | unchanged |

Repeat the procedure in order: dev, then prod/app only after dev observation, then awe only after prod observation. Never run legacy and OSDC service pods against `wellbore-claim` simultaneously.

## 1. Capture and back up one environment

Set the real context and host. Backups must remain outside Git.

```powershell
$context = "dev-context"
$namespace = "default"
$hostName = "dev.digiwells.no"
$stamp = Get-Date -Format "yyyyMMddTHHmmssZ"
$backupDirectory = Join-Path $PWD "deployment\backups\$context-$stamp"
New-Item -ItemType Directory -Path $backupDirectory -Force | Out-Null

helm --kube-context $context get values norcedrillingwellboreservice -n $namespace --all -o yaml |
  Out-File "$backupDirectory\old-service-values.yaml" -Encoding utf8
helm --kube-context $context get manifest norcedrillingwellboreservice -n $namespace |
  Out-File "$backupDirectory\old-service-manifest.yaml" -Encoding utf8
helm --kube-context $context get values norcedrillingwellborewebappclient -n $namespace --all -o yaml |
  Out-File "$backupDirectory\old-webapp-values.yaml" -Encoding utf8
kubectl --context $context get deployment,service,ingress,pvc,pod -n $namespace -o wide |
  Out-File "$backupDirectory\resources.txt" -Encoding utf8
kubectl --context $context get pvc wellbore-claim -n $namespace -o yaml |
  Out-File "$backupDirectory\wellbore-claim.yaml" -Encoding utf8

$oldPod = kubectl --context $context get pod -n $namespace `
  -l "app.kubernetes.io/instance=norcedrillingwellboreservice" `
  -o jsonpath='{.items[0].metadata.name}'
kubectl --context $context exec $oldPod -n $namespace -- ls -la /home
```

Freeze WellBore writes. Take a logical verification export and an independent database copy. If WAL files exist, copy the database with its `-wal` and `-shm` files while writes are frozen, or use a CSI/storage snapshot.

```powershell
$before = @(Invoke-RestMethod "https://$hostName/WellBore/api/WellBore/HeavyData")
$before | ConvertTo-Json -Depth 100 |
  Out-File "$backupDirectory\wellbores.json" -Encoding utf8
$before | ForEach-Object {
  [pscustomobject]@{
    ID = $_.MetaInfo.ID
    Name = $_.Name
    WellID = $_.WellID
    RigID = $_.RigID
    ParentWellBoreID = $_.ParentWellBoreID
  }
} | Sort-Object ID | Export-Csv "$backupDirectory\wellbore-manifest.csv" -NoTypeInformation
@($before).Count | Out-File "$backupDirectory\wellbore-count.txt"

kubectl --context $context cp "${namespace}/${oldPod}:/home/WellBore.db" "$backupDirectory\WellBore.db"
if (-not (Test-Path "$backupDirectory\WellBore.db")) { throw "WellBore.db backup failed." }
Get-FileHash "$backupDirectory\WellBore.db" |
  Out-File "$backupDirectory\WellBore.db.sha256.txt"
```

Record the PVC UID, PV name, storage class, access mode, capacity, reclaim policy, and Helm ownership. Where supported, take a volume snapshot too.

## 2. Protect the PVC and stop the legacy writer

Use the OSDC chart under the legacy release/resource names once so Helm records `helm.sh/resource-policy: keep`. Keep the legacy image during this metadata preparation.

```powershell
$serviceChart = Join-Path $PWD "Service\charts\osdcdrillingwellboreservice"

helm upgrade norcedrillingwellboreservice $serviceChart `
  --kube-context $context -n $namespace --reuse-values `
  --set-string nameOverride=norcedrillingwellboreservice `
  --set-string fullnameOverride=norcedrillingwellboreservice `
  --set-string image.repository=docker.io/digiwells/norcedrillingwellboreservice `
  --set-string image.tag=stable `
  --set-string strategy.type=Recreate `
  --set persistence.enabled=true `
  --set-string persistence.existingClaim= `
  --set-string persistence.claimName=wellbore-claim

helm --kube-context $context get manifest norcedrillingwellboreservice -n $namespace |
  Select-String "helm.sh/resource-policy: keep"

helm upgrade norcedrillingwellboreservice $serviceChart `
  --kube-context $context -n $namespace --reuse-values `
  --set-string nameOverride=norcedrillingwellboreservice `
  --set-string fullnameOverride=norcedrillingwellboreservice `
  --set replicaCount=0

kubectl --context $context wait --for=delete pod `
  -l "app.kubernetes.io/instance=norcedrillingwellboreservice" `
  -n $namespace --timeout=180s
```

Do not continue unless there is no writer pod, the claim remains Bound, the independent backup is readable, and the keep annotation is present.

## 3. Start the OSDC service without ingress and verify data

Use the reviewed immutable image tag in place of `stable` when available.

```powershell
helm upgrade --install osdcdrillingwellboreservice $serviceChart `
  --kube-context $context -n $namespace `
  --set-string image.repository=docker.io/digiwells/osdcdrillingwellboreservice `
  --set-string image.tag=stable `
  --set-string persistence.existingClaim=wellbore-claim `
  --set ingress.enabled=false

kubectl --context $context rollout status deployment/osdcwellboreservice -n $namespace --timeout=300s
kubectl --context $context logs deployment/osdcwellboreservice -n $namespace --since=10m
kubectl --context $context exec deployment/osdcwellboreservice -n $namespace -- ls -la /home
kubectl --context $context port-forward service/osdcwellboreservice -n $namespace 5602:80
```

In another PowerShell window:

```powershell
$after = @(Invoke-RestMethod "http://localhost:5602/WellBore/api/WellBore/HeavyData")
$before = @(Get-Content "$backupDirectory\wellbores.json" -Raw | ConvertFrom-Json)
if ($after.Count -ne $before.Count) { throw "WellBore count changed." }
$difference = Compare-Object `
  ($before | ForEach-Object { $_.MetaInfo.ID } | Sort-Object) `
  ($after | ForEach-Object { $_.MetaInfo.ID } | Sort-Object)
if ($difference) { throw "WellBore UUID set changed: $difference" }
```

Also verify representative full records, Swagger, and MCP through the port-forward. If startup reports an unexpected schema, stop: the new service deliberately leaves the database unchanged. Investigate against the backup; do not delete or recreate the database.

## 4. Transfer PVC ownership and enable ingress

Only after verification, uninstall the stopped legacy service release. The keep annotation must preserve the PVC.

```powershell
helm uninstall norcedrillingwellboreservice --kube-context $context -n $namespace --wait
kubectl --context $context get pvc wellbore-claim -n $namespace

kubectl --context $context annotate pvc wellbore-claim -n $namespace `
  meta.helm.sh/release-name=osdcdrillingwellboreservice `
  meta.helm.sh/release-namespace=$namespace --overwrite
kubectl --context $context label pvc wellbore-claim -n $namespace `
  app.kubernetes.io/managed-by=Helm --overwrite

helm upgrade osdcdrillingwellboreservice $serviceChart `
  --kube-context $context -n $namespace --reuse-values `
  --set-string persistence.existingClaim= `
  --set-string persistence.claimName=wellbore-claim `
  --set ingress.enabled=true
```

Confirm the PVC UID and PV are unchanged and repeat the external count/UUID comparison.

## 5. Cut over the WebApp separately

```powershell
$webChart = Join-Path $PWD "WebApp\charts\osdcdrillingwellborewebappclient"
helm upgrade --install osdcdrillingwellborewebappclient $webChart `
  --kube-context $context -n $namespace `
  --set-string image.repository=docker.io/digiwells/osdcdrillingwellborewebappclient `
  --set-string image.tag=stable --set ingress.enabled=false
kubectl --context $context rollout status deployment/osdcwellborewebappclient -n $namespace --timeout=300s

helm uninstall norcedrillingwellborewebappclient --kube-context $context -n $namespace --wait
helm upgrade osdcdrillingwellborewebappclient $webChart `
  --kube-context $context -n $namespace --reuse-values --set ingress.enabled=true
```

Verify `/WellBore/webapp/WellBore`, edit, survey-run, trajectory, contextual pages, and statistics. Observe dev before repeating the complete procedure on prod/app and then awe.

## Rollback

Before removing the legacy release, uninstall the new releases and restore the legacy service replica count. After legacy removal, stop the OSDC writer, restore the PVC's legacy Helm ownership metadata, and reinstall from the captured values. If the database is damaged, preserve the failed volume first and restore from the independent SQLite/volume backup. Never attach both service deployments as writers during rollback.
