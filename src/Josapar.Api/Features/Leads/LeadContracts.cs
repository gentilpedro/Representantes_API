using Josapar.Api.Infrastructure.Persistence.Entities;

namespace Josapar.Api.Features.Leads;

public record CreateLeadRequest(
    string ContactName,
    string CompanyName,
    string? Cnpj,
    string Phone,
    string? Email,
    string? Source,
    string? Notes);

public record UpdateLeadRequest(
    string ContactName,
    string CompanyName,
    string? Cnpj,
    string Phone,
    string? Email,
    LeadStatus Status,
    string? Source,
    string? Notes,
    DateTime? LastContactAtUtc);

public record LeadResponse(
    string Id,
    string ContactName,
    string CompanyName,
    string? Cnpj,
    string Phone,
    string? Email,
    LeadStatus Status,
    string? Source,
    string? Notes,
    DateTime CreatedAtUtc,
    DateTime? LastContactAtUtc);
