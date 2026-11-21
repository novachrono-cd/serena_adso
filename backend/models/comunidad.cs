using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.models;

[Table("Comunidad")]
public class Comunidad
{
    [Key]
    [StringLength(100)]
    public required string id { get; set; }

    [Required]
    [StringLength(20)]
    public required string centro { get; set; }

    [Required]
    [StringLength(100)]
    public required string sub_nombre { get; set; }

    [Required]
    [StringLength(200)]
    public required string nombre_completo { get; set; }

    [Required]
    [StringLength(2000)]
    public required string descripcion { get; set; }

    [Required]
    [StringLength(100)]
    public string icono { get; set; } = "Compass";

    public bool es_oficial { get; set; }

    public int? creado_por_id { get; set; }
}
