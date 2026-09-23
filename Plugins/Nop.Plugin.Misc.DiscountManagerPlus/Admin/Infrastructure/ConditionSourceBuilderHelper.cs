using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Nop.Plugin.Misc.DiscountManagerPlus.Admin.Models;
using Nop.Plugin.Misc.DiscountManagerPlus.Domain;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Admin.Infrastructure
{
    public static class ConditionSourceBuilderHelper
    {
        private class ParsedConditionSourceEntry
        {
            public ParsedConditionSourceEntry()
            {
                OptionIds = new List<int>();
                OptionNames = new List<string>();
            }

            public int EntryId { get; set; }
            public int? RangeMin { get; set; }
            public int? RangeMax { get; set; }
            public IList<int> OptionIds { get; private set; }
            public IList<string> OptionNames { get; private set; }
        }

        public static bool IsSessionSourceType(int sourceTypeId)
        {
            if (!Enum.IsDefined(typeof(ConditionSourceType), sourceTypeId))
                return false;

            var sourceType = (ConditionSourceType)sourceTypeId;
            return sourceType == ConditionSourceType.DeviceType ||
                   sourceType == ConditionSourceType.SalesChannel ||
                   sourceType == ConditionSourceType.CampaignSource ||
                   sourceType == ConditionSourceType.ReferralSource;
        }

        public static bool IsFixedTokenSourceType(int sourceTypeId)
        {
            if (!Enum.IsDefined(typeof(ConditionSourceType), sourceTypeId))
                return false;

            var sourceType = (ConditionSourceType)sourceTypeId;
            return sourceType == ConditionSourceType.DeviceType ||
                   sourceType == ConditionSourceType.SalesChannel;
        }

        public static bool IsTagTokenSourceType(int sourceTypeId)
        {
            if (!Enum.IsDefined(typeof(ConditionSourceType), sourceTypeId))
                return false;

            var sourceType = (ConditionSourceType)sourceTypeId;
            return sourceType == ConditionSourceType.CampaignSource ||
                   sourceType == ConditionSourceType.ReferralSource;
        }

        public static bool SupportsRange(int sourceTypeId)
        {
            if (!Enum.IsDefined(typeof(ConditionSourceType), sourceTypeId))
                return false;

            var sourceType = (ConditionSourceType)sourceTypeId;
            return sourceType == ConditionSourceType.Products ||
                   sourceType == ConditionSourceType.Categories ||
                   sourceType == ConditionSourceType.Manufacturers ||
                   sourceType == ConditionSourceType.Vendors ||
                   sourceType == ConditionSourceType.SpecificationAttributeOptions ||
                   sourceType == ConditionSourceType.ProductAttributeValues ||
                   sourceType == ConditionSourceType.ExpiryDays;
        }

        public static ConditionSourceBuilderStateModel ParseBuilderStateJson(string rawJson)
        {
            if (string.IsNullOrWhiteSpace(rawJson))
                return new ConditionSourceBuilderStateModel();

            try
            {
                var state = JsonConvert.DeserializeObject<ConditionSourceBuilderStateModel>(rawJson);
                return state ?? new ConditionSourceBuilderStateModel();
            }
            catch
            {
                return new ConditionSourceBuilderStateModel();
            }
        }

        public static string SerializeBuilderStateJson(ConditionSourceBuilderStateModel state)
        {
            if (state == null)
                state = new ConditionSourceBuilderStateModel();
            if (state.Rows == null)
                state.Rows = new List<ConditionSourceBuilderRowModel>();
            if (state.Tokens == null)
                state.Tokens = new List<string>();
            return JsonConvert.SerializeObject(state);
        }

        public static IList<string> ParseSessionTokens(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return new string[0];

            return raw
                .Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(NormalizeToken)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static string SerializeSourceData(int sourceTypeId, ConditionSourceBuilderStateModel state)
        {
            if (state == null)
                state = new ConditionSourceBuilderStateModel();

            if (sourceTypeId <= 0 || !Enum.IsDefined(typeof(ConditionSourceType), sourceTypeId))
                return string.Empty;

            if (IsSessionSourceType(sourceTypeId))
            {
                var tokens = (state.Tokens ?? new string[0])
                    .Select(NormalizeToken)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return tokens.Any() ? string.Join(", ", tokens) : string.Empty;
            }

            var rows = (state.Rows ?? new ConditionSourceBuilderRowModel[0])
                .Where(row => row != null)
                .Select(row => SerializeRow(sourceTypeId, row))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            return rows.Any() ? string.Join(", ", rows) : string.Empty;
        }

        public static IList<ConditionSourceBuilderRowModel> ParseRows(int sourceTypeId, string raw)
        {
            if (string.IsNullOrWhiteSpace(raw) || sourceTypeId <= 0 || IsSessionSourceType(sourceTypeId))
                return new ConditionSourceBuilderRowModel[0];

            var parsedEntries = ParseEntries(raw);
            if (!parsedEntries.Any())
                return new ConditionSourceBuilderRowModel[0];

            return parsedEntries.Select((entry, index) => new ConditionSourceBuilderRowModel
            {
                Key = "row-" + (index + 1),
                EntryId = entry.EntryId,
                SelectedOptionValues = entry.OptionIds.Select(x => x.ToString()).Concat(entry.OptionNames).ToList(),
                SelectedOptionTexts = entry.OptionNames.ToList(),
                RangeMin = entry.RangeMin,
                RangeMax = entry.RangeMax
            }).ToList();
        }

        public static bool HasConfiguredValues(int sourceTypeId, ConditionSourceBuilderStateModel state)
        {
            if (sourceTypeId <= 0)
                return false;

            if (IsSessionSourceType(sourceTypeId))
                return (state != null && state.Tokens != null) && state.Tokens.Any(x => !string.IsNullOrWhiteSpace(x));

            var rows = state != null && state.Rows != null ? state.Rows : new ConditionSourceBuilderRowModel[0];
            return rows
                .Select(row => SerializeRow(sourceTypeId, row))
                .Any(x => !string.IsNullOrWhiteSpace(x));
        }

        private static string SerializeRow(int sourceTypeId, ConditionSourceBuilderRowModel row)
        {
            if (row == null)
                return string.Empty;

            var sourceType = Enum.IsDefined(typeof(ConditionSourceType), sourceTypeId)
                ? (ConditionSourceType)sourceTypeId
                : 0;

            var supportsZeroId = sourceType == ConditionSourceType.ExpiryDays;
            var entryId = row.EntryId.GetValueOrDefault();
            if (!supportsZeroId && entryId <= 0)
                return string.Empty;
            if (supportsZeroId && entryId < 0)
                return string.Empty;

            var builder = new StringBuilder();
            builder.Append(entryId);

            var optionTokens = BuildOptionTokens(sourceType, row);
            if (optionTokens.Any())
            {
                builder.Append('(');
                builder.Append(string.Join("|", optionTokens));
                builder.Append(')');
            }

            var hasRange = row.RangeMin.HasValue || row.RangeMax.HasValue;
            if (hasRange)
            {
                var min = row.RangeMin.GetValueOrDefault();
                var max = row.RangeMax.GetValueOrDefault();
                builder.Append(':');
                if (row.RangeMin.HasValue && row.RangeMax.HasValue)
                    builder.Append(min + "-" + max);
                else if (row.RangeMin.HasValue)
                    builder.Append(min);
                else
                    builder.Append("0-" + max);
            }

            return builder.ToString();
        }

        private static IList<string> BuildOptionTokens(ConditionSourceType sourceType, ConditionSourceBuilderRowModel row)
        {
            var rawValues = (row.SelectedOptionValues ?? new string[0])
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToList();
            if (!rawValues.Any())
                return new string[0];

            if (sourceType == ConditionSourceType.ProductAttributeValues)
            {
                return rawValues
                    .Select(NormalizeOptionToken)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            return rawValues
                .Select(x =>
                {
                    int id;
                    return int.TryParse(x, out id) && id > 0 ? id.ToString() : NormalizeOptionToken(x);
                })
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string NormalizeOptionToken(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().Trim('"', '\'');
        }

        private static string NormalizeToken(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().Trim('"', '\'').ToLowerInvariant();
        }

        private static IList<ParsedConditionSourceEntry> ParseEntries(string raw)
        {
            var entries = new List<ParsedConditionSourceEntry>();
            if (string.IsNullOrWhiteSpace(raw))
                return entries;

            foreach (var token in SplitOutsideParentheses(raw))
            {
                var text = token != null ? token.Trim() : null;
                if (string.IsNullOrWhiteSpace(text))
                    continue;

                var colonIndex = IndexOfOutsideParentheses(text, ':');
                var rangePart = colonIndex >= 0 ? text.Substring(colonIndex + 1).Trim() : string.Empty;
                var mainPart = colonIndex >= 0 ? text.Substring(0, colonIndex).Trim() : text;

                var openIndex = mainPart.IndexOf('(');
                var closeIndex = mainPart.LastIndexOf(')');
                var idPart = mainPart;
                var optionsPart = string.Empty;
                if (openIndex >= 0 && closeIndex > openIndex)
                {
                    idPart = mainPart.Substring(0, openIndex).Trim();
                    optionsPart = mainPart.Substring(openIndex + 1, closeIndex - openIndex - 1);
                }

                int entryId;
                if (!int.TryParse(idPart, out entryId) || entryId < 0)
                    continue;

                var entry = new ParsedConditionSourceEntry
                {
                    EntryId = entryId
                };

                if (!string.IsNullOrWhiteSpace(rangePart))
                {
                    var rangeTokens = rangePart.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                    if (rangeTokens.Length == 2)
                    {
                        int rangeMin;
                        int rangeMax;
                        if (int.TryParse(rangeTokens[0].Trim(), out rangeMin) && rangeMin >= 0)
                            entry.RangeMin = rangeMin;
                        if (int.TryParse(rangeTokens[1].Trim(), out rangeMax) && rangeMax >= 0)
                            entry.RangeMax = rangeMax;
                    }
                    else
                    {
                        int rangeMinOnly;
                        if (int.TryParse(rangePart, out rangeMinOnly) && rangeMinOnly >= 0)
                            entry.RangeMin = rangeMinOnly;
                    }
                }

                if (!string.IsNullOrWhiteSpace(optionsPart))
                {
                    foreach (var option in optionsPart.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        var trimmedOption = option.Trim();
                        int optionId;
                        if (int.TryParse(trimmedOption, out optionId) && optionId > 0)
                            entry.OptionIds.Add(optionId);
                        else if (!string.IsNullOrWhiteSpace(trimmedOption))
                            entry.OptionNames.Add(NormalizeOptionToken(trimmedOption));
                    }
                }

                entries.Add(entry);
            }

            return entries;
        }

        private static IList<string> SplitOutsideParentheses(string input)
        {
            var parts = new List<string>();
            if (string.IsNullOrWhiteSpace(input))
                return parts;

            var current = new StringBuilder();
            var depth = 0;
            foreach (var ch in input)
            {
                if (ch == '(')
                    depth++;
                else if (ch == ')')
                    depth = Math.Max(0, depth - 1);

                if (ch == ',' && depth == 0)
                {
                    parts.Add(current.ToString());
                    current.Clear();
                    continue;
                }

                current.Append(ch);
            }

            if (current.Length > 0)
                parts.Add(current.ToString());

            return parts;
        }

        private static int IndexOfOutsideParentheses(string input, char token)
        {
            if (string.IsNullOrEmpty(input))
                return -1;

            var depth = 0;
            for (var i = 0; i < input.Length; i++)
            {
                if (input[i] == '(')
                    depth++;
                else if (input[i] == ')')
                    depth = Math.Max(0, depth - 1);

                if (input[i] == token && depth == 0)
                    return i;
            }

            return -1;
        }
    }
}
