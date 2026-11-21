using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.interfaces;
using WebApplication1.models;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FormularioController : ControllerBase
{
    private readonly IFormulario repository;

    public FormularioController(IFormulario repository) => this.repository = repository;

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Ok(await repository.Getformulario(userId));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var item = await repository.GetformularioById(id);
        return item == null ? NotFound() : Ok(item);
    }

    [Authorize(Roles = "2,3")]
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] Formulario value)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        if (!TryGetUserId(out var userId)) return Unauthorized();
        value.id_usuario = userId;
        var item = await repository.Postformulario(value);
        return CreatedAtAction(nameof(Get), new { id = item.id_formulario }, item);
    }

    [Authorize(Roles = "2,3")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Put(int id, [FromBody] Formulario value)
    {
        if (id != value.id_formulario) return BadRequest(new { mensaje = "El ID de la ruta no coincide con el cuerpo." });
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var item = await repository.Putformulario(value);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpPost("{id:int}/responder")]
    public async Task<IActionResult> Responder(int id, [FromBody] FormularioRespuestaRequest request)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var form = await repository.GetformularioById(id);
        if (form == null) return NotFound();
        if (!await repository.RegistrarRespuesta(id, userId, request.respuesta))
            return Conflict(new { mensaje = "Ya respondiste este formulario." });
        return NoContent();
    }

    [Authorize(Roles = "2,3")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        await repository.Deleteformulario(id) ? NoContent() : NotFound();

    private bool TryGetUserId(out int userId) =>
        int.TryParse(User.FindFirstValue("id_usuario"), out userId);
}

public sealed class FormularioRespuestaRequest
{
    [Required]
    [StringLength(5000, MinimumLength = 1)]
    public string respuesta { get; set; } = string.Empty;
}
