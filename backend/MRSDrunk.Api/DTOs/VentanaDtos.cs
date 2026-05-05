namespace MRSDrunk.Api.DTOs;

public sealed record VentanaDto(int Id, int ModuloId, string Modulo, string Nombre, string Ruta, string? Icono, int Orden, bool Estado);
