using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.models;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ComunidadController : ControllerBase
{
    private readonly SerenaDbContext db;

    public ComunidadController(SerenaDbContext db) => this.db = db;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Comunidad>>> Get(CancellationToken cancellationToken)
    {
        var comunidades = await db.Comunidad
            .AsNoTracking()
            .OrderBy(comunidad => comunidad.centro)
            .ThenBy(comunidad => comunidad.sub_nombre)
            .ToListAsync(cancellationToken);
        return Ok(comunidades);
    }

    [HttpGet("{*id}")]
    public async Task<ActionResult<Comunidad>> Get(string id, CancellationToken cancellationToken)
    {
        var comunidad = await db.Comunidad.AsNoTracking()
            .FirstOrDefaultAsync(item => item.id == id, cancellationToken);
        return comunidad == null ? NotFound() : Ok(comunidad);
    }

    [Authorize(Roles = "2,3")]
    [HttpPost]
    public async Task<ActionResult<Comunidad>> Post([FromBody] Comunidad comunidad, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue("id_usuario"), out var creadorId))
            return Unauthorized();
        if (comunidad.es_oficial)
            return BadRequest(new { mensaje = "No se pueden crear comunidades oficiales desde la aplicación." });
        if (await db.Comunidad.AnyAsync(item => item.id == comunidad.id, cancellationToken))
            return Conflict(new { mensaje = "Ya existe una comunidad con ese identificador." });

        comunidad.creado_por_id = creadorId;
        db.Comunidad.Add(comunidad);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = comunidad.id }, comunidad);
    }

    [Authorize(Roles = "2,3")]
    [HttpPut("{*id}")]
    public async Task<ActionResult<Comunidad>> Put(string id, [FromBody] Comunidad value, CancellationToken cancellationToken)
    {
        if (id != value.id)
            return BadRequest(new { mensaje = "El ID de la ruta no coincide con el cuerpo." });
        if (value.es_oficial)
            return BadRequest(new { mensaje = "No se pueden editar comunidades oficiales desde la aplicación." });

        var comunidad = await db.Comunidad.FirstOrDefaultAsync(item => item.id == id, cancellationToken);
        if (comunidad == null) return NotFound();
        comunidad.centro = value.centro;
        comunidad.sub_nombre = value.sub_nombre;
        comunidad.nombre_completo = value.nombre_completo;
        comunidad.descripcion = value.descripcion;
        comunidad.icono = value.icono;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(comunidad);
    }

    [Authorize(Roles = "2,3")]
    [HttpDelete("{*id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        var comunidad = await db.Comunidad.FirstOrDefaultAsync(item => item.id == id, cancellationToken);
        if (comunidad == null) return NotFound();
        if (comunidad.es_oficial)
            return BadRequest(new { mensaje = "No se pueden eliminar comunidades oficiales desde la aplicación." });
        if (await db.Publicaciones.AnyAsync(post => post.id_comunidad == id, cancellationToken))
            return Conflict(new { mensaje = "No se puede eliminar una comunidad que tiene publicaciones." });

        db.Comunidad.Remove(comunidad);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
