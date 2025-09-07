using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace ThesisNest.Services
{
    public interface IFileTextExtractor
    {
        Task<string> ExtractTextAsync(IFormFile file);
    }
}
