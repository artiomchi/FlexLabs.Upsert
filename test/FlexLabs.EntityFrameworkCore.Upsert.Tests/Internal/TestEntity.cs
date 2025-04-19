using System;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.Internal
{
    public class TestEntity
    {
        public int ID { get; set; }
        public int Num1 { get; set; }
        public int Num2 { get; set; }
        public int? NumNullable1 { get; set; }
        public string Text1 { get; set; }
        public string Text2 { get; set; }
        public bool Boolean { get; set; }
        public DateTime Updated { get; set; }
        public OwnedChildEntity Child { get; set; }
    }

    public class OwnedChildEntity {
        public int ID { get; set; }
        public int Num1 { get; set; }
        public int Num2 { get; set; }
        public int? NumNullable1 { get; set; }
        public string Text1 { get; set; }
        public string Text2 { get; set; }
        public DateTime Updated { get; set; }
        public NestedOwnedChildEntity NestedChild { get; set; }
    }

    public class NestedOwnedChildEntity {
        public int Num1 { get; set; }
        public int Num2 { get; set; }
        public string Text1 { get; set; }
    }
}
