namespace HandilyCommerce.Domain.Courses;

/// <summary>
/// BNCC Computação training course offered to municipal school networks (public site catalog).
/// Free of ASP.NET / infrastructure types.
/// </summary>
/// <param name="Id">Stable slug (e.g. <c>cultura-digital-e-cidadania</c>).</param>
/// <param name="Title">Display title (PT-BR).</param>
/// <param name="Axis">BNCC Computação axis.</param>
/// <param name="Summary">Short description (PT-BR).</param>
/// <param name="Audience">Target audience (público-alvo).</param>
/// <param name="WorkloadHours">Total workload in hours (carga horária), when defined.</param>
public sealed record Course(
    string Id,
    string Title,
    BnccAxis Axis,
    string Summary,
    string Audience,
    int? WorkloadHours = null);
