namespace Business.DTOS
{
    public class PersonalDataExportDto
    {
        public bool Success { get; set; }
        public bool RequiresLogin { get; set; }
        public byte[]? Content { get; set; }
        public string ContentType { get; set; } = "application/json";
        public string FileName { get; set; } = string.Empty;
    }
}
