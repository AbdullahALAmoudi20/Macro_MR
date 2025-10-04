using System;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;

namespace MacroMR
{
    public partial class EmailInput : Window
    {
        private string generatedCode;
        private bool isArabic = false;

        public EmailInput()
        {
            InitializeComponent();
            UpdateLanguage();
        }

        private void EmailTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            EmailPlaceholder.Visibility = string.IsNullOrWhiteSpace(EmailTextBox.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void SendVerificationCode_Click(object sender, RoutedEventArgs e)
        {
            string email = EmailTextBox.Text.Trim();

            if (!IsValidEmail(email))
            {
                MessageBox.Show(isArabic ? "يرجى إدخال بريد إلكتروني صحيح." : "Please enter a valid email address.",
                                isArabic ? "خطأ" : "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            generatedCode = GenerateRandomCode(6);

            try
            {
                // إرسال رمز التحقق إلى البريد
                EmailSender.SendVerificationCode(email, generatedCode);

                MessageBox.Show(isArabic ? "تم إرسال رمز التحقق بنجاح!" : "Verification code sent successfully!",
                                isArabic ? "نجاح" : "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // ✅ عرض نافذة التحقق باستخدام ShowDialog مع تعيين المالك
                EmailVerification verificationWindow = new EmailVerification(email, generatedCode, isArabic)
                {
                    Owner = this // هذا هو التعديل المهم
                };
                bool? verified = verificationWindow.ShowDialog();

                if (verified == true)
                {
                    this.DialogResult = true;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show((isArabic ? "فشل في إرسال رمز التحقق:\n" : "Failed to send verification code:\n") + ex.Message,
                                isArabic ? "خطأ" : "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ToggleLanguage_Click(object sender, RoutedEventArgs e)
        {
            isArabic = !isArabic;
            UpdateLanguage();
        }

        private void Support_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://wa.me/966559611927",
                UseShellExecute = true
            });
        }

        private void UpdateLanguage()
        {
            EmailPlaceholder.Text = isArabic ? "أدخل بريدك الإلكتروني" : "Enter your email";
            SendCodeButton.Content = isArabic ? "إرسال رمز التحقق" : "Send Verification Code";
            LanguageToggleButton.Content = isArabic ? "EN / العربية" : "العربية / EN";
            SupportButton.Content = isArabic ? "الدعم" : "Support";
        }

        private string GenerateRandomCode(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            StringBuilder sb = new StringBuilder();
            Random rnd = new Random();

            for (int i = 0; i < length; i++)
                sb.Append(chars[rnd.Next(chars.Length)]);

            return sb.ToString();
        }

        private bool IsValidEmail(string email)
        {
            return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }
    }
}