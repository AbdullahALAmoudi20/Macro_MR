using System;
using System.Net;
using System.Net.Mail;

namespace MacroMR
{
    public static class EmailSender
    {
        public static string LastVerificationCode { get; set; }

        public static void SendVerificationCode(string email, string code)
        {
            LastVerificationCode = code;

            string fromEmail = "marwanmacro66@gmail.com"; // بريد الإرسال
            string appPassword = "zedzndvmvbgtygvf";  // كلمة مرور تطبيق Gmail

            MailMessage message = new MailMessage(fromEmail, email);
            message.Subject = "Verification Code / رمز التحقق";
            message.Body = $"Your code is: {code}\nرمز التحقق الخاص بك هو: {code}";
            message.IsBodyHtml = false;

            using (SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587))
            {
                smtp.Credentials = new NetworkCredential(fromEmail, appPassword);
                smtp.EnableSsl = true;

                try
                {
                    smtp.Send(message);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("Failed to send email:\n" + ex.Message);
                }
            }
        }

        public static bool VerifyCode(string inputCode)
        {
            return inputCode == LastVerificationCode;
        }

        // ✅ هذه هي الدالة المطلوبة عشان تنحل المشكلة
        public static string GenerateNewCode(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            Random rnd = new Random();
            char[] code = new char[length];

            for (int i = 0; i < length; i++)
                code[i] = chars[rnd.Next(chars.Length)];

            return new string(code);
        }
    }
}