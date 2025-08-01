// Wrote by Alois de Gouvello https://github.com/aloisdg

// The MIT License (MIT)

// Copyright (c) 2015 Alois de Gouvello

// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

namespace Humanizer;

/// <summary>
/// Contains extension methods for changing a number to Metric representation (ToMetric)
/// and from Metric representation back to the number (FromMetric)
/// </summary>
public static class MetricNumeralExtensions
{
    const int limit = 27;

    static readonly double BigLimit = Math.Pow(10, limit);
    static readonly double SmallLimit = Math.Pow(10, -limit);

    /// <summary>
    /// Symbols is a list of every symbols for the Metric system.
    /// </summary>
    static readonly List<char>[] Symbols =
    [
        ['k', 'M', 'G', 'T', 'P', 'E', 'Z', 'Y'],
        ['m', 'μ', 'n', 'p', 'f', 'a', 'z', 'y']
    ];

    /// <summary>
    /// UnitPrefixes link a Metric symbol (as key) to its prefix (as value).
    /// </summary>
    /// <remarks>
    /// We dont support :
    /// {'h', "hecto"},
    /// {'da', "deca" }, // !string
    /// {'d', "deci" },
    /// {'c', "centi"},
    /// </remarks>
    static readonly Dictionary<char, UnitPrefix> UnitPrefixes = new()
    {
        {
            'Y', new("yotta", "septillion", "quadrillion")
        },
        {
            'Z', new("zetta", "sextillion", "trilliard")
        },
        {
            'E', new("exa", "quintillion", "trillion")
        },
        {
            'P', new("peta", "quadrillion", "billiard")
        },
        {
            'T', new("tera", "trillion", "billion")
        },
        {
            'G', new("giga", "billion", "milliard")
        },
        {
            'M', new("mega", "million")
        },
        {
            'k', new("kilo", "thousand")
        },

        {
            'm', new("milli", "thousandth")
        },
        {
            'μ', new("micro", "millionth")
        },
        {
            'n', new("nano", "billionth", "milliardth")
        },
        {
            'p', new("pico", "trillionth", "billionth")
        },
        {
            'f', new("femto", "quadrillionth", "billiardth")
        },
        {
            'a', new("atto", "quintillionth", "trillionth")
        },
        {
            'z', new("zepto", "sextillionth", "trilliardth")
        },
        {
            'y', new("yocto", "septillionth", "quadrillionth")
        }
    };

    /// <summary>
    /// Converts a Metric representation into a number.
    /// </summary>
    /// <remarks>
    /// We don't support input in the format {number}{name} nor {number} {name}.
    /// We only provide a solution for {number}{symbol} and {number} {symbol}.
    /// </remarks>
    /// <param name="input">Metric representation to convert to a number</param>
    /// <example>
    /// <code>
    /// "1k".FromMetric() => 1000d
    /// "123".FromMetric() => 123d
    /// "100m".FromMetric() => 1E-1
    /// </code>
    /// </example>
    /// <returns>A number after a conversion from a Metric representation.</returns>
    public static double FromMetric(this string input)
    {
        input = CleanRepresentation(input);
        return BuildNumber(input, input[^1]);
    }

    /// <summary>
    /// Converts a number into a valid and Human-readable Metric representation.
    /// </summary>
    /// <remarks>
    /// Inspired by a snippet from Thom Smith.
    /// See <a href="http://stackoverflow.com/questions/12181024/formatting-a-number-with-a-metric-prefix">this link</a> for more.
    /// </remarks>
    /// <param name="input">Number to convert to a Metric representation.</param>
    /// <param name="formats">A bitwise combination of <see cref="MetricNumeralFormats"/> enumeration values that format the metric representation.</param>
    /// <param name="decimals">If not null it is the numbers of decimals to round the number to</param>
    /// <example>
    /// <code>
    /// 1000.ToMetric() => "1k"
    /// 123.ToMetric() => "123"
    /// 1E-1.ToMetric() => "100m"
    /// </code>
    /// </example>
    /// <returns>A valid Metric representation</returns>
    public static string ToMetric(this int input, MetricNumeralFormats? formats = null, int? decimals = null) =>
        ((double) input).ToMetric(formats, decimals);

