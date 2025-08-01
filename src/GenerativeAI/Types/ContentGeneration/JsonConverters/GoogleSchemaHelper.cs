﻿using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using GenerativeAI.Utility;

#if NET8_0_OR_GREATER || NET462_OR_GREATER || NETSTANDARD2_0
using System.Text.Json.Schema;
#else
using Json.More;
using Json.Schema;
using Json.Schema.Generation;
#endif

namespace GenerativeAI.Types;

/// <summary>
/// Provides helper methods for converting JSON schemas to Google-compatible formats.
/// </summary>
public static class GoogleSchemaHelper
{
    /// <summary>
    /// Converts a JSON document that contains valid json schema <see href="https://json-schema.org/specification"/> as e.g. 
    /// generated by <code>Microsoft.Extensions.AI.AIJsonUtilities.CreateJsonSchema</code> or <code>JsonSchema.Net</code>'s
    /// JsonSchemaBuilder to a subset that is compatible with Google's APIs.
    /// </summary>
    /// <param name="constructedSchema">Generated, valid json schema.</param>
    /// <returns>Subset of the given json schema in a google-comaptible format.</returns>
    public static Schema? ConvertToCompatibleSchemaSubset(JsonDocument constructedSchema)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(constructedSchema);
        var node = constructedSchema.RootElement.AsNode();

        ConvertNullableProperties(node);


        var x1 = node;
        if (x1 == null)
            return null;
        var x2 = x1.ToJsonString();
        var schema = JsonSerializer.Deserialize(x2, SchemaSourceGenerationContext.Default.Schema);
        return schema;
#else
        if(constructedSchema == null)
            throw new ArgumentNullException(nameof(constructedSchema));
        var schema = JsonSerializer.Deserialize<Schema>(constructedSchema.RootElement.GetRawText());
        return schema;
#endif
    }

    /// <summary>
    /// Converts a JSON document that contains valid json schema <see href="https://json-schema.org/specification"/> as e.g. 
    /// generated by <code>Microsoft.Extensions.AI.AIJsonUtilities.CreateJsonSchema</code> or <code>JsonSchema.Net</code>'s
    /// JsonSchemaBuilder to a subset that is compatible with Google's APIs.
    /// </summary>
    /// <param name="node">Generated, valid json schema as a JsonNode.</param>
    /// <returns>Subset of the given json schema in a google-comaptible format.</returns>
    public static Schema ConvertToCompatibleSchemaSubset(JsonNode node)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(node);
        ConvertNullableProperties(node);


        var x1 = node;
        var x2 = x1.ToJsonString();
        var schema = JsonSerializer.Deserialize(x2, SchemaSourceGenerationContext.Default.Schema);
        if (schema == null)
            throw new InvalidOperationException("Failed to deserialize schema. The JSON content was invalid or empty.");
        return schema;
#else
        if (node == null) throw new ArgumentNullException(nameof(node));
        var schema = JsonSerializer.Deserialize<Schema>(node.ToJsonString());
        if (schema == null)
            throw new InvalidOperationException("Failed to deserialize schema. The JSON content was invalid or empty.");
        return schema;
