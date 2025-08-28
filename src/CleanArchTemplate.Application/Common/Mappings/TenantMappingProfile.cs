using AutoMapper;
using CleanArchTemplate.Application.Common.Models;
using CleanArchTemplate.Domain.Entities;
using System.Text.Json;

namespace CleanArchTemplate.Application.Common.Mappings;

/// <summary>
/// AutoMapper profile for tenant-related mappings
/// </summary>
public class TenantMappingProfile : Profile
{
    public TenantMappingProfile()
    {
        CreateMap<Tenant, TenantInfo>()
            .ForMember(dest => dest.Configuration, opt => opt.MapFrom(src => ParseConfiguration(src.Configuration)));

        CreateMap<TenantInfo, Tenant>()
            .ForMember(dest => dest.Configuration, opt => opt.MapFrom(src => SerializeConfiguration(src.Configuration)))
            .ForMember(dest => dest.DomainEvents, opt => opt.Ignore());

        // TenantUsageMetric mappings
        CreateMap<Domain.Entities.TenantUsageMetric, Models.TenantUsageMetric>()
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => ParseTags(src.Tags)))
            .ForMember(dest => dest.Metadata, opt => opt.MapFrom(src => ParseConfiguration(src.Metadata)));

        CreateMap<Models.TenantUsageMetric, Domain.Entities.TenantUsageMetric>()
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => SerializeTags(src.Tags)))
            .ForMember(dest => dest.Metadata, opt => opt.MapFrom(src => SerializeConfiguration(src.Metadata)));
    }

    /// <summary>
    /// Parses JSON configuration string to dictionary
    /// </summary>
    /// <param name="configurationJson">The JSON configuration string</param>
    /// <returns>Configuration dictionary</returns>
    private static Dictionary<string, object> ParseConfiguration(string configurationJson)
    {
        if (string.IsNullOrWhiteSpace(configurationJson))
            return new Dictionary<string, object>();

        try
        {
            var jsonDocument = JsonDocument.Parse(configurationJson);
            return JsonElementToDictionary(jsonDocument.RootElement);
        }
        catch (JsonException)
        {
            return new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Serializes configuration dictionary to JSON string
    /// </summary>
    /// <param name="configuration">The configuration dictionary</param>
    /// <returns>JSON configuration string</returns>
    private static string SerializeConfiguration(Dictionary<string, object> configuration)
    {
        if (configuration == null || !configuration.Any())
            return "{}";

        try
        {
            return JsonSerializer.Serialize(configuration);
        }
        catch (JsonException)
        {
            return "{}";
        }
    }

    /// <summary>
    /// Parses JSON tags string to dictionary
    /// </summary>
    /// <param name="tagsJson">The JSON tags string</param>
    /// <returns>Tags dictionary</returns>
    private static Dictionary<string, string> ParseTags(string tagsJson)
    {
        if (string.IsNullOrWhiteSpace(tagsJson))
            return new Dictionary<string, string>();

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(tagsJson) ?? new Dictionary<string, string>();
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>();
        }
    }

    /// <summary>
    /// Serializes tags dictionary to JSON string
    /// </summary>
    /// <param name="tags">The tags dictionary</param>
    /// <returns>JSON tags string</returns>
    private static string SerializeTags(Dictionary<string, string> tags)
    {
        if (tags == null || !tags.Any())
            return "{}";

        try
        {
            return JsonSerializer.Serialize(tags);
        }
        catch (JsonException)
        {
            return "{}";
        }
    }

    /// <summary>
    /// Converts JsonElement to dictionary
    /// </summary>
    /// <param name="element">The JSON element</param>
    /// <returns>Dictionary representation</returns>
    private static Dictionary<string, object> JsonElementToDictionary(JsonElement element)
    {
        var dictionary = new Dictionary<string, object>();

        foreach (var property in element.EnumerateObject())
        {
            dictionary[property.Name] = JsonElementToObject(property.Value);
        }

        return dictionary;
    }

    /// <summary>
    /// Converts JsonElement to appropriate object type
    /// </summary>
    /// <param name="element">The JSON element</param>
    /// <returns>Object representation</returns>
    private static object JsonElementToObject(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.TryGetInt32(out var intValue) ? intValue : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null!,
            JsonValueKind.Object => JsonElementToDictionary(element),
            JsonValueKind.Array => element.EnumerateArray().Select(JsonElementToObject).ToArray(),
            _ => element.ToString()
        };
    }
}