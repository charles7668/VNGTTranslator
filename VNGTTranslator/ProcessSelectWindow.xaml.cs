#region

using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using VNGTTranslator.Hooker;
using VNGTTranslator.LunaHook;

#endregion

namespace VNGTTranslator
{
    public partial class ProcessSelectWindow : INotifyPropertyChanged
    {
        public ProcessSelectWindow()
        {
            ProcessList = GetProcessList();
            InitializeComponent();
            Topmost = true;
            SelectedProcess = ProcessList.FirstOrDefault(x => x.EndsWith(Program.PID.ToString()));
            DataContext = this;
        }

        private string? _selectedProcess;

        public List<string> ProcessList { get; set; }

        public string? SelectedProcess
        {
            get => _selectedProcess;
            set => SetField(ref _selectedProcess, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void BtnClose_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BtnSave_OnClick(object sender, RoutedEventArgs e)
        {
            string? pidString = SelectedProcess?.Split("—").Last();
            if (!uint.TryParse(pidString, out uint pid))
            {
                MessageBox.Show("please select a process");
                return;
            }

            IHooker hooker = Program.ServiceProvider.GetRequiredService<IHooker>();
            try
            {
                hooker.Detach(Program.PID);
            }
            catch (Exception)
            {
                // ignore
            }

            hooker.Inject(pid, LunaDll.LunaHookDllPath);
            Program.PID = pid;
            Close();
        }

        private static List<string> GetProcessList()
        {
            var processList = new List<string>();
            foreach (Process p in Process.GetProcesses())
            {
                if (p.MainWindowHandle != IntPtr.Zero)
                {
                    string info = p.ProcessName + "—" + p.Id;
                    processList.Add(info);
                }

                p.Dispose();
            }

            return processList;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}