#endif
    }

    private static void ConvertNullableProperties(JsonNode? node)
    {
        // If the node is an object, look for a "type" property or nested definitions
        if (node is JsonObject obj)
        {
            // If "type" is an array, remove "null" and collapse if it leaves only one type
            if (obj.TryGetPropertyValue("type", out var typeValue) && typeValue is JsonArray array)
            {
                if (array.Count == 2)
                {
                    var notNullTypes = array.Where(x => x is not null && x.GetValue<string>() != "null").ToList();
                    if (notNullTypes.Count == 1)
                    {
                        obj["type"] = notNullTypes[0]!.GetValue<string>();
                        obj["nullable"] = true;
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            $"Google's API for strucutured output requires every property to have one defined type, not multiple options. Path: {obj.GetPath()} Schema: {obj.ToJsonString()}");
                    }
                }
                else if (array.Count > 2)
                {
                    throw new InvalidOperationException(
                        $"Google's API for strucutured output requires every property to have one defined type, not multiple options. Path: {obj.GetPath()} Schema: {obj.ToJsonString()}");
                }
            }

            // Recursively convert any nested schema in "properties"
            if (obj.TryGetPropertyValue("properties", out var propertiesNode) &&
                propertiesNode is JsonObject propertiesObj)
            {
                foreach (var property in propertiesObj)
                {
                    ConvertNullableProperties(property.Value);
                }
            }

            if (obj.TryGetPropertyValue("type", out var newTypeValue)
                && newTypeValue is JsonNode
                && newTypeValue.GetValueKind() == JsonValueKind.String
                && "object".Equals(newTypeValue.GetValue<string>(), StringComparison.OrdinalIgnoreCase)
                && propertiesNode is not JsonObject)
            {
                throw new InvalidOperationException(
                    $"Google's API for strucutured output requires every object to have predefined properties. Notably, it does not support dictionaries. Path: {obj.GetPath()} Schema: {obj.ToJsonString()}");
            }

            // Recursively convert any nested schema in "items"
            if (obj.TryGetPropertyValue("items", out var itemsNode))
            {
                ConvertNullableProperties(itemsNode);
            }
        }

        // If the node is an array, traverse each element
        if (node is JsonArray arr)
        {
            foreach (var element in arr)
            {
                ConvertNullableProperties(element);
            }
        }
    }

    /// <summary>
    /// Converts a .NET type to a Google-compatible schema.
    /// </summary>
    /// <typeparam name="T">The type to convert to a schema.</typeparam>
    /// <param name="jsonOptions">Optional JSON serializer options to use during conversion.</param>
    /// <returns>A Google-compatible schema representing the specified type.</returns>
    public static Schema ConvertToSchema<T>(JsonSerializerOptions? jsonOptions = null)
    {
#if NET8_0_OR_GREATER || NET462_OR_GREATER || NETSTANDARD2_0
      
        if (jsonOptions == null)
            jsonOptions = DefaultSerializerOptions.GenerateObjectJsonOptions;

        var newJsonOptions = new JsonSerializerOptions(jsonOptions)
        {
            NumberHandling = JsonNumberHandling.Strict
        };

        newJsonOptions.Converters.Clear();
        var typeInfo = newJsonOptions.GetTypeInfo(typeof(T));

        var schema = GetSchema(typeInfo,null);
        return JsonSerializer.Deserialize(schema.ToJsonString(),TypesSerializerContext.Default.Schema)?? ConvertToCompatibleSchemaSubset(schema);

#else
        return ConvertToSchema(typeof(T), jsonOptions);
#endif
    }

    /// <summary>
    /// Converts a .NET type to a Google-compatible schema.
    /// </summary>
    /// <param name="type">The type to convert to a schema.</param>
    /// <param name="jsonOptions">Optional JSON serializer options to use during conversion.</param>
    /// <param name="descriptionTable">Optional dictionary containing custom descriptions for properties.</param>
    /// <returns>A Google-compatible schema representing the specified type.</returns>
    public static Schema ConvertToSchema(Type type, JsonSerializerOptions? jsonOptions = null,
        Dictionary<string, string>? descriptionTable = null)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(type);
#else
        if (type == null) throw new ArgumentNullException(nameof(type));
#endif

#if NET8_0_OR_GREATER || NET462_OR_GREATER || NETSTANDARD2_0
     
        if (jsonOptions == null)
            jsonOptions = DefaultSerializerOptions.GenerateObjectJsonOptions;

        var newJsonOptions = new JsonSerializerOptions(jsonOptions)
        {
            NumberHandling = JsonNumberHandling.Strict,
        };

        newJsonOptions.Converters.Clear();

        var typeInfo = newJsonOptions.GetTypeInfo(type);
        
        var schema = GetSchema(typeInfo, descriptionTable);
        return JsonSerializer.Deserialize(schema.ToJsonString(),TypesSerializerContext.Default.Schema)?? ConvertToCompatibleSchemaSubset(schema);

