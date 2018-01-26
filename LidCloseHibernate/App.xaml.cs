using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using Hardcodet.Wpf.TaskbarNotification;

namespace LidCloseHibernate
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		private TaskbarIcon notifyIcon;

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			//create the notifyicon (it's a resource declared in NotifyIconResources.xaml
			notifyIcon = (TaskbarIcon)FindResource("NotifyIcon");
			if (WhoIsLoggedIn() == "KRISTIN")
			{
				int i = 0;
			}

			this.MainWindow = new MainWindow();

			this.MainWindow.Show();

			MainWindow_SourceInitialized(null, null);

			this.MainWindow.Hide();
		}

		protected override void OnExit(ExitEventArgs e)
		{
			notifyIcon.Dispose(); //the icon would clean up automatically, but this is cleaner
			base.OnExit(e);
		}

        [DllImport(@"User32", SetLastError = true, EntryPoint = "RegisterPowerSettingNotification",
            CallingConvention = CallingConvention.StdCall)]

        private static extern IntPtr RegisterPowerSettingNotification(IntPtr hRecipient, ref Guid PowerSettingGuid,
            Int32 Flags);

        internal struct POWERBROADCAST_SETTING
        {
            public Guid PowerSetting;
            public uint DataLength;
            public byte Data;
        }

        Guid GUID_LIDSWITCH_STATE_CHANGE = new Guid(0xBA3E0F4D, 0xB817, 0x4094, 0xA2, 0xD1, 0xD5, 0x63, 0x79, 0xE6, 0xA0, 0xF3);
        const int DEVICE_NOTIFY_WINDOW_HANDLE = 0x00000000;
        const int WM_POWERBROADCAST = 0x0218;
        const int PBT_POWERSETTINGCHANGE = 0x8013;

        private bool? _previousLidState = null;

        void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            RegisterForPowerNotifications();
            IntPtr hwnd = new WindowInteropHelper(MainWindow).Handle;
            HwndSource.FromHwnd(hwnd).AddHook(new HwndSourceHook(WndProc));
        }

        private void RegisterForPowerNotifications()
        {
            IntPtr handle = new WindowInteropHelper(Application.Current.Windows[0]).Handle;
            IntPtr hLIDSWITCHSTATECHANGE = RegisterPowerSettingNotification(handle,
                 ref GUID_LIDSWITCH_STATE_CHANGE,
                 DEVICE_NOTIFY_WINDOW_HANDLE);
        }

        IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WM_POWERBROADCAST:
                    OnPowerBroadcast(wParam, lParam);
                    break;
                default:
                    break;
            }
            return IntPtr.Zero;
        }

        private void OnPowerBroadcast(IntPtr wParam, IntPtr lParam)
        {
            if ((int)wParam == PBT_POWERSETTINGCHANGE)
            {
                POWERBROADCAST_SETTING ps = (POWERBROADCAST_SETTING)Marshal.PtrToStructure(lParam, typeof(POWERBROADCAST_SETTING));
                IntPtr pData = (IntPtr)((int)lParam + Marshal.SizeOf(ps));
                Int32 iData = (Int32)Marshal.PtrToStructure(pData, typeof(Int32));
                if (ps.PowerSetting == GUID_LIDSWITCH_STATE_CHANGE)
                {
                    bool isLidOpen = ps.Data != 0;

                    if (!isLidOpen == _previousLidState)
                    {
                        LidStatusChanged(isLidOpen);
                    }

                    _previousLidState = isLidOpen;
                }
            }
        }

        private void LidStatusChanged(bool isLidOpen)
        {
            if (isLidOpen)
            {
                //Do some action on lid open event
                //Debug.WriteLine("{0}: Lid opened!", DateTime.Now);
            }
            else
            {
                //Do some action on lid close event
                //Debug.WriteLine("{0}: Lid closed!", DateTime.Now);
				if (WhoIsLoggedIn().ToLower().Contains("kristin"))
					Process.Start(new ProcessStartInfo("shutdown.exe", "/h"));
			}
        }

		string WhoIsLoggedIn()
		{
			ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT UserName FROM Win32_ComputerSystem");
			ManagementObjectCollection collection = searcher.Get();
			string username = (string)collection.Cast<ManagementBaseObject>().First()["UserName"];

			return username;
		}
	}
}
