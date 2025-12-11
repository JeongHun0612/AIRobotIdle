using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using BreakInfinity;

/// <summary>
/// 헤더 기반 CSV 파서 (쉼표 기준, 큰따옴표 감싼 값은 Trim 처리)
/// </summary>
public static class CsvUtil
{
    public static IEnumerable<CsvRow> Read(string path, bool skipHeader = true)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"CSV not found: {path}");

        var lines = File.ReadAllLines(path);
        if (lines.Length == 0)
            yield break;

        var headerCells = SplitLine(lines[0]);
        var headerIndex = BuildHeaderIndex(headerCells);

        var start = skipHeader ? 1 : 0;

        for (int i = start; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var cells = SplitLine(line);
            yield return new CsvRow(cells, i + 1, headerIndex);
        }
    }

    private static string[] SplitLine(string line)
    {
        return line.Split(',');
    }

    private static string Normalize(string cell)
    {
        if (cell == null)
            return string.Empty;
        return cell.Trim().Trim('"');
    }

    private static Dictionary<string, int> BuildHeaderIndex(string[] headerCells)
    {
        var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headerCells.Length; i++)
        {
            var name = Normalize(headerCells[i]);
            if (string.IsNullOrEmpty(name))
                continue; // 빈 헤더는 무시
            if (!dict.ContainsKey(name))
                dict.Add(name, i);
        }
        return dict;
    }

    public readonly struct CsvRow
    {
        private readonly string[] _cells;
        private readonly int _lineNumber;
        private readonly IReadOnlyDictionary<string, int> _headerIndex;

        public CsvRow(string[] cells, int lineNumber, IReadOnlyDictionary<string, int> headerIndex)
        {
            _cells = cells;
            _lineNumber = lineNumber;
            _headerIndex = headerIndex;
        }

        public string String(string name) => SafeCellByName(name);

        public int Int(string name)
        {
            var cell = Normalize(SafeCellByName(name));
            if (string.IsNullOrWhiteSpace(cell))
                return 0;
            return int.Parse(cell, CultureInfo.InvariantCulture);
        }

        public float Float(string name)
        {
            var cell = Normalize(SafeCellByName(name));
            if (string.IsNullOrWhiteSpace(cell))
                return 0f;
            if (!float.TryParse(cell, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                throw new FormatException($"CSV float parse 실패 (line {_lineNumber}, column '{name}', value '{cell}')");
            return value;
        }

        public double Double(string name)
        {
            var cell = Normalize(SafeCellByName(name));
            if (string.IsNullOrWhiteSpace(cell))
                return 0d;
            if (!double.TryParse(cell, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                throw new FormatException($"CSV double parse 실패 (line {_lineNumber}, column '{name}', value '{cell}')");
            return value;
        }

        public T EnumValue<T>(string name) where T : struct => (T)global::System.Enum.Parse(typeof(T), Normalize(SafeCellByName(name)), true);

        public BigDouble BigDoubleValue(string name)
        {
            var cell = Normalize(SafeCellByName(name));
            if (string.IsNullOrWhiteSpace(cell))
                return global::BreakInfinity.BigDouble.Zero;

            try
            {
                return global::BreakInfinity.BigDouble.Parse(cell);
            }
            catch (Exception ex)
            {
                throw new FormatException($"CSV BigDouble parse 실패 (line {_lineNumber}, column '{name}', value '{cell}')", ex);
            }
        }

        private static string Normalize(string cell)
        {
            if (cell == null)
                return string.Empty;
            // 앞뒤 공백/따옴표 제거 (예: "1 -> 1, 1" -> 1)
            return cell.Trim().Trim('"');
        }

        private string SafeCellByName(string name)
        {
            if (!_headerIndex.TryGetValue(name, out var idx))
                throw new KeyNotFoundException($"CSV header '{name}'를 찾을 수 없습니다 (line {_lineNumber})");
            return SafeCell(idx);
        }

        private string SafeCell(int i)
        {
            if (i < 0 || i >= _cells.Length)
                throw new IndexOutOfRangeException($"CSV cell out of range at line {_lineNumber}, index {i}");
            return _cells[i];
        }
    }
}

