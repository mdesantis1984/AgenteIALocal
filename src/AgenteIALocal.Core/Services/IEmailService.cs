namespace AgenteIALocal.Core.Services
{
    /// <summary>
    /// Service for sending contact emails
    /// NUEVO - ID: 20260126_122101
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Send contact email asynchronously
        /// </summary>
        /// <param name="senderEmail">Sender's email address</param>
        /// <param name="message">Message content</param>
        /// <returns>True if sent successfully, false otherwise</returns>
        System.Threading.Tasks.Task<bool> SendContactEmailAsync(string senderEmail, string message);
    }
}
