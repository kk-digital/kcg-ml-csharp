using Minio;
using Minio.ApiEndpoints;
using Minio.DataModel;
using Minio.DataModel.Args;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;

namespace Utils.MinioHelper
{
    // TODO: fix usage of minioclient some use MinioHelper some use methods from dataloaders/Utils.cs
    public static class MinioHelper
    {
        private static string MINIO_ADDRESS = "http://localhost:9000"; // Default address (can be configured)
        private static IMinioClient minioClient;

        public static void Initialize(string accessKey, string secretKey, string minioIpAddr = null)
        {
            AssertValidInput(accessKey, secretKey);

            if (minioIpAddr != null)
            {
                MINIO_ADDRESS = minioIpAddr;
            }

            minioClient = new MinioClient()
                .WithEndpoint(MINIO_ADDRESS)
                .WithCredentials(accessKey, secretKey)
                .Build();
        }

        public static MinioClient GetMinioClient(string accessKey, string secretKey, string minioIpAddr = null)
        {
            AssertValidInput(accessKey, secretKey);

            if (minioIpAddr != null)
            {
                MINIO_ADDRESS = minioIpAddr;
            }

            return (MinioClient)new MinioClient()
                .WithEndpoint(MINIO_ADDRESS)
                .WithCredentials(accessKey, secretKey)
                .Build();
        }

        public static bool IsMinioServerAccessible(string address = null)
        {
            address ??= MINIO_ADDRESS;

            try
            {

                Console.WriteLine("Checking if minio server is accessible...");
                using (HttpClient client = new HttpClient())
                {
                    var response = client.GetAsync($"http://{address}/minio/health/live").GetAwaiter().GetResult();
                    return response.IsSuccessStatusCode;
                }
            }
            catch
            {
                return false;
            }
        }

        public static void DownloadObject(string bucketName, string objectName, string outputPath)
        {
            AssertValidInput(bucketName, objectName, outputPath);
            AssertMinioClientInitialized();

            try
            {
                var args = new GetObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithFile(outputPath);

                minioClient.GetObjectAsync(args).GetAwaiter().GetResult();
                Console.WriteLine($"Object {objectName} downloaded successfully to {outputPath}");
            }
            catch
            {
                Console.WriteLine($"Error downloading object {objectName} to {outputPath}");
            }
        }

        public static void DownloadFolder(string bucketName, string folderName, string outputFolder)
        {
            AssertValidInput(bucketName, folderName, outputFolder);
            AssertMinioClientInitialized();

            try
            {
                Directory.CreateDirectory(outputFolder);

                var listArgs = new ListObjectsArgs()
                    .WithBucket(bucketName)
                    .WithPrefix(folderName)
                    .WithRecursive(true);

                var objects = minioClient.ListObjectsAsync(listArgs).ToEnumerable().ToList();

                foreach (var obj in objects)
                {
                    string objectName = obj.Key;
                    string relativePath = Path.GetRelativePath(folderName, objectName);
                    string outputPath = Path.Combine(outputFolder, relativePath);

                    if (!File.Exists(outputPath))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                        var args = new GetObjectArgs()
                            .WithBucket(bucketName)
                            .WithObject(objectName)
                            .WithFile(outputPath);

                        minioClient.GetObjectAsync(args).GetAwaiter().GetResult();
                        Console.WriteLine($"Downloaded: {objectName}");
                    }
                    else
                    {
                        Console.WriteLine($"{objectName} already exists.");
                    }
                }
            }
            catch
            {
                Console.WriteLine($"Error downloading folder {folderName}");
            }
        }

        public static List<string> GetListOfBuckets()
        {
            AssertMinioClientInitialized();
            var buckets = minioClient.ListBucketsAsync().GetAwaiter().GetResult();
            return buckets.Buckets.Select(b => b.Name).ToList();
        }

