using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ClassLibraryCommon;

using NonVisuals;
using ArmAFlightpanels.Properties;

namespace ArmAFlightpanels
{
    /// <summary>
    /// Interaction logic for MultiPanelUserControlArmA.xaml
    /// </summary>

    public partial class MultiPanelUserControlArmA : ISaitekPanelListener, IProfileHandlerListener, ISaitekUserControl
    {
        private readonly MultiPanelPZ70ArmA _multiPanelPZ70ArmA;
        private TabItem _parentTabItem;
        private string _parentTabItemHeader;
        private IGlobalHandler _globalHandler;
        private bool _userControlLoaded;
        private bool _textBoxTagsSet;

        public MultiPanelUserControlArmA(HIDSkeleton hidSkeleton, TabItem parentTabItem, IGlobalHandler globalHandler)
        {
            InitializeComponent();
            _parentTabItem = parentTabItem;
            _parentTabItemHeader = _parentTabItem.Header.ToString();
            _multiPanelPZ70ArmA = new MultiPanelPZ70ArmA(hidSkeleton);
            _multiPanelPZ70ArmA.Attach((ISaitekPanelListener)this);
            globalHandler.Attach(_multiPanelPZ70ArmA);
            _globalHandler = globalHandler;

            HideAllImages();
        }

        private void MultiPanelUserControlArmA_OnLoaded(object sender, RoutedEventArgs e)
        {
            ComboBoxLcdKnobSensitivity.SelectedValue = Settings.Default.PZ70LcdKnobSensitivity;
            SetTextBoxTagObjects();
            SetContextMenuClickHandlers();
            _userControlLoaded = true;
            ShowGraphicConfiguration();
        }

        public void BipPanelRegisterEvent(object sender, BipPanelRegisteredEventArgs e)
        {
            var now = DateTime.Now.Ticks;
            Debug.WriteLine("Start BipPanelRegisterEvent");
            RemoveContextMenuClickHandlers();
            SetContextMenuClickHandlers();
            Debug.WriteLine("End BipPanelRegisterEvent" + new TimeSpan(DateTime.Now.Ticks - now).Milliseconds);
        }

        public SaitekPanel GetSaitekPanel()
        {
            return _multiPanelPZ70ArmA;
        }
        
        public string GetName()
        {
            return GetType().Name;
        }

        public void SelectedAirframe(object sender, AirframEventArgs e)
        {
            try
            {
                //SetApplicationMode(e.Airframe);
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(471573, ex);
            }
        }


        public void SwitchesChanged(object sender, SwitchesChangedEventArgs e)
        {
            try
            {
                if (e.SaitekPanelEnum == SaitekPanelsEnum.PZ70MultiPanel && e.UniqueId.Equals(_multiPanelPZ70ArmA.InstanceId))
                {
                    NotifyKnobChanges(e.Switches);
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(1018, ex);
            }
        }

        public void PanelSettingsReadFromFile(object sender, SettingsReadFromFileEventArgs e)
        {
            try
            {
                ShowGraphicConfiguration();
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(1019, ex);
            }
        }

        public void SettingsCleared(object sender, PanelEventArgs e)
        {
            try
            {
                ClearAll(false);
                ShowGraphicConfiguration();
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(1020, ex);
            }
        }

        private void ClearAll(bool clearAlsoProfile = true)
        {
            foreach (var textBox in Common.FindVisualChildren<TextBox>(this))
            {
                var tagHolderClass = (TagDataClassPZ70)textBox.Tag;
                textBox.Text = "";
                tagHolderClass.ClearAll();
            }

            if (clearAlsoProfile)
            {
                _multiPanelPZ70ArmA.ClearSettings();
            }
        }

        private void SetTextBoxTagObjects()
        {
            if (_textBoxTagsSet || !Common.FindVisualChildren<TextBox>(this).Any())
            {
                return;
            }

            foreach (var textBox in Common.FindVisualChildren<TextBox>(this))
            {
                //Debug.WriteLine("Adding TextBoxTagHolderClass for TextBox " + textBox.Name);
                textBox.Tag = new TagDataClassPZ70();
            }

            _textBoxTagsSet = true;
        }

        public void LedLightChanged(object sender, LedLightChangeEventArgs e)
        {
            try
            {
                //todo
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(1021, ex);
            }
        }

        public void PanelSettingsChanged(object sender, PanelEventArgs e)
        {
            try
            {
                //todo nada?
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(1022, ex);
            }
        }
        

        public void SettingsApplied(object sender, PanelEventArgs e)
        {
            try
            {
                if (e.UniqueId.Equals(_multiPanelPZ70ArmA.InstanceId) && e.SaitekPanelEnum == SaitekPanelsEnum.PZ70MultiPanel)
                {
                    Dispatcher.BeginInvoke((Action)(ShowGraphicConfiguration));
                    Dispatcher.BeginInvoke((Action)(() => TextBoxLogPZ70.Text = ""));
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(992032, ex);
            }
        }

        public void DeviceAttached(object sender, PanelEventArgs e)
        {
            try
            {
                //todo
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(1025, ex);
            }
        }

        public void DeviceDetached(object sender, PanelEventArgs e)
        {
            try
            {
                //todo
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(1026, ex);
            }
        }

        private void ButtonGetId_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_multiPanelPZ70ArmA != null)
                {
                    Clipboard.SetText(_multiPanelPZ70ArmA.InstanceId);
                    MessageBox.Show("Instance id has been copied to the ClipBoard.");
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(2000, ex);
            }
        }

