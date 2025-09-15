using System;

namespace Thesiss.Models.modell
{
    
    public class ErrorViewModel
    {
        
        
        public string RequestId { get; set; }

        public string Message { get; set; }

        public int StatusCode { get; set; }

        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;

        public string StackTrace { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
