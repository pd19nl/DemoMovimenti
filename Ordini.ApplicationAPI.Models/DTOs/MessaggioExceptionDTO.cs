namespace Ordini.ApplicationAPI.Models.DTOs
{
    public class MessaggioExceptionDTO
    {
        public int Status { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
        public string TraceId { get; set; } = string.Empty;
        public object Errors { get; set; }
    }
}
