using System;
using System.Diagnostics;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ClassLibraryCommon;
using ArmAFlightpanels.Properties;

using MessageBox = System.Windows.MessageBox;

namespace ArmAFlightpanels
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {

        private bool _generalChanged = false;
        private bool _armaRedisChanged = false;

        private string _ipArmARedisAddress;
        private int _portArmARedis;
        private string _pwdArmARedis;
        private string _keywordArmARedis;

        private ProfileMode _profileMode;

        public SettingsWindow(ProfileMode profileMode)
        {
            InitializeComponent();
            _profileMode = profileMode;
        }

        private void SettingsWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                ButtonOk.IsEnabled = false;
                LoadSettings();
                if (_profileMode == ProfileMode.KEYEMULATOR)
                {
                    LabelArmARedis.Visibility = Visibility.Collapsed;
                }
                StackPanelGeneralSettings.Visibility = Visibility.Visible;
                StackPanelArmARedisSettings.Visibility = Visibility.Collapsed;
                ActivateTriggers();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message + Environment.NewLine + exception.StackTrace);
            }
        }

        private void ActivateTriggers()
        {
            RadioButtonBelowNormal.Checked += RadioButtonProcessPriority_OnChecked;
            RadioButtonNormal.Checked += RadioButtonProcessPriority_OnChecked;
            RadioButtonAboveNormal.Checked += RadioButtonProcessPriority_OnChecked;
            RadioButtonHigh.Checked += RadioButtonProcessPriority_OnChecked;
            RadioButtonRealtime.Checked += RadioButtonProcessPriority_OnChecked;
            RadioButtonKeyBd.Checked += RadioButtonAPI_OnChecked;
            RadioButtonSendInput.Checked += RadioButtonAPI_OnChecked;
            
            TextBoxArmARedisIPAddress.TextChanged += TextBoxArmARedis_OnTextChanged;
            TextBoxArmARedisPort.TextChanged += TextBoxArmARedis_OnTextChanged;
            TextBoxArmAPassword.TextChanged += TextBoxArmARedis_OnTextChanged;
            TextBoxRedisDataKeyword.TextChanged += TextBoxArmARedis_OnTextChanged;

            CheckBoxDoDebug.Checked += CheckBoxDebug_OnChecked;
            CheckBoxDebugToFile.Checked += CheckBoxDebug_OnChecked;
        }

        private void LoadSettings()
        {
            switch (Settings.Default.ProcessPriority)
            {
                case ProcessPriorityClass.BelowNormal:
                    {
                        RadioButtonBelowNormal.IsChecked = true;
                        break;
                    }
                case ProcessPriorityClass.Normal:
                    {
                        RadioButtonNormal.IsChecked = true;
                        break;
                    }
                case ProcessPriorityClass.AboveNormal:
                    {
                        RadioButtonAboveNormal.IsChecked = true;
                        break;
                    }
                case ProcessPriorityClass.High:
                    {
                        RadioButtonHigh.IsChecked = true;
                        break;
                    }
                case ProcessPriorityClass.RealTime:
                    {
                        RadioButtonRealtime.IsChecked = true;
                        break;
                    }
            }
            if (Settings.Default.APIMode == 0)
            {
                RadioButtonKeyBd.IsChecked = true;
            }
            else
            {
                RadioButtonSendInput.IsChecked = true;
            }

            CheckBoxDoDebug.IsChecked = Settings.Default.DebugOn;
            CheckBoxDebugToFile.IsChecked = Settings.Default.DebugToFile;

                        if (_profileMode == ProfileMode.KEYEMULATOR_ARMA)
            {
                TextBoxArmARedisIPAddress.Text = Settings.Default.ArmARedisIPAddress;
                TextBoxArmARedisPort.Text = Convert.ToString(Settings.Default.ArmARedisPort);
                TextBoxArmAPassword.Text = Settings.Default.ArmARedisPwd;
                TextBoxRedisDataKeyword.Text = Settings.Default.ArmARedisKey;
            }
        }

        private void GeneralSettings_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            StackPanelGeneralSettings.Visibility = Visibility.Visible;
            StackPanelArmARedisSettings.Visibility = Visibility.Collapsed;
        }

        
        private void LabelArmARedis_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            StackPanelGeneralSettings.Visibility = Visibility.Collapsed;
            StackPanelArmARedisSettings.Visibility = Visibility.Visible;
        }

        private void ButtonCancel_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ButtonOk_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                CheckValuesArmARedis();

                if (_generalChanged)
                {
                    if (RadioButtonKeyBd.IsChecked == true)
                    {
                        Settings.Default.APIMode = 0;
                        Common.APIMode = APIModeEnum.keybd_event;
                        Settings.Default.Save();
                    }
                    if (RadioButtonSendInput.IsChecked == true)
                    {
                        Settings.Default.APIMode = 1;
                        Common.APIMode = APIModeEnum.SendInput;
                        Settings.Default.Save();
                    }
                    if (RadioButtonBelowNormal.IsChecked == true)
                    {
                        Settings.Default.ProcessPriority = ProcessPriorityClass.BelowNormal;
                        Settings.Default.Save();
                        Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;
                    }
                    if (RadioButtonNormal.IsChecked == true)
                    {
                        Settings.Default.ProcessPriority = ProcessPriorityClass.Normal;
                        Settings.Default.Save();
                        Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
                    }
                    if (RadioButtonAboveNormal.IsChecked == true)
                    {
                        Settings.Default.ProcessPriority = ProcessPriorityClass.AboveNormal;
                        Settings.Default.Save();
                        Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.AboveNormal;
                    }
                    if (RadioButtonHigh.IsChecked == true)
                    {
                        Settings.Default.ProcessPriority = ProcessPriorityClass.High;
                        Settings.Default.Save();
                        Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
                    }
                    if (RadioButtonRealtime.IsChecked == true)
                    {
                        Settings.Default.ProcessPriority = ProcessPriorityClass.RealTime;
                        Settings.Default.Save();
                        Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;
                    }

                    Settings.Default.DebugOn = CheckBoxDoDebug.IsChecked == true;
                    Settings.Default.DebugToFile = CheckBoxDebugToFile.IsChecked == true;
                    Settings.Default.Save();
                }

                
                if (_armaRedisChanged)
                {
                    Settings.Default.ArmARedisIPAddress = ArmARedisIPAddress;
                    Settings.Default.ArmARedisPort = ArmARedisPort;
                    Settings.Default.ArmARedisPwd = ArmARedisPassword;
                    Settings.Default.ArmARedisKey = ArmARedisKeyword;
                    Settings.Default.Save();
                }

                DialogResult = true;
                Close();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message + Environment.NewLine + exception.StackTrace);
            }
        }

        private void CheckValuesArmARedis()
        {
            try
            {
                IPAddress ipAddress;
                if (string.IsNullOrEmpty(TextBoxArmARedisIPAddress.Text))
                {
                    throw new Exception("ArmA Redis IP address cannot be empty");
                }
                if (string.IsNullOrEmpty(TextBoxArmARedisPort.Text))
                {
                    throw new Exception("ArmA Redis Port cannot be empty");
                }
                try
                {
                    if (!IPAddress.TryParse(TextBoxArmARedisIPAddress.Text, out ipAddress))
                    {
                        throw new Exception();
                    }
                    _ipArmARedisAddress = TextBoxArmARedisIPAddress.Text;
                }
                catch (Exception e)
                {
                    throw new Exception("ArmA Redis Error while checking IP : " + e.Message);
                }
                try
                {
                    _portArmARedis = Convert.ToInt32(TextBoxArmARedisPort.Text);
                }
                catch (Exception e)
                {
                    throw new Exception("ArmA Redis Error while checking Port : " + e.Message);
                }
                _pwdArmARedis = TextBoxArmAPassword.Text;

                if (String.IsNullOrWhiteSpace(TextBoxRedisDataKeyword.Text))
                {
                    throw new Exception("ArmA Redis Data Key cannot be empty");
                }
                _keywordArmARedis = TextBoxRedisDataKeyword.Text;

            }
            catch (Exception e)
            {
                throw new Exception("ArmA Redis Error checking values : " + Environment.NewLine + e.Message);
            }
        }
        

        public bool GeneralChanged => _generalChanged;
        
        public bool ArmAChanged => _armaRedisChanged;

        public string ArmARedisIPAddress => _ipArmARedisAddress;

        public int ArmARedisPort => _portArmARedis;

        public string ArmARedisPassword => _pwdArmARedis;

        public string ArmARedisKeyword => _keywordArmARedis;
        
        private void TextBoxArmARedis_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            _armaRedisChanged = true;
            ButtonOk.IsEnabled = true;
        }

        private void RadioButtonProcessPriority_OnChecked(object sender, RoutedEventArgs e)
        {
            _generalChanged = true;
            ButtonOk.IsEnabled = true;
        }

        private void CheckBoxDebug_OnChecked(object sender, RoutedEventArgs e)
        {
            _generalChanged = true;
            ButtonOk.IsEnabled = true;
        }

        private void RadioButtonAPI_OnChecked(object sender, RoutedEventArgs e)
        {
            _generalChanged = true;
            ButtonOk.IsEnabled = true;
        }

        private void CheckBoxDebug_OnUnchecked(object sender, RoutedEventArgs e)
        {
            _generalChanged = true;
            ButtonOk.IsEnabled = true;
        }

    }
}
