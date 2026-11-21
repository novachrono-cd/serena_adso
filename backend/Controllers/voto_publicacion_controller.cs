using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.models;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/Publicaciones")]
public sealed class VotoPublicacionController(SerenaDbContext db) : ControllerBase
{
    [HttpGet("guardadas/usuario/{idUsuario:int}")]
    public async Task<IActionResult> ListarGuardadas(int idUsuario, CancellationToken cancellationToken)
    {
        var currentUserId = ObtenerIdUsuario();
        if (currentUserId is null) return Unauthorized();
        if (currentUserId != idUsuario) return Forbid();

        var ids = await db.PublicacionGuardada
            .AsNoTracking()
            .Where(item => item.id_usuario == idUsuario)
            .Select(item => item.id_publicacion)
            .ToListAsync(cancellationToken);
        return Ok(ids);
    }

    [HttpPost("{idPublicacion:int}/guardar")]
    public async Task<IActionResult> CambiarGuardado(int idPublicacion, CancellationToken cancellationToken)
    {
        var currentUserId = ObtenerIdUsuario();
        if (currentUserId is null) return Unauthorized();
        if (!await db.Publicaciones.AnyAsync(p => p.id_publicaciones == idPublicacion, cancellationToken))
            return NotFound();

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var guardado = await db.PublicacionGuardada.SingleOrDefaultAsync(
            item => item.id_usuario == currentUserId && item.id_publicacion == idPublicacion,
            cancellationToken);

        var estaGuardada = guardado is null;
        if (guardado is null)
        {
            db.PublicacionGuardada.Add(new PublicacionGuardada
            {
                id_usuario = currentUserId.Value,
                id_publicacion = idPublicacion,
                fecha = DateTime.UtcNow
            });
        }
        else
        {
            db.PublicacionGuardada.Remove(guardado);
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(new { id_publicaciones = idPublicacion, guardada = estaGuardada });
    }

    [HttpGet("votos/usuario/{idUsuario:int}")]
    public async Task<IActionResult> ListarPorUsuario(int idUsuario, CancellationToken cancellationToken)
    {
        var currentUserId = ObtenerIdUsuario();
        if (currentUserId is null) return Unauthorized();
        if (currentUserId != idUsuario && !User.IsInRole("2") && !User.IsInRole("3"))
            return Forbid();

        var votos = await (
            from vote in db.VotoPublicacion.AsNoTracking()
            join publication in db.Publicaciones.AsNoTracking()
                on vote.id_publicacion equals publication.id_publicaciones
            where vote.id_usuario == idUsuario
            select new
            {
                vote.tipo,
                publicacion = new
                {
                    publication.id_publicaciones,
                    publication.titulo,
                    publication.contenido,
                    publication.fecha_publicacion,
                    publication.id_usuario,
                    publication.id_comunidad,
                    publication.etiqueta,
                    publication.votos,
                    publication.comentarios_count,
                    publication.imagen_url
                }
            }).ToListAsync(cancellationToken);

        return Ok(new
        {
            upvotes = votos.Where(v => v.tipo == "upvote").Select(v => v.publicacion),
            downvotes = votos.Where(v => v.tipo == "downvote").Select(v => v.publicacion)
        });
    }

    [HttpPost("{idPublicacion:int}/votar")]
    public async Task<IActionResult> Votar(
        int idPublicacion,
        [FromBody] VotarPublicacionRequest request,
        CancellationToken cancellationToken)
    {
        if (request.delta is not (-1 or 1))
            return BadRequest(new { mensaje = "El voto debe ser positivo (1) o negativo (-1)." });

        var currentUserId = ObtenerIdUsuario();
        if (currentUserId is null) return Unauthorized();

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var publicacion = await db.Publicaciones.SingleOrDefaultAsync(
            p => p.id_publicaciones == idPublicacion, cancellationToken);
        if (publicacion is null) return NotFound();

        var tipoSolicitado = request.delta > 0 ? "upvote" : "downvote";
        string? votoActualizado = tipoSolicitado;
        var voto = await db.VotoPublicacion.SingleOrDefaultAsync(
            v => v.id_usuario == currentUserId && v.id_publicacion == idPublicacion,
            cancellationToken);

        if (voto?.tipo == tipoSolicitado)
        {
            db.VotoPublicacion.Remove(voto);
            publicacion.votos -= request.delta;
            votoActualizado = null;
        }
        else
        {
            if (voto is null)
            {
                db.VotoPublicacion.Add(new VotoPublicacion
                {
                    id_usuario = currentUserId.Value,
                    id_publicacion = idPublicacion,
                    tipo = tipoSolicitado,
                    fecha = DateTime.UtcNow
                });
            }
            else
            {
                publicacion.votos -= voto.tipo == "upvote" ? 1 : -1;
                voto.tipo = tipoSolicitado;
                voto.fecha = DateTime.UtcNow;
            }

            publicacion.votos += request.delta;
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(new { id_publicaciones = idPublicacion, votos = publicacion.votos, voto = votoActualizado });
    }

    private int? ObtenerIdUsuario()
    {
        var claim = User.FindFirst("id_usuario")?.Value;
        return int.TryParse(claim, out var id) && id > 0 ? id : null;
    }
}

public sealed record VotarPublicacionRequest(int delta);
