using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Net.Mail;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WebApplication1.interfaces;
using WebApplication1.models;

namespace WebApplication1.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/[controller]")]
public class LoginController : ControllerBase
{
    private readonly ILoginService _loginService;
    private readonly IConfiguration _config;
    private readonly SerenaDbContext _db;
    private readonly IEmailSender _emailSender;
    private readonly IPasswordService _passwordService;
    private readonly ILogger<LoginController> _logger;

    public LoginController(
        ILoginService loginService,
        IConfiguration config,
        SerenaDbContext db,
        IEmailSender emailSender,
        IPasswordService passwordService,
        ILogger<LoginController> logger)
    {
        _loginService = loginService;
        _config = config;
        _db = db;
        _emailSender = emailSender;
        _passwordService = passwordService;
        _logger = logger;
    }

    [HttpPost("registrar")]
    public async Task<IActionResult> Registrar([FromBody] Usuario usuario, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        if (string.IsNullOrWhiteSpace(usuario.email) || string.IsNullOrWhiteSpace(usuario.contrasena))
            return BadRequest(new { mensaje = "El correo y la contraseña son obligatorios." });
        if (usuario.id_rol is not (1 or 2))
            return BadRequest(new { mensaje = "El registro solo permite los roles de aprendiz o psicólogo." });
        if (!_emailSender.IsConfigured)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                mensaje = "El envío de correo no está configurado. Contacta al administrador para habilitar el registro."
            });

        usuario.email = usuario.email.Trim();
        if (await _loginService.BuscarPorCorreo(usuario.email) != null)
            return Conflict(new { mensaje = "El correo ya está registrado." });

        usuario.email_verificado = false;
        Usuario creado;
        string code;
        await using (var transaction = await _db.Database.BeginTransactionAsync(cancellationToken))
        {
            creado = await _loginService.Registrar(usuario);
            code = GenerarCodigoVerificacion();
            _db.CodigoVerificacionCorreo.Add(new CodigoVerificacionCorreo
            {
                id_usuario = creado.id_usuario,
                codigo_hash = CalcularHashCodigo(creado.email, code),
                proposito = "VerificacionCorreo",
                expira_en = DateTime.UtcNow.AddMinutes(15),
                proximo_envio = DateTime.UtcNow.AddMinutes(1),
                intentos = 0
            });
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        try
        {
            await _emailSender.SendVerificationCodeAsync(creado.email, code, cancellationToken);
        }
        catch (SmtpException exception)
        {
            _logger.LogError(exception, "No se pudo enviar el código de verificación a {Email}.", creado.email);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                mensaje = "La cuenta quedó pendiente de verificación, pero no se pudo enviar el correo. Intenta reenviar el código en un minuto.",
                emailPendiente = true
            });
        }

        return Ok(new
        {
            mensaje = "Cuenta creada. Revisa tu correo e ingresa el código de verificación.",
            email = creado.email
        });
    }

    [HttpPost("verificar-correo")]
    public async Task<IActionResult> VerificarCorreo(
        [FromBody] VerificarCorreoRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var email = request.email.Trim();
        var usuario = await _db.Usuario.SingleOrDefaultAsync(u => u.email == email, cancellationToken);
        if (usuario is null) return BadRequest(new { mensaje = "El correo o el código no son válidos." });
        if (usuario.email_verificado) return Ok(new { mensaje = "El correo ya está verificado." });

        var verification = await _db.CodigoVerificacionCorreo
            .SingleOrDefaultAsync(
                c => c.id_usuario == usuario.id_usuario && c.proposito == "VerificacionCorreo",
                cancellationToken);
        if (verification is null || verification.expira_en <= DateTime.UtcNow)
            return BadRequest(new { mensaje = "El código venció o no existe. Solicita uno nuevo." });
        if (verification.intentos >= 5)
            return StatusCode(StatusCodes.Status429TooManyRequests, new { mensaje = "Se agotaron los intentos. Solicita un nuevo código." });

        verification.intentos++;
        if (!VerificarHashCodigo(email, request.codigo, verification.codigo_hash))
        {
            await _db.SaveChangesAsync(cancellationToken);
            return BadRequest(new { mensaje = "El correo o el código no son válidos." });
        }

        usuario.email_verificado = true;
        _db.CodigoVerificacionCorreo.Remove(verification);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { mensaje = "Correo verificado. Ya puedes iniciar sesión." });
    }

    [HttpPost("reenviar-codigo")]
    public async Task<IActionResult> ReenviarCodigo(
        [FromBody] ReenviarCodigoRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        if (!_emailSender.IsConfigured)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                mensaje = "El envío de correo no está configurado. Contacta al administrador."
            });

        var email = request.email.Trim();
        var usuario = await _db.Usuario.SingleOrDefaultAsync(
            u => u.email == email && !u.email_verificado, cancellationToken);
        if (usuario is null) return Ok(new { mensaje = "Si la cuenta existe y está pendiente, recibirás un código." });

        var ahora = DateTime.UtcNow;
        var verification = await _db.CodigoVerificacionCorreo
            .SingleOrDefaultAsync(
                c => c.id_usuario == usuario.id_usuario && c.proposito == "VerificacionCorreo",
                cancellationToken);
        if (verification is not null && verification.proximo_envio > ahora)
            return StatusCode(StatusCodes.Status429TooManyRequests, new
            {
                mensaje = "Espera un minuto antes de solicitar otro código."
            });

        var code = GenerarCodigoVerificacion();
        if (verification is null)
        {
            verification = new CodigoVerificacionCorreo
            {
                id_usuario = usuario.id_usuario,
                codigo_hash = CalcularHashCodigo(email, code),
                proposito = "VerificacionCorreo",
                expira_en = ahora.AddMinutes(15),
                proximo_envio = ahora.AddMinutes(1),
                intentos = 0
            };
            _db.CodigoVerificacionCorreo.Add(verification);
        }
        else
        {
            verification.codigo_hash = CalcularHashCodigo(email, code);
            verification.proposito = "VerificacionCorreo";
            verification.expira_en = ahora.AddMinutes(15);
            verification.proximo_envio = ahora.AddMinutes(1);
            verification.intentos = 0;
        }

        await _db.SaveChangesAsync(cancellationToken);
        try
        {
            await _emailSender.SendVerificationCodeAsync(email, code, cancellationToken);
        }
        catch (SmtpException exception)
        {
            _logger.LogError(exception, "No se pudo reenviar el código de verificación a {Email}.", email);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                mensaje = "No se pudo enviar el correo. Intenta de nuevo más tarde."
            });
        }

        return Ok(new { mensaje = "Se envió un nuevo código de verificación." });
    }

    [HttpPost("solicitar-recuperacion")]
    public async Task<IActionResult> SolicitarRecuperacion(
        [FromBody] ReenviarCodigoRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        if (!_emailSender.IsConfigured)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                mensaje = "El servicio de correo no está disponible. Intenta más tarde."
            });

        var email = request.email.Trim();
        var usuario = await _db.Usuario.SingleOrDefaultAsync(
            u => u.email == email && u.email_verificado,
            cancellationToken);
        if (usuario is null)
            return Ok(new { mensaje = "Si existe una cuenta verificada con ese correo, recibirás un código." });

        var ahora = DateTime.UtcNow;
        var verification = await _db.CodigoVerificacionCorreo
            .SingleOrDefaultAsync(c => c.id_usuario == usuario.id_usuario, cancellationToken);
        if (verification is not null && verification.proximo_envio > ahora)
            return Ok(new { mensaje = "Si existe una cuenta verificada con ese correo, recibirás un código." });

        var code = GenerarCodigoVerificacion();
        if (verification is null)
        {
            verification = new CodigoVerificacionCorreo
            {
                id_usuario = usuario.id_usuario,
                codigo_hash = CalcularHashCodigo(email, code),
                proposito = "RecuperarContrasena",
                expira_en = ahora.AddMinutes(15),
                proximo_envio = ahora.AddMinutes(1),
                intentos = 0
            };
            _db.CodigoVerificacionCorreo.Add(verification);
        }
        else
        {
            verification.codigo_hash = CalcularHashCodigo(email, code);
            verification.proposito = "RecuperarContrasena";
            verification.expira_en = ahora.AddMinutes(15);
            verification.proximo_envio = ahora.AddMinutes(1);
            verification.intentos = 0;
        }

        await _db.SaveChangesAsync(cancellationToken);
        try
        {
            await _emailSender.SendVerificationCodeAsync(email, code, cancellationToken, isPasswordReset: true);
        }
        catch (SmtpException exception)
        {
            _logger.LogError(exception, "No se pudo enviar un código de recuperación a {Email}.", email);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                mensaje = "No se pudo enviar el correo de recuperación. Intenta de nuevo más tarde."
            });
        }

        return Ok(new { mensaje = "Si existe una cuenta verificada con ese correo, recibirás un código." });
    }

    [HttpPost("restablecer-contrasena")]
    public async Task<IActionResult> RestablecerContrasena(
        [FromBody] RestablecerContrasenaRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var email = request.email.Trim();
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var usuario = await _db.Usuario.SingleOrDefaultAsync(
            u => u.email == email && u.email_verificado,
            cancellationToken);
        var verification = usuario is null
            ? null
            : await _db.CodigoVerificacionCorreo.SingleOrDefaultAsync(
                c => c.id_usuario == usuario.id_usuario && c.proposito == "RecuperarContrasena",
                cancellationToken);

        if (usuario is null || verification is null || verification.expira_en <= DateTime.UtcNow)
            return BadRequest(new { mensaje = "El correo o el código no son válidos, o el código venció." });
        if (verification.intentos >= 5)
            return StatusCode(StatusCodes.Status429TooManyRequests, new
            {
                mensaje = "Se agotaron los intentos. Solicita un nuevo código de recuperación."
            });

        verification.intentos++;
        if (!VerificarHashCodigo(email, request.codigo, verification.codigo_hash))
        {
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return BadRequest(new { mensaje = "El correo o el código no son válidos." });
        }

        usuario.contrasena = _passwordService.HashPassword(request.nuevaContrasena);
        _db.CodigoVerificacionCorreo.Remove(verification);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Ok(new { mensaje = "Contraseña actualizada. Ya puedes iniciar sesión con tu nueva contraseña." });
    }

    [HttpPost]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        
        var Usuario = await _loginService.ValidarCredenciales(request.correo, request.contrasena);
        if (Usuario == null) return Unauthorized(new { mensaje = "Credenciales incorrectas." });
        if (!Usuario.email_verificado)
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                mensaje = "Debes verificar tu correo antes de iniciar sesión."
            });

        var token = GenerarJwtToken(Usuario);

        Usuario.contrasena = "[protegida]";
        return Ok(new 
        { 
            token, 
            usuario = Usuario,
            mensaje = "Autenticación correcta" 
        });
    }

    [HttpPost("cambiar-contrasena")]
    public async Task<IActionResult> CambiarContrasena([FromBody] ChangePasswordRequest request)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        if (request.contrasenaActual == request.nuevaContrasena)
            return BadRequest(new { mensaje = "La nueva contraseña debe ser diferente a la actual." });

        var cambioRealizado = await _loginService.CambiarContrasena(
            request.correo,
            request.contrasenaActual,
            request.nuevaContrasena);

        return cambioRealizado
            ? Ok(new { mensaje = "Contraseña actualizada correctamente." })
            : Unauthorized(new { mensaje = "El correo o la contraseña actual no son correctos." });
    }

    private string GenerarJwtToken(Usuario Usuario)
    {
        var secretKey = _config["Jwt:Key"];
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, Usuario.email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("id_usuario", Usuario.id_usuario.ToString()),
            new Claim(ClaimTypes.Role, Usuario.id_rol.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"] ?? "Serena",
            audience: _config["Jwt:Audience"] ?? "Serena",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerarCodigoVerificacion() =>
        RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    private string CalcularHashCodigo(string email, string code)
    {
        var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"]!);
        var payload = Encoding.UTF8.GetBytes($"{email.ToUpperInvariant()}:{code}");
        return Convert.ToHexString(HMACSHA256.HashData(key, payload));
    }

    private bool VerificarHashCodigo(string email, string code, string expectedHash)
    {
        var actual = Convert.FromHexString(CalcularHashCodigo(email, code));
        var expected = Convert.FromHexString(expectedHash);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}

public sealed class VerificarCorreoRequest
{
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.EmailAddress]
    public string email { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.RegularExpression("^[0-9]{6}$")]
    public string codigo { get; set; } = string.Empty;
}

public sealed class ReenviarCodigoRequest
{
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.EmailAddress]
    public string email { get; set; } = string.Empty;
}

public sealed class RestablecerContrasenaRequest
{
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.EmailAddress]
    public string email { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.RegularExpression("^[0-9]{6}$")]
    public string codigo { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.MinLength(8)]
    public string nuevaContrasena { get; set; } = string.Empty;
}

public class LoginRequest
{
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.EmailAddress]
    public string correo { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required]
    public string contrasena { get; set; } = string.Empty;
}

public class ChangePasswordRequest
{
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.EmailAddress]
    public string correo { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required]
    public string contrasenaActual { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.MinLength(8)]
    public string nuevaContrasena { get; set; } = string.Empty;
}