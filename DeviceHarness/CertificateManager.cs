//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace DeviceHarness
{
    internal class CertificateManager
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
            if (string.IsNullOrWhiteSpace(certificatePath))
            {
                throw new ArgumentNullException(nameof(certificatePath));
            }

            Console.WriteLine($"User configured CA certificate path: {certificatePath}");
            if (!File.Exists(certificatePath))
            {
                // cannot proceed further without a proper cert file
                Console.WriteLine($"Invalid certificate file: {certificatePath}");
                throw new InvalidOperationException("Invalid certificate file.");
            }
            else
            {
                Console.WriteLine($"Attempting to install CA certificate: {certificatePath}");
                var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadWrite);
                store.Add(new X509Certificate2(X509Certificate2.CreateFromCertFile(certificatePath)));
                Console.WriteLine($"Successfully added certificate: {certificatePath}");
                store.Close();
            }
        }
    }
}