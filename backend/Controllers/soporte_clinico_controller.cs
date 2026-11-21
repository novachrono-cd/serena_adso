using System.IO.Compression;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.models;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/soportes-clinicos")]
[Authorize(Roles = "2,3")]
public sealed class SoporteClinicoController(SerenaDbContext db) : ControllerBase
{
    private const int MaxFileSize = 10 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".docx", ".jpg", ".jpeg"
    };

    [HttpGet("usuario/{idUsuario:int}")]
    public async Task<IActionResult> ListarPorUsuario(int idUsuario, CancellationToken cancellationToken)
    {
        if (idUsuario <= 0) return BadRequest(new { mensaje = "El ID del usuario no es válido." });

        var soportes = await db.SoporteClinico
            .AsNoTracking()
            .Where(s => s.id_usuario == idUsuario)
            .OrderByDescending(s => s.fecha_emision)
            .Select(s => new SoporteClinicoResponse(
                s.id_soporte,
                s.id_usuario,
                s.nombre_documento,
                s.entidad,
                s.fecha_emision,
                s.medico_especialista,
                s.tipo_archivo,
                s.archivo_nombre,
                s.archivo_contenido.Length,
                s.diagnostico_cie10,
                s.observaciones))
            .ToListAsync(cancellationToken);

        return Ok(soportes);
    }

    [HttpPost("usuario/{idUsuario:int}")]
    [RequestSizeLimit(MaxFileSize + 1024 * 1024)]
    public async Task<IActionResult> Crear(
        int idUsuario,
        [FromForm] SoporteClinicoRequest request,
        CancellationToken cancellationToken)
    {
        if (idUsuario <= 0 || !await db.Usuario.AnyAsync(u => u.id_usuario == idUsuario, cancellationToken))
            return NotFound(new { mensaje = "No se encontró el aprendiz." });

        if (request.archivo is null || request.archivo.Length == 0)
            return BadRequest(new { mensaje = "Debes seleccionar un archivo." });
        if (request.archivo.Length > MaxFileSize)
            return BadRequest(new { mensaje = "El archivo no puede superar los 10 MB." });
        if (string.IsNullOrWhiteSpace(request.nombre_documento) || string.IsNullOrWhiteSpace(request.entidad))
            return BadRequest(new { mensaje = "El nombre del documento y la entidad emisora son obligatorios." });

        var extension = Path.GetExtension(request.archivo.FileName);
        if (!AllowedExtensions.Contains(extension))
            return BadRequest(new { mensaje = "Solo se permiten archivos PDF, DOCX o JPG." });

        await using var memoryStream = new MemoryStream((int)request.archivo.Length);
        await request.archivo.CopyToAsync(memoryStream, cancellationToken);
        var content = memoryStream.ToArray();
        if (!TieneFormatoValido(content, extension))
            return BadRequest(new { mensaje = "El contenido no coincide con el tipo de archivo permitido." });

        var soporte = new SoporteClinico
        {
            id_usuario = idUsuario,
            nombre_documento = request.nombre_documento.Trim(),
            entidad = request.entidad.Trim(),
            fecha_emision = request.fecha_emision ?? DateTime.UtcNow,
            medico_especialista = NormalizarOpcional(request.medico_especialista),
            tipo_archivo = extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ? "JPG" : extension[1..].ToUpperInvariant(),
            archivo_nombre = Path.GetFileName(request.archivo.FileName),
            archivo_tipo_contenido = extension.ToLowerInvariant() switch
            {
                ".pdf" => "application/pdf",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                _ => "image/jpeg"
            },
            archivo_contenido = content,
            diagnostico_cie10 = NormalizarOpcional(request.diagnostico_cie10),
            observaciones = NormalizarOpcional(request.observaciones)
        };

        db.SoporteClinico.Add(soporte);
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(Descargar), new { id = soporte.id_soporte }, ToResponse(soporte));
    }

    [HttpGet("{id:int}/archivo")]
    public async Task<IActionResult> Descargar(int id, CancellationToken cancellationToken)
    {
        var soporte = await db.SoporteClinico
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.id_soporte == id, cancellationToken);
        if (soporte is null) return NotFound();

        Response.Headers.CacheControl = "no-store";
        return File(soporte.archivo_contenido, soporte.archivo_tipo_contenido, soporte.archivo_nombre);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id, CancellationToken cancellationToken)
    {
        var soporte = await db.SoporteClinico.FindAsync([id], cancellationToken);
        if (soporte is null) return NotFound();

        db.SoporteClinico.Remove(soporte);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private SoporteClinicoResponse ToResponse(SoporteClinico soporte) =>
        new(soporte.id_soporte, soporte.id_usuario, soporte.nombre_documento, soporte.entidad,
            soporte.fecha_emision, soporte.medico_especialista, soporte.tipo_archivo,
            soporte.archivo_nombre, soporte.archivo_contenido.Length, soporte.diagnostico_cie10,
            soporte.observaciones);

    private static string? NormalizarOpcional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool TieneFormatoValido(byte[] content, string extension)
    {
        if (extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            return content.AsSpan().StartsWith("%PDF-"u8);
        if (extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
            return content.Length >= 3 && content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF;
        if (content.Length < 4 || content[0] != 0x50 || content[1] != 0x4B) return false;

        try
        {
            using var archive = new ZipArchive(new MemoryStream(content), ZipArchiveMode.Read);
            return archive.GetEntry("word/document.xml") is not null;
        }
        catch (InvalidDataException)
        {
            return false;
        }
    }
}

public sealed class SoporteClinicoRequest
{
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(200)]
    public required string nombre_documento { get; set; }

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(200)]
    public required string entidad { get; set; }

    public DateTime? fecha_emision { get; set; }

    [System.ComponentModel.DataAnnotations.StringLength(200)]
    public string? medico_especialista { get; set; }

    [System.ComponentModel.DataAnnotations.StringLength(100)]
    public string? diagnostico_cie10 { get; set; }

    [System.ComponentModel.DataAnnotations.StringLength(2000)]
    public string? observaciones { get; set; }

    [System.ComponentModel.DataAnnotations.Required]
    public required IFormFile archivo { get; set; }
}

public sealed record SoporteClinicoResponse(
    int id_soporte,
    int id_usuario,
    string nombre_documento,
    string entidad,
    DateTime fecha_emision,
    string? medico_especialista,
    string tipo_archivo,
    string archivo_nombre,
    long tamano_bytes,
    string? diagnostico_cie10,
    string? observaciones);
