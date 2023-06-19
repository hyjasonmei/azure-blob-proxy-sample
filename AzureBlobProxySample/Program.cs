using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace AzureBlobProxySample
{
    internal class Program
    {

        static void Main(string[] args)
        {
            Console.WriteLine("Start");

            string storageKey = ConfigurationManager.AppSettings["storageKey"];
            string storageAccount = ConfigurationManager.AppSettings["storageAccount"];
            string containerName = ConfigurationManager.AppSettings["containerName"];
            string filePath = ConfigurationManager.AppSettings["filePath"];
            string proxyHost = ConfigurationManager.AppSettings["proxyHost"];
            string proxyPort = ConfigurationManager.AppSettings["proxyPort"];
            string blobName = Path.GetFileName(filePath);
            string apiVersion = ConfigurationManager.AppSettings["apiVersion"];

            // var blobUploader = new BlobUploader(storageKey, storageAccount, apiVersion);
            // blobUploader.UploadBlobWithRestAPI(containerName, blobName, filePath, proxyHost, proxyPort);

            Console.WriteLine("Finished");
            Console.WriteLine("Start download");

            var downloader = new BlobDownloader(storageKey, storageAccount, apiVersion);
            downloader.Download(containerName, blobName, filePath, proxyHost, proxyPort);
            
            Console.WriteLine("Finished");

            Console.ReadLine();
        }
    }
}
