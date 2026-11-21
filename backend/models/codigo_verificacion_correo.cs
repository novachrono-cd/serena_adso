using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.models;

[Table("CodigoVerificacionCorreo")]
public class CodigoVerificacionCorreo
{
    [Key]
    public int id_usuario { get; set; }

    [Required]
    [StringLength(64)]
    public required string codigo_hash { get; set; }

    [Required]
    [StringLength(30)]
    public string proposito { get; set; } = "VerificacionCorreo";

    public DateTime expira_en { get; set; }

    public DateTime proximo_envio { get; set; }

    public int intentos { get; set; }

    public virtual Usuario? Usuario { get; set; }
}
