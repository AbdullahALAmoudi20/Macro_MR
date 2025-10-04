using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace MacroMR
{
    public partial class App : Application
    {
        private static readonly string appFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MarwanMacro");

        private static readonly string verificationFile = Path.Combine(appFolder, "verified.dat");
        private static readonly string bannedFile = Path.Combine(appFolder, "banned.dat");
        private static readonly string trialStartPath = Path.Combine(appFolder, "trial_start.txt");
        private static readonly string activatedFile = Path.Combine(appFolder, "activated.dat");
        private static readonly string secretKey = "MARWAN_SECURE_KEY";

        public static bool IsArabic { get; set; } = false;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Directory.CreateDirectory(appFolder);
            string hwid = GetHardwareId();

            if (!IsUserVerified())
            {
                var emailInputWindow = new EmailInput();
                bool? result = emailInputWindow.ShowDialog();

                string emailAfterVerify = LoadVerifiedEmail();

                if (result != true || string.IsNullOrWhiteSpace(emailAfterVerify))
                {
                    MessageBox.Show(
                        IsArabic ? "فشل التحقق من البريد الإلكتروني." : "Email verification failed.",
                        IsArabic ? "خطأ" : "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Shutdown();
                    return;
                }

                File.WriteAllText(trialStartPath, DateTime.UtcNow.ToString("o"));

                if (IsUserBanned(emailAfterVerify, hwid))
                {
                    MessageBox.Show(
                        IsArabic ? "تم حظر هذا المستخدم. يرجى التواصل مع الدعم." : "This user is banned. Please contact support.",
                        IsArabic ? "ممنوع" : "Banned",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Shutdown();
                    return;
                }
            }

            if (File.Exists(activatedFile))
            {
                OpenMainProgram();
                return;
            }

            if (IsTrialExpired())
            {
                var licenseWindow = new LicenseActivationWindow();
                bool? licenseResult = licenseWindow.ShowDialog();

                if (licenseResult != true)
                {
                    MessageBox.Show("لم يتم تفعيل البرنامج. سيتم الإغلاق.");
                    Shutdown();
                    return;
                }

                File.WriteAllText(activatedFile, "activated");
            }

            OpenMainProgram();
        }

        private void OpenMainProgram()
        {
            MainWindow = new MainWindow();
            MainWindow.Show();
        }

        private bool IsUserVerified()
        {
            return File.Exists(verificationFile);
        }

        public static void SaveVerifiedEmail(string email)
        {
            Directory.CreateDirectory(appFolder);

            string hwid = GetHardwareId();
            string data = $"{email}|{hwid}";
            string encrypted = Encrypt(data);
            File.WriteAllText(verificationFile, encrypted);

            File.WriteAllText(trialStartPath, DateTime.UtcNow.ToString("o"));
        }

        private string LoadVerifiedEmail()
        {
            if (!File.Exists(verificationFile))
                return string.Empty;

            try
            {
                string encrypted = File.ReadAllText(verificationFile);
                string decrypted = Decrypt(encrypted);
                return decrypted.Split('|')[0];
            }
            catch
            {
                return string.Empty;
            }
        }

        public static string LoadVerifiedEmailStatic()
        {
            if (!File.Exists(verificationFile))
                return string.Empty;

            try
            {
                string encrypted = File.ReadAllText(verificationFile);
                string decrypted = Decrypt(encrypted);
                return decrypted.Split('|')[0];
            }
            catch
            {
                return string.Empty;
            }
        }

        private bool IsUserBanned(string email, string hwid)
        {
            if (!File.Exists(bannedFile)) return false;

            string[] lines = File.ReadAllLines(bannedFile);
            foreach (string line in lines)
            {
                if (line.Contains(email) || line.Contains(hwid))
                    return true;
            }
            return false;
        }

        public static void BanUser(string email, string hwid)
        {
            Directory.CreateDirectory(appFolder);
            File.AppendAllText(bannedFile, $"{email}|{hwid}{Environment.NewLine}");
        }

        public static void UnbanUser(string email)
        {
            if (!File.Exists(bannedFile)) return;

            var lines = File.ReadAllLines(bannedFile);
            File.WriteAllLines(bannedFile, Array.FindAll(lines, line => !line.Contains(email)));
        }

        private static string GetHardwareId()
        {
            string deviceId = Environment.MachineName;
            return ComputeSHA256(deviceId);
        }

        private static bool IsTrialExpired()
        {
            if (!File.Exists(trialStartPath))
                return true;

            string startTimeStr = File.ReadAllText(trialStartPath);
            if (!DateTime.TryParse(startTimeStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime startTime))
                return true;

            return (DateTime.UtcNow - startTime).TotalHours > 4;
        }

        private static string Encrypt(string plainText)
        {
            byte[] key = Encoding.UTF8.GetBytes(secretKey.PadRight(32));
            byte[] iv = new byte[16];

            using Aes aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            using ICryptoTransform encryptor = aes.CreateEncryptor();
            byte[] inputBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encrypted = encryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);

            return Convert.ToBase64String(encrypted);
        }

        private static string Decrypt(string cipherText)
        {
            byte[] key = Encoding.UTF8.GetBytes(secretKey.PadRight(32));
            byte[] iv = new byte[16];

            using Aes aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            using ICryptoTransform decryptor = aes.CreateDecryptor();
            byte[] inputBytes = Convert.FromBase64String(cipherText);
            byte[] decrypted = decryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);

            return Encoding.UTF8.GetString(decrypted);
        }

        private static string ComputeSHA256(string input)
        {
            using SHA256 sha = SHA256.Create();
            byte[] hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            StringBuilder sb = new();
            foreach (byte b in hashBytes)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        public static string ComputeSHA256Static(string input)
        {
            using SHA256 sha = SHA256.Create();
            byte[] hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            StringBuilder sb = new();
            foreach (byte b in hashBytes)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}