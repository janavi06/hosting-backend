// Services/IPrintForwarder.cs
using System.Threading.Tasks;
using Restaurant_Menu.DTOs;

namespace Restaurant_Menu.Services
{
    public interface IPrintForwarder
    {
        /// <summary>
        /// Forwards DTO to the restaurant's local PrintService and returns result.
        /// </summary>
        Task<ForwardResult> ForwardPrintAsync(PrintRequestDto dto, int restaurantId);
    }

    public class ForwardResult
    {
        public bool Success { get; set; }
        public int StatusCode { get; set; } // HTTP status code from agent if available
        public string Message { get; set; }
    }
}
