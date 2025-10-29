namespace Sheessential_Sales_Finance.helpers
{
    using System.Net;
    using System.Net.Mail;

    public static class EmailSender
    {
        public static void Send(string toEmail, string subject, string body)
        {
            var smtp = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                EnableSsl = true,
                Credentials = new NetworkCredential("adrialsarmiento@gmail.com", "awyj nxps qfmj wwyu")
            };

            var mail = new MailMessage("adrialsarmiento@gmail.com", toEmail, subject, body)
            {
                IsBodyHtml = true
            };

            smtp.Send(mail);
        }
    }

}
