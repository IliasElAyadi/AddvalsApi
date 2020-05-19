using Microsoft.Extensions.Configuration;  
using System.Net.Mail;  
using System.Text;  
using WebApplicationSendEmail.Model;  
namespace WebApplicationSendEmail.Helper {  
    public class EmailHelper {  

        private string _host;  
        private string _from;  
        private string _alias;  
         public EmailHelper(IConfiguration iConfiguration)
         {
            var smtpSection = iConfiguration.GetSection("SMTP");
            if (smtpSection != null)
            {
               _host = smtpSection.GetSection("Host").Value;
               _from = smtpSection.GetSection("From").Value;
               _alias = smtpSection.GetSection("Alias").Value;
            }
         }
  
        public void SendEmail(EmailModel emailModel) {  
            try {  
                using(SmtpClient client = new SmtpClient(_host)) {  
                    MailMessage mailMessage = new MailMessage();  
                    mailMessage.From = new MailAddress(_from, _alias);  
                    mailMessage.BodyEncoding = Encoding.UTF8;  
                    mailMessage.To.Add(emailModel.To);  
                    mailMessage.Body = emailModel.Message;  
                    mailMessage.Subject = emailModel.Subject;  
                    mailMessage.IsBodyHtml = emailModel.IsBodyHtml;  
                    client.Send(mailMessage);  
                }  
            } catch {  
                throw;  
            }  
        }  
    }  
} 