    /// <summary>
    /// Converts a number into a valid and Human-readable Metric representation.
    /// </summary>
    /// <remarks>
    /// Inspired by a snippet from Thom Smith.
    /// See <a href="http://stackoverflow.com/questions/12181024/formatting-a-number-with-a-metric-prefix">this link</a> for more.
    /// </remarks>
    /// <param name="input">Number to convert to a Metric representation.</param>
    /// <param name="formats">A bitwise combination of <see cref="MetricNumeralFormats"/> enumeration values that format the metric representation.</param>
    /// <param name="decimals">If not null it is the numbers of decimals to round the number to</param>
    /// <example>
    /// <code>
    /// 1000d.ToMetric() => "1k"
    /// 123d.ToMetric() => "123"
    /// 1E-1.ToMetric() => "100m"
    /// </code>
    /// </example>
    /// <returns>A valid Metric representation</returns>
    public static string ToMetric(this double input, MetricNumeralFormats? formats = null, int? decimals = null)
    {
        if (input.Equals(0))
        {
            return input.ToString();
        }

        if (input.IsOutOfRange())
        {
            throw new ArgumentOutOfRangeException(nameof(input));
        }

        return BuildRepresentation(input, formats, decimals);
    }

    /// <summary>
    /// Clean or handle any wrong input
    /// </summary>
    /// <param name="input">Metric representation to clean</param>
    /// <returns>A cleaned representation</returns>
    static string CleanRepresentation(string input)
    {
        if (input == null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        input = input.Trim();
        input = ReplaceNameBySymbol(input);
        if (input.Length == 0 || input.IsInvalidMetricNumeral())
        {
            throw new ArgumentException("Empty or invalid Metric string.", nameof(input));
        }

        return input.Replace(" ", string.Empty);
    }

    /// <summary>
    /// Build a number from a metric representation or from a number
    /// </summary>
    /// <param name="input">A Metric representation to parse to a number</param>
    /// <param name="last">The last character of input</param>
    /// <returns>A number build from a Metric representation</returns>
    static double BuildNumber(string input, char last) =>
        char.IsLetter(last)
            ? BuildMetricNumber(input, last)
            : double.Parse(input);

    /// <summary>
    /// Build a number from a metric representation
    /// </summary>
    /// <param name="input">A Metric representation to parse to a number</param>
    /// <param name="last">The last character of input</param>
    /// <returns>A number build from a Metric representation</returns>
    static double BuildMetricNumber(string input, char last)
    {
        double getExponent(List<char> symbols) => (symbols.IndexOf(last) + 1) * 3;
        var number = double.Parse(input.Remove(input.Length - 1));
        var exponent = Math.Pow(10, Symbols[0]
            .Contains(last)
            ? getExponent(Symbols[0])
            : -getExponent(Symbols[1]));
        return number * exponent;
    }

    /// <summary>
    /// Replace every symbol's name by its symbol representation.
    /// </summary>
    /// <param name="input">Metric representation with a name or a symbol</param>
    /// <returns>A metric representation with a symbol</returns>
    static string ReplaceNameBySymbol(string input) =>
        UnitPrefixes.Aggregate(input, (current, unitPrefix) =>
            current.Replace(unitPrefix.Value.Name, unitPrefix.Key.ToString()));

    /// <summary>
    /// Build a Metric representation of the number.
    /// </summary>
    /// <param name="input">Number to convert to a Metric representation.</param>
    /// <param name="formats">A bitwise combination of <see cref="MetricNumeralFormats"/> enumeration values that format the metric representation.</param>
    /// <param name="decimals">If not null it is the numbers of decimals to round the number to</param>
    /// <returns>A number in a Metric representation</returns>
    static string BuildRepresentation(double input, MetricNumeralFormats? formats, int? decimals)
    {
        var exponent = (int) Math.Floor(Math.Log10(Math.Abs(input)) / 3);

        if (!exponent.Equals(0)) return BuildMetricRepresentation(input, exponent, formats, decimals);
        var representation = decimals.HasValue
            ? Math
                .Round(input, decimals.Value)
                .ToString()
            : input.ToString();
        if ((formats & MetricNumeralFormats.WithSpace) == MetricNumeralFormats.WithSpace)
        {
            representation += " ";
        }

        return representation;
    }

    /// <summary>
    /// Build a Metric representation of the number.
    /// </summary>
    /// <param name="input">Number to convert to a Metric representation.</param>
    /// <param name="exponent">Exponent of the number in a scientific notation</param>
    /// <param name="formats">A bitwise combination of <see cref="MetricNumeralFormats"/> enumeration values that format the metric representation.</param>
    /// <param name="decimals">If not null it is the numbers of decimals to round the number to</param>
    /// <returns>A number in a Metric representation</returns>
    static string BuildMetricRepresentation(double input, int exponent, MetricNumeralFormats? formats, int? decimals)
    {
        var number = input * Math.Pow(1000, -exponent);
        if (decimals.HasValue)
        {
            number = Math.Round(number, decimals.Value);
        }

        if (Math.Abs(number) >= 1000 && exponent < Symbols[0].Count)
        {
            number /= 1000;
            exponent += 1;
        }

        var symbol = Math.Sign(exponent) == 1
            ? Symbols[0][exponent - 1]
            : Symbols[1][-exponent - 1];
        return number.ToString("G15")
               + (formats.HasValue && formats.Value.HasFlag(MetricNumeralFormats.WithSpace) ? " " : string.Empty)
               + GetUnitText(symbol, formats);
    }

    /// <summary>
    /// Get the unit from a symbol of from the symbol's name.
    /// </summary>
    /// <param name="symbol">The symbol linked to the unit</param>
    /// <param name="formats">A bitwise combination of <see cref="MetricNumeralFormats"/> enumeration values that format the metric representation.</param>
    /// <returns>A symbol, a symbol's name, a symbol's short scale word or a symbol's long scale word</returns>
    static string GetUnitText(char symbol, MetricNumeralFormats? formats)
    {
        if (formats.HasValue
            && formats.Value.HasFlag(MetricNumeralFormats.UseName))
        {
            return UnitPrefixes[symbol].Name;
        }

        if (formats.HasValue
            && formats.Value.HasFlag(MetricNumeralFormats.UseShortScaleWord))
        {
            return UnitPrefixes[symbol].ShortScaleWord;
        }

        if (formats.HasValue
            && formats.Value.HasFlag(MetricNumeralFormats.UseLongScaleWord))
        {
            return UnitPrefixes[symbol].LongScaleWord;
        }

        return symbol.ToString();
    }

    /// <summary>
    /// Check if a Metric representation is out of the valid range.
    /// </summary>
    /// <param name="input">A Metric representation that may be out of the valid range.</param>
    /// <returns>True if input is out of the valid range.</returns>
    static bool IsOutOfRange(this double input)
    {
        bool outside(double min, double max) => !(max > input && input > min);

        return (Math.Sign(input) == 1 && outside(SmallLimit, BigLimit))
               || (Math.Sign(input) == -1 && outside(-BigLimit, -SmallLimit));
    }

    /// <summary>
    /// Check if a string is not a valid Metric representation.
    /// A valid representation is in the format "{0}{1}" or "{0} {1}"
    /// where {0} is a number and {1} is an allowed symbol.
    /// </summary>
    /// <remarks>
    /// ToDo: Performance: Use (string input, out number) to escape the double use of Parse()
    /// </remarks>
    /// <param name="input">>A string that may contain an invalid Metric representation.</param>
    /// <returns>True if input is not a valid Metric representation.</returns>
    static bool IsInvalidMetricNumeral(this string input)
    {
        var index = input.Length - 1;
        var last = input[index];
        var isSymbol = Symbols[0]
            .Contains(last) || Symbols[1]
            .Contains(last);
        return !double.TryParse(isSymbol ? input.Remove(index) : input, out _);
    }

    struct UnitPrefix(string name, string shortScaleWord, string? longScaleWord = null)
    {
        public string Name { get; } = name;
        public string ShortScaleWord { get; } = shortScaleWord;
        public string LongScaleWord => longScaleWord ?? ShortScaleWord;
    }
}
