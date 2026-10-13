namespace HighConvertAI.Server.Models.ApiResponse
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public T? Data { get; set; }
    }

    public class ApiResponse
    {
        public bool Success { get; set; }


        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