        public static bool CheckIfBucketExists(string bucketName)
        {
            AssertValidInput(bucketName);
            AssertMinioClientInitialized();
            return minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName)).GetAwaiter().GetResult();
        }

        public static void CreateBucket(string bucketName)
        {
            AssertValidInput(bucketName);
            AssertMinioClientInitialized();
            minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucketName)).GetAwaiter().GetResult();
            Console.WriteLine($"Bucket: {bucketName} successfully created...");
        }

        public static void RemoveBucket(string bucketName)
        {
            AssertValidInput(bucketName);
            AssertMinioClientInitialized();
            minioClient.RemoveBucketAsync(new RemoveBucketArgs().WithBucket(bucketName)).GetAwaiter().GetResult();
            Console.WriteLine($"Bucket: {bucketName} successfully deleted...");
        }

        public static List<string> GetListOfObjects(string bucketName)
        {
            AssertValidInput(bucketName);
            AssertMinioClientInitialized();
            var objects = minioClient.ListObjectsAsync(new ListObjectsArgs().WithBucket(bucketName)).ToEnumerable();
            return objects.Select(obj => obj.Key.Replace("/", "")).ToList();
        }

        public static List<string> GetListOfObjectsWithPrefix(string bucketName, string prefix)
        {
            AssertValidInput(bucketName, prefix);
            AssertMinioClientInitialized();
            var objects = minioClient.ListObjectsAsync(new ListObjectsArgs().WithBucket(bucketName).WithPrefix(prefix).WithRecursive(true)).ToEnumerable();
            return objects.Select(obj => obj.Key).ToList();
        }

        public static void UploadFromFile(string bucketName, string objectName, string filePath)
        {
            AssertValidInput(bucketName, objectName, filePath);
            AssertMinioClientInitialized();

            try
            {
                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithFileName(filePath);

                minioClient.PutObjectAsync(putObjectArgs).GetAwaiter().GetResult();
                Console.WriteLine($"Uploaded: {objectName}");
            }
            catch
            {
                Console.WriteLine($"Error uploading {objectName}");
            }
        }

        public static void UploadData(string bucketName, string objectName, byte[] data)
        {
            AssertValidInput(bucketName, objectName, data);
            AssertMinioClientInitialized();

            try
            {
                using (var stream = new MemoryStream(data))
                {
                    var putObjectArgs = new PutObjectArgs()
                        .WithBucket(bucketName)
                        .WithObject(objectName)
                        .WithStreamData(stream)
                        .WithObjectSize(stream.Length);

                    minioClient.PutObjectAsync(putObjectArgs).GetAwaiter().GetResult();
                }

                Console.WriteLine($"Uploaded: {objectName}");
            }
            catch
            {
                Console.WriteLine($"Error uploading {objectName}");
            }
        }

        public static void RemoveObject(string bucketName, string objectName)
        {
            AssertValidInput(bucketName, objectName);
            AssertMinioClientInitialized();

            try
            {
                var removeObjectArgs = new RemoveObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName);

                minioClient.RemoveObjectAsync(removeObjectArgs).GetAwaiter().GetResult();
                Console.WriteLine($"Removed: {objectName}");
            }
            catch
            {
                Console.WriteLine($"Error removing {objectName}");
            }
        }
        public static bool IsObjectExists(MinioClient client, string bucketName, string objectName)
        {
            try
            {
                var statObjectArgs = new StatObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName);

                var result = client.StatObjectAsync(statObjectArgs).GetAwaiter().GetResult();

                // If object exists, return true
                if (!string.IsNullOrEmpty(result.ObjectName))
                {
                    return true;
                }
            }
            catch (Exception)
            {
                // Return false if an error occurs
                return false;
            }

            // Return false if object does not exist
            return false;
        }



        private static void AssertMinioClientInitialized()
        {
            if (minioClient == null)
            {
                throw new InvalidOperationException("Minio client is not initialized.");
            }
        }

        private static void AssertValidInput(params object[] parameters)
        {
            foreach (var param in parameters)
            {
                if (param == null)
                {
                    throw new ArgumentNullException(nameof(param));
                }
            }
        }
    }
}
