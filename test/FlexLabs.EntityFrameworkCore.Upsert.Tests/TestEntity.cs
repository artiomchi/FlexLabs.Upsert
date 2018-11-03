using System;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests
{
    class TestEntity
    {
        public int Num1 { get; set; }
        public int Num2 { get; set; }
        public int? NumNullable1 { get; set; }
        public string Text1 { get; set; }
        public string Text2 { get; set; }
        public DateTime Updated { get; set; }
    }
}
