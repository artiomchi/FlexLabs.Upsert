namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.Runners.Models;

public class TestEntityWithNullableKey
{
    public int ID { get; set; }
    public int ID1 { get; set; }
    public int? ID2 { get; set; }
    public string Name { get; set; }
    public string Status { get; set; }
    public int Total { get; set; }
}
