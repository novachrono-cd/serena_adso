using Microsoft.EntityFrameworkCore;
using WebApplication1.interfaces;
using WebApplication1.models;
namespace WebApplication1.repositories;
public class FormularioRepository : IFormulario
{
    private readonly SerenaDbContext context;
    public FormularioRepository(SerenaDbContext context)=>this.context=context;
    public Task<List<Formulario>> Getformulario(int id_usuario) =>
        context.Formulario
            .AsNoTracking()
            .Select(formulario => new Formulario
            {
                id_formulario = formulario.id_formulario,
                id_usuario = formulario.id_usuario,
                nombre_formulario = formulario.nombre_formulario,
                descripcion = formulario.descripcion,
                preguntas_count = formulario.preguntas_count,
                tiempo_estimado = formulario.tiempo_estimado,
                id_comunidad = formulario.id_comunidad,
                respondido = formulario.respuestas.Any(respuesta => respuesta.id_usuario == id_usuario),
                respuestas_count = formulario.respuestas.Count()
            })
            .ToListAsync();
    public Task<Formulario?> GetformularioById(int id)=>context.Formulario.FindAsync(id).AsTask();
    public async Task<Formulario> Postformulario(Formulario value){context.Formulario.Add(value);await context.SaveChangesAsync();return value;}
    public async Task<Formulario?> Putformulario(Formulario value){var item=await context.Formulario.FindAsync(value.id_formulario);if(item==null)return null;item.nombre_formulario=value.nombre_formulario;item.descripcion=value.descripcion;item.preguntas_count=value.preguntas_count;item.tiempo_estimado=value.tiempo_estimado;item.id_comunidad=value.id_comunidad;await context.SaveChangesAsync();return item;}
    public async Task<bool> Deleteformulario(int id){var item=await context.Formulario.FindAsync(id);if(item==null)return false;context.Formulario.Remove(item);await context.SaveChangesAsync();return true;}
    public async Task<bool> RegistrarRespuesta(int id_formulario, int id_usuario, string respuesta)
    {
        var exists = await context.FormularioRespuesta.AnyAsync(x =>
            x.id_formulario == id_formulario && x.id_usuario == id_usuario);
        if (exists || !await context.Formulario.AnyAsync(x => x.id_formulario == id_formulario))
            return false;

        context.FormularioRespuesta.Add(new FormularioRespuesta
        {
            id_formulario = id_formulario,
            id_usuario = id_usuario,
            respuesta = respuesta,
            fecha_respuesta = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
        return true;
    }
}
