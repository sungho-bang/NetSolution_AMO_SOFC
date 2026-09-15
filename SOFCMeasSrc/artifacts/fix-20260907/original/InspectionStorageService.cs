using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace SOFCMeas
{
    internal sealed class LotFileEntry
    {
        internal DateTime Date { get; set; }
        internal string FilePath { get; set; }
        internal string FileName { get { return Path.GetFileName(FilePath); } }
    }
    internal sealed class InspectionSample
    {
        internal int Number { get; set; }
        internal DateTime ReadTime { get; set; }
        internal int RawValue { get; set; }
        internal double LoadKgf { get; set; }
    }

    internal sealed class InspectionLogRecord
    {
        internal DateTime Time { get; set; }
        internal DateTime StartedAt { get; set; }
        internal string Station { get; set; }
        internal string LotNumber { get; set; }
        internal string FileName { get; set; }
        internal string OperatorName { get; set; }
        internal string Result { get; set; }
        internal double PeakLoadKgf { get; set; }
        internal double LowerSpecKgf { get; set; }
        internal double UpperSpecKgf { get; set; }
        internal int SampleCount { get; set; }
        internal string DataFileName { get; set; }
        internal int DataRowNumber { get; set; }
        internal string Message { get; set; }
    }

    internal sealed class BufferedInspectionData
    {
        internal InspectionLogRecord Record { get; set; }
        internal IReadOnlyList<InspectionSample> Samples { get; set; }
    }

    internal sealed class LotStorageData
    {
        internal DateTime CreatedAt { get; set; }
        internal DateTime CompletedAt { get; set; }
        internal string Station { get; set; }
        internal string LotNumber { get; set; }
        internal string FileName { get; set; }
        internal string OperatorName { get; set; }
        internal string SpecText { get; set; }
        internal IReadOnlyList<BufferedInspectionData> Inspections { get; set; }
    }

    internal sealed class InspectionStorageService
    {
        private const string InspectionLogHeader =
            "Time,StartedAt,Station,LotNumber,FileName,OperatorName,Result," +
            "PeakLoadKgf,LowerSpecKgf,UpperSpecKgf,SampleCount,DataFileName,Message,DataRowNumber";

        private static readonly string[] LotSummaryHeader =
        {
            "모델명",
            "LOT NO.",
            "작업자",
            "SPEC",
            "생산수량",
            "GOOD",
            "NG",
            "Yield(%)"
        };

        private static readonly Encoding CsvEncoding = new UTF8Encoding(true);

        private readonly object m_SyncRoot = new object();
        private readonly string m_DataRootDirectory;
        private readonly string m_LogRootDirectory;
        private readonly ApplicationLogService m_LogService;

        internal InspectionStorageService()
            : this(
                ApplicationPaths.DataRootDirectory,
                ApplicationPaths.LogRootDirectory,
                null)
        {
        }

        internal InspectionStorageService(ApplicationLogService logService)
            : this(
                ApplicationPaths.DataRootDirectory,
                ApplicationPaths.LogRootDirectory,
                logService)
        {
        }

        internal InspectionStorageService(
            string dataRootDirectory,
            string logRootDirectory)
            : this(dataRootDirectory, logRootDirectory, null)
        {
        }

        internal InspectionStorageService(
            string dataRootDirectory,
            string logRootDirectory,
            ApplicationLogService logService)
        {
            m_DataRootDirectory = Path.GetFullPath(dataRootDirectory);
            m_LogRootDirectory = Path.GetFullPath(logRootDirectory);
            m_LogService = logService;
        }

        internal string SaveLot(LotStorageData lot)
        {
            if (lot == null)
            {
                throw new ArgumentNullException(nameof(lot));
            }

            if (lot.Inspections == null || lot.Inspections.Count == 0)
            {
                throw new ArgumentException("저장할 LOT 검사 데이터가 없습니다.", nameof(lot));
            }

            lock (m_SyncRoot)
            {
                string phase = "write_lot_data";
                try
                {
                    string dataFilePath = WriteLotDataFile(lot);
                    string dataFileName = Path.GetFileName(dataFilePath);

                    phase = "append_inspection_index";
                    int dataRowNumber = 0;
                    foreach (BufferedInspectionData inspection in lot.Inspections)
                    {
                        inspection.Record.DataFileName = dataFileName;
                        inspection.Record.DataRowNumber = ++dataRowNumber;
                        inspection.Record.SampleCount = inspection.Samples.Count;
                        AppendInspectionLog(inspection.Record);
                    }

                    if (m_LogService != null)
                    {
                        int sampleCount = lot.Inspections.Sum(
                            inspection => inspection.Samples.Count);
                        int maximumRawDataCount = lot.Inspections.Max(
                            inspection => inspection.Samples.Count);
                        m_LogService.Info(
                            GetStationLogSource(lot.Station),
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "event=LOT_FILE_SAVED lot=\"{0}\" inspectionCount={1} " +
                                "sampleCount={2} maximumRawDataCount={3} " +
                                "format=lot_summary_2row_kgf_matrix file=\"{4}\"",
                                EscapeLogValue(lot.LotNumber),
                                lot.Inspections.Count,
                                sampleCount,
                                maximumRawDataCount,
                                EscapeLogValue(dataFilePath)));
                    }

                    return dataFilePath;
                }
                catch (Exception exception)
                {
                    if (m_LogService != null)
                    {
                        m_LogService.Error(
                            GetStationLogSource(lot.Station),
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "event=LOT_STORAGE_FAILED phase={0} lot=\"{1}\" " +
                                "inspectionCount={2} dataRoot=\"{3}\"",
                                phase,
                                EscapeLogValue(lot.LotNumber),
                                lot.Inspections.Count,
                                EscapeLogValue(m_DataRootDirectory)),
                            exception);
                    }

                    throw;
                }
            }
        }

        internal List<InspectionSample> ReadSamples(InspectionLogRecord record)
        {
            string name = record.DataFileName;
            if (string.IsNullOrWhiteSpace(name) || Path.GetFileName(name) != name)
                throw new InvalidDataException("저장 데이터 파일명이 올바르지 않습니다.");
            string stationRoot = Path.Combine(m_DataRootDirectory, GetPlcFolder(record.Station));
            var files = Directory.Exists(stationRoot) ? Directory.GetFiles(stationRoot, name, SearchOption.AllDirectories) : new string[0];
            // Legacy files were stored directly below Data/year/month/day.
            if (files.Length == 0 && Directory.Exists(m_DataRootDirectory))
                files = Directory.GetDirectories(m_DataRootDirectory)
                    .Where(path => Path.GetFileName(path).Length == 4 && Path.GetFileName(path).All(char.IsDigit))
                    .SelectMany(path => Directory.GetFiles(path, name, SearchOption.AllDirectories)).ToArray();
            if (files.Length != 1) throw new IOException("저장 데이터 파일을 고유하게 찾을 수 없습니다: " + name);
            var samples = new List<InspectionSample>();
            string time = record.Time.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
            string start = record.StartedAt.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
            if (record.DataRowNumber > 0)
            {
                using (var reader = new StreamReader(files[0], CsvEncoding, true))
                {
                    string firstLine = reader.ReadLine();
                    List<string> firstFields = ParseCsvLine(firstLine);
                    bool valuesAreKgf = false;
                    if (IsLotSummaryHeader(firstFields))
                    {
                        ValidateLotSummaryValues(ParseCsvLine(reader.ReadLine()));
                        firstFields = ReadNextNonEmptyCsvLine(reader);
                        valuesAreKgf = true;
                    }

                    if (IsRawDataHeader(firstFields))
                    {
                        ValidateRawDataHeader(firstFields);
                        double loadScale = GetLoadScale(record.Station);
                        string rowLine;
                        while ((rowLine = reader.ReadLine()) != null)
                        {
                            List<string> row = ParseCsvLine(rowLine);
                            if (row.Count == 0 || row[0] != record.DataRowNumber.ToString(CultureInfo.InvariantCulture))
                            {
                                continue;
                            }

                            return valuesAreKgf
                                ? ParseKgfDataSamples(
                                    row,
                                    firstFields.Count - 3,
                                    loadScale)
                                : ParseRawDataSamples(
                                    row,
                                    firstFields.Count - 3,
                                    loadScale);
                        }

                        throw new InvalidDataException("선택한 검사의 RAWDATA가 없습니다.");
                    }
                }

                bool data = false;
                foreach (string line in File.ReadLines(files[0], CsvEncoding))
                {
                    var fields = ParseCsvLine(line);
                    if (fields.Count >= 2 && fields[0] == "NO." && fields[1] == "판정결과") { data = true; continue; }
                    if (!data || fields.Count < 2 || fields[0] != record.DataRowNumber.ToString(CultureInfo.InvariantCulture)) continue;
                    for (int i = 2; i < fields.Count; i++)
                    {
                        if (string.IsNullOrEmpty(fields[i])) continue;
                        double kgf;
                        if (!double.TryParse(fields[i], NumberStyles.Float, CultureInfo.InvariantCulture, out kgf) || double.IsNaN(kgf) || double.IsInfinity(kgf))
                            throw new InvalidDataException("저장된 하중 값이 올바르지 않습니다.");
                        samples.Add(new InspectionSample { Number = i - 1, LoadKgf = kgf });
                    }
                    break;
                }
                if (samples.Count == 0) throw new InvalidDataException("선택한 검사의 하중 데이터가 없습니다.");
                return samples;
            }

            foreach (string line in File.ReadLines(files[0], CsvEncoding))
            {
                var fields = ParseCsvLine(line);
                if (fields.Count != 13 || fields[1] != time || fields[2] != start) continue;
                int number, raw; double kgf; DateTime readTime;
                if (!int.TryParse(fields[8], out number) || !int.TryParse(fields[10], out raw) ||
                    !double.TryParse(fields[11], NumberStyles.Float, CultureInfo.InvariantCulture, out kgf) ||
                    !DateTime.TryParseExact(fields[9], "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture, DateTimeStyles.None, out readTime))
                    throw new InvalidDataException("저장된 하중 데이터 형식이 올바르지 않습니다.");
                samples.Add(new InspectionSample { Number = number, RawValue = raw, LoadKgf = kgf, ReadTime = readTime });
            }
            if (samples.Count == 0) throw new InvalidDataException("선택한 검사의 하중 데이터가 없습니다.");
            return samples;
        }

        internal List<LotFileEntry> GetLotFiles(DateTime from, DateTime to, string station)
        {
            if (from.Date > to.Date) throw new ArgumentException("시작일은 종료일보다 늦을 수 없습니다.");
            string root = Path.Combine(m_DataRootDirectory, GetPlcFolder(station));
            var results = new List<LotFileEntry>();
            for (DateTime day = from.Date; day <= to.Date; day = day.AddDays(1))
            {
                string directory = ApplicationPaths.GetDateDirectory(root, day);
                if (Directory.Exists(directory))
                    foreach (string file in Directory.GetFiles(directory, "*.csv", SearchOption.TopDirectoryOnly))
                        results.Add(new LotFileEntry { Date = day, FilePath = file });
                if (day == DateTime.MaxValue.Date) break;
            }
            return results.OrderByDescending(f => f.Date).ThenByDescending(f => f.FileName).ToList();
        }

        internal List<BufferedInspectionData> ReadLotFile(LotFileEntry file, string station)
        {
            string root = Path.GetFullPath(Path.Combine(m_DataRootDirectory, GetPlcFolder(station))) + Path.DirectorySeparatorChar;
            if (!Path.GetFullPath(file.FilePath).StartsWith(root, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("해당 설비 폴더의 파일을 선택하세요.");
            var results = new List<BufferedInspectionData>();
            using (var reader = new StreamReader(file.FilePath, CsvEncoding, true))
            {
                var summary = ParseCsvLine(reader.ReadLine());
                if (IsRawDataHeader(summary))
                {
                    ValidateRawDataHeader(summary);
                    int maximumRawDataCount = summary.Count - 3;
                    double loadScale = GetLoadScale(station);
                    double rawDataThreshold = GetInspectionThreshold(station);
                    string rawLine;
                    while ((rawLine = reader.ReadLine()) != null)
                    {
                        if (string.IsNullOrWhiteSpace(rawLine))
                        {
                            continue;
                        }

                        List<string> fields = ParseCsvLine(rawLine);
                        int number;
                        if (fields.Count < 3 ||
                            !int.TryParse(fields[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out number) ||
                            number < 1)
                        {
                            throw new InvalidDataException("검사 번호가 올바르지 않습니다.");
                        }

                        List<InspectionSample> samples = ParseRawDataSamples(
                            fields,
                            maximumRawDataCount,
                            loadScale);
                        results.Add(new BufferedInspectionData
                        {
                            Samples = samples,
                            Record = new InspectionLogRecord
                            {
                                Time = file.Date,
                                Station = station,
                                Result = fields[1],
                                DataRowNumber = number,
                                SampleCount = samples.Count,
                                LowerSpecKgf = rawDataThreshold,
                                PeakLoadKgf = samples.Count == 0
                                    ? 0D
                                    : samples.Max(sample => sample.LoadKgf)
                            }
                        });
                    }

                    if (results.Count == 0)
                    {
                        throw new InvalidDataException("파일에 검사 결과가 없습니다.");
                    }

                    return results;
                }

                if (IsLotSummaryHeader(summary))
                {
                    List<string> summaryValues = ParseCsvLine(reader.ReadLine());
                    ValidateLotSummaryValues(summaryValues);
                    double summaryThreshold;
                    if (!double.TryParse(
                        summaryValues[3],
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out summaryThreshold))
                    {
                        throw new InvalidDataException("SPEC 기준값을 읽을 수 없습니다.");
                    }

                    List<string> rawDataHeader = ReadNextNonEmptyCsvLine(reader);
                    ValidateRawDataHeader(rawDataHeader);
                    int maximumRawDataCount = rawDataHeader.Count - 3;
                    double loadScale = GetLoadScale(station);
                    string rawLine;
                    while ((rawLine = reader.ReadLine()) != null)
                    {
                        if (string.IsNullOrWhiteSpace(rawLine))
                        {
                            continue;
                        }

                        List<string> fields = ParseCsvLine(rawLine);
                        int number;
                        if (fields.Count < 3 ||
                            !int.TryParse(
                                fields[0],
                                NumberStyles.Integer,
                                CultureInfo.InvariantCulture,
                                out number) ||
                            number < 1)
                        {
                            throw new InvalidDataException("검사 번호가 올바르지 않습니다.");
                        }

                        List<InspectionSample> samples = ParseKgfDataSamples(
                            fields,
                            maximumRawDataCount,
                            loadScale);
                        results.Add(new BufferedInspectionData
                        {
                            Samples = samples,
                            Record = new InspectionLogRecord
                            {
                                Time = file.Date,
                                Station = station,
                                FileName = summaryValues[0],
                                LotNumber = summaryValues[1],
                                OperatorName = summaryValues[2],
                                Result = fields[1],
                                DataRowNumber = number,
                                SampleCount = samples.Count,
                                LowerSpecKgf = summaryThreshold,
                                PeakLoadKgf = samples.Count == 0
                                    ? 0D
                                    : samples.Max(sample => sample.LoadKgf)
                            }
                        });
                    }

                    if (results.Count == 0)
                    {
                        throw new InvalidDataException("파일에 검사 결과가 없습니다.");
                    }

                    return results;
                }

                if (summary.Count < 16 || summary[0] != "모델명") throw new InvalidDataException("지원하지 않는 LOT CSV 형식입니다.");
                double threshold;
                if (!double.TryParse(summary[7], NumberStyles.Float, CultureInfo.InvariantCulture, out threshold))
                    throw new InvalidDataException("SPEC 기준값을 읽을 수 없습니다.");
                bool data = false; string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var fields = ParseCsvLine(line);
                    if (fields.Count > 1 && fields[0] == "NO." && fields[1] == "판정결과") { data = true; continue; }
                    if (!data || string.IsNullOrWhiteSpace(line)) continue;
                    int number;
                    if (fields.Count < 2 || !int.TryParse(fields[0], out number)) throw new InvalidDataException("검사 번호가 올바르지 않습니다.");
                    var samples = new List<InspectionSample>();
                    for (int i = 2; i < fields.Count; i++)
                    {
                        if (string.IsNullOrWhiteSpace(fields[i])) continue;
                        double kgf;
                        if (!double.TryParse(fields[i], NumberStyles.Float, CultureInfo.InvariantCulture, out kgf) || double.IsNaN(kgf) || double.IsInfinity(kgf))
                            throw new InvalidDataException("하중 데이터가 올바르지 않습니다.");
                        samples.Add(new InspectionSample { Number = i - 1, LoadKgf = kgf });
                    }
                    results.Add(new BufferedInspectionData { Samples = samples, Record = new InspectionLogRecord {
                        Time = file.Date, Station = station, FileName = summary[1], LotNumber = summary[3],
                        Result = fields[1], DataRowNumber = number, LowerSpecKgf = threshold,
                        PeakLoadKgf = samples.Count == 0 ? 0 : samples.Max(s => s.LoadKgf) } });
                }
            }
            if (results.Count == 0) throw new InvalidDataException("파일에 검사 결과가 없습니다.");
            return results;
        }

        internal IReadOnlyList<InspectionLogRecord> GetInspectionLogs(
            DateTime fromDate,
            DateTime toDate,
            string station)
        {
            DateTime firstDate = fromDate.Date;
            DateTime lastDate = toDate.Date;
            if (firstDate > lastDate)
            {
                throw new ArgumentException("조회 시작일이 종료일보다 늦습니다.");
            }

            List<InspectionLogRecord> records = new List<InspectionLogRecord>();
            for (DateTime date = firstDate; date <= lastDate; date = date.AddDays(1))
            {
                string filePath = GetInspectionLogFilePath(date);
                if (File.Exists(filePath))
                {
                    ReadInspectionLogFile(filePath, station, records);
                }

                if (date == DateTime.MaxValue.Date)
                {
                    break;
                }
            }

            return records
                .OrderByDescending(record => record.Time)
                .ToArray();
        }

        internal void ExportInspectionLogs(
            string filePath,
            IEnumerable<InspectionLogRecord> records)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("내보낼 파일 경로가 비어 있습니다.", nameof(filePath));
            }

            if (records == null)
            {
                throw new ArgumentNullException(nameof(records));
            }

            using (StreamWriter writer = new StreamWriter(filePath, false, CsvEncoding))
            {
                writer.WriteLine(InspectionLogHeader);
                foreach (InspectionLogRecord record in records)
                {
                    writer.WriteLine(CreateInspectionLogLine(record));
                }
            }
        }

        private string WriteLotDataFile(LotStorageData lot)
        {
            string directory = ApplicationPaths.GetDateDirectory(
                Path.Combine(m_DataRootDirectory, GetPlcFolder(lot.Station)),
                lot.CompletedAt);
            Directory.CreateDirectory(directory);

            string baseFileName = lot.CompletedAt.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture)
                + "_" + NormalizeFileNamePart(lot.LotNumber)
                + "_" + NormalizeFileNamePart(lot.FileName);
            string filePath = GetUniqueFilePath(directory, baseFileName, ".csv");
            string temporaryPath = filePath + ".tmp";

            try
            {
                using (StreamWriter writer = new StreamWriter(
                    temporaryPath,
                    false,
                    CsvEncoding))
                {
                    int productionCount = lot.Inspections.Count;
                    int goodCount = lot.Inspections.Count(inspection =>
                        IsGood(inspection.Record.Result));
                    int ngCount = productionCount - goodCount;
                    double yieldPercent = productionCount == 0
                        ? 0D
                        : goodCount * 100D / productionCount;

                    WriteCsvLine(writer, LotSummaryHeader);
                    WriteCsvLine(writer, new[]
                    {
                        lot.FileName,
                        lot.LotNumber,
                        lot.OperatorName,
                        lot.SpecText,
                        productionCount.ToString(CultureInfo.InvariantCulture),
                        goodCount.ToString(CultureInfo.InvariantCulture),
                        ngCount.ToString(CultureInfo.InvariantCulture),
                        yieldPercent.ToString("0.00", CultureInfo.InvariantCulture)
                    });
                    writer.WriteLine();

                    int width = lot.Inspections.Max(i => i.Samples.Count);
                    var header = new List<string> { "NO", "판정결과", "RAWDATA개수" };
                    for (int i = 1; i <= width; i++)
                    {
                        header.Add(i.ToString(CultureInfo.InvariantCulture));
                    }
                    WriteCsvLine(writer, header.ToArray());
                    for (int i = 0; i < lot.Inspections.Count; i++)
                    {
                        var inspection = lot.Inspections[i];
                        var row = new List<string> { (i + 1).ToString(CultureInfo.InvariantCulture),
                            IsGood(inspection.Record.Result) ? "GOOD" : inspection.Record.Result == "INVALID" ? "INVALID" : "NG",
                            inspection.Samples.Count.ToString(CultureInfo.InvariantCulture) };
                        row.AddRange(inspection.Samples.Select(sample =>
                            sample.LoadKgf.ToString(
                                "0.000",
                                CultureInfo.InvariantCulture)));
                        while (row.Count < width + 3)
                        {
                            row.Add(string.Empty);
                        }
                        WriteCsvLine(writer, row.ToArray());
                    }
                }
                File.Move(temporaryPath, filePath);
                return filePath;
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }

        private static bool IsGood(string verdict)
        { return string.Equals(verdict, "GOOD", StringComparison.OrdinalIgnoreCase) || string.Equals(verdict, "PASS", StringComparison.OrdinalIgnoreCase); }

        private static bool IsRawDataHeader(IReadOnlyList<string> fields)
        {
            return fields != null &&
                fields.Count >= 3 &&
                string.Equals(fields[0], "NO", StringComparison.OrdinalIgnoreCase) &&
                fields[1] == "판정결과" &&
                string.Equals(fields[2], "RAWDATA개수", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsLotSummaryHeader(IReadOnlyList<string> fields)
        {
            if (fields == null || fields.Count != LotSummaryHeader.Length)
            {
                return false;
            }

            for (int index = 0; index < LotSummaryHeader.Length; index++)
            {
                if (!string.Equals(
                    fields[index],
                    LotSummaryHeader[index],
                    StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        private static void ValidateLotSummaryValues(IReadOnlyList<string> fields)
        {
            if (fields == null || fields.Count != LotSummaryHeader.Length)
            {
                throw new InvalidDataException("LOT 요약 값 행이 올바르지 않습니다.");
            }
        }

        private static List<string> ReadNextNonEmptyCsvLine(StreamReader reader)
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    return ParseCsvLine(line);
                }
            }

            throw new InvalidDataException("RAWDATA 헤더가 없습니다.");
        }

        private static void ValidateRawDataHeader(IReadOnlyList<string> fields)
        {
            for (int index = 3; index < fields.Count; index++)
            {
                string expected = (index - 2).ToString(CultureInfo.InvariantCulture);
                if (fields[index] != expected)
                {
                    throw new InvalidDataException(
                        "RAWDATA 순번 헤더가 올바르지 않습니다: " + expected);
                }
            }
        }

        private static List<InspectionSample> ParseRawDataSamples(
            IReadOnlyList<string> fields,
            int maximumRawDataCount,
            double loadScale)
        {
            int sampleCount;
            if (fields.Count < 3 ||
                !int.TryParse(
                    fields[2],
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out sampleCount) ||
                sampleCount < 0 ||
                sampleCount > maximumRawDataCount)
            {
                throw new InvalidDataException("RAWDATA 개수가 올바르지 않습니다.");
            }

            if (fields.Count < sampleCount + 3)
            {
                throw new InvalidDataException("RAWDATA 개수보다 저장된 값이 적습니다.");
            }

            var samples = new List<InspectionSample>(sampleCount);
            for (int index = 0; index < sampleCount; index++)
            {
                int rawValue;
                if (!int.TryParse(
                    fields[index + 3],
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out rawValue))
                {
                    throw new InvalidDataException(
                        "RAWDATA " + (index + 1).ToString(CultureInfo.InvariantCulture) +
                        "번 값이 올바르지 않습니다.");
                }

                samples.Add(new InspectionSample
                {
                    Number = index + 1,
                    RawValue = rawValue,
                    LoadKgf = rawValue / loadScale
                });
            }

            for (int index = sampleCount + 3; index < fields.Count; index++)
            {
                if (!string.IsNullOrWhiteSpace(fields[index]))
                {
                    throw new InvalidDataException(
                        "RAWDATA 개수 뒤에 예상하지 않은 값이 있습니다.");
                }
            }

            return samples;
        }

        private static List<InspectionSample> ParseKgfDataSamples(
            IReadOnlyList<string> fields,
            int maximumRawDataCount,
            double loadScale)
        {
            int sampleCount;
            if (fields.Count < 3 ||
                !int.TryParse(
                    fields[2],
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out sampleCount) ||
                sampleCount < 0 ||
                sampleCount > maximumRawDataCount)
            {
                throw new InvalidDataException("RAWDATA 개수가 올바르지 않습니다.");
            }

            if (fields.Count < sampleCount + 3)
            {
                throw new InvalidDataException("RAWDATA 개수보다 저장된 값이 적습니다.");
            }

            var samples = new List<InspectionSample>(sampleCount);
            for (int index = 0; index < sampleCount; index++)
            {
                double loadKgf;
                if (!double.TryParse(
                    fields[index + 3],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out loadKgf) ||
                    double.IsNaN(loadKgf) ||
                    double.IsInfinity(loadKgf))
                {
                    throw new InvalidDataException(
                        "RAWDATA " + (index + 1).ToString(CultureInfo.InvariantCulture) +
                        "번 kgf 값이 올바르지 않습니다.");
                }

                samples.Add(new InspectionSample
                {
                    Number = index + 1,
                    RawValue = Convert.ToInt32(Math.Round(
                        loadKgf * loadScale,
                        MidpointRounding.AwayFromZero)),
                    LoadKgf = loadKgf
                });
            }

            for (int index = sampleCount + 3; index < fields.Count; index++)
            {
                if (!string.IsNullOrWhiteSpace(fields[index]))
                {
                    throw new InvalidDataException(
                        "RAWDATA 개수 뒤에 예상하지 않은 값이 있습니다.");
                }
            }

            return samples;
        }

        private static double GetLoadScale(string station)
        {
            Dictionary<string, string> settings = SettingsCatalog.Load();
            return SettingsCatalog.Number(
                settings,
                GetStationSettingPrefix(station) + "Plc.LoadScale");
        }

        private static double GetInspectionThreshold(string station)
        {
            Dictionary<string, string> settings = SettingsCatalog.Load();
            return SettingsCatalog.Number(
                settings,
                GetStationSettingPrefix(station) + "Inspection.ThresholdKgf");
        }

        private static string GetStationSettingPrefix(string station)
        {
            if (station == "#1" || station == "PLC1") return "Station1.";
            if (station == "#2" || station == "PLC2") return "Station2.";
            throw new ArgumentException("설비 설정 대상을 확인하세요: " + station);
        }

        private static string GetPlcFolder(string station)
        {
            if (station == "#1" || station == "PLC1") return "PLC1";
            if (station == "#2" || station == "PLC2") return "PLC2";
            throw new ArgumentException("저장 대상 설비를 확인하세요: " + station);
        }
        private void AppendInspectionLog(InspectionLogRecord record)
        {
            string filePath = GetInspectionLogFilePath(record.Time);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            bool writeHeader = !File.Exists(filePath) || new FileInfo(filePath).Length == 0;

            if (!writeHeader && File.ReadLines(filePath, CsvEncoding).First() != InspectionLogHeader)
            {
                // Upgrade legacy index rows without changing their data or losing old REVIEW support.
                string upgraded = filePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
                try
                {
                    using (var writer = new StreamWriter(upgraded, false, CsvEncoding))
                    {
                        writer.WriteLine(InspectionLogHeader);
                        foreach (string line in File.ReadLines(filePath, CsvEncoding).Skip(1))
                            writer.WriteLine(ParseCsvLine(line).Count == 13 ? line + ",0" : line);
                    }
                    File.Replace(upgraded, filePath, null);
                }
                finally { if (File.Exists(upgraded)) File.Delete(upgraded); }
            }

            using (FileStream stream = new FileStream(
                filePath,
                FileMode.Append,
                FileAccess.Write,
                FileShare.Read))
            using (StreamWriter writer = new StreamWriter(stream, CsvEncoding))
            {
                if (writeHeader)
                {
                    writer.WriteLine(InspectionLogHeader);
                }

                writer.WriteLine(CreateInspectionLogLine(record));
            }
        }

        private void ReadInspectionLogFile(
            string filePath,
            string station,
            ICollection<InspectionLogRecord> records)
        {
            using (FileStream stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete))
            using (StreamReader reader = new StreamReader(stream, CsvEncoding, true))
            {
                string line;
                bool firstLine = true;
                int lineNumber = 0;
                while ((line = reader.ReadLine()) != null)
                {
                    lineNumber++;
                    if (firstLine)
                    {
                        firstLine = false;
                        continue;
                    }

                    InspectionLogRecord record;
                    if (!TryParseInspectionLogLine(line, out record))
                    {
                        if (m_LogService != null)
                        {
                            m_LogService.Warning(
                                "INSPECTION-STORAGE",
                                string.Format(
                                    CultureInfo.InvariantCulture,
                                    "event=INSPECTION_LOG_ROW_SKIPPED reason=parse_failed " +
                                    "file=\"{0}\" line={1}",
                                    EscapeLogValue(filePath),
                                    lineNumber));
                        }

                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(station) ||
                        string.Equals(record.Station, station, StringComparison.OrdinalIgnoreCase))
                    {
                        records.Add(record);
                    }
                }
            }
        }

        private string GetInspectionLogFilePath(DateTime date)
        {
            string directory = ApplicationPaths.GetDateDirectory(
                m_LogRootDirectory,
                date);
            return Path.Combine(
                directory,
                "Inspection_" +
                date.ToString("yyyyMMdd", CultureInfo.InvariantCulture) +
                ".csv");
        }

        private static string CreateInspectionLogLine(InspectionLogRecord record)
        {
            return JoinCsvFields(
                record.Time.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture),
                record.StartedAt.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture),
                record.Station,
                record.LotNumber,
                record.FileName,
                record.OperatorName,
                record.Result,
                record.PeakLoadKgf.ToString("0.000000", CultureInfo.InvariantCulture),
                record.LowerSpecKgf.ToString("0.000000", CultureInfo.InvariantCulture),
                record.UpperSpecKgf.ToString("0.000000", CultureInfo.InvariantCulture),
                record.SampleCount.ToString(CultureInfo.InvariantCulture),
                record.DataFileName,
                NormalizeSingleLine(record.Message),
                record.DataRowNumber.ToString(CultureInfo.InvariantCulture));
        }

        private static bool TryParseInspectionLogLine(
            string line,
            out InspectionLogRecord record)
        {
            record = null;
            List<string> fields = ParseCsvLine(line);
            if (fields.Count != 13 && fields.Count != 14)
            {
                return false;
            }

            DateTime time;
            DateTime startedAt;
            double peakLoad;
            double lowerSpec;
            double upperSpec;
            int sampleCount;
            int dataRowNumber = 0;
            if (fields.Count == 14 && (!int.TryParse(fields[13], NumberStyles.Integer, CultureInfo.InvariantCulture, out dataRowNumber) || dataRowNumber < 0)) return false;
            if (!DateTime.TryParseExact(
                    fields[0],
                    "yyyy-MM-dd HH:mm:ss.fff",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out time) ||
                !DateTime.TryParseExact(
                    fields[1],
                    "yyyy-MM-dd HH:mm:ss.fff",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out startedAt) ||
                !double.TryParse(fields[7], NumberStyles.Float, CultureInfo.InvariantCulture, out peakLoad) ||
                !double.TryParse(fields[8], NumberStyles.Float, CultureInfo.InvariantCulture, out lowerSpec) ||
                !double.TryParse(fields[9], NumberStyles.Float, CultureInfo.InvariantCulture, out upperSpec) ||
                !int.TryParse(fields[10], NumberStyles.Integer, CultureInfo.InvariantCulture, out sampleCount))
            {
                return false;
            }

            record = new InspectionLogRecord
            {
                Time = time,
                StartedAt = startedAt,
                Station = fields[2],
                LotNumber = fields[3],
                FileName = fields[4],
                OperatorName = fields[5],
                Result = fields[6],
                PeakLoadKgf = peakLoad,
                LowerSpecKgf = lowerSpec,
                UpperSpecKgf = upperSpec,
                SampleCount = sampleCount,
                DataFileName = fields[11],
                DataRowNumber = dataRowNumber,
                Message = fields[12]
            };
            return true;
        }

        private static string GetUniqueFilePath(
            string directory,
            string baseFileName,
            string extension)
        {
            string filePath = Path.Combine(directory, baseFileName + extension);
            int index = 1;
            while (File.Exists(filePath) || File.Exists(filePath + ".tmp"))
            {
                filePath = Path.Combine(
                    directory,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}_{1:000}{2}",
                        baseFileName,
                        index,
                        extension));
                index++;
            }

            return filePath;
        }

        private static string NormalizeFileNamePart(string value)
        {
            string candidate = string.IsNullOrWhiteSpace(value)
                ? "Station"
                : value.Trim();
            char[] invalidCharacters = Path.GetInvalidFileNameChars();
            StringBuilder builder = new StringBuilder();
            foreach (char character in candidate)
            {
                builder.Append(
                    char.IsControl(character) || invalidCharacters.Contains(character)
                        ? '_'
                        : character);
            }

            string normalized = builder.ToString().Trim().TrimEnd('.');
            if (normalized.Length == 0)
            {
                return "Station";
            }

            if (normalized.Length > 120)
            {
                normalized = normalized.Substring(0, 120).TrimEnd(' ', '.');
            }

            string reservedName = Path.GetFileNameWithoutExtension(normalized);
            string[] reservedNames =
            {
                "CON", "PRN", "AUX", "NUL",
                "COM1", "COM2", "COM3", "COM4", "COM5",
                "COM6", "COM7", "COM8", "COM9",
                "LPT1", "LPT2", "LPT3", "LPT4", "LPT5",
                "LPT6", "LPT7", "LPT8", "LPT9"
            };
            if (reservedNames.Contains(
                reservedName,
                StringComparer.OrdinalIgnoreCase))
            {
                normalized = "_" + normalized;
            }

            return normalized;
        }

        private static void WriteCsvLine(StreamWriter writer, params string[] fields)
        {
            writer.WriteLine(JoinCsvFields(fields));
        }

        private static string JoinCsvFields(params string[] fields)
        {
            return string.Join(",", fields.Select(EscapeCsvField));
        }

        private static string EscapeCsvField(string value)
        {
            string normalized = value ?? string.Empty;
            if (normalized.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0)
            {
                return normalized;
            }

            return "\"" + normalized.Replace("\"", "\"\"") + "\"";
        }

        private static List<string> ParseCsvLine(string line)
        {
            List<string> fields = new List<string>();
            StringBuilder field = new StringBuilder();
            bool quoted = false;

            for (int index = 0; index < (line ?? string.Empty).Length; index++)
            {
                char character = line[index];
                if (quoted)
                {
                    if (character == '"')
                    {
                        if (index + 1 < line.Length && line[index + 1] == '"')
                        {
                            field.Append('"');
                            index++;
                        }
                        else
                        {
                            quoted = false;
                        }
                    }
                    else
                    {
                        field.Append(character);
                    }
                }
                else if (character == '"')
                {
                    quoted = true;
                }
                else if (character == ',')
                {
                    fields.Add(field.ToString());
                    field.Clear();
                }
                else
                {
                    field.Append(character);
                }
            }

            fields.Add(field.ToString());
            return fields;
        }

        private static string NormalizeSingleLine(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Replace('\r', ' ').Replace('\n', ' ').Trim();
        }

        private static string GetStationLogSource(string station)
        {
            if (string.Equals(station, "#1", StringComparison.OrdinalIgnoreCase))
            {
                return "STATION-1";
            }

            if (string.Equals(station, "#2", StringComparison.OrdinalIgnoreCase))
            {
                return "STATION-2";
            }

            return "INSPECTION-STORAGE";
        }

        private static string EscapeLogValue(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace('\r', ' ')
                    .Replace('\n', ' ');
        }
    }
}

