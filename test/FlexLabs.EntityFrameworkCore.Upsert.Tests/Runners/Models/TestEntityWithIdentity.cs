namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.Runners.Models
{
    public class TestEntityWithIdentity
    {
        public int ID { get; set; }
        public int Sequence { get; set; }
        public string Name { get; set; }
    }
}
