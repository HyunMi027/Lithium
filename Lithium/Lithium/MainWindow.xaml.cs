using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MahApps.Metro.Controls;
using MaterialDesignThemes.Wpf;
using Newtonsoft.Json;

namespace Lithium
{
    public partial class MainWindow : MetroWindow, INotifyPropertyChanged
    {
        //TODO: use IValueConverter
        public List<string> PeriodDateFormatted => this._periodDate.Select(e => e.ToLongDateString()).ToList(); 

        private SortedSet<DateTime> _periodDate = File.Exists(PeriodDataFile) ? JsonConvert.DeserializeObject<SortedSet<DateTime>>(File.ReadAllText(PeriodDataFile)) : new SortedSet<DateTime>();

        private static string PeriodDataFile => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "periodData.json");


        public SortedSet<DateTime> PeriodData
        {
            get { return this._periodDate; }
            set
            {
                this._periodDate = value;
                this.OnPropertyChanged(nameof(this.PeriodData));
            }
        }

        private DateTime _selectedPriodDate = DateTime.Now;

        public DateTime SelectedPeriodDate
        {
            get { return this._selectedPriodDate; }
            set
            {
                this._selectedPriodDate = value;
                this.OnPropertyChanged(nameof(this.SelectedPeriodDate));
            }
        }

        private List<int> _periodLengths => this.PeriodData.TakeWhile((t, i) => i != this.PeriodData.Count - 1).Select((t, i) => (this.PeriodData.ToArray()[i + 1] - this.PeriodData.ToArray()[i]).Days).ToList();

        private double _averagePeriodLength => (double) this._periodLengths.Sum()/this._periodLengths.Count;

        public string DisplayAverage => this._periodLengths.Count > 1 ? $"Average: {this._averagePeriodLength.ToString("##,##")} days" : string.Empty;
        public string DisplayMax => this._periodLengths.Count > 1 ? $"Max: {this._periodLengths.Max()} days" : string.Empty;
        public string DisplayMin => this._periodLengths.Count > 1 ? $"Min: {this._periodLengths.Min()} days" : string.Empty;

        public List<string> Statistics
        {
            get
            {
                List<string> data = new List<string>();
                foreach (int length in this._periodLengths)
                {
                    string entry = $"Length: {length.ToString().PadRight(2)} days".PadRight(20);
                    if (this._periodLengths.Count > 1)
                    {
                        entry += $"Percentage from average deviation: {Math.Abs(length/this._averagePeriodLength*100 - 100).ToString("##,##")}%";
                    }
                    data.Add(entry);
                }
                return data;
            }
        }

        public MainWindow()
        {
            this.InitializeComponent();

            this.Loaded += delegate(object sender, RoutedEventArgs args)
            {
                this.Title = $"Lithium - {Assembly.GetExecutingAssembly().GetName().Version}";

                this.PopupAddCustom.DialogOpened += delegate(object o, DialogOpenedEventArgs eventArgs)
                {
                    ((DatePicker)((StackPanel)eventArgs.Session.Content).Children[1]).SelectedDate = DateTime.Now;
                };

                this.PopupAddCustom.DialogClosing += delegate(object o, DialogClosingEventArgs eventArgs)
                {
                    if (!eventArgs.IsCancelled)
                    {
                        var selectedDate = ((DatePicker) ((StackPanel) eventArgs.Session.Content).Children[1]).SelectedDate;
                        if (selectedDate.HasValue)
                        {
                            this.AddPeriod(selectedDate.Value);
                        }
                    }
                };
            };

            this.Closing += delegate(object sender, CancelEventArgs args)
            {
                if (!Directory.Exists("data"))
                {
                    Directory.CreateDirectory("data");
                }
                File.WriteAllText(PeriodDataFile, JsonConvert.SerializeObject(this.PeriodData));
            };
        }

        private void RemovePeriod(string str)
        {
            this.PeriodData.RemoveWhere(d => d.ToLongDateString() == str);
            this.OnPropertyChanged(nameof(this.PeriodData));
        }

        private void AddPeriod(DateTime date)
        {
            if (this.PeriodData.Select(e => e.ToLongDateString()).Any(e => e == date.ToLongDateString()))
            {
                return;
            }

            this.PeriodData.Add(date);
            this.OnPropertyChanged(nameof(this.PeriodData));
        }

        private void PeriodsRemove_OnClick(object sender, RoutedEventArgs e) => this.RemovePeriod((string)(sender as Button)?.Tag);

        private void TabcMain_callback(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button))
            {
                throw new Exception("TabcMain_callback sender was not a button");
            }

            string tag = (string)((Button)sender).Tag;

            int index;
            if (!int.TryParse(tag, out index))
            {
                throw new Exception("TabcMain_callback index tryparse");
            }

            this.TabcMain.SelectedIndex = index;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            if (propertyName == "PeriodData")
            {
                this.OnPropertyChanged(nameof(this.PeriodDateFormatted));
                this.OnPropertyChanged(nameof(this.Statistics));
                this.OnPropertyChanged(nameof(this.DisplayAverage));
                this.OnPropertyChanged(nameof(this.DisplayMax));
                this.OnPropertyChanged(nameof(this.DisplayMin));
            }
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
