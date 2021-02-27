using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.Runners
{
    internal class MockProperty : IProperty
    {
        private readonly string _name;
        private readonly IAnnotation _nameAnnotation;

        public MockProperty(string name)
        {
            _name = name;
            _nameAnnotation = new MockAnnotation(name);
        }

        public object this[string name] => _name;
        public IAnnotation FindAnnotation(string name) => name == "Relational:ColumnName" ? _nameAnnotation : null;
        public IEnumerable<IAnnotation> GetAnnotations() => new[] { _nameAnnotation };

        public IEntityType DeclaringEntityType => throw new NotImplementedException();

        public bool IsNullable => throw new NotImplementedException();

        public PropertySaveBehavior BeforeSaveBehavior => throw new NotImplementedException();

        public PropertySaveBehavior AfterSaveBehavior => throw new NotImplementedException();

        public bool IsReadOnlyBeforeSave => throw new NotImplementedException();

        public bool IsReadOnlyAfterSave => throw new NotImplementedException();

        public bool IsStoreGeneratedAlways => throw new NotImplementedException();

        public ValueGenerated ValueGenerated => throw new NotImplementedException();

        public bool IsConcurrencyToken => throw new NotImplementedException();

        public Type ClrType => throw new NotImplementedException();

        public bool IsShadowProperty => throw new NotImplementedException();

        public string Name => throw new NotImplementedException();

        public ITypeBase DeclaringType => throw new NotImplementedException();

        public PropertyInfo PropertyInfo => throw new NotImplementedException();

        public FieldInfo FieldInfo => throw new NotImplementedException();
    }
}
