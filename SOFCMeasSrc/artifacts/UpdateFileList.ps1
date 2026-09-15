$p='SOFCMeas/StationView.cs';$s=Get-Content $p -Raw
$s=$s.Replace('historyStorage.GetInspectionLogs(from, to, historyStation)','historyStorage.GetLotFiles(from, to, historyStation)')
$s=$s.Replace('historyResults.Rows.Add(record.Time.ToString("yyyy-MM-dd HH:mm:ss"), record.Result,'+"`r`n"+'                            record.FileName, record.LotNumber, record.PeakLoadKgf.ToString("0.000"))','historyResults.Rows.Add(record.Date.ToString("yyyy-MM-dd"), record.FileName)')
$s=$s.Replace('historyResults.CurrentRow.Tag as InspectionLogRecord','historyResults.CurrentRow.Tag as LotFileEntry')
$s=$s.Replace('var samples = await System.Threading.Tasks.Task.Run(() => historyStorage.ReadSamples(record));','var inspections = await System.Threading.Tasks.Task.Run(() => historyStorage.ReadLotFile(record, historyStation));')
$start=$s.IndexOf('                    using (var review = new Form { Text = "REVIEW - "');$end=$s.IndexOf('                catch (Exception ex)', $start)
$s=$s.Substring(0,$start)+@"
                    using (var review = new Form { Text = "REVIEW - " + record.FileName, Size = new Size(900, 600), StartPosition = FormStartPosition.CenterParent })
                    using (var graph = new Chart { Dock = DockStyle.Fill })
                    {
                        var selector = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList };
                        foreach (var inspection in inspections)
                            selector.Items.Add("검사 " + inspection.Record.DataRowNumber + "  " + inspection.Record.Result);
                        selector.SelectedIndexChanged += delegate
                        {
                            var inspection = inspections[selector.SelectedIndex];
                            var samples = inspection.Samples;
                            var result = inspection.Record;
                            graph.Series.Clear(); graph.ChartAreas.Clear(); graph.Titles.Clear();
                            var area = new ChartArea("Review"); graph.ChartAreas.Add(area);
                            area.AxisX.Minimum = 0; area.AxisX.Maximum = Math.Max(m_XMax, samples.Count == 0 ? 0 : samples.Max(s => s.Number)) * 1.1;
                            area.AxisY.Minimum = samples.Count == 0 ? 0 : Math.Min(0, samples.Min(s => s.LoadKgf) * 1.1);
                            area.AxisY.Maximum = Math.Max(0.001, Math.Max(result.LowerSpecKgf, result.PeakLoadKgf)) * 1.1;
                            area.AxisY.Interval = (area.AxisY.Maximum - area.AxisY.Minimum) / 10D;
                            ConfigureAxisLabels(area);
                            var series = new Series("하중") { ChartType = SeriesChartType.Line, BorderWidth = 2 };
                            graph.Series.Add(series);
                            foreach (var sample in samples) series.Points.AddXY(sample.Number, sample.LoadKgf);
                            graph.Titles.Add(string.Format("{0}  최대: {1:0.000} kgf  SPEC: {2:0.00}", result.Result, result.PeakLoadKgf, result.LowerSpecKgf));
                        };
                        review.Controls.Add(graph); review.Controls.Add(selector);
                        selector.SelectedIndex = 0;
                        review.ShowDialog(this);
                    }
                }
"@+$s.Substring($end)
Set-Content $p $s -Encoding UTF8
$p='SOFCMeas/StationView.Designer.cs';$s=Get-Content $p -Raw
$s=$s.Replace('            this.Result,'+"`r`n",'').Replace('            this.Model,'+"`r`n",'').Replace('            this.Lot,'+"`r`n",'').Replace('            this.Peak});','            this.Model});')
$s=$s.Replace('this.Time.HeaderText = "검사 일시"','this.Time.HeaderText = "날짜"').Replace('this.Time.Width = 145','this.Time.Width = 90').Replace('this.Model.HeaderText = "모델 명"','this.Model.HeaderText = "파일"')
$s=$s.Replace('            this.Model.ReadOnly = true;', '            this.Model.ReadOnly = true;'+"`r`n"+'            this.Model.Width = 340;')
Set-Content $p $s -Encoding UTF8
