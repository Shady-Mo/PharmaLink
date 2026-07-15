using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
    }
}
