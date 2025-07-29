using Microsoft.AspNetCore.Mvc;

namespace Restaurant_System.Models
{
    public class UploadImageRequest
    {
        [FromForm(Name = "file")]
        public IFormFile? File { get; set; }
    }

}
