$p='SOFCMeas/StationView.cs';$s=Get-Content $p -Raw
$s=$s.Replace('m_XMax = 10','m_XMax = 60').Replace('m_YMax = 0.7','m_YMax = 0.4')
$s=$s.Replace('            ResetLoadGraph();' , '            ResetLoadGraph();')
$s=$s.Replace('            LotSaveCount = (int)SettingsCatalog.Number(settings, "Station" + station + ".Lot.SaveCount");','            LotSaveCount = (int)SettingsCatalog.Number(settings, "Station" + station + ".Lot.SaveCount");' + "`r`n" + '            txtSpec.Text = SettingsCatalog.Number(settings, "Station" + station + ".Inspection.ThresholdKgf").ToString("0.00", CultureInfo.InvariantCulture);')
$s=$s.Replace('chartArea.AxisX.Maximum = m_XMax;', 'chartArea.AxisX.Maximum = m_AutoX ? m_XMin + (m_XMax - m_XMin) * 1.1D : m_XMax;')
$s=$s.Replace('chartArea.AxisY.Maximum = m_YMax;', 'chartArea.AxisY.Maximum = m_AutoY ? m_YMin + (m_YMax - m_YMin) * 1.1D : m_YMax;')
$s=$s.Replace('chartArea.AxisY.Interval = m_YInterval;', 'chartArea.AxisY.Interval = m_AutoY ? (chartArea.AxisY.Maximum - chartArea.AxisY.Minimum) / 5D : m_YInterval;' + "`r`n" + '            ConfigureAxisLabels(chartArea);')
$s=$s.Replace('peakLoad >= lowerSpec && peakLoad <= upperSpec','peakLoad >= lowerSpec').Replace('ApplyVerdict("PASS"','ApplyVerdict("GOOD"').Replace('ApplyVerdict("FAIL"','ApplyVerdict("NG"')
$s=$s.Replace('SPEC 형식이 올바르지 않습니다. 예: 0.350 ~ 0.600','SPEC 기준값이 올바르지 않습니다. 예: 0.40')
$s=$s.Replace('txtSpec.ReadOnly = !editing;','txtSpec.ReadOnly = true;')
$start=$s.IndexOf('        private void AdjustYAxis(');$end=$s.IndexOf('        private static double CalculateXAxisInterval', $start)
$s=$s.Substring(0,$start)+@"
        private static void ConfigureAxisLabels(ChartArea area)
        {
            area.AxisX.Enabled = AxisEnabled.True;
            area.AxisY.Enabled = AxisEnabled.True;
            area.AxisX.LabelStyle.Enabled = true;
            area.AxisY.LabelStyle.Enabled = true;
            area.AxisX.LabelStyle.ForeColor = Color.Black;
            area.AxisY.LabelStyle.ForeColor = Color.Black;
            area.AxisY.LabelStyle.Format = "0.###";
            area.AxisY.Title = "하중 (kgf)";
            area.AxisX.Title = "샘플 횟수 (기본 1초/회)";
            area.AxisX.MajorGrid.Enabled = true;
            area.AxisY.MajorGrid.Enabled = true;
            area.AxisY.MajorTickMark.Enabled = true;
            area.AxisX.MajorGrid.Interval = 1;
            area.AxisX.LabelStyle.Interval = Math.Max(1, Math.Ceiling(area.AxisX.Maximum / 11D));
            area.AxisY.MajorGrid.Interval = area.AxisY.Interval;
            area.AxisY.LabelStyle.Interval = area.AxisY.Interval;
        }

        private void AdjustYAxis(double loadKgf)
        {
            if (!m_AutoY) return;
            var area = chartLoad.ChartAreas[0];
            double minimum = Math.Min(m_YMin, Math.Min(0, loadKgf * 1.1D));
            area.AxisY.Minimum = Math.Min(area.AxisY.Minimum, minimum);
            double maximum = Math.Max(m_YMax, m_PeakLoadKgf);
            area.AxisY.Maximum = maximum + (maximum - area.AxisY.Minimum) * 0.1D;
            area.AxisY.Interval = (area.AxisY.Maximum - area.AxisY.Minimum) / 5D;
            ConfigureAxisLabels(area);
        }

        private void AdjustXAxis(Series series, int readNumber)
        {
            if (!m_AutoX) return;
            var area = chartLoad.ChartAreas[0];
            area.AxisX.Minimum = m_XMin;
            area.AxisX.Maximum = m_XMin + (Math.Max(m_XMax, readNumber) - m_XMin) * 1.1D;
            ConfigureAxisLabels(area);
        }

"@+$s.Substring($end)
$start=$s.IndexOf('            MatchCollection matches =', $s.IndexOf('private static bool TryParseSpecRange'));$end=$s.IndexOf('            return true;', $start)
$s=$s.Substring(0,$start)+@"
            if (!TryParseSpecNumber((specText ?? "").Trim(), out lowerSpec) ||
                double.IsNaN(lowerSpec) || double.IsInfinity(lowerSpec) || lowerSpec < 0)
                return false;
            // Keep legacy CSV columns readable; both store the single threshold for new results.
            upperSpec = lowerSpec;

"@+$s.Substring($end)
$s=$s.Replace('            lblCountValue.Text =', '            lblCompletedCount.Text = "검사 횟수: " + m_InspectionCount.ToString(CultureInfo.InvariantCulture);' + "`r`n" + '            lblCountValue.Text =')
Set-Content $p $s -Encoding UTF8
foreach($p in @('SOFCMeas/StationView.Designer.cs','SOFCMeas/SOFCMeas.Designer.cs')) { $s=Get-Content $p -Raw; $s=$s.Replace('0.350 ~ 0.600','0.40').Replace('PASS(EA)','GOOD(EA)').Replace('FAIL(EA)','NG(EA)'); Set-Content $p $s -Encoding UTF8 }
