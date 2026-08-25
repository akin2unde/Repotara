using Repotara.Aggregation;
using Repotara.Metadata;

namespace Repotara.Definition;

/// <summary>
/// Validates a <see cref="ReportDefinition"/> against cached source metadata
/// before it is ever translated into a query: every referenced field must
/// exist and be reportable, every aggregate must be allowed for its field,
/// and every join must reference real keys.
/// </summary>
public static class ReportDefinitionValidator
{
    /// <summary>Validates the given definition, returning every error found.</summary>
    public static ValidationResult Validate(ReportDefinition definition, IReadOnlyDictionary<string, ReportTypeMetadata> metadata)
    {
        var errors = new List<ValidationError>();

        foreach (var sourceName in definition.Types)
        {
            if (metadata.ContainsKey(sourceName) == false)
            {
                errors.Add(new ValidationError { Field = sourceName, Reason = "Unknown report source." });
            }
        }

        if (definition.Joins != null)
        {
            foreach (var join in definition.Joins)
            {
                ValidateJoinSide(join.Left, join.LeftKey, metadata, errors);
                ValidateJoinSide(join.Right, join.RightKey, metadata, errors);
            }
        }

        foreach (var field in definition.Fields)
        {
            ValidateFieldSelection(field, metadata, errors);
        }

        if (definition.GroupBy != null)
        {
            foreach (var groupField in definition.GroupBy)
            {
                ValidateFieldPath(groupField, metadata, errors);
            }

            ValidateFieldsAreGroupedOrAggregated(definition, errors);
        }

        return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors);
    }

    private static void ValidateJoinSide(string sourceName, string key, IReadOnlyDictionary<string, ReportTypeMetadata> metadata, List<ValidationError> errors)
    {
        if (metadata.TryGetValue(sourceName, out var sourceMetadata) == false)
        {
            errors.Add(new ValidationError { Field = sourceName, Reason = "Join references unknown source." });
            return;
        }

        if (sourceMetadata.GetField(key) == null)
        {
            errors.Add(new ValidationError { Field = sourceName + "." + key, Reason = "Join key is not a reportable field." });
        }
    }

    private static void ValidateFieldSelection(ReportFieldSelection field, IReadOnlyDictionary<string, ReportTypeMetadata> metadata, List<ValidationError> errors)
    {
        var hasField = field.Field != null;
        var hasConcat = field.Concat != null;

        if (hasField == false && hasConcat == false)
        {
            errors.Add(new ValidationError { Field = "(unnamed)", Reason = "Field selection must set either Field or Concat." });
            return;
        }

        if (hasField == true && hasConcat == true)
        {
            errors.Add(new ValidationError { Field = field.Field!, Reason = "Field selection cannot set both Field and Concat." });
            return;
        }

        if (hasConcat == true)
        {
            if (field.Concat!.Fields.Count < 2)
            {
                errors.Add(new ValidationError { Field = "Concat", Reason = "Concat requires at least two fields." });
            }

            if (field.Aggregate != null)
            {
                errors.Add(new ValidationError { Field = "Concat", Reason = "Aggregate is not supported on a Concat field." });
            }

            foreach (var concatField in field.Concat.Fields)
            {
                ValidateFieldPath(concatField, metadata, errors);
            }

            return;
        }

        var fieldMetadata = ValidateFieldPath(field.Field!, metadata, errors);
        if (fieldMetadata == null)
        {
            return;
        }

        if (field.Aggregate != null && fieldMetadata.AllowedAggregates.Length > 0)
        {
            if (fieldMetadata.AllowedAggregates.Contains(field.Aggregate.Value) == false)
            {
                errors.Add(new ValidationError
                {
                    Field = field.Field!,
                    Reason = "Aggregate '" + field.Aggregate + "' is not allowed on this field."
                });
            }
        }
    }

    private static FieldMetadata? ValidateFieldPath(string path, IReadOnlyDictionary<string, ReportTypeMetadata> metadata, List<ValidationError> errors)
    {
        try
        {
            return FieldResolver.ResolveField(path, metadata);
        }
        catch (InvalidOperationException ex)
        {
            errors.Add(new ValidationError { Field = path, Reason = ex.Message });
            return null;
        }
    }

    private static void ValidateFieldsAreGroupedOrAggregated(ReportDefinition definition, List<ValidationError> errors)
    {
        foreach (var field in definition.Fields)
        {
            if (field.Field == null)
            {
                continue;
            }

            var isGrouped = definition.GroupBy!.Contains(field.Field);
            var isAggregated = field.Aggregate != null;

            if (isGrouped == false && isAggregated == false)
            {
                errors.Add(new ValidationError
                {
                    Field = field.Field,
                    Reason = "Field must either be in GroupBy or have an Aggregate when GroupBy is used."
                });
            }
        }
    }
}
