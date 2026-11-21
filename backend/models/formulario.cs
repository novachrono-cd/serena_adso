using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace WebApplication1.models
{
	public class Formulario
	{
		[Key]
		public int id_formulario { get; set; }

		[Required]
		[StringLength(150)]
		public string nombre_formulario { get; set; } = string.Empty;

		[ForeignKey("id_usuario")]
		public int id_usuario { get; set; }

		[StringLength(2000)]
		public string descripcion { get; set; } = string.Empty;

		[Range(1, 100)]
		public int preguntas_count { get; set; } = 1;

		[StringLength(50)]
		public string tiempo_estimado { get; set; } = "3 minutos";

		[StringLength(100)]
		public string? id_comunidad { get; set; }

		[NotMapped]
		public bool respondido { get; set; }

		[NotMapped]
		public int respuestas_count { get; set; }

		[JsonIgnore]
		public ICollection<FormularioRespuesta> respuestas { get; set; } = new List<FormularioRespuesta>();
	}
}
