$p='SOFCMeas/InspectionStorageService.cs';$s=Get-Content $p -Raw
$s=$s.Replace('internal string DataFileName { get; set; }','internal string DataFileName { get; set; }'+"`r`n"+'        internal int DataRowNumber { get; set; }')
$s=$s.Replace('SampleCount,DataFileName,Message";', 'SampleCount,DataFileName,Message,DataRowNumber";')
$s=$s.Replace('                    foreach (BufferedInspectionData inspection in lot.Inspections)', '                    int dataRowNumber = 0;'+"`r`n"+'                    foreach (BufferedInspectionData inspection in lot.Inspections)')
$s=$s.Replace('                        inspection.Record.DataFileName = dataFileName;', '                        inspection.Record.DataFileName = dataFileName;'+"`r`n"+'                        inspection.Record.DataRowNumber = ++dataRowNumber;')
$start=$s.IndexOf('            string requestedName =');$end=$s.IndexOf('            string filePath =', $start)
$s=$s.Substring(0,$start)+@"
            bool allGood = lot.Inspections.All(i => IsGood(i.Record.Result));
            string baseFileName = lot.CompletedAt.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture)
                + "_" + NormalizeFileNamePart(lot.LotNumber) + "_" + (allGood ? "GOOD" : "NG");

"@+$s.Substring($end)
$start=$s.IndexOf('                    writer.WriteLine(', $s.IndexOf('private string WriteLotDataFile'));$end=$s.IndexOf('                File.Move(temporaryPath', $start)
$s=$s.Substring(0,$start)+@"
                    int good = lot.Inspections.Count(i => IsGood(i.Record.Result));
                    int ng = lot.Inspections.Count - good;
                    WriteCsvLine(writer, "모델명", lot.FileName, "LOT NO.", lot.LotNumber,
                        "작업자", lot.OperatorName, "SPEC", lot.SpecText,
                        "생산수량", lot.Inspections.Count.ToString(CultureInfo.InvariantCulture),
                        "GOOD", good.ToString(CultureInfo.InvariantCulture), "NG", ng.ToString(CultureInfo.InvariantCulture),
                        "Yield(%)", (100D * good / lot.Inspections.Count).ToString("0.00", CultureInfo.InvariantCulture));
                    writer.WriteLine();
                    int width = lot.Inspections.Max(i => i.Samples.Count);
                    var header = new List<string> { "NO.", "판정결과" };
                    for (int i = 1; i <= width; i++) header.Add("LOW DATA " + i + " (kgf)");
                    WriteCsvLine(writer, header.ToArray());
                    for (int i = 0; i < lot.Inspections.Count; i++)
                    {
                        var inspection = lot.Inspections[i];
                        var row = new List<string> { (i + 1).ToString(CultureInfo.InvariantCulture),
                            IsGood(inspection.Record.Result) ? "GOOD" : inspection.Record.Result == "INVALID" ? "INVALID" : "NG" };
                        row.AddRange(inspection.Samples.Select(sample => sample.LoadKgf.ToString("0.000000", CultureInfo.InvariantCulture)));
                        while (row.Count < width + 2) row.Add("");
                        WriteCsvLine(writer, row.ToArray());
                    }
                }

"@+$s.Substring($end)
$start=$s.IndexOf('        private static void WriteLotSampleLine(');$end=$s.IndexOf('        private void AppendInspectionLog', $start)
$s=$s.Substring(0,$start)+@"
        private static bool IsGood(string verdict)
        { return string.Equals(verdict, "GOOD", StringComparison.OrdinalIgnoreCase) || string.Equals(verdict, "PASS", StringComparison.OrdinalIgnoreCase); }

"@+$s.Substring($end)
$s=$s.Replace('if (fields.Count != 13)', 'if (fields.Count != 13 && fields.Count != 14)')
$s=$s.Replace('                Message = fields[12]', '                DataRowNumber = fields.Count == 14 ? int.Parse(fields[13], CultureInfo.InvariantCulture) : 0,'+"`r`n"+'                Message = fields[12]')
# append row index only to CreateInspectionLogLine, keep exports and stored indexes identical
$s=$s.Replace('                NormalizeSingleLine(record.Message));','                NormalizeSingleLine(record.Message),'+"`r`n"+'                record.DataRowNumber.ToString(CultureInfo.InvariantCulture));')
$pos=$s.IndexOf('            foreach (string line in File.ReadLines(files[0], CsvEncoding))')
$s=$s.Insert($pos,@"
            if (record.DataRowNumber > 0)
            {
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
"@)
Set-Content $p $s -Encoding UTF8
