using System;
using System.Diagnostics;
using System.Windows;

namespace MacroMR
{
    public partial class EmailVerification : Window
    {
        private string email;
        private string verificationCode;
        private bool isArabic;

        public EmailVerification(string email, string verificationCode, bool isArabic)
        {
            InitializeComponent();
            this.email = email;
            this.verificationCode = verificationCode;
            this.isArabic = isArabic;

            UpdateLanguage();
        }

        private void UpdateLanguage()
        {
            InstructionText.Text = isArabic ? "أدخل رمز التحقق" : "Enter verification code";
            VerifyButton.Content = isArabic ? "تحقق" : "Verify";
            ResendButton.Content = isArabic ? "إعادة إرسال الرمز" : "Resend Code";
            LanguageToggle.Content = isArabic ? "EN" : "SA";
            SupportButton.Content = isArabic ? "الدعم" : "Support";
        }

        private void VerifyButton_Click(object sender, RoutedEventArgs e)
        {
            string enteredCode = CodeTextBox.Text.Trim();

            if (enteredCode == verificationCode)
            {
                MessageBox.Show(
                    isArabic ? "تم التحقق بنجاح!" : "Verification successful!",
                    isArabic ? "نجاح" : "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                App.SaveVerifiedEmail(email);

                // ✅ يجب إغلاق النافذة بعد التحقق
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                MessageBox.Show(
                    isArabic ? "رمز التحقق غير صحيح!" : "Incorrect verification code!",
                    isArabic ? "خطأ" : "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ResendButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                verificationCode = EmailSender.GenerateNewCode(6);
                EmailSender.SendVerificationCode(email, verificationCode);

                MessageBox.Show(
                    isArabic ? "تمت إعادة إرسال الرمز." : "Verification code resent.",
                    isArabic ? "إعادة إرسال" : "Resent",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    (isArabic ? "حدث خطأ أثناء إرسال الرمز:\n" : "Error while sending code:\n") + ex.Message,
                    isArabic ? "خطأ" : "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ToggleLanguage_Click(object sender, RoutedEventArgs e)
        {
            isArabic = !isArabic;
            UpdateLanguage();
        }

        private void Support_Click(object sender, RoutedEventArgs e)
        {
            string whatsappNumber = "0559611927";
            Process.Start(new ProcessStartInfo
            {
                FileName = $"https://wa.me/966{whatsappNumber}",
                UseShellExecute = true
            });
        }
    }
}