using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FlexLabs.EntityFrameworkCore.Upsert.Internal;

/// <summary>
/// SQL Server and SQLite expect JSON values to be already serialized.
/// So we need to serialize them here with the same logic EF Core uses.
/// Since EF Core 10 removed the serialization in the internals we used before,
/// we are now forced to rebuild the serialization logic here.
/// --------------------------------
/// This is a json serialization extension based on <see cref="Microsoft.EntityFrameworkCore.Query.Internal.RelationalJsonUtilities"/>
/// It can be used to JSON serialize owned json entities (INavigation).
/// The original `RelationalJsonUtilities` only supports complex types, not owned entities.
/// </summary>
internal static class RelationalJsonHelper
{
    /// <summary>
    /// Based on json utilities for IComplexType: https://github.com/dotnet/efcore/blob/v10.0.0/src/EFCore.Relational/Query/Internal/RelationalJsonUtilities.cs
    /// and modified to support INavigation inspired by: https://github.com/dotnet/efcore/blob/d1731e0e2b3a900fa94f78b3c6664c47c5de69fd/src/EFCore.Relational/Update/ModificationCommand.cs#L883-L1028
    /// </summary>
    public static string? SerializeToJson(INavigation navigation, object? value)
    {
        // Note that we treat toplevel null differently: we return a relational NULL for that case. For nested nulls,
        // we return JSON null string (so you get { "foo": null })
        if (value is null)
        {
            return null;
        }

        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false });

        WriteJsonNavigation(writer, navigation, value, navigation.IsCollection);
        writer.Flush();

        return Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int)stream.Length);

        void WriteJsonNavigation(Utf8JsonWriter writer, INavigation navigation, object? value, bool collection)
        {
            if (collection)
            {
                if (value is null)
                {
                    writer.WriteNullValue();
                    return;
                }

                writer.WriteStartArray();

                foreach (var element in (IEnumerable)value)
                {
                    WriteJsonObjectNavigation(writer, navigation, element);
                }

                writer.WriteEndArray();
                return;
            }

            WriteJsonObjectNavigation(writer, navigation, value);
        }

        void WriteJsonObjectNavigation(Utf8JsonWriter writer, INavigation navigation, object? objectValue)
        {
            if (objectValue is null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();

            foreach (var property in navigation.TargetEntityType.GetProperties().Where(p => !p.IsShadowProperty()))
            {
                var jsonPropertyName = property.GetJsonPropertyName();
                Debug.Assert(jsonPropertyName is not null);
                writer.WritePropertyName(jsonPropertyName);

                var propertyValue = property.GetGetter().GetClrValue(objectValue);
                if (propertyValue is null)
                {
                    if (!property.IsNullable)
                    {
                        throw new InvalidOperationException(RelationalStrings.NullValueInRequiredJsonProperty(property.Name));
                    }

                    writer.WriteNullValue();
                }
                else
                {
                    var jsonValueReaderWriter = property.GetJsonValueReaderWriter() ?? property.GetTypeMapping().JsonValueReaderWriter;
                    Debug.Assert(jsonValueReaderWriter is not null, "Missing JsonValueReaderWriter on JSON property");
                    jsonValueReaderWriter.ToJson(writer, propertyValue);
                }
            }

            foreach (var complexProperty in navigation.TargetEntityType.GetComplexProperties())
            {
                var jsonPropertyName = complexProperty.GetJsonPropertyName();
                Debug.Assert(jsonPropertyName is not null);
                writer.WritePropertyName(jsonPropertyName);

                var propertyValue = complexProperty.GetGetter().GetClrValue(objectValue);
                if (propertyValue is null && !complexProperty.IsNullable)
                {
                    throw new InvalidOperationException(RelationalStrings.NullValueInRequiredJsonProperty(complexProperty.Name));
                }

                WriteJson(writer, complexProperty.ComplexType, propertyValue, complexProperty.IsCollection);
            }

            foreach (var property in navigation.TargetEntityType.GetNavigations().Where(_ => _.ForeignKey.IsOwnership))
            {
                // skip back-references to the parent
                // https://github.com/dotnet/efcore/blob/d1731e0e2b3a900fa94f78b3c6664c47c5de69fd/src/EFCore.Relational/Update/ModificationCommand.cs#L1005C17-L1009C18
                if (property.IsOnDependent)
                {
                    continue;
                }

                var jsonPropertyName = property.TargetEntityType.GetJsonPropertyName();
                Debug.Assert(jsonPropertyName is not null);
                writer.WritePropertyName(jsonPropertyName);

                var propertyValue = property.GetGetter().GetClrValue(objectValue);
                if (propertyValue is null && property.ForeignKey.IsRequired)
                {
                    throw new InvalidOperationException(RelationalStrings.NullValueInRequiredJsonProperty(property.Name));
                }

                WriteJsonNavigation(writer, property, propertyValue, property.IsCollection);
            }

            writer.WriteEndObject();
        }

        void WriteJson(Utf8JsonWriter writer, IComplexType complexType, object? value, bool collection)
        {
            if (collection)
            {
                if (value is null)
                {
                    writer.WriteNullValue();
                    return;
                }

                writer.WriteStartArray();

                foreach (var element in (IEnumerable)value)
                {
                    WriteJsonObject(writer, complexType, element);
                }

                writer.WriteEndArray();
                return;
            }

            WriteJsonObject(writer, complexType, value);
        }

        void WriteJsonObject(Utf8JsonWriter writer, IComplexType complexType, object? objectValue)
        {
            if (objectValue is null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();

            foreach (var property in complexType.GetProperties())
            {
                var jsonPropertyName = property.GetJsonPropertyName();
                Debug.Assert(jsonPropertyName is not null);
                writer.WritePropertyName(jsonPropertyName);

                var propertyValue = property.GetGetter().GetClrValue(objectValue);
                if (propertyValue is null)
                {
                    if (!property.IsNullable)
                    {
                        throw new InvalidOperationException(RelationalStrings.NullValueInRequiredJsonProperty(property.Name));
                    }

                    writer.WriteNullValue();
                }
                else
                {
                    var jsonValueReaderWriter = property.GetJsonValueReaderWriter() ?? property.GetTypeMapping().JsonValueReaderWriter;
                    Debug.Assert(jsonValueReaderWriter is not null, "Missing JsonValueReaderWriter on JSON property");
                    jsonValueReaderWriter.ToJson(writer, propertyValue);
                }
            }

            foreach (var complexProperty in complexType.GetComplexProperties())
            {
                var jsonPropertyName = complexProperty.GetJsonPropertyName();
                Debug.Assert(jsonPropertyName is not null);
                writer.WritePropertyName(jsonPropertyName);

                var propertyValue = complexProperty.GetGetter().GetClrValue(objectValue);

                if (propertyValue is null && !complexProperty.IsNullable)
                {
                    throw new InvalidOperationException(RelationalStrings.NullValueInRequiredJsonProperty(complexProperty.Name));
                }

                WriteJson(writer, complexProperty.ComplexType, propertyValue, complexProperty.IsCollection);
            }

            writer.WriteEndObject();
        }
    }
}