        private void SetGraphicsState(HashSet<object> knobs)
        {
            try
            {
                foreach (var multiKnobO in knobs)
                {
                    var multiKnob = (MultiPanelKnob)multiKnobO;
                    switch (multiKnob.MultiPanelPZ70Knob)
                    {
                        case MultiPanelPZ70Knobs.KNOB_ALT:
                            {
                                var key = multiKnob;
                                Dispatcher.BeginInvoke(
                                    (Action)delegate
                                   {
                                        ImageLeftKnobAlt.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                        if (key.IsOn)
                                        {
                                            ClearAll(false);
                                            ShowGraphicConfiguration();
                                        }
                                    });
                                break;
                            }
                        case MultiPanelPZ70Knobs.KNOB_VS:
                            {
                                var key = multiKnob;
                                Dispatcher.BeginInvoke(
                                    (Action)delegate
                                   {
                                        ImageLeftKnobVs.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                        if (key.IsOn)
                                        {
                                            ClearAll(false);
                                            ShowGraphicConfiguration();
                                        }
                                    });
                                break;
                            }
                        case MultiPanelPZ70Knobs.KNOB_IAS:
                            {
                                var key = multiKnob;
                                Dispatcher.BeginInvoke(
                                    (Action)delegate
                                   {
                                        ImageLeftKnobIas.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                        if (key.IsOn)
                                        {
                                            ClearAll(false);
                                            ShowGraphicConfiguration();
                                        }
                                    });
                                break;
                            }
                        case MultiPanelPZ70Knobs.KNOB_HDG:
                            {
                                var key = multiKnob;
                                Dispatcher.BeginInvoke(
                                    (Action)delegate
                                   {
                                        ImageLeftKnobHdg.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                        if (key.IsOn)
                                        {
                                            ClearAll(false);
                                            ShowGraphicConfiguration();
                                        }
                                    });
                                break;
                            }
                        case MultiPanelPZ70Knobs.KNOB_CRS:
                            {
                                var key = multiKnob;
                                Dispatcher.BeginInvoke(
                                    (Action)delegate
                                   {
                                        ImageLeftKnobCrs.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;

                                        if (key.IsOn)
                                        {
                                            ClearAll(false);
                                            ShowGraphicConfiguration();
                                        }
                                    });
                                break;
                            }
                        case MultiPanelPZ70Knobs.AP_BUTTON:
                            {
                                Dispatcher.BeginInvoke(
                                    (Action)delegate
                                   {
                                        ImageLcdButtonAp.Visibility =
                                            _multiPanelPZ70ArmA.LCDButtonByte.IsOn(multiKnob.MultiPanelPZ70Knob)
                                                ? Visibility.Visible
                                                : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case MultiPanelPZ70Knobs.HDG_BUTTON:
                            {
                                Dispatcher.BeginInvoke(
                                    (Action)delegate
                                   {
                                        ImageLcdButtonHdg.Visibility =
                                            _multiPanelPZ70ArmA.LCDButtonByte.IsOn(multiKnob.MultiPanelPZ70Knob)
                                                ? Visibility.Visible
                                                : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case MultiPanelPZ70Knobs.NAV_BUTTON:
                            {
                                Dispatcher.BeginInvoke(
                                    (Action)delegate
                                   {
                                        ImageLcdButtonNav.Visibility =
                                            _multiPanelPZ70ArmA.LCDButtonByte.IsOn(multiKnob.MultiPanelPZ70Knob)
                                                ? Visibility.Visible
                                                : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case MultiPanelPZ70Knobs.IAS_BUTTON:
                            {
                                Dispatcher.BeginInvoke(
                                    (Action)delegate
                                   {
                                        ImageLcdButtonIas.Visibility =
                                            _multiPanelPZ70ArmA.LCDButtonByte.IsOn(multiKnob.MultiPanelPZ70Knob)
                                                ? Visibility.Visible
                                                : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case MultiPanelPZ70Knobs.ALT_BUTTON:
                            {
                                Dispatcher.BeginInvoke(
                                    (Action)delegate
                                   {
                                        ImageLcdButtonAlt.Visibility =
                                            _multiPanelPZ70ArmA.LCDButtonByte.IsOn(multiKnob.MultiPanelPZ70Knob)
                                                ? Visibility.Visible
                                                : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case MultiPanelPZ70Knobs.VS_BUTTON:
                            {
                                Dispatcher.BeginInvoke(
                                    (Action)delegate
                                   {
                                        ImageLcdButtonVs.Visibility =
                                            _multiPanelPZ70ArmA.LCDButtonByte.IsOn(multiKnob.MultiPanelPZ70Knob)
                                                ? Visibility.Visible
                                                : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case MultiPanelPZ70Knobs.APR_BUTTON:
                            {
                                Dispatcher.BeginInvoke(
                                    (Action)delegate
                                   {
                                        ImageLcdButtonApr.Visibility =
                                            _multiPanelPZ70ArmA.LCDButtonByte.IsOn(multiKnob.MultiPanelPZ70Knob)
                                                ? Visibility.Visible
                                                : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case MultiPanelPZ70Knobs.REV_BUTTON:
                            {
                                Dispatcher.BeginInvoke(
                                    (Action)delegate
                                   {
                                        ImageLcdButtonRev.Visibility =
                                            _multiPanelPZ70ArmA.LCDButtonByte.IsOn(multiKnob.MultiPanelPZ70Knob)
                                                ? Visibility.Visible
                                                : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case MultiPanelPZ70Knobs.LCD_WHEEL_DEC:
                            {
                                var key = multiKnob;
                                Dispatcher.BeginInvoke(
                                    (Action)delegate
                                   {
                                        ImageLcdKnobDec.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case MultiPanelPZ70Knobs.LCD_WHEEL_INC:
                            {
                                var key = multiKnob;
                                Dispatcher.BeginInvoke(
                                    (Action)delegate
                                   {
                                        ImageLcdKnobInc.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case MultiPanelPZ70Knobs.AUTO_THROTTLE:
                            {
                                var key = multiKnob;
                                Dispatcher.BeginInvoke(
                                    (Action)delegate
                                   {
                                        ImageLcdAutoThrottleArm.Visibility =
                                            key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                        ImageLcdAutoThrottleOff.Visibility =
                                            !key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case MultiPanelPZ70Knobs.FLAPS_LEVER_UP:
                            {
                                var key = multiKnob;
                                Dispatcher.BeginInvoke(
                                    (Action)delegate
                                   {
                                        ImageFlapsUp.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case MultiPanelPZ70Knobs.FLAPS_LEVER_DOWN:
                            {
                                var key = multiKnob;
                                Dispatcher.BeginInvoke(
                                    (Action)delegate
                                   {
                                        ImageFlapsDown.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case MultiPanelPZ70Knobs.PITCH_TRIM_WHEEL_UP:
                            {
                                var key = multiKnob;
                                Dispatcher.BeginInvoke(
                                    (Action)delegate
                                   {
                                        ImagePitchUp.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case MultiPanelPZ70Knobs.PITCH_TRIM_WHEEL_DOWN:
                            {
                                var key = multiKnob;
                                Dispatcher.BeginInvoke(
                                    (Action)delegate
                                   {
                                        ImagePitchDown.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                    }
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(2019, ex);
            }
        }

        private void HideAllImages()
        {
            ImageLeftKnobAlt.Visibility = Visibility.Collapsed;
            ImageLeftKnobVs.Visibility = Visibility.Collapsed;
            ImageLeftKnobIas.Visibility = Visibility.Collapsed;
            ImageLeftKnobHdg.Visibility = Visibility.Collapsed;
            ImageLeftKnobCrs.Visibility = Visibility.Collapsed;
            ImageLcdButtonAp.Visibility = Visibility.Collapsed;
            ImageLcdButtonHdg.Visibility = Visibility.Collapsed;
            ImageLcdButtonNav.Visibility = Visibility.Collapsed;
            ImageLcdButtonIas.Visibility = Visibility.Collapsed;
            ImageLcdButtonAlt.Visibility = Visibility.Collapsed;
            ImageLcdButtonVs.Visibility = Visibility.Collapsed;
            ImageLcdButtonApr.Visibility = Visibility.Collapsed;
            ImageLcdButtonRev.Visibility = Visibility.Collapsed;
            ImageLcdKnobDec.Visibility = Visibility.Collapsed;
            ImageLcdKnobInc.Visibility = Visibility.Collapsed;
            ImageLcdAutoThrottleOff.Visibility = Visibility.Collapsed;
            ImageLcdAutoThrottleArm.Visibility = Visibility.Collapsed;
            ImageFlapsUp.Visibility = Visibility.Collapsed;
            ImageFlapsDown.Visibility = Visibility.Collapsed;
            ImagePitchUp.Visibility = Visibility.Collapsed;
            ImagePitchDown.Visibility = Visibility.Collapsed;

        }

        private void TextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                var textBox = (TextBox)sender;
                if (((TagDataClassPZ70)textBox.Tag).ContainsBIPLink())
                {
                    ((TextBox)sender).Background = Brushes.Bisque;
                }
                else
                {
                    ((TextBox)sender).Background = Brushes.White;
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(3005, ex);
            }
        }

        private void ShowGraphicConfiguration()
        {
            try
            {
                if (!_userControlLoaded || !_textBoxTagsSet)
                {
                    return;
                }

                ImageLcdButtonAp.Visibility =
                    _multiPanelPZ70ArmA.LCDButtonByte.IsOn(MultiPanelPZ70Knobs.AP_BUTTON)
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                ImageLcdButtonHdg.Visibility =
                    _multiPanelPZ70ArmA.LCDButtonByte.IsOn(MultiPanelPZ70Knobs.HDG_BUTTON)
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                ImageLcdButtonNav.Visibility =
                    _multiPanelPZ70ArmA.LCDButtonByte.IsOn(MultiPanelPZ70Knobs.NAV_BUTTON)
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                ImageLcdButtonIas.Visibility =
                    _multiPanelPZ70ArmA.LCDButtonByte.IsOn(MultiPanelPZ70Knobs.IAS_BUTTON)
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                ImageLcdButtonAlt.Visibility =
                    _multiPanelPZ70ArmA.LCDButtonByte.IsOn(MultiPanelPZ70Knobs.ALT_BUTTON)
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                ImageLcdButtonVs.Visibility =
                    _multiPanelPZ70ArmA.LCDButtonByte.IsOn(MultiPanelPZ70Knobs.VS_BUTTON)
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                ImageLcdButtonApr.Visibility =
                    _multiPanelPZ70ArmA.LCDButtonByte.IsOn(MultiPanelPZ70Knobs.APR_BUTTON)
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                ImageLcdButtonRev.Visibility =
                    _multiPanelPZ70ArmA.LCDButtonByte.IsOn(MultiPanelPZ70Knobs.REV_BUTTON)
                        ? Visibility.Visible
                        : Visibility.Collapsed;

                foreach (var keyBinding in _multiPanelPZ70ArmA.KeyBindingsHashSet)
                {
                    if (keyBinding.MultiPanelPZ70Knob == MultiPanelPZ70Knobs.KNOB_ALT)
                    {
                        if (keyBinding.WhenTurnedOn)
                        {
                            if (keyBinding.OSKeyPress != null)
                            {
                                ((TagDataClassPZ70)TextBoxALT.Tag).KeyPress = keyBinding.OSKeyPress;
                                TextBoxALT.Text = ((TagDataClassPZ70)TextBoxALT.Tag).GetTextBoxKeyPressInfo();
                            }
                        }
                    }
                    if (keyBinding.MultiPanelPZ70Knob == MultiPanelPZ70Knobs.KNOB_VS)
                    {
                        if (keyBinding.WhenTurnedOn)
                        {
                            if (keyBinding.OSKeyPress != null)
                            {
                                ((TagDataClassPZ70)TextBoxVS.Tag).KeyPress = keyBinding.OSKeyPress;
                                TextBoxVS.Text = ((TagDataClassPZ70)TextBoxVS.Tag).GetTextBoxKeyPressInfo();
                            }
                        }
                    }
                    if (keyBinding.MultiPanelPZ70Knob == MultiPanelPZ70Knobs.KNOB_IAS)
                    {
                        if (keyBinding.WhenTurnedOn)
                        {
                            if (keyBinding.OSKeyPress != null)
                            {
                                ((TagDataClassPZ70)TextBoxIAS.Tag).KeyPress = keyBinding.OSKeyPress;
                                TextBoxIAS.Text = ((TagDataClassPZ70)TextBoxIAS.Tag).GetTextBoxKeyPressInfo();
                            }
                        }
                    }
                    if (keyBinding.MultiPanelPZ70Knob == MultiPanelPZ70Knobs.KNOB_HDG)
                    {
                        if (keyBinding.WhenTurnedOn)
                        {
                            if (keyBinding.OSKeyPress != null)
                            {
                                ((TagDataClassPZ70)TextBoxHDG.Tag).KeyPress = keyBinding.OSKeyPress;
                                TextBoxHDG.Text = ((TagDataClassPZ70)TextBoxHDG.Tag).GetTextBoxKeyPressInfo();
                            }
                        }
                    }
                    if (keyBinding.MultiPanelPZ70Knob == MultiPanelPZ70Knobs.KNOB_CRS)
                    {
                        if (keyBinding.WhenTurnedOn)
                        {
                            if (keyBinding.OSKeyPress != null)
                            {
                                ((TagDataClassPZ70)TextBoxCRS.Tag).KeyPress = keyBinding.OSKeyPress;
                                TextBoxCRS.Text = ((TagDataClassPZ70)TextBoxCRS.Tag).GetTextBoxKeyPressInfo();
                            }
                        }
                    }

                    if (keyBinding.MultiPanelPZ70Knob == MultiPanelPZ70Knobs.LCD_WHEEL_DEC)
                    {
                        if (keyBinding.WhenTurnedOn)
                        {
                            if (keyBinding.OSKeyPress != null)
                            {
                                ((TagDataClassPZ70) TextBoxLcdKnobDecrease.Tag).KeyPress = keyBinding.OSKeyPress;
                                TextBoxLcdKnobDecrease.Text = ((TagDataClassPZ70) TextBoxLcdKnobDecrease.Tag)
                                    .GetTextBoxKeyPressInfo();
                            }
                        }
                    }

                    if (keyBinding.MultiPanelPZ70Knob == MultiPanelPZ70Knobs.LCD_WHEEL_INC)
                    {
                        if (keyBinding.WhenTurnedOn)
                        {
                            if (keyBinding.OSKeyPress != null)
                            {
                                ((TagDataClassPZ70) TextBoxLcdKnobIncrease.Tag).KeyPress = keyBinding.OSKeyPress;
                                TextBoxLcdKnobIncrease.Text = ((TagDataClassPZ70) TextBoxLcdKnobIncrease.Tag)
                                    .GetTextBoxKeyPressInfo();
                            }
                        }
                    }

                    if (keyBinding.MultiPanelPZ70Knob == MultiPanelPZ70Knobs.AUTO_THROTTLE)
                    {
                        if (keyBinding.WhenTurnedOn)
                        {
                            if (keyBinding.OSKeyPress != null)
                            {
                                ((TagDataClassPZ70) TextBoxAutoThrottleOn.Tag).KeyPress = keyBinding.OSKeyPress;
                                TextBoxAutoThrottleOn.Text =
                                    ((TagDataClassPZ70) TextBoxAutoThrottleOn.Tag).GetTextBoxKeyPressInfo();
                            }
                        }
                        else
                        {
                            if (keyBinding.OSKeyPress != null)
                            {
                                ((TagDataClassPZ70) TextBoxAutoThrottleOff.Tag).KeyPress = keyBinding.OSKeyPress;
                                TextBoxAutoThrottleOff.Text = ((TagDataClassPZ70) TextBoxAutoThrottleOff.Tag)
                                    .GetTextBoxKeyPressInfo();
                            }
                        }
                    }

                    if (keyBinding.MultiPanelPZ70Knob == MultiPanelPZ70Knobs.FLAPS_LEVER_UP)
                    {
                        if (keyBinding.WhenTurnedOn)
                        {
                            if (keyBinding.OSKeyPress != null)
                            {
                                ((TagDataClassPZ70) TextBoxFlapsUp.Tag).KeyPress = keyBinding.OSKeyPress;
                                TextBoxFlapsUp.Text = ((TagDataClassPZ70) TextBoxFlapsUp.Tag).GetTextBoxKeyPressInfo();
                            }
                        }
                    }

                    if (keyBinding.MultiPanelPZ70Knob == MultiPanelPZ70Knobs.FLAPS_LEVER_DOWN)
                    {
                        if (keyBinding.WhenTurnedOn)
                        {
                            if (keyBinding.OSKeyPress != null)
                            {
                                ((TagDataClassPZ70) TextBoxFlapsDown.Tag).KeyPress = keyBinding.OSKeyPress;
                                TextBoxFlapsDown.Text =
                                    ((TagDataClassPZ70) TextBoxFlapsDown.Tag).GetTextBoxKeyPressInfo();
                            }
                        }
                    }

                    if (keyBinding.MultiPanelPZ70Knob == MultiPanelPZ70Knobs.PITCH_TRIM_WHEEL_UP)
                    {
                        if (keyBinding.WhenTurnedOn)
                        {
                            if (keyBinding.OSKeyPress != null)
                            {
                                ((TagDataClassPZ70) TextBoxPitchTrimUp.Tag).KeyPress = keyBinding.OSKeyPress;
                                TextBoxPitchTrimUp.Text =
                                    ((TagDataClassPZ70) TextBoxPitchTrimUp.Tag).GetTextBoxKeyPressInfo();
                            }
                        }
                    }

                    if (keyBinding.MultiPanelPZ70Knob == MultiPanelPZ70Knobs.PITCH_TRIM_WHEEL_DOWN)
                    {
                        if (keyBinding.WhenTurnedOn)
                        {
                            if (keyBinding.OSKeyPress != null)
                            {
                                ((TagDataClassPZ70) TextBoxPitchTrimDown.Tag).KeyPress = keyBinding.OSKeyPress;
                                TextBoxPitchTrimDown.Text =
                                    ((TagDataClassPZ70) TextBoxPitchTrimDown.Tag).GetTextBoxKeyPressInfo();
                            }
                        }
                    }

                    if (keyBinding.MultiPanelPZ70Knob == MultiPanelPZ70Knobs.AP_BUTTON)
                    {
                        if (keyBinding.WhenTurnedOn)
                        {
                            if (keyBinding.OSKeyPress != null)
                            {
                                ((TagDataClassPZ70) TextBoxApButtonOn.Tag).KeyPress = keyBinding.OSKeyPress;
                                TextBoxApButtonOn.Text =
                                    ((TagDataClassPZ70) TextBoxApButtonOn.Tag).GetTextBoxKeyPressInfo();
                            }
                        }
                        else
                        {
                            if (keyBinding.OSKeyPress != null)
                            {
                                ((TagDataClassPZ70) TextBoxApButtonOff.Tag).KeyPress = keyBinding.OSKeyPress;
                                TextBoxApButtonOff.Text =
                                    ((TagDataClassPZ70) TextBoxApButtonOff.Tag).GetTextBoxKeyPressInfo();
                            }
                        }
                    }

                    if (keyBinding.MultiPanelPZ70Knob == MultiPanelPZ70Knobs.HDG_BUTTON)
                    {
                        if (keyBinding.WhenTurnedOn)
                        {
                            if (keyBinding.OSKeyPress != null)
                            {
                                ((TagDataClassPZ70) TextBoxHdgButtonOn.Tag).KeyPress = keyBinding.OSKeyPress;
                                TextBoxHdgButtonOn.Text =
                                    ((TagDataClassPZ70) TextBoxHdgButtonOn.Tag).GetTextBoxKeyPressInfo();
                            }
                        }
                        else
                        {
                            if (keyBinding.OSKeyPress != null)
                            {
                                ((TagDataClassPZ70) TextBoxHdgButtonOff.Tag).KeyPress = keyBinding.OSKeyPress;
                                TextBoxHdgButtonOff.Text =
                                    ((TagDataClassPZ70) TextBoxHdgButtonOff.Tag).GetTextBoxKeyPressInfo();
                            }
                        }
                    }

                    if (keyBinding.MultiPanelPZ70Knob == MultiPanelPZ70Knobs.NAV_BUTTON)
                    {
                        if (keyBinding.WhenTurnedOn)
                        {
                            if (keyBinding.OSKeyPress != null)
                            {
                                ((TagDataClassPZ70) TextBoxNavButtonOn.Tag).KeyPress = keyBinding.OSKeyPress;
                                TextBoxNavButtonOn.Text =
                                    ((TagDataClassPZ70) TextBoxNavButtonOn.Tag).GetTextBoxKeyPressInfo();
                            }
                        }
                        else
                        {
                            if (keyBinding.OSKeyPress != null)
                            {
                                ((TagDataClassPZ70) TextBoxNavButtonOff.Tag).KeyPress = keyBinding.OSKeyPress;
                                TextBoxNavButtonOff.Text =
                                    ((TagDataClassPZ70) TextBoxNavButtonOff.Tag).GetTextBoxKeyPressInfo();
                            }
                        }
                    }

                    if (keyBinding.MultiPanelPZ70Knob == MultiPanelPZ70Knobs.IAS_BUTTON)
                    {
                        if (keyBinding.WhenTurnedOn)
                        {
                            if (keyBinding.OSKeyPress != null)
                            {
                                ((TagDataClassPZ70) TextBoxIasButtonOn.Tag).KeyPress = keyBinding.OSKeyPress;
                                TextBoxIasButtonOn.Text =
                                    ((TagDataClassPZ70) TextBoxIasButtonOn.Tag).GetTextBoxKeyPressInfo();
                            }
                        }
                        else
                        {
                            if (keyBinding.OSKeyPress != null)
                            {
                                ((TagDataClassPZ70) TextBoxIasButtonOff.Tag).KeyPress = keyBinding.OSKeyPress;
                                TextBoxIasButtonOff.Text =
                                    ((TagDataClassPZ70) TextBoxIasButtonOff.Tag).GetTextBoxKeyPressInfo();
                            }
                        }
                    }

                    if (keyBinding.MultiPanelPZ70Knob == MultiPanelPZ70Knobs.ALT_BUTTON)
                    {
                        if (keyBinding.WhenTurnedOn)
                        {
                            if (keyBinding.OSKeyPress != null)
                            {
                                ((TagDataClassPZ70) TextBoxAltButtonOn.Tag).KeyPress = keyBinding.OSKeyPress;
                                TextBoxAltButtonOn.Text =
                                    ((TagDataClassPZ70) TextBoxAltButtonOn.Tag).GetTextBoxKeyPressInfo();
                            }
                        }
                        else
                        {
                            if (keyBinding.OSKeyPress != null)
                            {
                                ((TagDataClassPZ70) TextBoxAltButtonOff.Tag).KeyPress = keyBinding.OSKeyPress;
                                TextBoxAltButtonOff.Text =
                                    ((TagDataClassPZ70) TextBoxAltButtonOff.Tag).GetTextBoxKeyPressInfo();
                            }
                        }
                    }

                    if (keyBinding.MultiPanelPZ70Knob == MultiPanelPZ70Knobs.VS_BUTTON)
                    {
                        if (keyBinding.WhenTurnedOn)
                        {
                            if (keyBinding.OSKeyPress != null)
                            {
                                ((TagDataClassPZ70) TextBoxVsButtonOn.Tag).KeyPress = keyBinding.OSKeyPress;
                                TextBoxVsButtonOn.Text =
                                    ((TagDataClassPZ70) TextBoxVsButtonOn.Tag).GetTextBoxKeyPressInfo();
                            }
                        }
                        else
                        {
                            if (keyBinding.OSKeyPress != null)
                            {
                                ((TagDataClassPZ70) TextBoxVsButtonOff.Tag).KeyPress = keyBinding.OSKeyPress;
                                TextBoxVsButtonOff.Text =
                                    ((TagDataClassPZ70) TextBoxVsButtonOff.Tag).GetTextBoxKeyPressInfo();
                            }
                        }
                    }

                    if (keyBinding.MultiPanelPZ70Knob == MultiPanelPZ70Knobs.APR_BUTTON)
                    {
                        if (keyBinding.WhenTurnedOn)
                        {
                            if (keyBinding.OSKeyPress != null)
                            {
                                ((TagDataClassPZ70) TextBoxAprButtonOn.Tag).KeyPress = keyBinding.OSKeyPress;
                                TextBoxAprButtonOn.Text =
                                    ((TagDataClassPZ70) TextBoxAprButtonOn.Tag).GetTextBoxKeyPressInfo();
                            }
                        }
                        else
                        {
                            if (keyBinding.OSKeyPress != null)
                            {
                                ((TagDataClassPZ70) TextBoxAprButtonOff.Tag).KeyPress = keyBinding.OSKeyPress;
                                TextBoxAprButtonOff.Text =
                                    ((TagDataClassPZ70) TextBoxAprButtonOff.Tag).GetTextBoxKeyPressInfo();
                            }
                        }
                    }

                    if (keyBinding.MultiPanelPZ70Knob == MultiPanelPZ70Knobs.REV_BUTTON)
                    {
                        if (keyBinding.WhenTurnedOn)
                        {
                            if (keyBinding.OSKeyPress != null)
                            {
                                ((TagDataClassPZ70) TextBoxRevButtonOn.Tag).KeyPress = keyBinding.OSKeyPress;
                                TextBoxRevButtonOn.Text =
                                    ((TagDataClassPZ70) TextBoxRevButtonOn.Tag).GetTextBoxKeyPressInfo();
                            }
                        }
                        else
                        {
                            if (keyBinding.OSKeyPress != null)
                            {
                                ((TagDataClassPZ70) TextBoxRevButtonOff.Tag).KeyPress = keyBinding.OSKeyPress;
                                TextBoxRevButtonOff.Text =
                                    ((TagDataClassPZ70) TextBoxRevButtonOff.Tag).GetTextBoxKeyPressInfo();
                            }
                        }
                    }
                }
               

                foreach (var bipLink in _multiPanelPZ70ArmA.BIPLinkHashSet)
                {
                    if (bipLink.DialPosition == _multiPanelPZ70ArmA.PZ70_DialPosition && bipLink.MultiPanelPZ70Knob == MultiPanelPZ70Knobs.KNOB_ALT && bipLink.WhenTurnedOn && bipLink.BIPLights.Count > 0)
                    {
                        ((TagDataClassPZ70)TextBoxLcdKnobDecrease.Tag).BIPLink = bipLink;
                        TextBoxALT.Background = Brushes.Bisque;
                    }
                    if (bipLink.DialPosition == _multiPanelPZ70ArmA.PZ70_DialPosition && bipLink.MultiPanelPZ70Knob == MultiPanelPZ70Knobs.KNOB_VS && bipLink.WhenTurnedOn && bipLink.BIPLights.Count > 0)
                    {
                        ((TagDataClassPZ70)TextBoxLcdKnobDecrease.Tag).BIPLink = bipLink;
                        TextBoxVS.Background = Brushes.Bisque;
                    }
                    if (bipLink.DialPosition == _multiPanelPZ70ArmA.PZ70_DialPosition && bipLink.MultiPanelPZ70Knob == MultiPanelPZ70Knobs.KNOB_IAS && bipLink.WhenTurnedOn && bipLink.BIPLights.Count > 0)
                    {
                        ((TagDataClassPZ70)TextBoxLcdKnobDecrease.Tag).BIPLink = bipLink;
                        TextBoxIAS.Background = Brushes.Bisque;
                    }
                    if (bipLink.DialPosition == _multiPanelPZ70ArmA.PZ70_DialPosition && bipLink.MultiPanelPZ70Knob == MultiPanelPZ70Knobs.KNOB_HDG && bipLink.WhenTurnedOn && bipLink.BIPLights.Count > 0)
                    {
                        ((TagDataClassPZ70)TextBoxLcdKnobDecrease.Tag).BIPLink = bipLink;
                        TextBoxHDG.Background = Brushes.Bisque;
                    }
                    if (bipLink.DialPosition == _multiPanelPZ70ArmA.PZ70_DialPosition && bipLink.MultiPanelPZ70Knob == MultiPanelPZ70Knobs.KNOB_CRS && bipLink.WhenTurnedOn && bipLink.BIPLights.Count > 0)
                    {
                        ((TagDataClassPZ70)TextBoxLcdKnobDecrease.Tag).BIPLink = bipLink;
                        TextBoxCRS.Background = Brushes.Bisque;
                    }

                    if (bipLink.DialPosition == _multiPanelPZ70ArmA.PZ70_DialPosition && bipLink.MultiPanelPZ70Knob == MultiPanelPZ70Knobs.LCD_WHEEL_DEC && bipLink.WhenTurnedOn && bipLink.BIPLights.Count > 0)
                    {
                        ((TagDataClassPZ70)TextBoxLcdKnobDecrease.Tag).BIPLink = bipLink;
                        TextBoxLcdKnobDecrease.Background = Brushes.Bisque;
                    }

                    if (bipLink.DialPosition == _multiPanelPZ70ArmA.PZ70_DialPosition && bipLink.MultiPanelPZ70Knob == MultiPanelPZ70Knobs.LCD_WHEEL_INC && bipLink.WhenTurnedOn && bipLink.BIPLights.Count > 0)
                    {
                        ((TagDataClassPZ70)TextBoxLcdKnobIncrease.Tag).BIPLink = bipLink;
                        TextBoxLcdKnobIncrease.Background = Brushes.Bisque;
                    }

                    if (bipLink.DialPosition == _multiPanelPZ70ArmA.PZ70_DialPosition && bipLink.MultiPanelPZ70Knob == MultiPanelPZ70Knobs.FLAPS_LEVER_UP && bipLink.WhenTurnedOn && bipLink.BIPLights.Count > 0)
                    {
                        ((TagDataClassPZ70)TextBoxFlapsUp.Tag).BIPLink = bipLink;
                        TextBoxFlapsUp.Background = Brushes.Bisque;
                    }

                    if (bipLink.DialPosition == _multiPanelPZ70ArmA.PZ70_DialPosition && bipLink.MultiPanelPZ70Knob == MultiPanelPZ70Knobs.FLAPS_LEVER_DOWN && bipLink.WhenTurnedOn && bipLink.BIPLights.Count > 0)
                    {
                        ((TagDataClassPZ70)TextBoxFlapsDown.Tag).BIPLink = bipLink;
                        TextBoxFlapsDown.Background = Brushes.Bisque;
                    }

                    if (bipLink.DialPosition == _multiPanelPZ70ArmA.PZ70_DialPosition && bipLink.MultiPanelPZ70Knob == MultiPanelPZ70Knobs.PITCH_TRIM_WHEEL_UP && bipLink.WhenTurnedOn && bipLink.BIPLights.Count > 0)
                    {
                        ((TagDataClassPZ70)TextBoxPitchTrimUp.Tag).BIPLink = bipLink;
                        TextBoxPitchTrimUp.Background = Brushes.Bisque;
                    }

                    if (bipLink.DialPosition == _multiPanelPZ70ArmA.PZ70_DialPosition && bipLink.MultiPanelPZ70Knob == MultiPanelPZ70Knobs.PITCH_TRIM_WHEEL_DOWN && bipLink.WhenTurnedOn && bipLink.BIPLights.Count > 0)
                    {
                        ((TagDataClassPZ70)TextBoxPitchTrimDown.Tag).BIPLink = bipLink;
                        TextBoxPitchTrimDown.Background = Brushes.Bisque;
                    }

                    if (bipLink.DialPosition == _multiPanelPZ70ArmA.PZ70_DialPosition && bipLink.MultiPanelPZ70Knob == MultiPanelPZ70Knobs.AUTO_THROTTLE)
                    {
                        if (bipLink.WhenTurnedOn)
                        {
                            if (bipLink.BIPLights.Count > 0)
                            {
                                ((TagDataClassPZ70)TextBoxAutoThrottleOn.Tag).BIPLink = bipLink;
                                TextBoxAutoThrottleOn.Background = Brushes.Bisque;
                            }
                        }
                        else
                        {
                            if (bipLink.BIPLights.Count > 0)
                            {
                                ((TagDataClassPZ70)TextBoxAutoThrottleOff.Tag).BIPLink = bipLink;
                                TextBoxAutoThrottleOff.Background = Brushes.Bisque;
                            }
                        }
                    }

                    if (bipLink.DialPosition == _multiPanelPZ70ArmA.PZ70_DialPosition && bipLink.MultiPanelPZ70Knob == MultiPanelPZ70Knobs.AP_BUTTON)
                    {
                        if (bipLink.WhenTurnedOn)
                        {
                            if (bipLink.BIPLights.Count > 0)
                            {
                                ((TagDataClassPZ70)TextBoxApButtonOn.Tag).BIPLink = bipLink;
                                TextBoxApButtonOn.Background = Brushes.Bisque;
                            }
                        }
                        else
                        {
                            if (bipLink.BIPLights.Count > 0)
                            {
                                ((TagDataClassPZ70)TextBoxApButtonOff.Tag).BIPLink = bipLink;
                                TextBoxApButtonOff.Background = Brushes.Bisque;
                            }
                        }
                    }

                    if (bipLink.DialPosition == _multiPanelPZ70ArmA.PZ70_DialPosition && bipLink.MultiPanelPZ70Knob == MultiPanelPZ70Knobs.HDG_BUTTON)
                    {
                        if (bipLink.WhenTurnedOn)
                        {
                            if (bipLink.BIPLights.Count > 0)
                            {
                                ((TagDataClassPZ70)TextBoxHdgButtonOn.Tag).BIPLink = bipLink;
                                TextBoxHdgButtonOn.Background = Brushes.Bisque;
                            }
                        }
                        else
                        {
                            if (bipLink.BIPLights.Count > 0)
                            {
                                ((TagDataClassPZ70)TextBoxHdgButtonOff.Tag).BIPLink = bipLink;
                                TextBoxHdgButtonOff.Background = Brushes.Bisque;
                            }
                        }
                    }

                    if (bipLink.DialPosition == _multiPanelPZ70ArmA.PZ70_DialPosition && bipLink.MultiPanelPZ70Knob == MultiPanelPZ70Knobs.NAV_BUTTON)
                    {
                        if (bipLink.WhenTurnedOn)
                        {
                            if (bipLink.BIPLights.Count > 0)
                            {
                                ((TagDataClassPZ70)TextBoxNavButtonOn.Tag).BIPLink = bipLink;
                                TextBoxNavButtonOn.Background = Brushes.Bisque;
                            }
                        }
                        else
                        {
                            if (bipLink.BIPLights.Count > 0)
                            {
                                ((TagDataClassPZ70)TextBoxNavButtonOff.Tag).BIPLink = bipLink;
                                TextBoxNavButtonOff.Background = Brushes.Bisque;
                            }
                        }
                    }

                    if (bipLink.DialPosition == _multiPanelPZ70ArmA.PZ70_DialPosition && bipLink.MultiPanelPZ70Knob == MultiPanelPZ70Knobs.IAS_BUTTON)
                    {
                        if (bipLink.WhenTurnedOn)
                        {
                            if (bipLink.BIPLights.Count > 0)
                            {
                                ((TagDataClassPZ70)TextBoxIasButtonOn.Tag).BIPLink = bipLink;
                                TextBoxIasButtonOn.Background = Brushes.Bisque;
                            }
                        }
                        else
                        {
                            if (bipLink.BIPLights.Count > 0)
                            {
                                ((TagDataClassPZ70)TextBoxIasButtonOff.Tag).BIPLink = bipLink;
                                TextBoxIasButtonOff.Background = Brushes.Bisque;
                            }
                        }
                    }

                    if (bipLink.DialPosition == _multiPanelPZ70ArmA.PZ70_DialPosition && bipLink.MultiPanelPZ70Knob == MultiPanelPZ70Knobs.ALT_BUTTON)
                    {
                        if (bipLink.WhenTurnedOn)
                        {
                            if (bipLink.BIPLights.Count > 0)
                            {
                                ((TagDataClassPZ70)TextBoxAltButtonOn.Tag).BIPLink = bipLink;
                                TextBoxAltButtonOn.Background = Brushes.Bisque;
                            }
                        }
                        else
                        {
                            if (bipLink.BIPLights.Count > 0)
                            {
                                ((TagDataClassPZ70)TextBoxAltButtonOff.Tag).BIPLink = bipLink;
                                TextBoxAltButtonOff.Background = Brushes.Bisque;
                            }
                        }
                    }

                    if (bipLink.DialPosition == _multiPanelPZ70ArmA.PZ70_DialPosition && bipLink.MultiPanelPZ70Knob == MultiPanelPZ70Knobs.VS_BUTTON)
                    {
                        if (bipLink.WhenTurnedOn)
                        {
                            if (bipLink.BIPLights.Count > 0)
                            {
                                ((TagDataClassPZ70)TextBoxVsButtonOn.Tag).BIPLink = bipLink;
                                TextBoxVsButtonOn.Background = Brushes.Bisque;
                            }
                        }
                        else
                        {
                            if (bipLink.BIPLights.Count > 0)
                            {
                                ((TagDataClassPZ70)TextBoxVsButtonOff.Tag).BIPLink = bipLink;
                                TextBoxVsButtonOff.Background = Brushes.Bisque;
                            }
                        }
                    }

                    if (bipLink.DialPosition == _multiPanelPZ70ArmA.PZ70_DialPosition && bipLink.MultiPanelPZ70Knob == MultiPanelPZ70Knobs.APR_BUTTON)
                    {
                        if (bipLink.WhenTurnedOn)
                        {
                            if (bipLink.BIPLights.Count > 0)
                            {
                                ((TagDataClassPZ70)TextBoxAprButtonOn.Tag).BIPLink = bipLink;
                                TextBoxAprButtonOn.Background = Brushes.Bisque;
                            }
                        }
                        else
                        {
                            if (bipLink.BIPLights.Count > 0)
                            {
                                ((TagDataClassPZ70)TextBoxAprButtonOff.Tag).BIPLink = bipLink;
                                TextBoxAprButtonOff.Background = Brushes.Bisque;
                            }
                        }
                    }

                    if (bipLink.DialPosition == _multiPanelPZ70ArmA.PZ70_DialPosition && bipLink.MultiPanelPZ70Knob == MultiPanelPZ70Knobs.REV_BUTTON)
                    {
                        if (bipLink.WhenTurnedOn)
                        {
                            if (bipLink.BIPLights.Count > 0)
                            {
                                ((TagDataClassPZ70)TextBoxRevButtonOn.Tag).BIPLink = bipLink;
                                TextBoxRevButtonOn.Background = Brushes.Bisque;
                            }
                        }
                        else
                        {
                            if (bipLink.BIPLights.Count > 0)
                            {
                                ((TagDataClassPZ70)TextBoxRevButtonOff.Tag).BIPLink = bipLink;
                                TextBoxRevButtonOff.Background = Brushes.Bisque;
                            }
                        }
                    }
                }

                //Dial position IAS HDG CRS -> Only upper LCD row can be used -> Hide Lower Button
                
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(993013, ex);
            }
        }


        private void TextBoxShortcutKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                var textBox = ((TextBox)sender);
                //Check if this textbox contains sequence. If so then exit
                if (((TagDataClassPZ70)textBox.Tag).ContainsKeySequence())
                {
                    return;
                }

                var keyPressed = (VirtualKeyCode)KeyInterop.VirtualKeyFromKey(e.Key);
                e.Handled = true;

                var hashSetOfKeysPressed = new HashSet<string>();
                hashSetOfKeysPressed.Add(Enum.GetName(typeof(VirtualKeyCode), keyPressed));

                var modifiers = CommonVK.GetPressedVirtualKeyCodesThatAreModifiers();
                foreach (var virtualKeyCode in modifiers)
                {
                    hashSetOfKeysPressed.Add(Enum.GetName(typeof(VirtualKeyCode), virtualKeyCode));
                }

                var result = "";
                foreach (var str in hashSetOfKeysPressed)
                {
                    if (!string.IsNullOrEmpty(result))
                    {
                        result = str + " + " + result;
                    }
                    else
                    {
                        result = str + " " + result;
                    }
                }

                textBox.Text = result;
                ((TagDataClassPZ70)textBox.Tag).KeyPress = new OSKeyPress(result);
                UpdateKeyBindingProfileSequencedKeyStrokesPZ70(textBox);
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(3008, ex);
            }
        }

        private void TextBoxMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var textBox = (TextBox)sender;

                if (e.ChangedButton == MouseButton.Left)
                {
                    if (((TagDataClassPZ70)textBox.Tag).ContainsKeySequence())
                    {
                        //Check if this textbox contains sequence information. If so then prompt the user for deletion
                        if (MessageBox.Show("Do you want to delete the key sequence?", "Delete key sequence?", MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)
                        {
                            return;
                        }

                        ((TagDataClassPZ70)textBox.Tag).KeyPress.KeySequence.Clear();
                        textBox.Text = "";
                        UpdateKeyBindingProfileSimpleKeyStrokes(textBox);
                    }
                    else if (((TagDataClassPZ70)textBox.Tag).ContainsSingleKey())
                    {
                        ((TagDataClassPZ70)textBox.Tag).KeyPress.KeySequence.Clear();
                        textBox.Text = "";
                        UpdateKeyBindingProfileSimpleKeyStrokes(textBox);
                    }

                    if (((TagDataClassPZ70)textBox.Tag).ContainsBIPLink())
                    {
                        //Check if this textbox contains sequence information. If so then prompt the user for deletion
                        if (MessageBox.Show("Do you want to delete BIP Links?", "Delete BIP Link?", MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)
                        {
                            return;
                        }

                        ((TagDataClassPZ70)textBox.Tag).BIPLink.BIPLights.Clear();
                        textBox.Background = Brushes.White;
                        UpdateBIPLinkBindings(textBox);
                    }
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(3001, ex);
            }
        }

        private void TextBoxGotFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                ((TextBox)sender).Background = Brushes.Yellow;
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(993004, ex);
            }
        }


        private void TextBoxPreviewKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                var textBox = ((TextBox)sender);

                if (((TagDataClassPZ70)textBox.Tag).ContainsKeySequence())
                {
                    return;
                }

                var hashSetOfKeysPressed = new HashSet<string>();

                /*if (((TextBoxTagHolderClass)textBox.Tag) == null)
                {
                    ((TextBoxTagHolderClass)textBox.Tag) = xxKeyPressLength.FiftyMilliSec;
                }*/

                var keyCode = KeyInterop.VirtualKeyFromKey(e.Key);
                e.Handled = true;

                if (keyCode > 0)
                {
                    hashSetOfKeysPressed.Add(Enum.GetName(typeof(VirtualKeyCode), keyCode));
                }

                var modifiers = CommonVK.GetPressedVirtualKeyCodesThatAreModifiers();
                foreach (var virtualKeyCode in modifiers)
                {
                    hashSetOfKeysPressed.Add(Enum.GetName(typeof(VirtualKeyCode), virtualKeyCode));
                }

                var result = "";
                foreach (var str in hashSetOfKeysPressed)
                {
                    if (!string.IsNullOrEmpty(result))
                    {
                        result = str + " + " + result;
                    }
                    else
                    {
                        result = str + " " + result;
                    }
                }

                textBox.Text = result;
                ((TagDataClassPZ70)textBox.Tag).KeyPress = new OSKeyPress(result);
                UpdateKeyBindingProfileSimpleKeyStrokes(textBox);
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(3006, ex);
            }
        }

        private void TextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                //MAKE SURE THE Tag iss SET BEFORE SETTING TEXT! OTHERWISE THIS DOESN'T FIRE
                var textBox = (TextBox)sender;
                if (((TagDataClassPZ70)textBox.Tag).ContainsKeySequence())
                {
                    textBox.FontStyle = FontStyles.Oblique;
                }
                else
                {
                    textBox.FontStyle = FontStyles.Normal;
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(993007, ex);
            }
        }

        private void MouseDownFocusLogTextBox(object sender, MouseButtonEventArgs e)
        {
            try
            {
                TextBoxLogPZ70.Focus();
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(992014, ex);
            }
        }

        private void TextBoxContextMenuIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            try
            {
                var contextMenu = (ContextMenu)sender;
                var textBox = GetTextBoxInFocus();
                if (textBox == null)
                {
                    foreach (MenuItem contextMenuItem in contextMenu.Items)
                    {
                        contextMenuItem.Visibility = Visibility.Collapsed;
                    }

                    return;
                    //throw new Exception("Failed to locate which textbox is focused.");
                }

                if (!(bool)e.NewValue)
                {
                    //Do not show if not visible
                    return;
                }



                if (!((TagDataClassPZ70)textBox.Tag).ContainsSingleKey())
                {
                    return;
                }

                var keyPressLength = ((TagDataClassPZ70)textBox.Tag).KeyPress.GetLengthOfKeyPress();

                foreach (MenuItem item in contextMenu.Items)
                {
                    item.IsChecked = false;
                }

                foreach (MenuItem item in contextMenu.Items)
                {
                    if (item.Name == "contextMenuItemFiftyMilliSec" && keyPressLength == KeyPressLength.FiftyMilliSec)
                    {
                        item.IsChecked = true;
                    }
                    else if (item.Name == "contextMenuItemHalfSecond" && keyPressLength == KeyPressLength.HalfSecond)
                    {
                        item.IsChecked = true;
                    }
                    else if (item.Name == "contextMenuItemSecond" && keyPressLength == KeyPressLength.Second)
                    {
                        item.IsChecked = true;
                    }
                    else if (item.Name == "contextMenuItemSecondAndHalf" && keyPressLength == KeyPressLength.SecondAndHalf)
                    {
                        item.IsChecked = true;
                    }
                    else if (item.Name == "contextMenuItemTwoSeconds" && keyPressLength == KeyPressLength.TwoSeconds)
                    {
                        item.IsChecked = true;
                    }
                    else if (item.Name == "contextMenuItemThreeSeconds" && keyPressLength == KeyPressLength.ThreeSeconds)
                    {
                        item.IsChecked = true;
                    }
                    else if (item.Name == "contextMenuItemFourSeconds" && keyPressLength == KeyPressLength.FourSeconds)
                    {
                        item.IsChecked = true;
                    }
                    else if (item.Name == "contextMenuItemFiveSecs" && keyPressLength == KeyPressLength.FiveSecs)
                    {
                        item.IsChecked = true;
                    }
                    else if (item.Name == "contextMenuItemFifteenSecs" && keyPressLength == KeyPressLength.FifteenSecs)
                    {
                        item.IsChecked = true;
                    }
                    else if (item.Name == "contextMenuItemTenSecs" && keyPressLength == KeyPressLength.TenSecs)
                    {
                        item.IsChecked = true;
                    }
                    else if (item.Name == "contextMenuItemTwentySecs" && keyPressLength == KeyPressLength.TwentySecs)
                    {
                        item.IsChecked = true;
                    }
                    else if (item.Name == "contextMenuItemThirtySecs" && keyPressLength == KeyPressLength.ThirtySecs)
                    {
                        item.IsChecked = true;
                    }
                    else if (item.Name == "contextMenuItemFortySecs" && keyPressLength == KeyPressLength.FortySecs)
                    {
                        item.IsChecked = true;
                    }
                    else if (item.Name == "contextMenuItemSixtySecs" && keyPressLength == KeyPressLength.SixtySecs)
                    {
                        item.IsChecked = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(992061, ex);
            }
        }


        private void TextBoxContextMenuClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var textBox = GetTextBoxInFocus();
                if (textBox == null)
                {
                    throw new Exception("Failed to locate which textbox is focused.");
                }

                var contextMenuItem = (MenuItem)sender;
                if (contextMenuItem.Name == "contextMenuItemFiftyMilliSec")
                {
                    ((TagDataClassPZ70)textBox.Tag).KeyPress.SetLengthOfKeyPress(KeyPressLength.FiftyMilliSec);
                }
                else if (contextMenuItem.Name == "contextMenuItemHalfSecond")
                {
                    ((TagDataClassPZ70)textBox.Tag).KeyPress.SetLengthOfKeyPress(KeyPressLength.HalfSecond);
                }
                else if (contextMenuItem.Name == "contextMenuItemSecond")
                {
                    ((TagDataClassPZ70)textBox.Tag).KeyPress.SetLengthOfKeyPress(KeyPressLength.Second);
                }
                else if (contextMenuItem.Name == "contextMenuItemSecondAndHalf")
                {
                    ((TagDataClassPZ70)textBox.Tag).KeyPress.SetLengthOfKeyPress(KeyPressLength.SecondAndHalf);
                }
                else if (contextMenuItem.Name == "contextMenuItemTwoSeconds")
                {
                    ((TagDataClassPZ70)textBox.Tag).KeyPress.SetLengthOfKeyPress(KeyPressLength.TwoSeconds);
                }
                else if (contextMenuItem.Name == "contextMenuItemThreeSeconds")
                {
                    ((TagDataClassPZ70)textBox.Tag).KeyPress.SetLengthOfKeyPress(KeyPressLength.ThreeSeconds);
                }
                else if (contextMenuItem.Name == "contextMenuItemFourSeconds")
                {
                    ((TagDataClassPZ70)textBox.Tag).KeyPress.SetLengthOfKeyPress(KeyPressLength.FourSeconds);
                }
                else if (contextMenuItem.Name == "contextMenuItemFiveSecs")
                {
                    ((TagDataClassPZ70)textBox.Tag).KeyPress.SetLengthOfKeyPress(KeyPressLength.FiveSecs);
                }
                else if (contextMenuItem.Name == "contextMenuItemTenSecs")
                {
                    ((TagDataClassPZ70)textBox.Tag).KeyPress.SetLengthOfKeyPress(KeyPressLength.TenSecs);
                }
                else if (contextMenuItem.Name == "contextMenuItemFifteenSecs")
                {
                    ((TagDataClassPZ70)textBox.Tag).KeyPress.SetLengthOfKeyPress(KeyPressLength.FifteenSecs);
                }
                else if (contextMenuItem.Name == "contextMenuItemTwentySecs")
                {
                    ((TagDataClassPZ70)textBox.Tag).KeyPress.SetLengthOfKeyPress(KeyPressLength.TwentySecs);
                }
                else if (contextMenuItem.Name == "contextMenuItemThirtySecs")
                {
                    ((TagDataClassPZ70)textBox.Tag).KeyPress.SetLengthOfKeyPress(KeyPressLength.ThirtySecs);
                }
                else if (contextMenuItem.Name == "contextMenuItemFortySecs")
                {
                    ((TagDataClassPZ70)textBox.Tag).KeyPress.SetLengthOfKeyPress(KeyPressLength.FortySecs);
                }
                else if (contextMenuItem.Name == "contextMenuItemSixtySecs")
                {
                    ((TagDataClassPZ70)textBox.Tag).KeyPress.SetLengthOfKeyPress(KeyPressLength.SixtySecs);
                }

                UpdateKeyBindingProfileSimpleKeyStrokes(textBox);
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(992082, ex);
            }
        }

        private TextBox GetTextBoxInFocus()
        {
            foreach (var textBox in Common.FindVisualChildren<TextBox>(this))
            {
                if (!textBox.Equals(TextBoxLogPZ70) && textBox.IsFocused && Equals(textBox.Background, Brushes.Yellow))
                {
                    return textBox;
                }
            }

            return null;
        }


        private void MenuContextEditTextBoxClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var textBox = GetTextBoxInFocus();
                if (textBox == null)
                {
                    throw new Exception("Failed to locate which textbox is focused.");
                }

                SequenceWindow sequenceWindow;
                if (((TagDataClassPZ70)textBox.Tag).ContainsKeySequence())
                {
                    sequenceWindow =
                        new SequenceWindow(textBox.Text, ((TagDataClassPZ70)textBox.Tag).GetKeySequence());
                }
                else
                {
                    sequenceWindow = new SequenceWindow();
                }

                sequenceWindow.ShowDialog();
                if (sequenceWindow.DialogResult.HasValue && sequenceWindow.DialogResult.Value)
                {
                    //Clicked OK
                    //If the user added only a single key stroke combo then let's not treat this as a sequence
                    if (!sequenceWindow.IsDirty)
                    {
                        //User made no changes
                        return;
                    }

                    var sequenceList = sequenceWindow.GetSequence;
                    if (sequenceList.Count > 1)
                    {
                        var osKeyPress = new OSKeyPress("Key press sequence", sequenceList);
                        ((TagDataClassPZ70)textBox.Tag).KeyPress = osKeyPress;
                        ((TagDataClassPZ70)textBox.Tag).KeyPress.Information = sequenceWindow.GetInformation;
                        if (!String.IsNullOrEmpty(sequenceWindow.GetInformation))
                        {
                            textBox.Text = sequenceWindow.GetInformation;
                        }

                        //textBox.Text = string.IsNullOrEmpty(sequenceWindow.GetInformation) ? "Key press sequence" : sequenceWindow.GetInformation;
                        /*if (!string.IsNullOrEmpty(sequenceWindow.GetInformation))
                        {
                            var toolTip = new ToolTip { Content = sequenceWindow.GetInformation };
                            textBox.ToolTipa = toolTip;
                        }*/
                        UpdateKeyBindingProfileSequencedKeyStrokesPZ70(textBox);
                    }
                    else
                    {
                        //If only one press was created treat it as a simple keypress
                        ((TagDataClassPZ70)textBox.Tag).ClearAll();
                        var osKeyPress = new OSKeyPress(sequenceList[0].VirtualKeyCodesAsString, sequenceList[0].LengthOfKeyPress);
                        ((TagDataClassPZ70)textBox.Tag).KeyPress = osKeyPress;
                        ((TagDataClassPZ70)textBox.Tag).KeyPress.Information = sequenceWindow.GetInformation;
                        textBox.Text = sequenceList[0].VirtualKeyCodesAsString;
                        /*textBox.Text = sequenceList.Values[0].VirtualKeyCodesAsString;
                        if (!string.IsNullOrEmpty(sequenceWindow.GetInformation))
                        {
                            var toolTip = new ToolTip { Content = sequenceWindow.GetInformation };
                            textBox.ToolTipa = toolTip;
                        }*/
                        UpdateKeyBindingProfileSimpleKeyStrokes(textBox);
                    }
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(2044, ex);
            }
        }
        

        private void MenuContextEditBipTextBoxClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var textBox = GetTextBoxInFocus();
                if (textBox == null)
                {
                    throw new Exception("Failed to locate which textbox is focused.");
                }

                BIPLinkWindow bipLinkWindow;
                if (((TagDataClassPZ70)textBox.Tag).ContainsBIPLink())
                {
                    var bipLink = ((TagDataClassPZ70)textBox.Tag).BIPLink;
                    bipLinkWindow = new BIPLinkWindow(bipLink);
                }
                else
                {
                    var bipLink = new BIPLinkPZ70();
                    bipLinkWindow = new BIPLinkWindow(bipLink);
                }

                bipLinkWindow.ShowDialog();
                if (bipLinkWindow.DialogResult.HasValue && bipLinkWindow.DialogResult == true && bipLinkWindow.IsDirty && bipLinkWindow.BIPLink != null && bipLinkWindow.BIPLink.BIPLights.Count > 0)
                {
                    ((TagDataClassPZ70)textBox.Tag).BIPLink = (BIPLinkPZ70)bipLinkWindow.BIPLink;
                    UpdateBIPLinkBindings(textBox);
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(442044, ex);
            }
        }


        private void UpdateBIPLinkBindings(TextBox textBox)
        {
            try
            {

                if (textBox.Equals(TextBoxALT))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateBIPLinkKnobBinding(MultiPanelPZ70Knobs.KNOB_ALT, ((TagDataClassPZ70)textBox.Tag).BIPLink);
                }

                if (textBox.Equals(TextBoxVS))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateBIPLinkKnobBinding(MultiPanelPZ70Knobs.KNOB_VS, ((TagDataClassPZ70)textBox.Tag).BIPLink);
                }

                if (textBox.Equals(TextBoxIAS))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateBIPLinkKnobBinding(MultiPanelPZ70Knobs.KNOB_IAS, ((TagDataClassPZ70)textBox.Tag).BIPLink);
                }

                if (textBox.Equals(TextBoxHDG))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateBIPLinkKnobBinding(MultiPanelPZ70Knobs.KNOB_HDG, ((TagDataClassPZ70)textBox.Tag).BIPLink);
                }

                if (textBox.Equals(TextBoxCRS))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateBIPLinkKnobBinding(MultiPanelPZ70Knobs.KNOB_CRS, ((TagDataClassPZ70)textBox.Tag).BIPLink);
                }
                if (textBox.Equals(TextBoxLcdKnobDecrease))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateBIPLinkKnobBinding(MultiPanelPZ70Knobs.LCD_WHEEL_DEC, ((TagDataClassPZ70)textBox.Tag).BIPLink);
                }

                if (textBox.Equals(TextBoxLcdKnobIncrease))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateBIPLinkKnobBinding(MultiPanelPZ70Knobs.LCD_WHEEL_INC, ((TagDataClassPZ70)textBox.Tag).BIPLink);
                }

                if (textBox.Equals(TextBoxAutoThrottleOff))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateBIPLinkKnobBinding(MultiPanelPZ70Knobs.AUTO_THROTTLE, ((TagDataClassPZ70)textBox.Tag).BIPLink, false);
                }

                if (textBox.Equals(TextBoxAutoThrottleOn))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateBIPLinkKnobBinding(MultiPanelPZ70Knobs.AUTO_THROTTLE, ((TagDataClassPZ70)textBox.Tag).BIPLink);
                }

                if (textBox.Equals(TextBoxFlapsUp))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateBIPLinkKnobBinding(MultiPanelPZ70Knobs.FLAPS_LEVER_UP, ((TagDataClassPZ70)textBox.Tag).BIPLink);
                }

                if (textBox.Equals(TextBoxFlapsDown))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateBIPLinkKnobBinding(MultiPanelPZ70Knobs.FLAPS_LEVER_DOWN, ((TagDataClassPZ70)textBox.Tag).BIPLink);
                }

                if (textBox.Equals(TextBoxPitchTrimUp))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateBIPLinkKnobBinding(MultiPanelPZ70Knobs.PITCH_TRIM_WHEEL_UP, ((TagDataClassPZ70)textBox.Tag).BIPLink);
                }

                if (textBox.Equals(TextBoxPitchTrimDown))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateBIPLinkKnobBinding(MultiPanelPZ70Knobs.PITCH_TRIM_WHEEL_DOWN, ((TagDataClassPZ70)textBox.Tag).BIPLink);
                }

                if (textBox.Equals(TextBoxApButtonOn))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateBIPLinkKnobBinding(MultiPanelPZ70Knobs.AP_BUTTON, ((TagDataClassPZ70)textBox.Tag).BIPLink);
                }

                if (textBox.Equals(TextBoxApButtonOff))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateBIPLinkKnobBinding(MultiPanelPZ70Knobs.AP_BUTTON, ((TagDataClassPZ70)textBox.Tag).BIPLink, false);
                }

                if (textBox.Equals(TextBoxHdgButtonOn))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateBIPLinkKnobBinding(MultiPanelPZ70Knobs.HDG_BUTTON, ((TagDataClassPZ70)textBox.Tag).BIPLink);
                }

                if (textBox.Equals(TextBoxHdgButtonOff))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateBIPLinkKnobBinding(MultiPanelPZ70Knobs.HDG_BUTTON, ((TagDataClassPZ70)textBox.Tag).BIPLink, false);
                }

                if (textBox.Equals(TextBoxNavButtonOn))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateBIPLinkKnobBinding(MultiPanelPZ70Knobs.NAV_BUTTON, ((TagDataClassPZ70)textBox.Tag).BIPLink);
                }

                if (textBox.Equals(TextBoxNavButtonOff))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateBIPLinkKnobBinding(MultiPanelPZ70Knobs.NAV_BUTTON, ((TagDataClassPZ70)textBox.Tag).BIPLink, false);
                }

                if (textBox.Equals(TextBoxIasButtonOn))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateBIPLinkKnobBinding(MultiPanelPZ70Knobs.IAS_BUTTON, ((TagDataClassPZ70)textBox.Tag).BIPLink);
                }

                if (textBox.Equals(TextBoxIasButtonOff))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateBIPLinkKnobBinding(MultiPanelPZ70Knobs.IAS_BUTTON, ((TagDataClassPZ70)textBox.Tag).BIPLink, false);
                }

                if (textBox.Equals(TextBoxAltButtonOn))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateBIPLinkKnobBinding(MultiPanelPZ70Knobs.ALT_BUTTON, ((TagDataClassPZ70)textBox.Tag).BIPLink);
                }

                if (textBox.Equals(TextBoxAltButtonOff))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateBIPLinkKnobBinding(MultiPanelPZ70Knobs.ALT_BUTTON, ((TagDataClassPZ70)textBox.Tag).BIPLink, false);
                }

                if (textBox.Equals(TextBoxVsButtonOn))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateBIPLinkKnobBinding(MultiPanelPZ70Knobs.VS_BUTTON, ((TagDataClassPZ70)textBox.Tag).BIPLink);
                }

                if (textBox.Equals(TextBoxVsButtonOff))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateBIPLinkKnobBinding(MultiPanelPZ70Knobs.VS_BUTTON, ((TagDataClassPZ70)textBox.Tag).BIPLink, false);
                }

                if (textBox.Equals(TextBoxAprButtonOn))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateBIPLinkKnobBinding(MultiPanelPZ70Knobs.APR_BUTTON, ((TagDataClassPZ70)textBox.Tag).BIPLink);
                }

                if (textBox.Equals(TextBoxAprButtonOff))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateBIPLinkKnobBinding(MultiPanelPZ70Knobs.APR_BUTTON, ((TagDataClassPZ70)textBox.Tag).BIPLink, false);
                }

                if (textBox.Equals(TextBoxRevButtonOn))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateBIPLinkKnobBinding(MultiPanelPZ70Knobs.REV_BUTTON, ((TagDataClassPZ70)textBox.Tag).BIPLink);
                }

                if (textBox.Equals(TextBoxRevButtonOff))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateBIPLinkKnobBinding(MultiPanelPZ70Knobs.REV_BUTTON, ((TagDataClassPZ70)textBox.Tag).BIPLink, false);
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(3011, ex);
            }
        }

        private void UpdateKeyBindingProfileSequencedKeyStrokesPZ70(TextBox textBox)
        {
            try
            {
                if (textBox.Equals(TextBoxALT))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSequencedKeyBinding(textBox.Text, MultiPanelPZ70Knobs.KNOB_ALT, ((TagDataClassPZ70)textBox.Tag).GetKeySequence());
                }
                if (textBox.Equals(TextBoxVS))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSequencedKeyBinding(textBox.Text, MultiPanelPZ70Knobs.KNOB_VS, ((TagDataClassPZ70)textBox.Tag).GetKeySequence());
                }
                if (textBox.Equals(TextBoxIAS))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSequencedKeyBinding(textBox.Text, MultiPanelPZ70Knobs.KNOB_IAS, ((TagDataClassPZ70)textBox.Tag).GetKeySequence());
                }
                if (textBox.Equals(TextBoxHDG))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSequencedKeyBinding(textBox.Text, MultiPanelPZ70Knobs.KNOB_HDG, ((TagDataClassPZ70)textBox.Tag).GetKeySequence());
                }
                if (textBox.Equals(TextBoxCRS))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSequencedKeyBinding(textBox.Text, MultiPanelPZ70Knobs.KNOB_CRS, ((TagDataClassPZ70)textBox.Tag).GetKeySequence());
                }

                if (textBox.Equals(TextBoxLcdKnobDecrease))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSequencedKeyBinding(textBox.Text, MultiPanelPZ70Knobs.LCD_WHEEL_DEC, ((TagDataClassPZ70)textBox.Tag).GetKeySequence());
                }

                if (textBox.Equals(TextBoxLcdKnobIncrease))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSequencedKeyBinding(textBox.Text, MultiPanelPZ70Knobs.LCD_WHEEL_INC, ((TagDataClassPZ70)textBox.Tag).GetKeySequence());
                }

                if (textBox.Equals(TextBoxAutoThrottleOff))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSequencedKeyBinding(textBox.Text, MultiPanelPZ70Knobs.AUTO_THROTTLE, ((TagDataClassPZ70)textBox.Tag).GetKeySequence(), false);
                }

                if (textBox.Equals(TextBoxAutoThrottleOn))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSequencedKeyBinding(textBox.Text, MultiPanelPZ70Knobs.AUTO_THROTTLE, ((TagDataClassPZ70)textBox.Tag).GetKeySequence());
                }

                if (textBox.Equals(TextBoxFlapsUp))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSequencedKeyBinding(textBox.Text, MultiPanelPZ70Knobs.FLAPS_LEVER_UP, ((TagDataClassPZ70)textBox.Tag).GetKeySequence());
                }

