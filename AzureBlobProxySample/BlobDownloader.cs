﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AzureBlobProxySample
{

    public class BlobDownloader
    {
        private readonly string storageKey;
        private readonly string storageAccount;
        private readonly string version = "2012-02-12";

        public BlobDownloader(string storageKey, string storageAccount, string apiVersion)
        {
            this.storageKey = storageKey;
            this.storageAccount = storageAccount;
            this.version = apiVersion;
        }

        public void Download(string containerName, string blobName, string filePath, string proxyHost, string proxyPort)
        {
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            // Create a SAS token that's valid for one hour.
            string sasToken = GenerateSasToken(storageAccount, storageKey, containerName, blobName, DateTime.UtcNow.AddHours(1));
            // Construct the download URL with the SAS token
            string downloadUrl = $"https://{storageAccount}.blob.core.windows.net/{containerName}/{blobName}?{sasToken}";
            try
            {
                using (WebClient client = new WebClient())
                {
                    if (!string.IsNullOrEmpty(proxyHost) && !string.IsNullOrEmpty(proxyPort))
                    {
                        client.Proxy = new WebProxy(proxyHost, int.Parse(proxyPort));
                    }
                    client.DownloadFile(downloadUrl, filePath);
                }
                Console.WriteLine("File downloaded successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private string GenerateSasToken(string storageAccountName, string storageAccountKey, string containerName, string blobName, DateTime expirationTimeUtc)
        {
            // Set the SAS token parameters
            string signedPermissions = "r"; // Read permissions
            string signedResource = "b"; // Blob resource type
            string signedStart = DateTime.UtcNow.AddMinutes(-5).ToString("yyyy-MM-ddTHH:mm:ssZ"); // Allow 5 minutes before the start time
            string signedExpiry = expirationTimeUtc.ToString("yyyy-MM-ddTHH:mm:ssZ");

            // Create the canonicalized resource string
            string canonicalizedResource = $"/blob/{storageAccountName}/{containerName}/{blobName}";

            // Create the string-to-sign
            //string stringToSign = $"{signedPermissions}\n{signedStart}\n{signedExpiry}\n{canonicalizedResource}\n\n\n\n{version}\n{signedResource}\n\n\n\n\n";
            //string stringToSign = $"{signedPermissions}\n{signedStart}\n{signedExpiry}\n{canonicalizedResource}\n{version}\n{signedResource}\n";
            // string stringToSign = signedPermissions + "\n" +
            //      signedStart + "\n" +
            //      signedExpiry + "\n" +
            //      canonicalizedResource + "\n" +
            //      "" + "\n" +
            //      version;

            string stringToSign = String.Join("\n",
                signedPermissions, //Permissions,
                signedStart, //startTime,
                signedExpiry, //expiryTime,
                canonicalizedResource, //GetCanonicalName(sharedKeyCredential.AccountName, BlobContainerName ?? String.Empty, BlobName ?? String.Empty),
                "", //Identifier,
                "", //IPRange.ToString(),
                "https", //SasExtensions.ToProtocolString(Protocol),
                version, //Version,
                signedResource, //Resource,
                "", //Snapshot ?? BlobVersionId,
                "", //EncryptionScope,
                "", //CacheControl,
                "", //ContentDisposition,
                "", //ContentEncoding,
                "", //ContentLanguage,
                "" //ContentType
            );

            // Convert the storage account key from Base64
            byte[] keyBytes = Convert.FromBase64String(storageAccountKey);

            // Create the HMAC-SHA256 hash of the string-to-sign using the key
            using (HMACSHA256 hmacSha256 = new HMACSHA256(keyBytes))
            {
                byte[] signatureBytes = hmacSha256.ComputeHash(Encoding.UTF8.GetBytes(stringToSign));
                string signature = Convert.ToBase64String(signatureBytes);
                
                string encodedPermissions = Uri.EscapeDataString(signedPermissions);
                string encodedSignature = Uri.EscapeDataString(signature);

                // Construct the SAS token
                // string sasToken = $"sv={version}&sr={signedResource}&sp={signedPermissions}&st={signedStart}&se={signedExpiry}&sig={Uri.EscapeDataString(signature)}";
                string sasToken = $"sp={encodedPermissions}&st={signedStart}&se={signedExpiry}&spr=https&sv={version}&sr={signedResource}&sig={encodedSignature}";
                return sasToken;
            }
        }
    }
}
