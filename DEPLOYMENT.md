# Production Deployment Notes

## Environment
- Set `ASPNETCORE_ENVIRONMENT=Production`.
- Configure `ConnectionStrings:DefaultConnection` in a secure secret store.

## HTTPS
- Keep `UseHttpsRedirection()` and `UseHsts()` enabled (already configured in `Program.cs` for non-development).
- Terminate TLS at reverse proxy/load balancer and forward headers if needed.

## Logging
- Production log levels are defined in `appsettings.Production.json`.
- Send logs to centralized storage (e.g., ELK, Seq, CloudWatch) in hosting environment.

## Database Backups
- Schedule full backup daily and incremental backups every 4-6 hours.
- Keep backup retention at least 14-30 days.
- Test restore procedure regularly.

## Migration Rollout
- Run `dotnet ef database update` during deployment.
- Verify `dotnet ef migrations has-pending-model-changes` is clean before release.

