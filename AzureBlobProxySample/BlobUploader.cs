using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AzureBlobProxySample
{
    public class BlobUploader
    {
        private readonly string storageKey;
        private readonly string storageAccount;
        private readonly string version = "2015-12-11";

        public BlobUploader(string storageKey, string storageAccount)
        {
            this.storageKey = storageKey;
            this.storageAccount = storageAccount;
        }

        public void UploadBlobWithRestAPI(string containerName, string blobName, string filePath, string proxyHost, string proxyPort)
        {
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            string method = "PUT";
            var data = File.ReadAllBytes(filePath);
            int contentLength = data.Length;
            string requestUri = $"https://{storageAccount}.blob.core.windows.net/{containerName}/{blobName}";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requestUri);
            string now = DateTime.UtcNow.ToString("R");
            request.Method = method;
            //request.ContentType = "text/plain; charset=UTF-8";
            request.ContentLength = contentLength;
            if (!string.IsNullOrEmpty(proxyHost) && !string.IsNullOrEmpty(proxyPort))
            {
                request.Proxy = new WebProxy(proxyHost, int.Parse(proxyPort));
            }
            request.Headers.Add("x-ms-version", version);
            request.Headers.Add("x-ms-date", now);
            request.Headers.Add("x-ms-blob-type", "BlockBlob");
            request.Headers.Add("Authorization", AuthorizationHeader(method, now, request, storageAccount, storageKey, containerName, blobName));

            using (Stream requestStream = request.GetRequestStream())
            {
                requestStream.Write(data, 0, contentLength);
            }

            try
            {
                request.GetResponse();
            }
            catch (WebException ex)
            {
                using (var stream = ex.Response.GetResponseStream())
                using (var reader = new StreamReader(stream))
                {
                    var errorBody = reader.ReadToEnd();
                    Console.WriteLine(errorBody);
                    throw new Exception(errorBody);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        private string AuthorizationHeader(string method, string now, HttpWebRequest request, string storageAccount, string storageKey, string containerName, string blobName)
        {

            string headerResource = $"x-ms-blob-type:BlockBlob\nx-ms-date:{now}\nx-ms-version:{version}";
            string urlResource = $"/{storageAccount}/{containerName}/{blobName}";
            string stringToSign = $"{method}\n\n\n{request.ContentLength}\n\n{request.ContentType}\n\n\n\n\n\n\n{headerResource}\n{urlResource}";

            HMACSHA256 hmac = new HMACSHA256(Convert.FromBase64String(storageKey));
            string signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));

            String AuthorizationHeader = String.Format("{0} {1}:{2}", "SharedKey", storageAccount, signature);
            return AuthorizationHeader;
        }

    }
}
