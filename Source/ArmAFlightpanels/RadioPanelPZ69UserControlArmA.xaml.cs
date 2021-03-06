﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ArmAFlightpanels.Properties;
using ClassLibraryCommon;

using NonVisuals;

namespace ArmAFlightpanels
{
    /// <summary>
    /// Interaction logic for RadioPanelPZ69UserControArmA.xaml
    /// </summary>
    public partial class RadioPanelPZ69UserControArmA : ISaitekPanelListener, IProfileHandlerListener, ISaitekUserControl
    {
        private readonly RadioPanelPZ69ArmA _radioPanelPZ69;
        private TabItem _parentTabItem;
        private string _parentTabItemHeader;
        private IGlobalHandler _globalHandler;
        private bool _userControlLoaded;
        private bool _textBoxTagsSet;
        private readonly List<Key> _allowedKeys = new List<Key>() { Key.D0, Key.D1, Key.D2, Key.D3, Key.D4, Key.D5, Key.D6, Key.D7, Key.D8, Key.D9, Key.OemPeriod, Key.Delete, Key.Back, Key.Left, Key.Right, Key.NumPad0, Key.NumPad1, Key.NumPad2, Key.NumPad3, Key.NumPad4, Key.NumPad5, Key.NumPad6, Key.NumPad7, Key.NumPad8, Key.NumPad9 };

        public RadioPanelPZ69UserControArmA(HIDSkeleton hidSkeleton, TabItem parentTabItem, IGlobalHandler globalHandler)
        {
            InitializeComponent();
            _parentTabItem = parentTabItem;
            _parentTabItemHeader = _parentTabItem.Header.ToString();
            HideAllImages();

            _radioPanelPZ69 = new RadioPanelPZ69ArmA(hidSkeleton);
            _radioPanelPZ69.FrequencyKnobSensitivity = Settings.Default.RadioFrequencyKnobSensitivityEmulator;
            _radioPanelPZ69.Attach((ISaitekPanelListener)this);
            _radioPanelPZ69.Startup();
            globalHandler.Attach(_radioPanelPZ69);
            _globalHandler = globalHandler;

            //LoadConfiguration();
        }

