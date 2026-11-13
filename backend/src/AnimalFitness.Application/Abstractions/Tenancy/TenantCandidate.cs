namespace AnimalFitness.Application.Abstractions.Tenancy;

/// <summary>
/// Candidato de organización detectado en la petición. Todavía no está verificado.
/// </summary>
/// <param name="Slug">Identificador legible de la organización.</param>
/// <param name="Source">Origen del valor (ruta, cabecera o subdominio).</param>
public sealed record TenantCandidate(string Slug, TenantCandidateSource Source);
