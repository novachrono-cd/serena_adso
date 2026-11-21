namespace WebApplication1.interfaces;

public interface IEmailSender
{
    bool IsConfigured { get; }
    Task SendVerificationCodeAsync(
        string email,
        string code,
        CancellationToken cancellationToken,
        bool isPasswordReset = false);
}