        private void RadioPanelPZ69UserControArmA_OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                ComboBoxFreqKnobSensitivity.SelectedValue = Settings.Default.RadioFrequencyKnobSensitivityEmulator;
                SetTextBoxTagObjects();
                SetContextMenuClickHandlers();
                _userControlLoaded = true;
                ShowGraphicConfiguration();
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(204331, ex);
            }
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
            return _radioPanelPZ69;
        }

        public string GetName()
        {
            return GetType().Name;
        }
        

        public void SelectedAirframe(object sender, AirframEventArgs e)
        {
            try
            {
                //nada
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(471173, ex);
            }
        }

        public void SwitchesChanged(object sender, SwitchesChangedEventArgs e)
        {
            try
            {
                if (e.SaitekPanelEnum == SaitekPanelsEnum.PZ69RadioPanel && e.UniqueId.Equals(_radioPanelPZ69.InstanceId))
                {
                    NotifySwitchChanges(e.Switches);
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(1064, ex);
            }
        }

        public void PanelSettingsReadFromFile(object sender, SettingsReadFromFileEventArgs e)
        {
            try
            {
                //todo
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(1081, ex);
            }
        }

        public void SettingsCleared(object sender, PanelEventArgs e)
        {
            try
            {
                //todo
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(2001, ex);
            }
        }

        public void LedLightChanged(object sender, LedLightChangeEventArgs e)
        {
            try
            {
                //todo
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(2012, ex);
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
                Common.ShowErrorMessageBox(2014, ex);
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
                Common.ShowErrorMessageBox(2017, ex);
            }
        }

        public void SettingsApplied(object sender, PanelEventArgs e)
        {
            try
            {
                if (e.UniqueId.Equals(_radioPanelPZ69.InstanceId) && e.SaitekPanelEnum == SaitekPanelsEnum.PZ69RadioPanel)
                {
                    Dispatcher.BeginInvoke((Action)(ShowGraphicConfiguration));
                    Dispatcher.BeginInvoke((Action)(() => TextBoxLogPZ69.Text = ""));
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(2032, ex);
            }
        }

        public void PanelSettingsChanged(object sender, PanelEventArgs e)
        {
            try
            {
                Dispatcher.BeginInvoke((Action)(ShowGraphicConfiguration));
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(2010, ex);
            }
        }

        private void MouseDownFocusLogTextBox(object sender, MouseButtonEventArgs e)
        {
            try
            {
                TextBoxLogPZ69.Focus();
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(2014, ex);
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
                textBox.Tag = new TagDataClassPZ69();
            }
            _textBoxTagsSet = true;
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
                if (((TagDataClassPZ69)textBox.Tag).ContainsKeySequence())
                {
                    sequenceWindow = new SequenceWindow(textBox.Text, ((TagDataClassPZ69)textBox.Tag).GetKeySequence());
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
                    textBox.ToolTip = null;
                    if (sequenceList.Count > 1)
                    {
                        var osKeyPress = new OSKeyPress("Key press sequence", sequenceList);
                        ((TagDataClassPZ69)textBox.Tag).KeyPress = osKeyPress;
                        ((TagDataClassPZ69)textBox.Tag).KeyPress.Information = sequenceWindow.GetInformation;
                        if (!String.IsNullOrEmpty(sequenceWindow.GetInformation))
                        {
                            textBox.Text = sequenceWindow.GetInformation;
                        }
                        UpdateKeyBindingProfileSequencedKeyStrokesPZ69(textBox);
                    }
                    else
                    {
                        //If only one press was created treat it as a simple keypress
                        ((TagDataClassPZ69)textBox.Tag).ClearAll();
                        var osKeyPress = new OSKeyPress(sequenceList[0].VirtualKeyCodesAsString, sequenceList[0].LengthOfKeyPress);
                        ((TagDataClassPZ69)textBox.Tag).KeyPress = osKeyPress;
                        ((TagDataClassPZ69)textBox.Tag).KeyPress.Information = sequenceWindow.GetInformation;
                        textBox.Text = sequenceList[0].VirtualKeyCodesAsString;
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
                if (((TagDataClassPZ69)textBox.Tag).ContainsBIPLink())
                {
                    var bipLink = ((TagDataClassPZ69)textBox.Tag).BIPLink;
                    bipLinkWindow = new BIPLinkWindow(bipLink);
                }
                else
                {
                    var bipLink = new BIPLinkPZ69();
                    bipLinkWindow = new BIPLinkWindow(bipLink);
                }
                bipLinkWindow.ShowDialog();
                if (bipLinkWindow.DialogResult.HasValue && bipLinkWindow.DialogResult == true && bipLinkWindow.IsDirty && bipLinkWindow.BIPLink != null && bipLinkWindow.BIPLink.BIPLights.Count > 0)
                {
                    ((TagDataClassPZ69)textBox.Tag).BIPLink = (BIPLinkPZ69)bipLinkWindow.BIPLink;
                    UpdateBipLinkBindings(textBox);
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(442044, ex);
            }
        }


        private void UpdateKeyBindingProfileSequencedKeyStrokesPZ69(TextBox textBox)
        {
            try
            {
                if (textBox.Equals(TextBoxUpperCom1))
                {
                    _radioPanelPZ69.AddOrUpdateSequencedKeyBinding(textBox.Text, RadioPanelPZ69KnobsEmulator.UpperCOM1, ((TagDataClassPZ69)textBox.Tag).GetKeySequence());
                }
                if (textBox.Equals(TextBoxUpperCom2))
                {
                    _radioPanelPZ69.AddOrUpdateSequencedKeyBinding(textBox.Text, RadioPanelPZ69KnobsEmulator.UpperCOM2, ((TagDataClassPZ69)textBox.Tag).GetKeySequence());
                }
                if (textBox.Equals(TextBoxUpperNav1))
                {
                    _radioPanelPZ69.AddOrUpdateSequencedKeyBinding(textBox.Text, RadioPanelPZ69KnobsEmulator.UpperNAV1, ((TagDataClassPZ69)textBox.Tag).GetKeySequence());
                }
                if (textBox.Equals(TextBoxUpperNav2))
                {
                    _radioPanelPZ69.AddOrUpdateSequencedKeyBinding(textBox.Text, RadioPanelPZ69KnobsEmulator.UpperNAV2, ((TagDataClassPZ69)textBox.Tag).GetKeySequence());
                }
                if (textBox.Equals(TextBoxUpperADF))
                {
                    _radioPanelPZ69.AddOrUpdateSequencedKeyBinding(textBox.Text, RadioPanelPZ69KnobsEmulator.UpperADF, ((TagDataClassPZ69)textBox.Tag).GetKeySequence());
                }
                if (textBox.Equals(TextBoxUpperDME))
                {
                    _radioPanelPZ69.AddOrUpdateSequencedKeyBinding(textBox.Text, RadioPanelPZ69KnobsEmulator.UpperDME, ((TagDataClassPZ69)textBox.Tag).GetKeySequence());
                }
                if (textBox.Equals(TextBoxUpperXPDR))
                {
                    _radioPanelPZ69.AddOrUpdateSequencedKeyBinding(textBox.Text, RadioPanelPZ69KnobsEmulator.UpperXPDR, ((TagDataClassPZ69)textBox.Tag).GetKeySequence());
                }
                if (textBox.Equals(TextBoxLowerCom1))
                {
                    _radioPanelPZ69.AddOrUpdateSequencedKeyBinding(textBox.Text, RadioPanelPZ69KnobsEmulator.LowerCOM1, ((TagDataClassPZ69)textBox.Tag).GetKeySequence());
                }
                if (textBox.Equals(TextBoxLowerCom2))
                {
                    _radioPanelPZ69.AddOrUpdateSequencedKeyBinding(textBox.Text, RadioPanelPZ69KnobsEmulator.LowerCOM2, ((TagDataClassPZ69)textBox.Tag).GetKeySequence());
                }
                if (textBox.Equals(TextBoxLowerNav1))
                {
                    _radioPanelPZ69.AddOrUpdateSequencedKeyBinding(textBox.Text, RadioPanelPZ69KnobsEmulator.UpperNAV1, ((TagDataClassPZ69)textBox.Tag).GetKeySequence());
                }
                if (textBox.Equals(TextBoxLowerNav2))
                {
                    _radioPanelPZ69.AddOrUpdateSequencedKeyBinding(textBox.Text, RadioPanelPZ69KnobsEmulator.LowerNAV2, ((TagDataClassPZ69)textBox.Tag).GetKeySequence());
                }
                if (textBox.Equals(TextBoxLowerADF))
                {
                    _radioPanelPZ69.AddOrUpdateSequencedKeyBinding(textBox.Text, RadioPanelPZ69KnobsEmulator.LowerADF, ((TagDataClassPZ69)textBox.Tag).GetKeySequence());
                }
                if (textBox.Equals(TextBoxLowerDME))
                {
                    _radioPanelPZ69.AddOrUpdateSequencedKeyBinding(textBox.Text, RadioPanelPZ69KnobsEmulator.LowerDME, ((TagDataClassPZ69)textBox.Tag).GetKeySequence());
                }
                if (textBox.Equals(TextBoxLowerXPDR))
                {
                    _radioPanelPZ69.AddOrUpdateSequencedKeyBinding(textBox.Text, RadioPanelPZ69KnobsEmulator.LowerXPDR, ((TagDataClassPZ69)textBox.Tag).GetKeySequence());
                }



                if (textBox.Equals(TextBoxUpperLargePlus))
                {
                    _radioPanelPZ69.AddOrUpdateSequencedKeyBinding(textBox.Text, RadioPanelPZ69KnobsEmulator.UpperLargeFreqWheelInc, ((TagDataClassPZ69)textBox.Tag).GetKeySequence());
                }
                if (textBox.Equals(TextBoxUpperLargeMinus))
                {
                    _radioPanelPZ69.AddOrUpdateSequencedKeyBinding(textBox.Text, RadioPanelPZ69KnobsEmulator.UpperLargeFreqWheelDec, ((TagDataClassPZ69)textBox.Tag).GetKeySequence());
                }
                if (textBox.Equals(TextBoxUpperSmallPlus))
                {
                    _radioPanelPZ69.AddOrUpdateSequencedKeyBinding(textBox.Text, RadioPanelPZ69KnobsEmulator.UpperSmallFreqWheelInc, ((TagDataClassPZ69)textBox.Tag).GetKeySequence());
                }
                if (textBox.Equals(TextBoxUpperSmallMinus))
                {
                    _radioPanelPZ69.AddOrUpdateSequencedKeyBinding(textBox.Text, RadioPanelPZ69KnobsEmulator.UpperSmallFreqWheelDec, ((TagDataClassPZ69)textBox.Tag).GetKeySequence());
                }
                if (textBox.Equals(TextBoxUpperActStbyOn))
                {
                    _radioPanelPZ69.AddOrUpdateSequencedKeyBinding(textBox.Text, RadioPanelPZ69KnobsEmulator.UpperFreqSwitch, ((TagDataClassPZ69)textBox.Tag).GetKeySequence());
                }
                if (textBox.Equals(TextBoxUpperActStbyOff))
                {
                    _radioPanelPZ69.AddOrUpdateSequencedKeyBinding(textBox.Text, RadioPanelPZ69KnobsEmulator.UpperFreqSwitch, ((TagDataClassPZ69)textBox.Tag).GetKeySequence(), false);
                }
                if (textBox.Equals(TextBoxLowerLargePlus))
                {
                    _radioPanelPZ69.AddOrUpdateSequencedKeyBinding(textBox.Text, RadioPanelPZ69KnobsEmulator.LowerLargeFreqWheelInc, ((TagDataClassPZ69)textBox.Tag).GetKeySequence());
                }
                if (textBox.Equals(TextBoxLowerLargeMinus))
                {
                    _radioPanelPZ69.AddOrUpdateSequencedKeyBinding(textBox.Text, RadioPanelPZ69KnobsEmulator.LowerLargeFreqWheelDec, ((TagDataClassPZ69)textBox.Tag).GetKeySequence());
                }
                if (textBox.Equals(TextBoxLowerSmallPlus))
                {
                    _radioPanelPZ69.AddOrUpdateSequencedKeyBinding(textBox.Text, RadioPanelPZ69KnobsEmulator.LowerSmallFreqWheelInc, ((TagDataClassPZ69)textBox.Tag).GetKeySequence());
                }
                if (textBox.Equals(TextBoxLowerLargeMinus))
                {
                    _radioPanelPZ69.AddOrUpdateSequencedKeyBinding(textBox.Text, RadioPanelPZ69KnobsEmulator.LowerSmallFreqWheelDec, ((TagDataClassPZ69)textBox.Tag).GetKeySequence());
                }
                if (textBox.Equals(TextBoxLowerActStbyOn))
                {
                    _radioPanelPZ69.AddOrUpdateSequencedKeyBinding(textBox.Text, RadioPanelPZ69KnobsEmulator.LowerFreqSwitch, ((TagDataClassPZ69)textBox.Tag).GetKeySequence());
                }
                if (textBox.Equals(TextBoxLowerActStbyOff))
                {
                    _radioPanelPZ69.AddOrUpdateSequencedKeyBinding(textBox.Text, RadioPanelPZ69KnobsEmulator.LowerFreqSwitch, ((TagDataClassPZ69)textBox.Tag).GetKeySequence(), false);
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(3011, ex);
            }
        }


        private void UpdateBipLinkBindings(TextBox textBox)
        {
            try
            {
                if (textBox.Equals(TextBoxUpperCom1))
                {
                    _radioPanelPZ69.AddOrUpdateBIPLinkKeyBinding(RadioPanelPZ69KnobsEmulator.UpperCOM1, ((TagDataClassPZ69)textBox.Tag).BIPLink);
                }
                if (textBox.Equals(TextBoxUpperCom2))
                {
                    _radioPanelPZ69.AddOrUpdateBIPLinkKeyBinding(RadioPanelPZ69KnobsEmulator.UpperCOM2, ((TagDataClassPZ69)textBox.Tag).BIPLink);
                }
                if (textBox.Equals(TextBoxUpperNav1))
                {
                    _radioPanelPZ69.AddOrUpdateBIPLinkKeyBinding(RadioPanelPZ69KnobsEmulator.UpperNAV1, ((TagDataClassPZ69)textBox.Tag).BIPLink);
                }
                if (textBox.Equals(TextBoxUpperNav2))
                {
                    _radioPanelPZ69.AddOrUpdateBIPLinkKeyBinding(RadioPanelPZ69KnobsEmulator.UpperNAV2, ((TagDataClassPZ69)textBox.Tag).BIPLink);
                }
                if (textBox.Equals(TextBoxUpperADF))
                {
                    _radioPanelPZ69.AddOrUpdateBIPLinkKeyBinding(RadioPanelPZ69KnobsEmulator.UpperADF, ((TagDataClassPZ69)textBox.Tag).BIPLink);
                }
                if (textBox.Equals(TextBoxUpperDME))
                {
                    _radioPanelPZ69.AddOrUpdateBIPLinkKeyBinding(RadioPanelPZ69KnobsEmulator.UpperDME, ((TagDataClassPZ69)textBox.Tag).BIPLink);
                }
                if (textBox.Equals(TextBoxUpperXPDR))
                {
                    _radioPanelPZ69.AddOrUpdateBIPLinkKeyBinding(RadioPanelPZ69KnobsEmulator.UpperXPDR, ((TagDataClassPZ69)textBox.Tag).BIPLink);
                }
                if (textBox.Equals(TextBoxLowerCom1))
                {
                    _radioPanelPZ69.AddOrUpdateBIPLinkKeyBinding(RadioPanelPZ69KnobsEmulator.LowerCOM1, ((TagDataClassPZ69)textBox.Tag).BIPLink);
                }
                if (textBox.Equals(TextBoxLowerCom2))
                {
                    _radioPanelPZ69.AddOrUpdateBIPLinkKeyBinding(RadioPanelPZ69KnobsEmulator.LowerCOM2, ((TagDataClassPZ69)textBox.Tag).BIPLink);
                }
                if (textBox.Equals(TextBoxLowerNav1))
                {
                    _radioPanelPZ69.AddOrUpdateBIPLinkKeyBinding(RadioPanelPZ69KnobsEmulator.UpperNAV1, ((TagDataClassPZ69)textBox.Tag).BIPLink);
                }
                if (textBox.Equals(TextBoxLowerNav2))
                {
                    _radioPanelPZ69.AddOrUpdateBIPLinkKeyBinding(RadioPanelPZ69KnobsEmulator.LowerNAV2, ((TagDataClassPZ69)textBox.Tag).BIPLink);
                }
                if (textBox.Equals(TextBoxLowerADF))
                {
                    _radioPanelPZ69.AddOrUpdateBIPLinkKeyBinding(RadioPanelPZ69KnobsEmulator.LowerADF, ((TagDataClassPZ69)textBox.Tag).BIPLink);
                }
                if (textBox.Equals(TextBoxLowerDME))
                {
                    _radioPanelPZ69.AddOrUpdateBIPLinkKeyBinding(RadioPanelPZ69KnobsEmulator.LowerDME, ((TagDataClassPZ69)textBox.Tag).BIPLink);
                }
                if (textBox.Equals(TextBoxLowerXPDR))
                {
                    _radioPanelPZ69.AddOrUpdateBIPLinkKeyBinding(RadioPanelPZ69KnobsEmulator.LowerXPDR, ((TagDataClassPZ69)textBox.Tag).BIPLink);
                }



                if (textBox.Equals(TextBoxUpperLargePlus))
                {
                    _radioPanelPZ69.AddOrUpdateBIPLinkKeyBinding(RadioPanelPZ69KnobsEmulator.UpperLargeFreqWheelInc, ((TagDataClassPZ69)textBox.Tag).BIPLink);
                }
                if (textBox.Equals(TextBoxUpperLargeMinus))
                {
                    _radioPanelPZ69.AddOrUpdateBIPLinkKeyBinding(RadioPanelPZ69KnobsEmulator.UpperLargeFreqWheelDec, ((TagDataClassPZ69)textBox.Tag).BIPLink);
                }
                if (textBox.Equals(TextBoxUpperSmallPlus))
                {
                    _radioPanelPZ69.AddOrUpdateBIPLinkKeyBinding(RadioPanelPZ69KnobsEmulator.UpperSmallFreqWheelInc, ((TagDataClassPZ69)textBox.Tag).BIPLink);
                }
                if (textBox.Equals(TextBoxUpperSmallMinus))
                {
                    _radioPanelPZ69.AddOrUpdateBIPLinkKeyBinding(RadioPanelPZ69KnobsEmulator.UpperSmallFreqWheelDec, ((TagDataClassPZ69)textBox.Tag).BIPLink);
                }
                if (textBox.Equals(TextBoxUpperActStbyOn))
                {
                    _radioPanelPZ69.AddOrUpdateBIPLinkKeyBinding(RadioPanelPZ69KnobsEmulator.UpperFreqSwitch, ((TagDataClassPZ69)textBox.Tag).BIPLink);
                }
                if (textBox.Equals(TextBoxUpperActStbyOff))
                {
                    _radioPanelPZ69.AddOrUpdateBIPLinkKeyBinding(RadioPanelPZ69KnobsEmulator.UpperFreqSwitch, ((TagDataClassPZ69)textBox.Tag).BIPLink, false);
                }
                if (textBox.Equals(TextBoxLowerLargePlus))
                {
                    _radioPanelPZ69.AddOrUpdateBIPLinkKeyBinding(RadioPanelPZ69KnobsEmulator.LowerLargeFreqWheelInc, ((TagDataClassPZ69)textBox.Tag).BIPLink);
                }
                if (textBox.Equals(TextBoxLowerLargeMinus))
                {
                    _radioPanelPZ69.AddOrUpdateBIPLinkKeyBinding(RadioPanelPZ69KnobsEmulator.LowerLargeFreqWheelDec, ((TagDataClassPZ69)textBox.Tag).BIPLink);
                }
                if (textBox.Equals(TextBoxLowerSmallPlus))
                {
                    _radioPanelPZ69.AddOrUpdateBIPLinkKeyBinding(RadioPanelPZ69KnobsEmulator.LowerSmallFreqWheelInc, ((TagDataClassPZ69)textBox.Tag).BIPLink);
                }
                if (textBox.Equals(TextBoxLowerLargeMinus))
                {
                    _radioPanelPZ69.AddOrUpdateBIPLinkKeyBinding(RadioPanelPZ69KnobsEmulator.LowerSmallFreqWheelDec, ((TagDataClassPZ69)textBox.Tag).BIPLink);
                }
                if (textBox.Equals(TextBoxLowerActStbyOn))
                {
                    _radioPanelPZ69.AddOrUpdateBIPLinkKeyBinding(RadioPanelPZ69KnobsEmulator.LowerFreqSwitch, ((TagDataClassPZ69)textBox.Tag).BIPLink);
                }
                if (textBox.Equals(TextBoxLowerActStbyOff))
                {
                    _radioPanelPZ69.AddOrUpdateBIPLinkKeyBinding(RadioPanelPZ69KnobsEmulator.LowerFreqSwitch, ((TagDataClassPZ69)textBox.Tag).BIPLink, false);
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
                if (!((TagDataClassPZ69)textBox.Tag).ContainsOSKeyPress() || ((TagDataClassPZ69)textBox.Tag).KeyPress.KeySequence.Count == 0)
                {
                    keyPressLength = KeyPressLength.FiftyMilliSec;
                }
                else
                {
                    keyPressLength = ((TagDataClassPZ69)textBox.Tag).KeyPress.GetLengthOfKeyPress();
                }
                if (textBox.Equals(TextBoxUpperCom1))
                {
                    _radioPanelPZ69.AddOrUpdateSingleKeyBinding(RadioPanelPZ69KnobsEmulator.UpperCOM1, TextBoxUpperCom1.Text, keyPressLength);
                }
                if (textBox.Equals(TextBoxUpperCom2))
                {
                    _radioPanelPZ69.AddOrUpdateSingleKeyBinding(RadioPanelPZ69KnobsEmulator.UpperCOM2, TextBoxUpperCom2.Text, keyPressLength);
                }
                if (textBox.Equals(TextBoxUpperNav1))
                {
                    _radioPanelPZ69.AddOrUpdateSingleKeyBinding(RadioPanelPZ69KnobsEmulator.UpperNAV1, TextBoxUpperNav1.Text, keyPressLength);
                }
                if (textBox.Equals(TextBoxUpperNav2))
                {
                    _radioPanelPZ69.AddOrUpdateSingleKeyBinding(RadioPanelPZ69KnobsEmulator.UpperNAV2, TextBoxUpperNav2.Text, keyPressLength);
                }
                if (textBox.Equals(TextBoxUpperADF))
                {
                    _radioPanelPZ69.AddOrUpdateSingleKeyBinding(RadioPanelPZ69KnobsEmulator.UpperADF, TextBoxUpperADF.Text, keyPressLength);
                }
                if (textBox.Equals(TextBoxUpperDME))
                {
                    _radioPanelPZ69.AddOrUpdateSingleKeyBinding(RadioPanelPZ69KnobsEmulator.UpperDME, TextBoxUpperDME.Text, keyPressLength);
                }
                if (textBox.Equals(TextBoxUpperXPDR))
                {
                    _radioPanelPZ69.AddOrUpdateSingleKeyBinding(RadioPanelPZ69KnobsEmulator.UpperXPDR, TextBoxUpperXPDR.Text, keyPressLength);
                }
                if (textBox.Equals(TextBoxUpperLargePlus))
                {
                    _radioPanelPZ69.AddOrUpdateSingleKeyBinding(RadioPanelPZ69KnobsEmulator.UpperLargeFreqWheelInc, TextBoxUpperLargePlus.Text, keyPressLength);
                }
                if (textBox.Equals(TextBoxUpperLargeMinus))
                {
                    _radioPanelPZ69.AddOrUpdateSingleKeyBinding(RadioPanelPZ69KnobsEmulator.UpperLargeFreqWheelDec, TextBoxUpperLargeMinus.Text, keyPressLength);
                }
                if (textBox.Equals(TextBoxUpperSmallPlus))
                {
                    _radioPanelPZ69.AddOrUpdateSingleKeyBinding(RadioPanelPZ69KnobsEmulator.UpperSmallFreqWheelInc, TextBoxUpperSmallPlus.Text, keyPressLength);
                }
                if (textBox.Equals(TextBoxUpperSmallMinus))
                {
                    _radioPanelPZ69.AddOrUpdateSingleKeyBinding(RadioPanelPZ69KnobsEmulator.UpperSmallFreqWheelDec, TextBoxUpperSmallMinus.Text, keyPressLength);
                }
                if (textBox.Equals(TextBoxUpperActStbyOn))
                {
                    _radioPanelPZ69.AddOrUpdateSingleKeyBinding(RadioPanelPZ69KnobsEmulator.UpperFreqSwitch, TextBoxUpperActStbyOn.Text, keyPressLength);
                }
                if (textBox.Equals(TextBoxUpperActStbyOff))
                {
                    _radioPanelPZ69.AddOrUpdateSingleKeyBinding(RadioPanelPZ69KnobsEmulator.UpperFreqSwitch, TextBoxUpperActStbyOff.Text, keyPressLength, false);
                }
                if (textBox.Equals(TextBoxLowerCom1))
                {
                    _radioPanelPZ69.AddOrUpdateSingleKeyBinding(RadioPanelPZ69KnobsEmulator.LowerCOM1, TextBoxLowerCom1.Text, keyPressLength);
                }
                if (textBox.Equals(TextBoxLowerCom2))
                {
                    _radioPanelPZ69.AddOrUpdateSingleKeyBinding(RadioPanelPZ69KnobsEmulator.LowerCOM2, TextBoxLowerCom2.Text, keyPressLength);
                }
                if (textBox.Equals(TextBoxLowerNav1))
                {
                    _radioPanelPZ69.AddOrUpdateSingleKeyBinding(RadioPanelPZ69KnobsEmulator.LowerNAV1, TextBoxLowerNav1.Text, keyPressLength);
                }
                if (textBox.Equals(TextBoxLowerNav2))
                {
                    _radioPanelPZ69.AddOrUpdateSingleKeyBinding(RadioPanelPZ69KnobsEmulator.LowerNAV2, TextBoxLowerNav2.Text, keyPressLength);
                }
                if (textBox.Equals(TextBoxLowerADF))
                {
                    _radioPanelPZ69.AddOrUpdateSingleKeyBinding(RadioPanelPZ69KnobsEmulator.LowerADF, TextBoxLowerADF.Text, keyPressLength);
                }
                if (textBox.Equals(TextBoxLowerDME))
                {
                    _radioPanelPZ69.AddOrUpdateSingleKeyBinding(RadioPanelPZ69KnobsEmulator.LowerDME, TextBoxLowerDME.Text, keyPressLength);
                }
                if (textBox.Equals(TextBoxLowerXPDR))
                {
                    _radioPanelPZ69.AddOrUpdateSingleKeyBinding(RadioPanelPZ69KnobsEmulator.LowerXPDR, TextBoxLowerXPDR.Text, keyPressLength);
                }
                if (textBox.Equals(TextBoxLowerLargePlus))
                {
                    _radioPanelPZ69.AddOrUpdateSingleKeyBinding(RadioPanelPZ69KnobsEmulator.LowerLargeFreqWheelInc, TextBoxLowerLargePlus.Text, keyPressLength);
                }
                if (textBox.Equals(TextBoxLowerLargeMinus))
                {
                    _radioPanelPZ69.AddOrUpdateSingleKeyBinding(RadioPanelPZ69KnobsEmulator.LowerLargeFreqWheelDec, TextBoxLowerLargeMinus.Text, keyPressLength);
                }
                if (textBox.Equals(TextBoxLowerSmallPlus))
                {
                    _radioPanelPZ69.AddOrUpdateSingleKeyBinding(RadioPanelPZ69KnobsEmulator.LowerSmallFreqWheelInc, TextBoxLowerSmallPlus.Text, keyPressLength);
                }
                if (textBox.Equals(TextBoxLowerSmallMinus))
                {
                    _radioPanelPZ69.AddOrUpdateSingleKeyBinding(RadioPanelPZ69KnobsEmulator.LowerSmallFreqWheelDec, TextBoxLowerSmallMinus.Text, keyPressLength);
                }
                if (textBox.Equals(TextBoxLowerActStbyOn))
                {
                    _radioPanelPZ69.AddOrUpdateSingleKeyBinding(RadioPanelPZ69KnobsEmulator.LowerFreqSwitch, TextBoxLowerActStbyOn.Text, keyPressLength);
                }
                if (textBox.Equals(TextBoxLowerActStbyOff))
                {
                    _radioPanelPZ69.AddOrUpdateSingleKeyBinding(RadioPanelPZ69KnobsEmulator.LowerFreqSwitch, TextBoxLowerActStbyOff.Text, keyPressLength, false);
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(3012, ex);
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



                if (!((TagDataClassPZ69)textBox.Tag).ContainsSingleKey())
                {
                    return;
                }
                var keyPressLength = ((TagDataClassPZ69)textBox.Tag).KeyPress.GetLengthOfKeyPress();

                foreach (MenuItem item in contextMenu.Items)
                {
                    item.IsChecked = false;
                }

                foreach (MenuItem item in contextMenu.Items)
                {
                    /*if (item.Name == "contextMenuItemZero" && keyPressLength == KeyPressLength.Zero)
                    {
                        item.IsChecked = true;
                    }*/
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
                Common.ShowErrorMessageBox(2061, ex);
            }
        }

        private void ClearAll(bool clearAlsoProfile)
        {
            foreach (var textBox in Common.FindVisualChildren<TextBox>(this))
            {
                var tagHolderClass = (TagDataClassPZ69)textBox.Tag;
                textBox.Text = "";
                tagHolderClass.ClearAll();
            }
            if (clearAlsoProfile)
            {
                _radioPanelPZ69.ClearSettings();
            }
        }

        private void ClearAllDisplayValues()
        {
            foreach (var textBox in Common.FindVisualChildren<TextBox>(this))
            {
                if (textBox.Name.EndsWith("Numbers"))
                {
                    textBox.Text = "";
                }
            }
        }


        private void RemoveContextMenuClickHandlers()
        {
            foreach (var textBox in Common.FindVisualChildren<TextBox>(this))
            {
                if (!Equals(textBox, TextBoxLogPZ69))
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
                if (!Equals(textBox, TextBoxLogPZ69) && !textBox.Name.EndsWith("Numbers"))
                {
                    var contectMenu = (ContextMenu)Resources["TextBoxContextMenuPZ69"];
                    if (!BipFactory.HasBips())
                    {
                        MenuItem bipMenuItem = null;
                        foreach (var item in contectMenu.Items)
                        {
                            if (((MenuItem)item).Name == "contextMenuItemEditBIP")
                            {
                                bipMenuItem = (MenuItem)item;
                                break;
                            }
                        }
                        if (bipMenuItem != null)
                        {
                            contectMenu.Items.Remove(bipMenuItem);
                        }
                    }
                    textBox.ContextMenu = contectMenu;
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
                if (((TagDataClassPZ69)textBox.Tag).ContainsKeySequence())
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
                else if (((TagDataClassPZ69)textBox.Tag).IsEmpty())
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
                else if (((TagDataClassPZ69)textBox.Tag).ContainsSingleKey())
                {
                    // 5) 
                    foreach (MenuItem item in contextMenu.Items)
                    {
                        if (!item.Name.Contains("EditSequence"))
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
                else if (((TagDataClassPZ69)textBox.Tag).ContainsBIPLink())
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


        private TextBox GetTextBoxInFocus()
        {
            foreach (var textBox in Common.FindVisualChildren<TextBox>(this))
            {
                if (!Equals(textBox, TextBoxLogPZ69) && textBox.IsFocused && Equals(textBox.Background, Brushes.Yellow))
                {
                    return textBox;
                }
            }
            return null;
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
                    ((TagDataClassPZ69)textBox.Tag).KeyPress.SetLengthOfKeyPress(KeyPressLength.FiftyMilliSec);
                }
                else if (contextMenuItem.Name == "contextMenuItemHalfSecond")
                {
                    ((TagDataClassPZ69)textBox.Tag).KeyPress.SetLengthOfKeyPress(KeyPressLength.HalfSecond);
                }
                else if (contextMenuItem.Name == "contextMenuItemSecond")
                {
                    ((TagDataClassPZ69)textBox.Tag).KeyPress.SetLengthOfKeyPress(KeyPressLength.Second);
                }
                else if (contextMenuItem.Name == "contextMenuItemSecondAndHalf")
                {
                    ((TagDataClassPZ69)textBox.Tag).KeyPress.SetLengthOfKeyPress(KeyPressLength.SecondAndHalf);
                }
                else if (contextMenuItem.Name == "contextMenuItemTwoSeconds")
                {
                    ((TagDataClassPZ69)textBox.Tag).KeyPress.SetLengthOfKeyPress(KeyPressLength.TwoSeconds);
                }
                else if (contextMenuItem.Name == "contextMenuItemThreeSeconds")
                {
                    ((TagDataClassPZ69)textBox.Tag).KeyPress.SetLengthOfKeyPress(KeyPressLength.ThreeSeconds);
                }
                else if (contextMenuItem.Name == "contextMenuItemFourSeconds")
                {
                    ((TagDataClassPZ69)textBox.Tag).KeyPress.SetLengthOfKeyPress(KeyPressLength.FourSeconds);
                }
                else if (contextMenuItem.Name == "contextMenuItemFiveSecs")
                {
                    ((TagDataClassPZ69)textBox.Tag).KeyPress.SetLengthOfKeyPress(KeyPressLength.FiveSecs);
                }
                else if (contextMenuItem.Name == "contextMenuItemTenSecs")
                {
                    ((TagDataClassPZ69)textBox.Tag).KeyPress.SetLengthOfKeyPress(KeyPressLength.TenSecs);
                }
                else if (contextMenuItem.Name == "contextMenuItemFifteenSecs")
                {
                    ((TagDataClassPZ69)textBox.Tag).KeyPress.SetLengthOfKeyPress(KeyPressLength.FifteenSecs);
                }
                else if (contextMenuItem.Name == "contextMenuItemTwentySecs")
                {
                    ((TagDataClassPZ69)textBox.Tag).KeyPress.SetLengthOfKeyPress(KeyPressLength.TwentySecs);
                }
                else if (contextMenuItem.Name == "contextMenuItemThirtySecs")
                {
                    ((TagDataClassPZ69)textBox.Tag).KeyPress.SetLengthOfKeyPress(KeyPressLength.ThirtySecs);
                }
                else if (contextMenuItem.Name == "contextMenuItemFortySecs")
                {
                    ((TagDataClassPZ69)textBox.Tag).KeyPress.SetLengthOfKeyPress(KeyPressLength.FortySecs);
                }
                else if (contextMenuItem.Name == "contextMenuItemSixtySecs")
                {
                    ((TagDataClassPZ69)textBox.Tag).KeyPress.SetLengthOfKeyPress(KeyPressLength.SixtySecs);
                }

                UpdateKeyBindingProfileSimpleKeyStrokes(textBox);
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(2082, ex);
            }
        }

        private void TextBoxMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var textBox = (TextBox)sender;

                if (e.ChangedButton == MouseButton.Left)
                {
                    if (((TagDataClassPZ69)textBox.Tag).ContainsKeySequence())
                    {
                        //Check if this textbox contains sequence information. If so then prompt the user for deletion
                        if (MessageBox.Show("Do you want to delete the key sequence?", "Delete key sequence?", MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)
                        {
                            return;
                        }
                        ((TagDataClassPZ69)textBox.Tag).KeyPress.KeySequence.Clear();
                        textBox.Text = "";
                        UpdateKeyBindingProfileSimpleKeyStrokes(textBox);
                    }
                    else if (((TagDataClassPZ69)textBox.Tag).ContainsSingleKey())
                    {
                        ((TagDataClassPZ69)textBox.Tag).KeyPress.KeySequence.Clear();
                        textBox.Text = "";
                        UpdateKeyBindingProfileSimpleKeyStrokes(textBox);
                    }
                    if (((TagDataClassPZ69)textBox.Tag).ContainsBIPLink())
                    {
                        //Check if this textbox contains sequence information. If so then prompt the user for deletion
                        if (MessageBox.Show("Do you want to delete BIP Links?", "Delete BIP Link?", MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)
                        {
                            return;
                        }
                        ((TagDataClassPZ69)textBox.Tag).BIPLink.BIPLights.Clear();
                        textBox.Background = Brushes.White;
                        UpdateBipLinkBindings(textBox);
                    }
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(3001, ex);
            }
        }

        private void ButtonClearAllClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MessageBox.Show("Clear all settings for the Radio Panel?", "Confirm", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                {
                    ClearAll(true);
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(3003, ex);
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
                Common.ShowErrorMessageBox(3004, ex);
            }
        }

        private void TextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = (TextBox)sender;
            if (((TagDataClassPZ69)textBox.Tag).ContainsBIPLink())
            {
                ((TextBox)sender).Background = Brushes.Bisque;
            }
            else
            {
                ((TextBox)sender).Background = Brushes.White;
            }
        }


        private void TextBoxPreviewKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                var textBox = ((TextBox)sender);
                
                if (((TagDataClassPZ69)textBox.Tag).ContainsKeySequence())
                {
                    return;
                }
                var hashSetOfKeysPressed = new HashSet<string>();

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
                UpdateKeyBindingProfileSimpleKeyStrokes(textBox);
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(3006, ex);
            }
        }
        /* ------------------------------------------------------------------------------------------------------------------------------------------------------------
         * ------------------------------------------------------------------------------------------------------------------------------------------------------------
         * ------------------------------------------------------------------------------------------------------------------------------------------------------------
         * ------------------------------------------------------------------------------------------------------------------------------------------------------------
         * ------------------------------------------------------------------------------------------------------------------------------------------------------------
         * ------------------------------------------------------------------------------------------------------------------------------------------------------------
         */
        private void TextBoxPreviewKeyDownNumbers(object sender, KeyEventArgs e)
        {
            try
            {
                var textBox = ((TextBox)sender);

                if (textBox.Text.Contains("."))
                {
                    textBox.MaxLength = 6;
                }
                else
                {
                    textBox.MaxLength = 5;
                }
                if (!_allowedKeys.Contains(e.Key))
                {
                    //Only figures and persion allowed
                    e.Handled = true;
                    return;
                }
                if (textBox.Text.Contains(".") && e.Key == Key.OemPeriod)
                {
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(3006, ex);
            }
        }

        private void TextBoxTextChangedNumbers(object sender, TextChangedEventArgs e)
        {
            try
            {
                //MAKE SURE THE TAG IS SET BEFORE SETTING TEXT! OTHERWISE THIS DOESN'T FIRE
                var textBox = (TextBox)sender;
                if (((TagDataClassPZ69)textBox.Tag).ContainsKeySequence())
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
                Common.ShowErrorMessageBox(3007, ex);
            }
        }

        /* ------------------------------------------------------------------------------------------------------------------------------------------------------------
         * ------------------------------------------------------------------------------------------------------------------------------------------------------------
         * ------------------------------------------------------------------------------------------------------------------------------------------------------------
         * ------------------------------------------------------------------------------------------------------------------------------------------------------------
         * ------------------------------------------------------------------------------------------------------------------------------------------------------------
         * ------------------------------------------------------------------------------------------------------------------------------------------------------------
         */

        private void TextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                //MAKE SURE THE TAG IS SET BEFORE SETTING TEXT! OTHERWISE THIS DOESN'T FIRE
                var textBox = (TextBox)sender;
                if (((TagDataClassPZ69)textBox.Tag).ContainsKeySequence())
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
                Common.ShowErrorMessageBox(3007, ex);
            }
        }


        private void TextBoxShortcutKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                var textBox = ((TextBox)sender);
                if (((TagDataClassPZ69)textBox.Tag).ContainsKeySequence())
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
                UpdateKeyBindingProfileSequencedKeyStrokesPZ69(textBox);
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(3008, ex);
            }
        }


        private void NotifySwitchChanges(HashSet<object> switches)
        {
            try
            {
                //Set focus to this so that virtual keypresses won't affect settings
                Dispatcher.BeginInvoke((Action)(() => TextBoxLogPZ69.Focus()));
                foreach (var radioPanelKey in switches)
                {
                    var key = (RadioPanelPZ69KnobEmulator)radioPanelKey;

                    if (_radioPanelPZ69.ForwardKeyPresses)
                    {
                        if (!string.IsNullOrEmpty(_radioPanelPZ69.GetKeyPressForLoggingPurposes(key)))
                        {
                            Dispatcher.BeginInvoke(
                                (Action)
                                (() =>
                                 TextBoxLogPZ69.Text =
                                 TextBoxLogPZ69.Text.Insert(0, _radioPanelPZ69.GetKeyPressForLoggingPurposes(key) + "\n")));
                        }
                    }
                    else
                    {
                        Dispatcher.BeginInvoke(
                            (Action)
                            (() =>
                             TextBoxLogPZ69.Text =
                             TextBoxLogPZ69.Text = TextBoxLogPZ69.Text.Insert(0, "No action taken, virtual key press disabled.\n")));
                    }
                }
                SetGraphicsState(switches);
            }
            catch (Exception ex)
            {
                Dispatcher.BeginInvoke(
                    (Action)
                    (() =>
                     TextBoxLogPZ69.Text = TextBoxLogPZ69.Text.Insert(0, "0x16" + ex.Message + ".\n")));
                Common.ShowErrorMessageBox(3009, ex);
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
                
                foreach (var keyBinding in _radioPanelPZ69.KeyBindingsHashSet)
                {
                    if (keyBinding.RadioPanelPZ69Key == RadioPanelPZ69KnobsEmulator.UpperCOM1)
                    {
                        if (keyBinding.OSKeyPress != null)
                        {
                            ((TagDataClassPZ69)TextBoxUpperCom1.Tag).KeyPress = keyBinding.OSKeyPress;
                            TextBoxUpperCom1.Text = ((TagDataClassPZ69)TextBoxUpperCom1.Tag).GetTextBoxKeyPressInfo();
                        }
                    }
                    if (keyBinding.RadioPanelPZ69Key == RadioPanelPZ69KnobsEmulator.UpperCOM2)
                    {
                        if (keyBinding.OSKeyPress != null)
                        {
                            ((TagDataClassPZ69)TextBoxUpperCom2.Tag).KeyPress = keyBinding.OSKeyPress;
                            TextBoxUpperCom2.Text = keyBinding.OSKeyPress.GetSimpleVirtualKeyCodesAsString();
                        }
                    }
                    if (keyBinding.RadioPanelPZ69Key == RadioPanelPZ69KnobsEmulator.UpperNAV1)
                    {
                        if (keyBinding.OSKeyPress != null)
                        {
                            ((TagDataClassPZ69)TextBoxUpperNav1.Tag).KeyPress = keyBinding.OSKeyPress;
                            TextBoxUpperNav1.Text = keyBinding.OSKeyPress.GetSimpleVirtualKeyCodesAsString();
                        }
                    }
                    if (keyBinding.RadioPanelPZ69Key == RadioPanelPZ69KnobsEmulator.UpperNAV2)
                    {
                        if (keyBinding.OSKeyPress != null)
                        {
                            ((TagDataClassPZ69)TextBoxUpperNav2.Tag).KeyPress = keyBinding.OSKeyPress;
                            TextBoxUpperNav2.Text = keyBinding.OSKeyPress.GetSimpleVirtualKeyCodesAsString();
                        }
                    }
                    if (keyBinding.RadioPanelPZ69Key == RadioPanelPZ69KnobsEmulator.UpperADF)
                    {
                        if (keyBinding.OSKeyPress != null)
                        {
                            ((TagDataClassPZ69)TextBoxUpperADF.Tag).KeyPress = keyBinding.OSKeyPress;
                            TextBoxUpperADF.Text = keyBinding.OSKeyPress.GetSimpleVirtualKeyCodesAsString();
                        }
                    }

                    if (keyBinding.RadioPanelPZ69Key == RadioPanelPZ69KnobsEmulator.UpperDME)
                    {
                        if (keyBinding.OSKeyPress != null)
                        {
                            ((TagDataClassPZ69)TextBoxUpperDME.Tag).KeyPress = keyBinding.OSKeyPress;
                            TextBoxUpperDME.Text = keyBinding.OSKeyPress.GetSimpleVirtualKeyCodesAsString();
                        }
                    }
                    if (keyBinding.RadioPanelPZ69Key == RadioPanelPZ69KnobsEmulator.UpperXPDR)
                    {
                        if (keyBinding.OSKeyPress != null)
                        {
                            ((TagDataClassPZ69)TextBoxUpperXPDR.Tag).KeyPress = keyBinding.OSKeyPress;
                            TextBoxUpperXPDR.Text = keyBinding.OSKeyPress.GetSimpleVirtualKeyCodesAsString();
                        }

                    }
                    if (keyBinding.RadioPanelPZ69Key == RadioPanelPZ69KnobsEmulator.UpperLargeFreqWheelInc)
                    {
                        if (keyBinding.OSKeyPress != null)
                        {
                            ((TagDataClassPZ69)TextBoxUpperLargePlus.Tag).KeyPress = keyBinding.OSKeyPress;
                            TextBoxUpperLargePlus.Text = keyBinding.OSKeyPress.GetSimpleVirtualKeyCodesAsString();
                        }
                    }
                    if (keyBinding.RadioPanelPZ69Key == RadioPanelPZ69KnobsEmulator.UpperLargeFreqWheelDec)
                    {
                        if (keyBinding.OSKeyPress != null)
                        {
                            ((TagDataClassPZ69)TextBoxUpperLargeMinus.Tag).KeyPress = keyBinding.OSKeyPress;
                            TextBoxUpperLargeMinus.Text = keyBinding.OSKeyPress.GetSimpleVirtualKeyCodesAsString();
                        }
                    }
                    if (keyBinding.RadioPanelPZ69Key == RadioPanelPZ69KnobsEmulator.UpperSmallFreqWheelInc)
                    {
                        if (keyBinding.OSKeyPress != null)
                        {
                            ((TagDataClassPZ69)TextBoxUpperSmallPlus.Tag).KeyPress = keyBinding.OSKeyPress;
                            TextBoxUpperSmallPlus.Text = keyBinding.OSKeyPress.GetSimpleVirtualKeyCodesAsString();
                        }
                    }
                    if (keyBinding.RadioPanelPZ69Key == RadioPanelPZ69KnobsEmulator.UpperSmallFreqWheelDec)
                    {
                        if (keyBinding.OSKeyPress != null)
                        {
                            ((TagDataClassPZ69)TextBoxUpperSmallMinus.Tag).KeyPress = keyBinding.OSKeyPress;
                            TextBoxUpperSmallMinus.Text = keyBinding.OSKeyPress.GetSimpleVirtualKeyCodesAsString();
                        }
                    }
                    if (keyBinding.RadioPanelPZ69Key == RadioPanelPZ69KnobsEmulator.UpperFreqSwitch)
                    {
                        if (keyBinding.WhenTurnedOn)
                        {
                            if (keyBinding.OSKeyPress != null)
                            {
                                ((TagDataClassPZ69)TextBoxUpperActStbyOn.Tag).KeyPress = keyBinding.OSKeyPress;
                                TextBoxUpperActStbyOn.Text = keyBinding.OSKeyPress.GetSimpleVirtualKeyCodesAsString();
                            }
                        }
                        else
                        {
                            if (keyBinding.OSKeyPress != null)
                            {
                                ((TagDataClassPZ69)TextBoxUpperActStbyOff.Tag).KeyPress = keyBinding.OSKeyPress;
                                TextBoxUpperActStbyOff.Text = keyBinding.OSKeyPress.GetSimpleVirtualKeyCodesAsString();
                            }
                        }
                    }
                    if (keyBinding.RadioPanelPZ69Key == RadioPanelPZ69KnobsEmulator.LowerCOM1)
                    {
                        if (keyBinding.WhenTurnedOn)
                        {
                            if (keyBinding.OSKeyPress != null)
                            {
                                ((TagDataClassPZ69)TextBoxLowerCom1.Tag).KeyPress = keyBinding.OSKeyPress;
                                TextBoxLowerCom1.Text = keyBinding.OSKeyPress.GetSimpleVirtualKeyCodesAsString();
                            }
                        }
                    }
                    if (keyBinding.RadioPanelPZ69Key == RadioPanelPZ69KnobsEmulator.LowerCOM2)
                    {
                        if (keyBinding.WhenTurnedOn)
                        {
                            if (keyBinding.OSKeyPress != null)
                            {
                                ((TagDataClassPZ69)TextBoxLowerCom2.Tag).KeyPress = keyBinding.OSKeyPress;
                                TextBoxLowerCom2.Text = keyBinding.OSKeyPress.GetSimpleVirtualKeyCodesAsString();
                            }
                        }
                    }
                    if (keyBinding.RadioPanelPZ69Key == RadioPanelPZ69KnobsEmulator.LowerNAV1)
                    {
                        if (keyBinding.WhenTurnedOn)
                        {
                            if (keyBinding.OSKeyPress != null)
                            {
                                ((TagDataClassPZ69)TextBoxLowerNav1.Tag).KeyPress = keyBinding.OSKeyPress;
                                TextBoxLowerNav1.Text = keyBinding.OSKeyPress.GetSimpleVirtualKeyCodesAsString();
                            }
                        }
                    }
                    if (keyBinding.RadioPanelPZ69Key == RadioPanelPZ69KnobsEmulator.LowerNAV2)
                    {
                        if (keyBinding.WhenTurnedOn)
                        {
                            if (keyBinding.OSKeyPress != null)
                            {
                                ((TagDataClassPZ69)TextBoxLowerNav2.Tag).KeyPress = keyBinding.OSKeyPress;
                                TextBoxLowerNav2.Text = keyBinding.OSKeyPress.GetSimpleVirtualKeyCodesAsString();
                            }
                        }
                    }
                    if (keyBinding.RadioPanelPZ69Key == RadioPanelPZ69KnobsEmulator.LowerADF)
                    {
                        if (keyBinding.WhenTurnedOn)
                        {
                            if (keyBinding.OSKeyPress != null)
                            {
                                ((TagDataClassPZ69)TextBoxLowerADF.Tag).KeyPress = keyBinding.OSKeyPress;
                                TextBoxLowerADF.Text = keyBinding.OSKeyPress.GetSimpleVirtualKeyCodesAsString();
                            }
                        }
                    }

                    if (keyBinding.RadioPanelPZ69Key == RadioPanelPZ69KnobsEmulator.LowerDME)
                    {
                        if (keyBinding.WhenTurnedOn)
                        {
                            if (keyBinding.OSKeyPress != null)
                            {
                                ((TagDataClassPZ69)TextBoxLowerDME.Tag).KeyPress = keyBinding.OSKeyPress;
                                TextBoxLowerDME.Text = keyBinding.OSKeyPress.GetSimpleVirtualKeyCodesAsString();
                            }
                        }
                    }

                    if (keyBinding.RadioPanelPZ69Key == RadioPanelPZ69KnobsEmulator.LowerXPDR)
                    {
                        if (keyBinding.WhenTurnedOn)
                        {
                            if (keyBinding.OSKeyPress != null)
                            {
                                ((TagDataClassPZ69)TextBoxLowerXPDR.Tag).KeyPress = keyBinding.OSKeyPress;
                                TextBoxLowerXPDR.Text = keyBinding.OSKeyPress.GetSimpleVirtualKeyCodesAsString();
                            }
                        }
                    }
                    if (keyBinding.RadioPanelPZ69Key == RadioPanelPZ69KnobsEmulator.LowerLargeFreqWheelInc)
                    {
                        if (keyBinding.OSKeyPress != null)
                        {
                            ((TagDataClassPZ69)TextBoxLowerLargePlus.Tag).KeyPress = keyBinding.OSKeyPress;
                            TextBoxLowerLargePlus.Text = keyBinding.OSKeyPress.GetSimpleVirtualKeyCodesAsString();
                        }
                    }
                    if (keyBinding.RadioPanelPZ69Key == RadioPanelPZ69KnobsEmulator.LowerLargeFreqWheelDec)
                    {
                        if (keyBinding.OSKeyPress != null)
                        {
                            ((TagDataClassPZ69)TextBoxLowerLargeMinus.Tag).KeyPress = keyBinding.OSKeyPress;
                            TextBoxLowerLargeMinus.Text = keyBinding.OSKeyPress.GetSimpleVirtualKeyCodesAsString();
                        }
                    }
                    if (keyBinding.RadioPanelPZ69Key == RadioPanelPZ69KnobsEmulator.LowerSmallFreqWheelInc)
                    {
                        if (keyBinding.OSKeyPress != null)
                        {
                            ((TagDataClassPZ69)TextBoxLowerSmallPlus.Tag).KeyPress = keyBinding.OSKeyPress;
                            TextBoxLowerSmallPlus.Text = keyBinding.OSKeyPress.GetSimpleVirtualKeyCodesAsString();
                        }
                    }
                    if (keyBinding.RadioPanelPZ69Key == RadioPanelPZ69KnobsEmulator.LowerSmallFreqWheelDec)
                    {
                        if (keyBinding.OSKeyPress != null)
                        {
                            ((TagDataClassPZ69)TextBoxLowerSmallMinus.Tag).KeyPress = keyBinding.OSKeyPress;
                            TextBoxLowerSmallMinus.Text = keyBinding.OSKeyPress.GetSimpleVirtualKeyCodesAsString();
                        }
                    }
                    if (keyBinding.RadioPanelPZ69Key == RadioPanelPZ69KnobsEmulator.LowerFreqSwitch)
                    {
                        if (keyBinding.WhenTurnedOn)
                        {
                            if (keyBinding.OSKeyPress != null)
                            {
                                ((TagDataClassPZ69)TextBoxLowerActStbyOn.Tag).KeyPress = keyBinding.OSKeyPress;
                                TextBoxLowerActStbyOn.Text = keyBinding.OSKeyPress.GetSimpleVirtualKeyCodesAsString();
                            }
                        }
                        else
                        {
                            if (keyBinding.OSKeyPress != null)
                            {
                                ((TagDataClassPZ69)TextBoxLowerActStbyOff.Tag).KeyPress = keyBinding.OSKeyPress;
                                TextBoxLowerActStbyOff.Text = keyBinding.OSKeyPress.GetSimpleVirtualKeyCodesAsString();
                            }
                        }
                    }
                }


                foreach (var bipLinkPZ69 in _radioPanelPZ69.BipLinkHashSet)
                {
                    if (bipLinkPZ69.RadioPanelPZ69Knob == RadioPanelPZ69KnobsEmulator.UpperCOM1)
                    {
                        ((TagDataClassPZ69)TextBoxUpperCom1.Tag).BIPLink = bipLinkPZ69;
                        TextBoxUpperCom1.Background = Brushes.Bisque;

                    }
                    if (bipLinkPZ69.RadioPanelPZ69Knob == RadioPanelPZ69KnobsEmulator.UpperCOM2)
                    {
                        ((TagDataClassPZ69)TextBoxUpperCom2.Tag).BIPLink = bipLinkPZ69;
                        TextBoxUpperCom2.Background = Brushes.Bisque;
                    }
                    if (bipLinkPZ69.RadioPanelPZ69Knob == RadioPanelPZ69KnobsEmulator.UpperNAV1)
                    {
                        ((TagDataClassPZ69)TextBoxUpperNav1.Tag).BIPLink = bipLinkPZ69;
                        TextBoxUpperNav1.Background = Brushes.Bisque;
                    }
                    if (bipLinkPZ69.RadioPanelPZ69Knob == RadioPanelPZ69KnobsEmulator.UpperNAV2)
                    {
                        ((TagDataClassPZ69)TextBoxUpperNav2.Tag).BIPLink = bipLinkPZ69;
                        TextBoxUpperNav2.Background = Brushes.Bisque;
                    }
                    if (bipLinkPZ69.RadioPanelPZ69Knob == RadioPanelPZ69KnobsEmulator.UpperADF)
                    {
                        ((TagDataClassPZ69)TextBoxUpperADF.Tag).BIPLink = bipLinkPZ69;
                        TextBoxUpperADF.Background = Brushes.Bisque;
                    }

                    if (bipLinkPZ69.RadioPanelPZ69Knob == RadioPanelPZ69KnobsEmulator.UpperDME)
                    {
                        ((TagDataClassPZ69)TextBoxUpperDME.Tag).BIPLink = bipLinkPZ69;
                        TextBoxUpperDME.Background = Brushes.Bisque;
                    }
                    if (bipLinkPZ69.RadioPanelPZ69Knob == RadioPanelPZ69KnobsEmulator.UpperXPDR)
                    {
                        ((TagDataClassPZ69)TextBoxUpperXPDR.Tag).BIPLink = bipLinkPZ69;
                        TextBoxUpperXPDR.Background = Brushes.Bisque;

                    }
                    if (bipLinkPZ69.RadioPanelPZ69Knob == RadioPanelPZ69KnobsEmulator.UpperLargeFreqWheelInc)
                    {
                        ((TagDataClassPZ69)TextBoxUpperLargePlus.Tag).BIPLink = bipLinkPZ69;
                        TextBoxUpperLargePlus.Background = Brushes.Bisque;
                    }
                    if (bipLinkPZ69.RadioPanelPZ69Knob == RadioPanelPZ69KnobsEmulator.UpperLargeFreqWheelDec)
                    {
                        ((TagDataClassPZ69)TextBoxUpperLargeMinus.Tag).BIPLink = bipLinkPZ69;
                        TextBoxUpperLargeMinus.Background = Brushes.Bisque;
                    }
                    if (bipLinkPZ69.RadioPanelPZ69Knob == RadioPanelPZ69KnobsEmulator.UpperSmallFreqWheelInc)
                    {
                        ((TagDataClassPZ69)TextBoxUpperSmallPlus.Tag).BIPLink = bipLinkPZ69;
                        TextBoxUpperSmallPlus.Background = Brushes.Bisque;
                    }
                    if (bipLinkPZ69.RadioPanelPZ69Knob == RadioPanelPZ69KnobsEmulator.UpperSmallFreqWheelDec)
                    {
                        ((TagDataClassPZ69)TextBoxUpperSmallMinus.Tag).BIPLink = bipLinkPZ69;
                        TextBoxUpperSmallMinus.Background = Brushes.Bisque;
                    }
                    if (bipLinkPZ69.RadioPanelPZ69Knob == RadioPanelPZ69KnobsEmulator.UpperFreqSwitch)
                    {
                        if (bipLinkPZ69.WhenTurnedOn)
                        {
                            ((TagDataClassPZ69)TextBoxUpperActStbyOn.Tag).BIPLink = bipLinkPZ69;
                            TextBoxUpperActStbyOn.Background = Brushes.Bisque;
                        }
                        else
                        {
                            ((TagDataClassPZ69)TextBoxUpperActStbyOff.Tag).BIPLink = bipLinkPZ69;
                            TextBoxUpperActStbyOff.Background = Brushes.Bisque;
                        }
                    }
                    if (bipLinkPZ69.RadioPanelPZ69Knob == RadioPanelPZ69KnobsEmulator.LowerCOM1)
                    {
                        if (bipLinkPZ69.WhenTurnedOn)
                        {
                            ((TagDataClassPZ69)TextBoxLowerCom1.Tag).BIPLink = bipLinkPZ69;
                            TextBoxLowerCom1.Background = Brushes.Bisque;
                        }
                    }
                    if (bipLinkPZ69.RadioPanelPZ69Knob == RadioPanelPZ69KnobsEmulator.LowerCOM2)
                    {
                        if (bipLinkPZ69.WhenTurnedOn)
                        {
                            ((TagDataClassPZ69)TextBoxLowerCom2.Tag).BIPLink = bipLinkPZ69;
                            TextBoxLowerCom2.Background = Brushes.Bisque;
                        }
                    }
                    if (bipLinkPZ69.RadioPanelPZ69Knob == RadioPanelPZ69KnobsEmulator.LowerNAV1)
                    {
                        if (bipLinkPZ69.WhenTurnedOn)
                        {
                            ((TagDataClassPZ69)TextBoxLowerNav1.Tag).BIPLink = bipLinkPZ69;
                            TextBoxLowerNav1.Background = Brushes.Bisque;
                        }
                    }
                    if (bipLinkPZ69.RadioPanelPZ69Knob == RadioPanelPZ69KnobsEmulator.LowerNAV2)
                    {
                        if (bipLinkPZ69.WhenTurnedOn)
                        {
                            ((TagDataClassPZ69)TextBoxLowerNav2.Tag).BIPLink = bipLinkPZ69;
                            TextBoxLowerNav2.Background = Brushes.Bisque;
                        }
                    }
                    if (bipLinkPZ69.RadioPanelPZ69Knob == RadioPanelPZ69KnobsEmulator.LowerADF)
                    {
                        if (bipLinkPZ69.WhenTurnedOn)
                        {
                            ((TagDataClassPZ69)TextBoxLowerADF.Tag).BIPLink = bipLinkPZ69;
                            TextBoxLowerADF.Background = Brushes.Bisque;
                        }
                    }

                    if (bipLinkPZ69.RadioPanelPZ69Knob == RadioPanelPZ69KnobsEmulator.LowerDME)
                    {
                        if (bipLinkPZ69.WhenTurnedOn)
                        {
                            ((TagDataClassPZ69)TextBoxLowerDME.Tag).BIPLink = bipLinkPZ69;
                            TextBoxLowerDME.Background = Brushes.Bisque;
                        }
                    }

                    if (bipLinkPZ69.RadioPanelPZ69Knob == RadioPanelPZ69KnobsEmulator.LowerXPDR)
                    {
                        if (bipLinkPZ69.WhenTurnedOn)
                        {
                            ((TagDataClassPZ69)TextBoxLowerXPDR.Tag).BIPLink = bipLinkPZ69;
                            TextBoxLowerXPDR.Background = Brushes.Bisque;
                        }
                    }
                    if (bipLinkPZ69.RadioPanelPZ69Knob == RadioPanelPZ69KnobsEmulator.LowerLargeFreqWheelInc)
                    {
                        ((TagDataClassPZ69)TextBoxLowerLargePlus.Tag).BIPLink = bipLinkPZ69;
                        TextBoxLowerLargePlus.Background = Brushes.Bisque;
                    }
                    if (bipLinkPZ69.RadioPanelPZ69Knob == RadioPanelPZ69KnobsEmulator.LowerLargeFreqWheelDec)
                    {
                        ((TagDataClassPZ69)TextBoxLowerLargeMinus.Tag).BIPLink = bipLinkPZ69;
                        TextBoxLowerLargeMinus.Background = Brushes.Bisque;
                    }
                    if (bipLinkPZ69.RadioPanelPZ69Knob == RadioPanelPZ69KnobsEmulator.LowerSmallFreqWheelInc)
                    {
                        ((TagDataClassPZ69)TextBoxLowerSmallPlus.Tag).BIPLink = bipLinkPZ69;
                        TextBoxLowerSmallPlus.Background = Brushes.Bisque;
                    }
                    if (bipLinkPZ69.RadioPanelPZ69Knob == RadioPanelPZ69KnobsEmulator.LowerSmallFreqWheelDec)
                    {
                        ((TagDataClassPZ69)TextBoxLowerSmallMinus.Tag).BIPLink = bipLinkPZ69;
                        TextBoxLowerSmallMinus.Background = Brushes.Bisque;
                    }
                    if (bipLinkPZ69.RadioPanelPZ69Knob == RadioPanelPZ69KnobsEmulator.LowerFreqSwitch)
                    {
                        if (bipLinkPZ69.WhenTurnedOn)
                        {
                            ((TagDataClassPZ69)TextBoxLowerActStbyOn.Tag).BIPLink = bipLinkPZ69;
                            TextBoxLowerActStbyOn.Background = Brushes.Bisque;
                        }
                        else
                        {
                            ((TagDataClassPZ69)TextBoxLowerActStbyOff.Tag).BIPLink = bipLinkPZ69;
                            TextBoxLowerActStbyOff.Background = Brushes.Bisque;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(3013, ex);
            }
        }

        private void SetGraphicsState(HashSet<object> knobs)
        {
            try
            {
                foreach (var radioKnobO in knobs)
                {
                    var radioKnob = (RadioPanelPZ69KnobEmulator)radioKnobO;
                    switch (radioKnob.RadioPanelPZ69Knob)
                    {
                        case RadioPanelPZ69KnobsEmulator.UpperCOM1:
                            {
                                var key = radioKnob;
                                Dispatcher.BeginInvoke(
                                    (Action)delegate
                                    {
                                        TopLeftCom1.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsEmulator.UpperCOM2:
                            {
                                var key = radioKnob;
                                Dispatcher.BeginInvoke(
                                    (Action)delegate
                                    {
                                        TopLeftCom2.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsEmulator.UpperNAV1:
                            {
                                var key = radioKnob;
                                Dispatcher.BeginInvoke(
                                    (Action)delegate
                                    {
                                        TopLeftNav1.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsEmulator.UpperNAV2:
                            {
                                var key = radioKnob;
                                Dispatcher.BeginInvoke(
                                    (Action)delegate
                                    {
                                        TopLeftNav2.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsEmulator.UpperADF:
                            {
                                var key = radioKnob;
                                Dispatcher.BeginInvoke(
                                    (Action)delegate
                                    {
                                        TopLeftADF.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsEmulator.UpperDME:
                            {
                                var key = radioKnob;
                                Dispatcher.BeginInvoke(
                                    (Action)delegate
                                    {
                                        TopLeftDME.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsEmulator.UpperXPDR:
                            {
                                var key = radioKnob;
                                Dispatcher.BeginInvoke(
                                    (Action)delegate
                                    {
                                        TopLeftXPDR.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsEmulator.LowerCOM1:
                            {
                                var key = radioKnob;
                                Dispatcher.BeginInvoke(
                                    (Action)delegate
                                    {
                                        LowerLeftCom1.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsEmulator.LowerCOM2:
                            {
                                var key = radioKnob;
                                Dispatcher.BeginInvoke(
                                    (Action)delegate
                                    {
                                        LowerLeftCom2.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsEmulator.LowerNAV1:
                            {
                                var key = radioKnob;
                                Dispatcher.BeginInvoke(
                                    (Action)delegate
                                    {
                                        LowerLeftNav1.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsEmulator.LowerNAV2:
                            {
                                var key = radioKnob;
                                Dispatcher.BeginInvoke(
                                    (Action)delegate
                                    {
                                        LowerLeftNav2.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsEmulator.LowerADF:
                            {
                                var key = radioKnob;
                                Dispatcher.BeginInvoke(
                                    (Action)delegate
                                    {
                                        LowerLeftADF.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsEmulator.LowerDME:
                            {
                                var key = radioKnob;
                                Dispatcher.BeginInvoke(
                                    (Action)delegate
                                    {
                                        LowerLeftDME.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsEmulator.LowerXPDR:
                            {
                                var key = radioKnob;
                                Dispatcher.BeginInvoke(
                                    (Action)delegate
                                    {
                                        LowerLeftXPDR.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsEmulator.UpperSmallFreqWheelInc:
                            {
                                var key = radioKnob;
                                Dispatcher.BeginInvoke(
                                    (Action)delegate
                                    {
                                        UpperSmallerLCDKnobInc.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsEmulator.UpperSmallFreqWheelDec:
                            {
                                var key = radioKnob;
                                Dispatcher.BeginInvoke(
                                    (Action)delegate
                                    {
                                        UpperSmallerLCDKnobDec.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsEmulator.UpperLargeFreqWheelInc:
                            {
                                var key = radioKnob;
                                Dispatcher.BeginInvoke(
                                    (Action)delegate
                                    {
                                        UpperLargerLCDKnobInc.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsEmulator.UpperLargeFreqWheelDec:
                            {
                                var key = radioKnob;
                                Dispatcher.BeginInvoke(
                                    (Action)delegate
                                    {
                                        UpperLargerLCDKnobDec.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsEmulator.LowerSmallFreqWheelInc:
                            {
                                var key = radioKnob;
                                Dispatcher.BeginInvoke(
                                    (Action)delegate
                                    {
                                        LowerSmallerLCDKnobInc.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsEmulator.LowerSmallFreqWheelDec:
                            {
                                var key = radioKnob;
                                Dispatcher.BeginInvoke(
                                    (Action)delegate
                                    {
                                        LowerSmallerLCDKnobDec.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsEmulator.LowerLargeFreqWheelInc:
                            {
                                var key = radioKnob;
                                Dispatcher.BeginInvoke(
                                    (Action)delegate
                                    {
                                        LowerLargerLCDKnobInc.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsEmulator.LowerLargeFreqWheelDec:
                            {
                                var key = radioKnob;
                                Dispatcher.BeginInvoke(
                                    (Action)delegate
                                    {
                                        LowerLargerLCDKnobDec.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsEmulator.UpperFreqSwitch:
                            {
                                var key = radioKnob;
                                Dispatcher.BeginInvoke(
                                    (Action)delegate
                                    {
                                        UpperRightSwitch.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsEmulator.LowerFreqSwitch:
                            {
                                var key = radioKnob;
                                Dispatcher.BeginInvoke(
                                    (Action)delegate
                                    {
                                        LowerRightSwitch.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
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
            TopLeftCom1.Visibility = Visibility.Collapsed;
            TopLeftCom2.Visibility = Visibility.Collapsed;
            TopLeftNav1.Visibility = Visibility.Collapsed;
            TopLeftNav2.Visibility = Visibility.Collapsed;
            TopLeftADF.Visibility = Visibility.Collapsed;
            TopLeftDME.Visibility = Visibility.Collapsed;
            TopLeftXPDR.Visibility = Visibility.Collapsed;
            LowerLeftCom1.Visibility = Visibility.Collapsed;
            LowerLeftCom2.Visibility = Visibility.Collapsed;
            LowerLeftNav1.Visibility = Visibility.Collapsed;
            LowerLeftNav2.Visibility = Visibility.Collapsed;
            LowerLeftADF.Visibility = Visibility.Collapsed;
            LowerLeftDME.Visibility = Visibility.Collapsed;
            LowerLeftXPDR.Visibility = Visibility.Collapsed;
            LowerLargerLCDKnobDec.Visibility = Visibility.Collapsed;
            UpperLargerLCDKnobInc.Visibility = Visibility.Collapsed;
            UpperRightSwitch.Visibility = Visibility.Collapsed;
            UpperSmallerLCDKnobDec.Visibility = Visibility.Collapsed;
            UpperSmallerLCDKnobInc.Visibility = Visibility.Collapsed;
            UpperLargerLCDKnobDec.Visibility = Visibility.Collapsed;
            LowerLargerLCDKnobInc.Visibility = Visibility.Collapsed;
            LowerRightSwitch.Visibility = Visibility.Collapsed;
            LowerSmallerLCDKnobDec.Visibility = Visibility.Collapsed;
            LowerSmallerLCDKnobInc.Visibility = Visibility.Collapsed;
        }

        private void ButtonGetId_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_radioPanelPZ69 != null)
                {
                    Clipboard.SetText(_radioPanelPZ69.InstanceId);
                    MessageBox.Show("The Instance Id for the panel has been copied to the Clipboard.");
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(2030, ex);
            }
        }

        private void ComboBoxFreqKnobSensitivity_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (_userControlLoaded)
                {
                    Settings.Default.RadioFrequencyKnobSensitivityEmulator = int.Parse(ComboBoxFreqKnobSensitivity.SelectedValue.ToString());
                    _radioPanelPZ69.FrequencyKnobSensitivity = int.Parse(ComboBoxFreqKnobSensitivity.SelectedValue.ToString());
                    Settings.Default.Save();
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(204330, ex);
            }
        }

        private RadioPanelPZ69KnobEmulator.RadioPanelPZ69KeyOnOff GetPZ69Key(TextBox textBox)
        {
            try
            {
                if (textBox.Equals(TextBoxUpperCom1))
                {
                    return new RadioPanelPZ69KnobEmulator.RadioPanelPZ69KeyOnOff(RadioPanelPZ69KnobsEmulator.UpperCOM1, true);
                }
                if (textBox.Equals(TextBoxUpperCom2))
                {
                    return new RadioPanelPZ69KnobEmulator.RadioPanelPZ69KeyOnOff(RadioPanelPZ69KnobsEmulator.UpperCOM2, true);
                }
                if (textBox.Equals(TextBoxUpperNav1))
                {
                    return new RadioPanelPZ69KnobEmulator.RadioPanelPZ69KeyOnOff(RadioPanelPZ69KnobsEmulator.UpperNAV1, true);
                }
                if (textBox.Equals(TextBoxUpperNav2))
                {
                    return new RadioPanelPZ69KnobEmulator.RadioPanelPZ69KeyOnOff(RadioPanelPZ69KnobsEmulator.UpperNAV2, true);
                }
                if (textBox.Equals(TextBoxUpperADF))
                {
                    return new RadioPanelPZ69KnobEmulator.RadioPanelPZ69KeyOnOff(RadioPanelPZ69KnobsEmulator.UpperADF, true);
                }
                if (textBox.Equals(TextBoxUpperDME))
                {
                    return new RadioPanelPZ69KnobEmulator.RadioPanelPZ69KeyOnOff(RadioPanelPZ69KnobsEmulator.UpperDME, true);
                }
                if (textBox.Equals(TextBoxUpperXPDR))
                {
                    return new RadioPanelPZ69KnobEmulator.RadioPanelPZ69KeyOnOff(RadioPanelPZ69KnobsEmulator.UpperXPDR, true);
                }
                if (textBox.Equals(TextBoxUpperLargePlus))
                {
                    return new RadioPanelPZ69KnobEmulator.RadioPanelPZ69KeyOnOff(RadioPanelPZ69KnobsEmulator.UpperLargeFreqWheelInc, true);
                }
                if (textBox.Equals(TextBoxUpperLargeMinus))
                {
                    return new RadioPanelPZ69KnobEmulator.RadioPanelPZ69KeyOnOff(RadioPanelPZ69KnobsEmulator.UpperLargeFreqWheelDec, true);
                }
                if (textBox.Equals(TextBoxUpperSmallPlus))
                {
                    return new RadioPanelPZ69KnobEmulator.RadioPanelPZ69KeyOnOff(RadioPanelPZ69KnobsEmulator.UpperSmallFreqWheelInc, true);
                }
                if (textBox.Equals(TextBoxUpperSmallMinus))
                {
                    return new RadioPanelPZ69KnobEmulator.RadioPanelPZ69KeyOnOff(RadioPanelPZ69KnobsEmulator.UpperSmallFreqWheelDec, true);
                }
                if (textBox.Equals(TextBoxUpperActStbyOn))
                {
                    return new RadioPanelPZ69KnobEmulator.RadioPanelPZ69KeyOnOff(RadioPanelPZ69KnobsEmulator.UpperFreqSwitch, true);
                }
                if (textBox.Equals(TextBoxUpperActStbyOff))
                {
                    return new RadioPanelPZ69KnobEmulator.RadioPanelPZ69KeyOnOff(RadioPanelPZ69KnobsEmulator.UpperFreqSwitch, false);
                }
                if (textBox.Equals(TextBoxLowerCom1))
                {
                    return new RadioPanelPZ69KnobEmulator.RadioPanelPZ69KeyOnOff(RadioPanelPZ69KnobsEmulator.LowerCOM1, true);
                }
                if (textBox.Equals(TextBoxLowerCom2))
                {
                    return new RadioPanelPZ69KnobEmulator.RadioPanelPZ69KeyOnOff(RadioPanelPZ69KnobsEmulator.LowerCOM2, true);
                }
                if (textBox.Equals(TextBoxLowerNav1))
                {
                    return new RadioPanelPZ69KnobEmulator.RadioPanelPZ69KeyOnOff(RadioPanelPZ69KnobsEmulator.LowerNAV1, true);
                }
                if (textBox.Equals(TextBoxLowerNav2))
                {
                    return new RadioPanelPZ69KnobEmulator.RadioPanelPZ69KeyOnOff(RadioPanelPZ69KnobsEmulator.LowerNAV2, true);
                }
                if (textBox.Equals(TextBoxLowerADF))
                {
                    return new RadioPanelPZ69KnobEmulator.RadioPanelPZ69KeyOnOff(RadioPanelPZ69KnobsEmulator.LowerADF, true);
                }
                if (textBox.Equals(TextBoxLowerDME))
                {
                    return new RadioPanelPZ69KnobEmulator.RadioPanelPZ69KeyOnOff(RadioPanelPZ69KnobsEmulator.LowerDME, true);
                }
                if (textBox.Equals(TextBoxLowerXPDR))
                {
                    return new RadioPanelPZ69KnobEmulator.RadioPanelPZ69KeyOnOff(RadioPanelPZ69KnobsEmulator.LowerXPDR, true);
                }
                if (textBox.Equals(TextBoxLowerLargePlus))
                {
                    return new RadioPanelPZ69KnobEmulator.RadioPanelPZ69KeyOnOff(RadioPanelPZ69KnobsEmulator.LowerLargeFreqWheelInc, true);
                }
                if (textBox.Equals(TextBoxLowerLargeMinus))
                {
                    return new RadioPanelPZ69KnobEmulator.RadioPanelPZ69KeyOnOff(RadioPanelPZ69KnobsEmulator.LowerLargeFreqWheelDec, true);
                }
                if (textBox.Equals(TextBoxLowerSmallPlus))
                {
                    return new RadioPanelPZ69KnobEmulator.RadioPanelPZ69KeyOnOff(RadioPanelPZ69KnobsEmulator.LowerSmallFreqWheelInc, true);
                }
                if (textBox.Equals(TextBoxLowerSmallMinus))
                {
                    return new RadioPanelPZ69KnobEmulator.RadioPanelPZ69KeyOnOff(RadioPanelPZ69KnobsEmulator.LowerSmallFreqWheelDec, true);
                }
                if (textBox.Equals(TextBoxLowerActStbyOn))
                {
                    return new RadioPanelPZ69KnobEmulator.RadioPanelPZ69KeyOnOff(RadioPanelPZ69KnobsEmulator.LowerFreqSwitch, true);
                }
                if (textBox.Equals(TextBoxLowerActStbyOff))
                {
                    return new RadioPanelPZ69KnobEmulator.RadioPanelPZ69KeyOnOff(RadioPanelPZ69KnobsEmulator.LowerFreqSwitch, false);
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(345012, ex);
            }
            throw new Exception("Should not reach this point");
        }

    }
}
