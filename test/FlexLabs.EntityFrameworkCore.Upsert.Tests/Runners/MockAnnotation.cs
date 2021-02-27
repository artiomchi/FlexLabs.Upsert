using Microsoft.EntityFrameworkCore.Infrastructure;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.Runners
{
    public class MockAnnotation : IAnnotation
    {
        public MockAnnotation(string name)
        {
            Value = name;
        }

        public string Name => null;
        public object Value { get; private set; }
    }
}
