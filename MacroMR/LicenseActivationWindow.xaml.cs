using System;
using System.Windows;

namespace MacroMR
{
    public partial class LicenseActivationWindow : Window
    {
        public bool IsActivated { get; private set; }

        private bool isArabic = false;
        private int failedAttempts = 0;
        private readonly int maxAttempts = 5;

        public LicenseActivationWindow()
        {
            InitializeComponent();
            UpdateLanguage();

            // ✅ تحقق إذا الجهاز مفعّل مسبقاً
            if (LicenseManager.IsAlreadyActivated())
            {
                IsActivated = true;
                this.DialogResult = true;
                this.Close();
            }
        }

        private void Activate_Click(object sender, RoutedEventArgs e)
        {
            string key = LicenseKeyBox.Text.Trim();

            bool isValid = LicenseManager.IsLicenseValid(key);
            if (isValid)
            {
                LicenseManager.SaveUsedKey(key);    // ⛔️ حفظ الكود كـ "مستخدم"
                LicenseManager.SaveActivation();    // ✅ حفظ التفعيل للجهاز

                MessageBox.Show(isArabic ? "تم تفعيل الترخيص بنجاح." : "License activated successfully.");
                IsActivated = true;
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                failedAttempts++;

                if (failedAttempts >= maxAttempts)
                {
                    string email = App.LoadVerifiedEmailStatic();
                    string hwid = GetHardwareId();

                    App.BanUser(email, hwid);

                    MessageBox.Show(isArabic
                        ? "تم حظرك بعد 5 محاولات خاطئة. تواصل مع الدعم."
                        : "You’ve been banned after 5 invalid attempts. Please contact support.");

                    Application.Current.Shutdown();
                    return;
                }

                MessageBox.Show(isArabic
                    ? $"رمز التفعيل غير صحيح. تبقى {maxAttempts - failedAttempts} محاولات."
                    : $"Invalid key. {maxAttempts - failedAttempts} attempts remaining.");

                IsActivated = false;
            }
        }

        private void ToggleLanguage_Click(object sender, RoutedEventArgs e)
        {
            isArabic = !isArabic;
            UpdateLanguage();
        }

        private void UpdateLanguage()
        {
            InstructionText.Text = isArabic ? "ادخل رمز التفعيل:" : "Enter your license key:";
        }

        private string GetHardwareId()
        {
            string deviceId = Environment.MachineName;
            return App.ComputeSHA256Static(deviceId);
        }
    }
}