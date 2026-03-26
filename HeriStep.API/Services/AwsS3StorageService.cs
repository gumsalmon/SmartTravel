using HeriStep.API.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace HeriStep.API.Services
{
    public class AwsS3StorageService : IFileStorageService
    {
        public async Task<bool> DeleteFileAsync(string fileUrl)
        {
            // TODO: Call AmazonS3Client DeleteObjectRequest
            await Task.Delay(100); 
            return true;
        }

        public async Task<string> UploadFileAsync(IFormFile file, string folderName)
        {
            // TODO: Tích hợp thư viện AWSSDK.S3 và đẩy s3Client.PutObjectAsync()
            // Tạm thời trả giả định 1 URL S3 ảo để chứng minh concept
            await Task.Delay(200);
            var fileName = Guid.NewGuid().ToString() + System.IO.Path.GetExtension(file.FileName);
            return $"https://smarttravel-vinhkhanh-bucket.s3.ap-southeast-1.amazonaws.com/{folderName}/{fileName}";
        }
    }
}