#else
        var generatorConfig = new SchemaGeneratorConfiguration();
        var builder = new JsonSchemaBuilder();

        var constructedSchema = builder
            .FromType(type, generatorConfig)
            .Build().ToJsonDocument();

        //Work around to avoid type as array
        var schema = GoogleSchemaHelper.ConvertToCompatibleSchemaSubset(constructedSchema);
        if (schema == null)
            throw new InvalidOperationException($"Failed to convert schema for type {type.Name}.");
        return schema;
#endif
    }
    
#if NET8_0_OR_GREATER || NET462_OR_GREATER || NETSTANDARD2_0
    private static JsonNode GetSchema(JsonTypeInfo typeInfo, Dictionary<string, string>? dics)
    {
        if (dics == null)
            dics = new Dictionary<string, string>();
        var schema = typeInfo.GetJsonSchemaAsNode(exporterOptions: new JsonSchemaExporterOptions() {
            TransformSchemaNode = (context, schema) =>
            {
                if (context.TypeInfo.Type.IsEnum)
                {
                    schema["type"] = "string";
                    var array = new JsonArray();
                    foreach (var name in context.TypeInfo.Type.GetEnumNames())
                    {
                        array.Add(name);
                    }

                    schema["enum"] = array;
                }
               
                ExtractDescription(context, schema, dics);
                if (context.PropertyInfo == null)
                    return schema;
#if NET8_0_OR_GREATER 
                if (context.TypeInfo.Type.Name == "DateOnly" || context.TypeInfo.Type.Name == "TimeOnly")
                {
                    schema["format"] = "date-time";
                }
#endif 
                return schema;
            }});
        return schema;
    }
    
    private static void ExtractDescription(JsonSchemaExporterContext context, JsonNode schema, Dictionary<string, string> dics)
    {
        // Determine if a type or property and extract the relevant attribute provider.
        ICustomAttributeProvider? attributeProvider = context.PropertyInfo is not null
            ? context.PropertyInfo.AttributeProvider
            : context.TypeInfo.Type;

        var description = attributeProvider != null ? TypeDescriptionExtractor.GetDescription(attributeProvider) : null;
        if (string.IsNullOrEmpty(description))
        {
            if (context.PropertyInfo is null)
            {
                var propertyName = context.TypeInfo.Type.Name.ToCamelCase();
                dics.TryGetValue(propertyName, out description);
            }
        }

        FixType(schema);
        
        // Apply description attribute to the generated schema.
        if (description is not null)
        {
            if (schema is not JsonObject jObj)
            {
                // Handle the case where the schema is a Boolean.
                JsonValueKind valueKind = schema.GetValueKind();
                Debug.Assert(valueKind is JsonValueKind.True or JsonValueKind.False);
                schema = jObj = new JsonObject();
                if (valueKind is JsonValueKind.False)
                {
                    jObj.Add("not", true);
                }
            }
            
            jObj.Insert(0, "description", description);
        }
    }

    private static void FixType(JsonNode schema)
    {
        // If "type" is an array, remove "null" and collapse if it leaves only one type
        var typeValue = schema["type"];
        if (typeValue!= null && typeValue is JsonArray array)
        {
            if (array.Count == 2)
            {
                var notNullTypes = array.Where(x => x is not null && x.GetValue<string>() != "null").ToList();
                if (notNullTypes.Count == 1)
                {
                    schema["type"] = notNullTypes[0]!.GetValue<string>();
                    schema["nullable"] = true;
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Google's API for strucutured output requires every property to have one defined type, not multiple options. Path: {schema.GetPath()} Schema: {schema.ToJsonString()}");
                }
            }
            else if (array.Count > 2)
            {
                throw new InvalidOperationException(
                    $"Google's API for strucutured output requires every property to have one defined type, not multiple options. Path: {schema.GetPath()} Schema: {schema.ToJsonString()}");
            }
        }
    }

#endif
}