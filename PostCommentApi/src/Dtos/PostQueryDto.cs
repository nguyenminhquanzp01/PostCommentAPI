public class PostQueryDto
{
    public string? Keyword { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Sort { get; set; }  // "createdAt" or "-createdAt"
    public int Limit { get; set; } = 20;
    public int Offset { get; set; } = 0;
}