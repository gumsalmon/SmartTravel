using HeriStep.API.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace HeriStep.API.Services
{
    public class LocalFileStorageService : IFileStorageService
    {
        public async Task<string> UploadFileAsync(IFormFile file, string folderName)
        {
            if (file == null || file.Length == 0) return string.Empty;

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", folderName);
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }
            return $"/uploads/{folderName}/{fileName}";
        }

        public Task<bool> DeleteFileAsync(string fileUrl)
        {
            // Dummy logic delete cho Local File
            return Task.FromResult(true);
        }
    }
}
