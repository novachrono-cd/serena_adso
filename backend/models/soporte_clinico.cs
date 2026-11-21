using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.models;

[Table("SoporteClinico")]
public class SoporteClinico
{
    [Key]
    public int id_soporte { get; set; }

    [Required]
    public int id_usuario { get; set; }

    [Required]
    [StringLength(200)]
    public required string nombre_documento { get; set; }

    [Required]
    [StringLength(200)]
    public required string entidad { get; set; }

    public DateTime fecha_emision { get; set; }

    [StringLength(200)]
    public string? medico_especialista { get; set; }

    [StringLength(20)]
    public required string tipo_archivo { get; set; }

    [StringLength(200)]
    public required string archivo_nombre { get; set; }

    [Required]
    [StringLength(100)]
    public required string archivo_tipo_contenido { get; set; }

    [Required]
    public required byte[] archivo_contenido { get; set; }

    [StringLength(100)]
    public string? diagnostico_cie10 { get; set; }

    [StringLength(2000)]
    public string? observaciones { get; set; }

    public virtual Usuario? Usuario { get; set; }
}