                if (textBox.Equals(TextBoxFlapsDown))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSequencedKeyBinding(textBox.Text, MultiPanelPZ70Knobs.FLAPS_LEVER_DOWN, ((TagDataClassPZ70)textBox.Tag).GetKeySequence());
                }

                if (textBox.Equals(TextBoxPitchTrimUp))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSequencedKeyBinding(textBox.Text, MultiPanelPZ70Knobs.PITCH_TRIM_WHEEL_UP, ((TagDataClassPZ70)textBox.Tag).GetKeySequence());
                }

                if (textBox.Equals(TextBoxPitchTrimDown))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSequencedKeyBinding(textBox.Text, MultiPanelPZ70Knobs.PITCH_TRIM_WHEEL_DOWN, ((TagDataClassPZ70)textBox.Tag).GetKeySequence());
                }

                if (textBox.Equals(TextBoxApButtonOn))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSequencedKeyBinding(textBox.Text, MultiPanelPZ70Knobs.AP_BUTTON, ((TagDataClassPZ70)textBox.Tag).GetKeySequence());
                }

                if (textBox.Equals(TextBoxApButtonOff))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSequencedKeyBinding(textBox.Text, MultiPanelPZ70Knobs.AP_BUTTON, ((TagDataClassPZ70)textBox.Tag).GetKeySequence(), false);
                }

                if (textBox.Equals(TextBoxHdgButtonOn))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSequencedKeyBinding(textBox.Text, MultiPanelPZ70Knobs.HDG_BUTTON, ((TagDataClassPZ70)textBox.Tag).GetKeySequence());
                }

                if (textBox.Equals(TextBoxHdgButtonOff))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSequencedKeyBinding(textBox.Text, MultiPanelPZ70Knobs.HDG_BUTTON, ((TagDataClassPZ70)textBox.Tag).GetKeySequence(), false);
                }

                if (textBox.Equals(TextBoxNavButtonOn))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSequencedKeyBinding(textBox.Text, MultiPanelPZ70Knobs.NAV_BUTTON, ((TagDataClassPZ70)textBox.Tag).GetKeySequence());
                }

                if (textBox.Equals(TextBoxNavButtonOff))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSequencedKeyBinding(textBox.Text, MultiPanelPZ70Knobs.NAV_BUTTON, ((TagDataClassPZ70)textBox.Tag).GetKeySequence(), false);
                }

                if (textBox.Equals(TextBoxIasButtonOn))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSequencedKeyBinding(textBox.Text, MultiPanelPZ70Knobs.IAS_BUTTON, ((TagDataClassPZ70)textBox.Tag).GetKeySequence());
                }

                if (textBox.Equals(TextBoxIasButtonOff))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSequencedKeyBinding(textBox.Text, MultiPanelPZ70Knobs.IAS_BUTTON, ((TagDataClassPZ70)textBox.Tag).GetKeySequence(), false);
                }

                if (textBox.Equals(TextBoxAltButtonOn))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSequencedKeyBinding(textBox.Text, MultiPanelPZ70Knobs.ALT_BUTTON, ((TagDataClassPZ70)textBox.Tag).GetKeySequence());
                }

                if (textBox.Equals(TextBoxAltButtonOff))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSequencedKeyBinding(textBox.Text, MultiPanelPZ70Knobs.ALT_BUTTON, ((TagDataClassPZ70)textBox.Tag).GetKeySequence(), false);
                }

                if (textBox.Equals(TextBoxVsButtonOn))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSequencedKeyBinding(textBox.Text, MultiPanelPZ70Knobs.VS_BUTTON, ((TagDataClassPZ70)textBox.Tag).GetKeySequence());
                }

                if (textBox.Equals(TextBoxVsButtonOff))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSequencedKeyBinding(textBox.Text, MultiPanelPZ70Knobs.VS_BUTTON, ((TagDataClassPZ70)textBox.Tag).GetKeySequence(), false);
                }

                if (textBox.Equals(TextBoxAprButtonOn))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSequencedKeyBinding(textBox.Text, MultiPanelPZ70Knobs.APR_BUTTON, ((TagDataClassPZ70)textBox.Tag).GetKeySequence());
                }

                if (textBox.Equals(TextBoxAprButtonOff))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSequencedKeyBinding(textBox.Text, MultiPanelPZ70Knobs.APR_BUTTON, ((TagDataClassPZ70)textBox.Tag).GetKeySequence(), false);
                }

                if (textBox.Equals(TextBoxRevButtonOn))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSequencedKeyBinding(textBox.Text, MultiPanelPZ70Knobs.REV_BUTTON, ((TagDataClassPZ70)textBox.Tag).GetKeySequence());
                }

                if (textBox.Equals(TextBoxRevButtonOff))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSequencedKeyBinding(textBox.Text, MultiPanelPZ70Knobs.REV_BUTTON, ((TagDataClassPZ70)textBox.Tag).GetKeySequence(), false);
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(3011, ex);
            }
        }


        private void UpdateKeyBindingProfileSimpleKeyStrokes(TextBox textBox)
        {
            try
            {
                KeyPressLength keyPressLength;
                if (!((TagDataClassPZ70)textBox.Tag).ContainsOSKeyPress() ||
                    ((TagDataClassPZ70)textBox.Tag).KeyPress.KeySequence.Count == 0)
                {
                    keyPressLength = KeyPressLength.FiftyMilliSec;
                }
                else
                {
                    keyPressLength = ((TagDataClassPZ70)textBox.Tag).KeyPress.GetLengthOfKeyPress();
                }

                if (textBox.Equals(TextBoxALT))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSingleKeyBinding(MultiPanelPZ70Knobs.KNOB_ALT, TextBoxALT.Text, keyPressLength);
                }
                if (textBox.Equals(TextBoxVS))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSingleKeyBinding(MultiPanelPZ70Knobs.KNOB_VS, TextBoxVS.Text, keyPressLength);
                }
                if (textBox.Equals(TextBoxIAS))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSingleKeyBinding(MultiPanelPZ70Knobs.KNOB_IAS, TextBoxIAS.Text, keyPressLength);
                }
                if (textBox.Equals(TextBoxHDG))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSingleKeyBinding(MultiPanelPZ70Knobs.KNOB_HDG, TextBoxHDG.Text, keyPressLength);
                }
                if (textBox.Equals(TextBoxCRS))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSingleKeyBinding(MultiPanelPZ70Knobs.KNOB_CRS, TextBoxCRS.Text, keyPressLength);
                }

                if (textBox.Equals(TextBoxLcdKnobDecrease))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSingleKeyBinding(MultiPanelPZ70Knobs.LCD_WHEEL_DEC, TextBoxLcdKnobDecrease.Text, keyPressLength);
                }

                if (textBox.Equals(TextBoxLcdKnobIncrease))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSingleKeyBinding(MultiPanelPZ70Knobs.LCD_WHEEL_INC, TextBoxLcdKnobIncrease.Text, keyPressLength);
                }

                if (textBox.Equals(TextBoxAutoThrottleOff))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSingleKeyBinding(MultiPanelPZ70Knobs.AUTO_THROTTLE, TextBoxAutoThrottleOff.Text, keyPressLength, false);
                }

                if (textBox.Equals(TextBoxAutoThrottleOn))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSingleKeyBinding(MultiPanelPZ70Knobs.AUTO_THROTTLE, TextBoxAutoThrottleOn.Text, keyPressLength);
                }

                if (textBox.Equals(TextBoxFlapsUp))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSingleKeyBinding(MultiPanelPZ70Knobs.FLAPS_LEVER_UP, TextBoxFlapsUp.Text, keyPressLength);
                }

                if (textBox.Equals(TextBoxFlapsDown))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSingleKeyBinding(MultiPanelPZ70Knobs.FLAPS_LEVER_DOWN, TextBoxFlapsDown.Text, keyPressLength);
                }

                if (textBox.Equals(TextBoxPitchTrimUp))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSingleKeyBinding(MultiPanelPZ70Knobs.PITCH_TRIM_WHEEL_UP, TextBoxPitchTrimUp.Text, keyPressLength);
                }

                if (textBox.Equals(TextBoxPitchTrimDown))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSingleKeyBinding(MultiPanelPZ70Knobs.PITCH_TRIM_WHEEL_DOWN, TextBoxPitchTrimDown.Text, keyPressLength);
                }

                if (textBox.Equals(TextBoxApButtonOn))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSingleKeyBinding(MultiPanelPZ70Knobs.AP_BUTTON, TextBoxApButtonOn.Text, keyPressLength);
                }

                if (textBox.Equals(TextBoxApButtonOff))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSingleKeyBinding(MultiPanelPZ70Knobs.AP_BUTTON, TextBoxApButtonOff.Text, keyPressLength, false);
                }

                if (textBox.Equals(TextBoxHdgButtonOn))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSingleKeyBinding(MultiPanelPZ70Knobs.HDG_BUTTON, TextBoxHdgButtonOn.Text, keyPressLength);
                }

                if (textBox.Equals(TextBoxHdgButtonOff))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSingleKeyBinding(MultiPanelPZ70Knobs.HDG_BUTTON, TextBoxHdgButtonOff.Text, keyPressLength, false);
                }

                if (textBox.Equals(TextBoxNavButtonOn))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSingleKeyBinding(MultiPanelPZ70Knobs.NAV_BUTTON, TextBoxNavButtonOn.Text, keyPressLength);
                }

                if (textBox.Equals(TextBoxNavButtonOff))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSingleKeyBinding(MultiPanelPZ70Knobs.NAV_BUTTON, TextBoxNavButtonOff.Text, keyPressLength, false);
                }

                if (textBox.Equals(TextBoxIasButtonOn))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSingleKeyBinding(MultiPanelPZ70Knobs.IAS_BUTTON, TextBoxIasButtonOn.Text, keyPressLength);
                }

                if (textBox.Equals(TextBoxIasButtonOff))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSingleKeyBinding(MultiPanelPZ70Knobs.IAS_BUTTON, TextBoxIasButtonOff.Text, keyPressLength, false);
                }

                if (textBox.Equals(TextBoxAltButtonOn))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSingleKeyBinding(MultiPanelPZ70Knobs.ALT_BUTTON, TextBoxAltButtonOn.Text, keyPressLength);
                }

                if (textBox.Equals(TextBoxAltButtonOff))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSingleKeyBinding(MultiPanelPZ70Knobs.ALT_BUTTON, TextBoxAltButtonOff.Text, keyPressLength, false);
                }

                if (textBox.Equals(TextBoxVsButtonOn))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSingleKeyBinding(MultiPanelPZ70Knobs.VS_BUTTON, TextBoxVsButtonOn.Text, keyPressLength);
                }

                if (textBox.Equals(TextBoxVsButtonOff))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSingleKeyBinding(MultiPanelPZ70Knobs.VS_BUTTON, TextBoxVsButtonOff.Text, keyPressLength, false);
                }

                if (textBox.Equals(TextBoxAprButtonOn))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSingleKeyBinding(MultiPanelPZ70Knobs.APR_BUTTON, TextBoxAprButtonOn.Text, keyPressLength);
                }

                if (textBox.Equals(TextBoxAprButtonOff))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSingleKeyBinding(MultiPanelPZ70Knobs.APR_BUTTON, TextBoxAprButtonOff.Text, keyPressLength, false);
                }

                if (textBox.Equals(TextBoxRevButtonOn))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSingleKeyBinding(MultiPanelPZ70Knobs.REV_BUTTON, TextBoxRevButtonOn.Text, keyPressLength);
                }

                if (textBox.Equals(TextBoxRevButtonOff))
                {
                    _multiPanelPZ70ArmA.AddOrUpdateSingleKeyBinding(MultiPanelPZ70Knobs.REV_BUTTON, TextBoxRevButtonOff.Text, keyPressLength, false);
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(3012, ex);
            }
        }

        private MultiPanelPZ70KnobOnOff GetPZ70Knob(TextBox textBox)
        {
            try
            {
                if (textBox.Equals(TextBoxALT))
                {
                    return new MultiPanelPZ70KnobOnOff(_multiPanelPZ70ArmA.PZ70_DialPosition, MultiPanelPZ70Knobs.KNOB_ALT, true);
                }
                if (textBox.Equals(TextBoxVS))
                {
                    return new MultiPanelPZ70KnobOnOff(_multiPanelPZ70ArmA.PZ70_DialPosition, MultiPanelPZ70Knobs.KNOB_VS, true);
                }
                if (textBox.Equals(TextBoxIAS))
                {
                    return new MultiPanelPZ70KnobOnOff(_multiPanelPZ70ArmA.PZ70_DialPosition, MultiPanelPZ70Knobs.KNOB_IAS, true);
                }
                if (textBox.Equals(TextBoxHDG))
                {
                    return new MultiPanelPZ70KnobOnOff(_multiPanelPZ70ArmA.PZ70_DialPosition, MultiPanelPZ70Knobs.KNOB_HDG, true);
                }
                if (textBox.Equals(TextBoxCRS))
                {
                    return new MultiPanelPZ70KnobOnOff(_multiPanelPZ70ArmA.PZ70_DialPosition, MultiPanelPZ70Knobs.KNOB_CRS, true);
                }

                if (textBox.Equals(TextBoxLcdKnobDecrease))
                {
                    return new MultiPanelPZ70KnobOnOff(_multiPanelPZ70ArmA.PZ70_DialPosition, MultiPanelPZ70Knobs.LCD_WHEEL_DEC, true);
                }

                if (textBox.Equals(TextBoxLcdKnobIncrease))
                {
                    return new MultiPanelPZ70KnobOnOff(_multiPanelPZ70ArmA.PZ70_DialPosition, MultiPanelPZ70Knobs.LCD_WHEEL_INC, true);
                }

                if (textBox.Equals(TextBoxAutoThrottleOff))
                {
                    return new MultiPanelPZ70KnobOnOff(_multiPanelPZ70ArmA.PZ70_DialPosition, MultiPanelPZ70Knobs.AUTO_THROTTLE, false);
                }

                if (textBox.Equals(TextBoxAutoThrottleOn))
                {
                    return new MultiPanelPZ70KnobOnOff(_multiPanelPZ70ArmA.PZ70_DialPosition, MultiPanelPZ70Knobs.AUTO_THROTTLE, true);
                }

                if (textBox.Equals(TextBoxFlapsUp))
                {
                    return new MultiPanelPZ70KnobOnOff(_multiPanelPZ70ArmA.PZ70_DialPosition, MultiPanelPZ70Knobs.FLAPS_LEVER_UP, true);
                }

                if (textBox.Equals(TextBoxFlapsDown))
                {
                    return new MultiPanelPZ70KnobOnOff(_multiPanelPZ70ArmA.PZ70_DialPosition, MultiPanelPZ70Knobs.FLAPS_LEVER_DOWN, false);
                }

                if (textBox.Equals(TextBoxPitchTrimUp))
                {
                    return new MultiPanelPZ70KnobOnOff(_multiPanelPZ70ArmA.PZ70_DialPosition, MultiPanelPZ70Knobs.PITCH_TRIM_WHEEL_UP, true);
                }

                if (textBox.Equals(TextBoxPitchTrimDown))
                {
                    return new MultiPanelPZ70KnobOnOff(_multiPanelPZ70ArmA.PZ70_DialPosition, MultiPanelPZ70Knobs.PITCH_TRIM_WHEEL_DOWN, false);
                }

                if (textBox.Equals(TextBoxApButtonOn))
                {
                    return new MultiPanelPZ70KnobOnOff(_multiPanelPZ70ArmA.PZ70_DialPosition, MultiPanelPZ70Knobs.AP_BUTTON, true);
                }

                if (textBox.Equals(TextBoxApButtonOff))
                {
                    return new MultiPanelPZ70KnobOnOff(_multiPanelPZ70ArmA.PZ70_DialPosition, MultiPanelPZ70Knobs.AP_BUTTON, false);
                }

                if (textBox.Equals(TextBoxHdgButtonOn))
                {
                    return new MultiPanelPZ70KnobOnOff(_multiPanelPZ70ArmA.PZ70_DialPosition, MultiPanelPZ70Knobs.HDG_BUTTON, true);
                }

                if (textBox.Equals(TextBoxHdgButtonOff))
                {
                    return new MultiPanelPZ70KnobOnOff(_multiPanelPZ70ArmA.PZ70_DialPosition, MultiPanelPZ70Knobs.HDG_BUTTON, false);
                }

                if (textBox.Equals(TextBoxNavButtonOn))
                {
                    return new MultiPanelPZ70KnobOnOff(_multiPanelPZ70ArmA.PZ70_DialPosition, MultiPanelPZ70Knobs.NAV_BUTTON, true);
                }

                if (textBox.Equals(TextBoxNavButtonOff))
                {
                    return new MultiPanelPZ70KnobOnOff(_multiPanelPZ70ArmA.PZ70_DialPosition, MultiPanelPZ70Knobs.NAV_BUTTON, false);
                }

                if (textBox.Equals(TextBoxIasButtonOn))
                {
                    return new MultiPanelPZ70KnobOnOff(_multiPanelPZ70ArmA.PZ70_DialPosition, MultiPanelPZ70Knobs.IAS_BUTTON, true);
                }

                if (textBox.Equals(TextBoxIasButtonOff))
                {
                    return new MultiPanelPZ70KnobOnOff(_multiPanelPZ70ArmA.PZ70_DialPosition, MultiPanelPZ70Knobs.IAS_BUTTON, false);
                }

                if (textBox.Equals(TextBoxAltButtonOn))
                {
                    return new MultiPanelPZ70KnobOnOff(_multiPanelPZ70ArmA.PZ70_DialPosition, MultiPanelPZ70Knobs.ALT_BUTTON, true);
                }

                if (textBox.Equals(TextBoxAltButtonOff))
                {
                    return new MultiPanelPZ70KnobOnOff(_multiPanelPZ70ArmA.PZ70_DialPosition, MultiPanelPZ70Knobs.ALT_BUTTON, false);
                }

                if (textBox.Equals(TextBoxVsButtonOn))
                {
                    return new MultiPanelPZ70KnobOnOff(_multiPanelPZ70ArmA.PZ70_DialPosition, MultiPanelPZ70Knobs.VS_BUTTON, true);
                }

                if (textBox.Equals(TextBoxVsButtonOff))
                {
                    return new MultiPanelPZ70KnobOnOff(_multiPanelPZ70ArmA.PZ70_DialPosition, MultiPanelPZ70Knobs.VS_BUTTON, false);
                }

                if (textBox.Equals(TextBoxAprButtonOn))
                {
                    return new MultiPanelPZ70KnobOnOff(_multiPanelPZ70ArmA.PZ70_DialPosition, MultiPanelPZ70Knobs.APR_BUTTON, true);
                }

                if (textBox.Equals(TextBoxAprButtonOff))
                {
                    return new MultiPanelPZ70KnobOnOff(_multiPanelPZ70ArmA.PZ70_DialPosition, MultiPanelPZ70Knobs.APR_BUTTON, false);
                }

                if (textBox.Equals(TextBoxRevButtonOn))
                {
                    return new MultiPanelPZ70KnobOnOff(_multiPanelPZ70ArmA.PZ70_DialPosition, MultiPanelPZ70Knobs.REV_BUTTON, true);
                }

                if (textBox.Equals(TextBoxRevButtonOff))
                {
                    return new MultiPanelPZ70KnobOnOff(_multiPanelPZ70ArmA.PZ70_DialPosition, MultiPanelPZ70Knobs.REV_BUTTON, false);
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(3012, ex);
            }

            throw new Exception("Should not reach this point");
        }

        private void RemoveContextMenuClickHandlers()
        {
            foreach (var textBox in Common.FindVisualChildren<TextBox>(this))
            {
                if (!Equals(textBox, TextBoxLogPZ70))
                {
                    textBox.ContextMenu = null;
                    textBox.ContextMenuOpening -= TextBoxContextMenuOpening;
                }
            }
        }

        private void SetContextMenuClickHandlers()
        {
            foreach (var textBox in Common.FindVisualChildren<TextBox>(this))
            {
                if (!Equals(textBox, TextBoxLogPZ70))
                {
                    var contextMenu = (ContextMenu)Resources["TextBoxContextMenuPZ70"];

                    textBox.ContextMenu = contextMenu;
                    textBox.ContextMenuOpening += TextBoxContextMenuOpening;
                }
            }

           
        }

        private void TextBoxContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            try
            {
                var textBox = (TextBox)sender;
                var contextMenu = textBox.ContextMenu;
                if (!(textBox.IsFocused && Equals(textBox.Background, Brushes.Yellow)))
                {
                    //UGLY Must use this to get around problems having different color for BIPLink and Right Clicks
                    foreach (MenuItem item in contextMenu.Items)
                    {
                        item.Visibility = Visibility.Collapsed;
                    }

                    return;
                }

                foreach (MenuItem item in contextMenu.Items)
                {
                    item.Visibility = Visibility.Collapsed;
                }

                if (((TagDataClassPZ70)textBox.Tag).ContainsKeySequence())
                {
                    // 2) 
                    foreach (MenuItem item in contextMenu.Items)
                    {
                        if (item.Name.Contains("EditSequence"))
                        {
                            item.Visibility = Visibility.Visible;
                        }

                        if (BipFactory.HasBips() && item.Name.Contains("EditBIP"))
                        {
                            item.Visibility = Visibility.Visible;
                        }
                    }
                }
                else if (((TagDataClassPZ70)textBox.Tag).IsEmpty())
                {
                    // 4) 
                    foreach (MenuItem item in contextMenu.Items)
                    {
                        if (item.Name.Contains("EditSequence"))
                        {
                            item.Visibility = Visibility.Visible;
                        }
                        else if (BipFactory.HasBips() && item.Name.Contains("EditBIP"))
                        {
                            item.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            item.Visibility = Visibility.Collapsed;
                        }
                    }
                }
                else if (((TagDataClassPZ70)textBox.Tag).ContainsSingleKey())
                {
                    // 5) 
                    foreach (MenuItem item in contextMenu.Items)
                    {
                        if (!(item.Name.Contains("EditSequence") ))
                        {
                            if (item.Name.Contains("EditBIP"))
                            {
                                if (BipFactory.HasBips())
                                {
                                    item.Visibility = Visibility.Visible;
                                }
                            }
                            else
                            {
                                item.Visibility = Visibility.Visible;
                            }
                        }
                    }
                }
                else if (((TagDataClassPZ70)textBox.Tag).ContainsBIPLink())
                {
                    // 3) 
                    foreach (MenuItem item in contextMenu.Items)
                    {

                        if (BipFactory.HasBips() && item.Name.Contains("EditBIP"))
                        {
                            item.Visibility = Visibility.Visible;
                        }

                        if (item.Name.Contains("EditSequence"))
                        {
                            item.Visibility = Visibility.Visible;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(2081, ex);
            }
        }

        private void NotifyKnobChanges(HashSet<object> knobs)
        {
            try
            {
                //Set focus to this so that virtual keypresses won't affect settings
                Dispatcher.BeginInvoke((Action)(() => TextBoxLogPZ70.Focus()));
                foreach (var knob in knobs)
                {
                    var multiPanelKnob = (MultiPanelKnob)knob;

                    if (_multiPanelPZ70ArmA.ForwardKeyPresses)
                    {
                        if (!string.IsNullOrEmpty(_multiPanelPZ70ArmA.GetKeyPressForLoggingPurposes(multiPanelKnob)))
                        {
                            Dispatcher.BeginInvoke(
                                (Action)
                                (() =>
                                    TextBoxLogPZ70.Text = TextBoxLogPZ70.Text.Insert(0, _multiPanelPZ70ArmA.GetKeyPressForLoggingPurposes(multiPanelKnob) + "\n")));
                        }
                    }
                    else
                    {
                        Dispatcher.BeginInvoke(
                            (Action)
                            (() =>
                                TextBoxLogPZ70.Text = TextBoxLogPZ70.Text.Insert(0, "No action taken, virtual key press disabled.\n")));
                    }
                }

                SetGraphicsState(knobs);
            }
            catch (Exception ex)
            {
                Dispatcher.BeginInvoke(
                    (Action)
                    (() =>
                        TextBoxLogPZ70.Text = TextBoxLogPZ70.Text.Insert(0, "0x16" + ex.Message + ".\n")));
                Common.ShowErrorMessageBox(3009, ex);
            }
        }
        
        
        private void ComboBoxLcdKnobSensitivity_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (_userControlLoaded)
                {
                    Settings.Default.PZ70LcdKnobSensitivity =
                        int.Parse(ComboBoxLcdKnobSensitivity.SelectedValue.ToString());
                    _multiPanelPZ70ArmA.LCDKnobSensitivity =
                        int.Parse(ComboBoxLcdKnobSensitivity.SelectedValue.ToString());
                    Settings.Default.Save();
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(4370, ex);
            }
        }




        private void TextBox_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                ((TextBox)sender).Background = Brushes.Yellow;
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(3004, ex);
            }
        }

    }
}
