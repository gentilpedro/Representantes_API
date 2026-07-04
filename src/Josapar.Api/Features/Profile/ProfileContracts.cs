using Josapar.Api.Infrastructure.Persistence.Entities;

namespace Josapar.Api.Features.Profile;

public record PermissionResponse(string Title, string Description, PermissionStatus Status);
