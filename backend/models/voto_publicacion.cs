using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.models;

[Table("VotoPublicacion")]
public class VotoPublicacion
{
    [Key]
    public int id_voto { get; set; }

    [Required]
    public int id_usuario { get; set; }

    [Required]
    public int id_publicacion { get; set; }

    [Required]
    [StringLength(10)]
    public required string tipo { get; set; }

    public DateTime fecha { get; set; }
}
