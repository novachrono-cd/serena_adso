using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.models;

[Table("FormularioRespuesta")]
public class FormularioRespuesta
{
    [Key]
    public int id_respuesta { get; set; }

    [Required]
    public int id_formulario { get; set; }

    [Required]
    public int id_usuario { get; set; }

    [Required]
    public string respuesta { get; set; } = string.Empty;

    [Required]
    public DateTime fecha_respuesta { get; set; }

    [ForeignKey(nameof(id_formulario))]
    public Formulario? Formulario { get; set; }

    [ForeignKey(nameof(id_usuario))]
    public Usuario? Usuario { get; set; }
}
