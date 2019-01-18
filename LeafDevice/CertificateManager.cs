using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.IoT.Samples
{
    class CertificateManager
    {
        /// <summary>
        /// Add certificate in local cert store for use by downstream device
        /// client for secure connection to IoT Edge runtime.
        ///
        ///    Note: On Windows machines, if you have not run this from an Administrator prompt,
        ///    a prompt will likely come up to confirm the installation of the certificate.
        ///    This usually happens the first time a certificate will be installed.
        /// </summary>
        public static void InstallCACert(string certificatePath)
        {
            string certPath = certificatePath;
            if (string.IsNullOrWhiteSpace(certPath))
            {
                throw new ArgumentNullException(nameof(certificatePath));
            }

            Console.WriteLine($"User configured CA certificate path: {certPath}");
            if (!File.Exists(certPath))
            {
                // cannot proceed further without a proper cert file
                Console.WriteLine($"Invalid certificate file: {certPath}");
                throw new InvalidOperationException("Invalid certificate file.");
            }
            else
            {
                Console.WriteLine($"Attempting to install CA certificate: {certPath}");
                X509Store store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadWrite);
                store.Add(new X509Certificate2(X509Certificate2.CreateFromCertFile(certPath)));
                Console.WriteLine($"Successfully added certificate: {certPath}");
                store.Close();
            }
        }
    }
}