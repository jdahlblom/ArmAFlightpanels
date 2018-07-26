using System;
using System.Windows;
using System.Windows.Controls;
using ClassLibraryCommon;
using CommonClassLibraryJD;

namespace ArmAFlightpanels
{
    /// <summary>
    /// Interaction logic for ChooseProfileModuleWindow.xaml
    /// </summary>
    public partial class ChooseProfileModuleWindow : Window
    {
        private ProfileMode _profileMode = ProfileMode.KEYEMULATOR_ARMA;

        public ChooseProfileModuleWindow()
        {
            InitializeComponent();
        }

        private void ChooseProfileModuleWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            PopulateAirframeCombobox();
        }

        private void ButtonCancel_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                DialogResult = false;
                Close();
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(23060, ex);
            }
        }

        private void ButtonOk_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                DialogResult = true;
                SetAirframe();
                Close();
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(23060, ex);
            }
        }


        private void PopulateAirframeCombobox()
        {
            if (!IsLoaded)
            {
                return;
            }
            ComboBoxAirframe.SelectionChanged -= ComboBoxAirframe_OnSelectionChanged;
            ComboBoxAirframe.Items.Clear();
            foreach (ProfileMode airframe in Enum.GetValues(typeof(ProfileMode)))
            {
                if (airframe != ProfileMode.NOFRAMELOADEDYET)
                {
                    ComboBoxAirframe.Items.Add(EnumEx.GetDescription(airframe));
                }
            }
            ComboBoxAirframe.SelectedIndex = 0;
            ComboBoxAirframe.SelectionChanged += ComboBoxAirframe_OnSelectionChanged;
        }

        private void SetAirframe()
        {
            if (IsLoaded && ComboBoxAirframe.SelectedItem != null)
            {
                _profileMode = EnumEx.GetValueFromDescription<ProfileMode>(ComboBoxAirframe.SelectedItem.ToString());
            }
        }

        private void ComboBoxAirframe_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                SetAirframe();
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(2060, ex);
            }
        }

        public ProfileMode DCSAirframe
        {
            get { return _profileMode; }
        }


    }
}
