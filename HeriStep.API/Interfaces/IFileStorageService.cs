using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace HeriStep.API.Interfaces
{
    public interface IFileStorageService
    {
        Task<string> UploadFileAsync(IFormFile file, string folderName);
        Task<bool> DeleteFileAsync(string fileUrl);
    }
}
