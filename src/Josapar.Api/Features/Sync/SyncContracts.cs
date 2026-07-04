namespace Josapar.Api.Features.Sync;

public record SyncSummaryResponse(DateTime? LastSyncedAtUtc, int SuccessCount);
