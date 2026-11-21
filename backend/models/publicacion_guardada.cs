using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.models;

[Table("PublicacionGuardada")]
public class PublicacionGuardada
{
    [Key]
    public int id_guardado { get; set; }

    [Required]
    public int id_usuario { get; set; }

    [Required]
    public int id_publicacion { get; set; }

    public DateTime fecha { get; set; }
}
