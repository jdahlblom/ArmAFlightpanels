using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HidLibrary;
using System.Threading;
using ClassLibraryCommon;

namespace NonVisuals
{
    public class MultiPanelPZ70ArmA : SaitekPanel, IRedisDataListener
    {
        private int _lcdKnobSensitivity;
        private volatile byte _knobSensitivitySkipper;
        private HashSet<KnobBindingArmaPZ70> _knobBindings = new HashSet<KnobBindingArmaPZ70>();
        private HashSet<MultiPanelKnob> _multiPanelKnobs = new HashSet<MultiPanelKnob>();
        private HashSet<BIPLinkPZ70> _bipLinks = new HashSet<BIPLinkPZ70>();
        private bool _isFirstNotification = true;
        private readonly byte[] _oldMultiPanelValue = { 0, 0, 0 };
        private readonly byte[] _newMultiPanelValue = { 0, 0, 0 };
        private PZ70DialPosition _pz70DialPosition = PZ70DialPosition.ALT;
        //ALT =>  ALT & VS Visible
        //VS  =>  ALT & VS Visible
        //IAS =>  IAS Visible
        //HDG =>  HDG Visible
        //CRS =>  CRS Visible
        private readonly object _lcdLockObject = new object();
        private readonly object _lcdDataVariablesLockObject = new object();
        private readonly PZ70LCDArmAButtonByte _lcdButtonByte = new PZ70LCDArmAButtonByte();

        private readonly bool[] _buttonStatesForRedis = new bool[13] { false, false, false, false, false, false, false, false, false, false, false, false, false };
        //0 - 40000
        private int _altLCDArmAValue = 0;
        //-6000 - 6000
        private int _vsLCDArmAValue = 0;
        //0-600
        private int _iasLCDArmAValue = 0;
        //0-360
        private int _hdgLCDArmAValue = 0;
        //0-360
        private int _crsLCDArmAValue = 0;

        /*private ClickSpeedDetector _altLCDKeyEmulatorValueChangeMonitor = new ClickSpeedDetector(15);
        private ClickSpeedDetector _vsLCDKeyEmulatorValueChangeMonitor = new ClickSpeedDetector(15);
        private ClickSpeedDetector _iasLCDKeyEmulatorValueChangeMonitor = new ClickSpeedDetector(15);
        private ClickSpeedDetector _hdgLCDKeyEmulatorValueChangeMonitor = new ClickSpeedDetector(15);
        private ClickSpeedDetector _crsLCDKeyEmulatorValueChangeMonitor = new ClickSpeedDetector(15);*/

        private long _doUpdatePanelLCD;

        public MultiPanelPZ70ArmA(HIDSkeleton hidSkeleton) : base(SaitekPanelsEnum.PZ70MultiPanel, hidSkeleton)
        {
            VendorId = 0x6A3;
            ProductId = 0xD06;
            CreateMultiKnobs();
            RedisManager.AddRedisDataListener(this);
            Startup();
        }

        public sealed override void Startup()
        {
            try
            {
                if (HIDSkeletonBase.HIDReadDevice != null && !Closed)
                {
                    HIDSkeletonBase.HIDReadDevice.ReadReport(OnReport);
                }
            }
            catch (Exception ex)
            {
                Common.DebugP("MultiPanelPZ70ArmA.StartUp() : " + ex.Message);
                SetLastException(ex);
            }
        }


        public override void Shutdown()
        {
            try
            {
                Closed = true;
                RedisManager.RemoveRedisDataListener(this);
            }
            catch (Exception e)
            {
                SetLastException(e);
            }
        }

        public override void ImportSettings(List<string> settings)
        {
            //Clear current bindings
            ClearSettings();
            if (settings == null || settings.Count == 0)
            {
                return;
            }

            foreach (var setting in settings)
            {
                if (!setting.StartsWith("#") && setting.Length > 2 && setting.Contains(InstanceId) && setting.Contains(SettingsVersion()))
                {
                    if (setting.StartsWith("MultiPanelKnobArmA{"))
                    {
                        var knobBinding = new KnobBindingArmaPZ70();
                        knobBinding.ImportSettings(setting);
                        _knobBindings.Add(knobBinding);
                    }
                    else if (setting.StartsWith("MultipanelBIPLink{"))
                    {
                        var bipLinkPZ70 = new BIPLinkPZ70();
                        bipLinkPZ70.ImportSettings(setting);
                        _bipLinks.Add(bipLinkPZ70);
                    }
                }
            }
            OnSettingsApplied();
        }

        public void RedisDataAvailable(object sender, RedisDataListenerEventArgs args)
        {
            try
            {
                _altLCDArmAValue = args.Altitude;
                _vsLCDArmAValue = args.VerticalSpeed;
                _iasLCDArmAValue = args.IndicatedAirspeed;
                _hdgLCDArmAValue = args.Heading;
                _crsLCDArmAValue = args.Course;
                UpdateLCD();
            }
            catch (Exception e)
            {
                SetLastException(e);
            }
        }

        public override List<string> ExportSettings()
        {
            if (Closed)
            {
                return null;
            }
            var result = new List<string>();

            foreach (var knobBinding in _knobBindings)
            {
                if (knobBinding.OSKeyPress != null)
                {
                    result.Add(knobBinding.ExportSettings());
                }
            }
            foreach (var bipLink in _bipLinks)
            {
                if (bipLink.BIPLights.Count > 0)
                {
                    result.Add(bipLink.ExportSettings());
                }
            }
            return result;
        }

        public string GetKeyPressForLoggingPurposes(MultiPanelKnob multiPanelKnob)
        {
            var result = "";
            foreach (var knobBinding in _knobBindings)
            {
                if (knobBinding.OSKeyPress != null && knobBinding.MultiPanelPZ70Knob == multiPanelKnob.MultiPanelPZ70Knob && knobBinding.WhenTurnedOn == multiPanelKnob.IsOn)
                {
                    result = knobBinding.OSKeyPress.GetNonFunctioningVirtualKeyCodesAsString();
                }
            }
            return result;
        }

        public override void SavePanelSettings(object sender, ProfileHandlerEventArgs e)
        {
            e.ProfileHandlerEA.RegisterProfileData(this, ExportSettings());
        }

        public override void ClearSettings()
        {
            _knobBindings.Clear();
            _bipLinks.Clear();
        }

        private void OnReport(HidReport report)
        {
            //if (IsAttached == false) { return; }

            if (report.Data.Length == 3)
            {
                Array.Copy(_newMultiPanelValue, _oldMultiPanelValue, 3);
                Array.Copy(report.Data, _newMultiPanelValue, 3);
                var hashSet = GetHashSetOfChangedKnobs(_oldMultiPanelValue, _newMultiPanelValue);

                //Set _selectedMode and LCD button statuses
                //and performs the actual actions for key presses
                PZ70SwitchChanged(hashSet);
                //Sends event
                OnSwitchesChanged(hashSet);

                try
                {
                    RedisManager.SendRedisData("DCS_MultiPanelInput", GetRedisButtonDataAsString());
                }
                catch (Exception e)
                {
                    SetLastException(e);
                }

                _isFirstNotification = false;
                if (Common.DebugOn)
                {
                    var stringBuilder = new StringBuilder();
                    for (var i = 0; i < report.Data.Length; i++)
                    {
                        stringBuilder.Append(Convert.ToString(report.Data[i], 2).PadLeft(8, '0') + "  ");
                    }
                    Common.DebugP(stringBuilder.ToString());
                    if (hashSet.Count > 0)
                    {
                        Common.DebugP("\nFollowing knobs has been changed:\n");
                        foreach (var multiPanelKnob in hashSet)
                        {
                            Common.DebugP(((MultiPanelKnob)multiPanelKnob).MultiPanelPZ70Knob + ", value is " + FlagValue(_newMultiPanelValue, ((MultiPanelKnob)multiPanelKnob)));
                        }
                    }
                }
                Common.DebugP("\r\nDone!\r\n");
            }
            try
            {
                if (HIDSkeletonBase.HIDReadDevice != null && !Closed)
                {
                    Common.DebugP("Adding callback " + TypeOfSaitekPanel + " " + GuidString);
                    HIDSkeletonBase.HIDReadDevice.ReadReport(OnReport);
                }
            }
            catch (Exception ex)
            {
                Common.DebugP(ex.Message + "\n" + ex.StackTrace);
            }
        }

        private string GetRedisButtonDataAsString()
        {
            var result = string.Join(", ", _buttonStatesForRedis.Select(b => b ? "1" : "0").ToArray());
            return result;
        }

        public void AddOrUpdateSingleKeyBinding(MultiPanelPZ70Knobs multiPanelPZ70Knob, string keys, KeyPressLength keyPressLength, bool whenTurnedOn = true)
        {
            if (string.IsNullOrEmpty(keys))
            {
                RemoveMultiPanelKnobFromList(ControlListPZ70.KEYS, multiPanelPZ70Knob, whenTurnedOn);
                IsDirtyMethod();
                return;
            }
            //This must accept lists
            var found = false;
            foreach (var knobBinding in _knobBindings)
            {
                if (knobBinding.MultiPanelPZ70Knob == multiPanelPZ70Knob && knobBinding.WhenTurnedOn == whenTurnedOn)
                {
                    if (string.IsNullOrEmpty(keys))
                    {
                        knobBinding.OSKeyPress = null;
                    }
                    else
                    {
                        knobBinding.OSKeyPress = new OSKeyPress(keys, keyPressLength);
                        knobBinding.WhenTurnedOn = whenTurnedOn;
                    }
                    found = true;
                }
            }
            if (!found && !string.IsNullOrEmpty(keys))
            {
                var knobBinding = new KnobBindingArmaPZ70();
                knobBinding.MultiPanelPZ70Knob = multiPanelPZ70Knob;
                knobBinding.OSKeyPress = new OSKeyPress(keys, keyPressLength);
                knobBinding.WhenTurnedOn = whenTurnedOn;
                _knobBindings.Add(knobBinding);
            }
            Common.DebugP("MultiPanelPZ70ArmA _knobBindings : " + _knobBindings.Count);
            IsDirtyMethod();
        }

        public void AddOrUpdateSequencedKeyBinding(string information, MultiPanelPZ70Knobs multiPanelPZ70Knob, SortedList<int, KeyPressInfo> sortedList, bool whenTurnedOn = true)
        {
            //This must accept lists
            var found = false;
            RemoveMultiPanelKnobFromList(ControlListPZ70.KEYS, multiPanelPZ70Knob, whenTurnedOn);
            foreach (var knobBinding in _knobBindings)
            {
                if (knobBinding.MultiPanelPZ70Knob == multiPanelPZ70Knob && knobBinding.WhenTurnedOn == whenTurnedOn)
                {
                    if (sortedList.Count == 0)
                    {
                        knobBinding.OSKeyPress = null;
                    }
                    else
                    {
                        knobBinding.OSKeyPress = new OSKeyPress(information, sortedList);
                        knobBinding.WhenTurnedOn = whenTurnedOn;
                    }
                    found = true;
                    break;
                }
            }
            if (!found && sortedList.Count > 0)
            {
                var knobBinding = new KnobBindingArmaPZ70();
                knobBinding.MultiPanelPZ70Knob = multiPanelPZ70Knob;
                knobBinding.OSKeyPress = new OSKeyPress(information, sortedList);
                knobBinding.WhenTurnedOn = whenTurnedOn;
                _knobBindings.Add(knobBinding);
            }
            IsDirtyMethod();
        }

        public BIPLinkPZ70 AddOrUpdateBIPLinkKnobBinding(MultiPanelPZ70Knobs multiPanelKnob, BIPLinkPZ70 bipLinkPZ70, bool whenTurnedOn = true)
        {
            //This must accept lists
            var found = false;
            BIPLinkPZ70 tmpBIPLinkPZ70 = null;

            RemoveMultiPanelKnobFromList(ControlListPZ70.BIPS, multiPanelKnob, whenTurnedOn);
            foreach (var bipLink in _bipLinks)
            {
                if (bipLink.MultiPanelPZ70Knob == multiPanelKnob && bipLink.WhenTurnedOn == whenTurnedOn)
                {
                    bipLink.BIPLights = bipLinkPZ70.BIPLights;
                    bipLink.Description = bipLinkPZ70.Description;
                    bipLink.MultiPanelPZ70Knob = multiPanelKnob;
                    bipLink.WhenTurnedOn = whenTurnedOn;
                    tmpBIPLinkPZ70 = bipLink;
                    found = true;
                    break;
                }
            }
            if (!found && bipLinkPZ70.BIPLights.Count > 0)
            {
                bipLinkPZ70.MultiPanelPZ70Knob = multiPanelKnob;
                bipLinkPZ70.WhenTurnedOn = whenTurnedOn;
                tmpBIPLinkPZ70 = bipLinkPZ70;
                _bipLinks.Add(bipLinkPZ70);
            }
            IsDirtyMethod();
            return tmpBIPLinkPZ70;
        }

        public void RemoveMultiPanelKnobFromList(ControlListPZ70 controlListPZ70, MultiPanelPZ70Knobs multiPanelPZ70Knob, bool whenTurnedOn = true)
        {
            var found = false;
            if (controlListPZ70 == ControlListPZ70.ALL || controlListPZ70 == ControlListPZ70.KEYS)
            {
                foreach (var knobBindingPZ70 in _knobBindings)
                {
                    if (knobBindingPZ70.MultiPanelPZ70Knob == multiPanelPZ70Knob && knobBindingPZ70.WhenTurnedOn == whenTurnedOn)
                    {
                        knobBindingPZ70.OSKeyPress = null;
                        found = true;
                    }
                }
            }
            if (controlListPZ70 == ControlListPZ70.ALL || controlListPZ70 == ControlListPZ70.BIPS)
            {
                foreach (var bipLink in _bipLinks)
                {
                    if (bipLink.MultiPanelPZ70Knob == multiPanelPZ70Knob && bipLink.WhenTurnedOn == whenTurnedOn)
                    {
                        bipLink.BIPLights.Clear();
                        found = true;
                    }
                }
            }

            if (found)
            {
                IsDirtyMethod();
            }
        }

        private void PZ70SwitchChanged(IEnumerable<object> hashSet)
        {
            foreach (var o in hashSet)
            {
                var multiPanelKnob = (MultiPanelKnob)o;
                if (multiPanelKnob.IsOn)
                {
                    switch (multiPanelKnob.MultiPanelPZ70Knob)
                    {
                        case MultiPanelPZ70Knobs.KNOB_ALT:
                            {
                                _pz70DialPosition = PZ70DialPosition.ALT;
                                Interlocked.Add(ref _doUpdatePanelLCD, 1);
                                UpdateLCD();
                                break;
                            }
                        case MultiPanelPZ70Knobs.KNOB_VS:
                            {
                                _pz70DialPosition = PZ70DialPosition.VS;
                                Interlocked.Add(ref _doUpdatePanelLCD, 1);
                                UpdateLCD();
                                break;
                            }
                        case MultiPanelPZ70Knobs.KNOB_IAS:
                            {
                                _pz70DialPosition = PZ70DialPosition.IAS;
                                Interlocked.Add(ref _doUpdatePanelLCD, 1);
                                UpdateLCD();
                                break;
                            }
                        case MultiPanelPZ70Knobs.KNOB_HDG:
                            {
                                _pz70DialPosition = PZ70DialPosition.HDG;
                                Interlocked.Add(ref _doUpdatePanelLCD, 1);
                                UpdateLCD();
                                break;
                            }
                        case MultiPanelPZ70Knobs.KNOB_CRS:
                            {
                                _pz70DialPosition = PZ70DialPosition.CRS;
                                Interlocked.Add(ref _doUpdatePanelLCD, 1);
                                UpdateLCD();
                                break;
                            }
                        case MultiPanelPZ70Knobs.AP_BUTTON:
                            {
                                multiPanelKnob.IsOn = _lcdButtonByte.FlipButton(MultiPanelPZ70Knobs.AP_BUTTON);
                                _buttonStatesForRedis[5] = multiPanelKnob.IsOn;
                                Interlocked.Add(ref _doUpdatePanelLCD, 1);
                                break;
                            }
                        case MultiPanelPZ70Knobs.HDG_BUTTON:
                            {
                                multiPanelKnob.IsOn = _lcdButtonByte.FlipButton(MultiPanelPZ70Knobs.HDG_BUTTON);
                                _buttonStatesForRedis[6] = multiPanelKnob.IsOn;
                                Interlocked.Add(ref _doUpdatePanelLCD, 1);
                                break;
                            }
                        case MultiPanelPZ70Knobs.NAV_BUTTON:
                            {
                                multiPanelKnob.IsOn = _lcdButtonByte.FlipButton(MultiPanelPZ70Knobs.NAV_BUTTON);
                                _buttonStatesForRedis[7] = multiPanelKnob.IsOn;
                                Interlocked.Add(ref _doUpdatePanelLCD, 1);
                                break;
                            }
                        case MultiPanelPZ70Knobs.IAS_BUTTON:
                            {
                                multiPanelKnob.IsOn = _lcdButtonByte.FlipButton(MultiPanelPZ70Knobs.IAS_BUTTON);
                                _buttonStatesForRedis[8] = multiPanelKnob.IsOn;
                                Interlocked.Add(ref _doUpdatePanelLCD, 1);
                                break;
                            }
                        case MultiPanelPZ70Knobs.ALT_BUTTON:
                            {
                                multiPanelKnob.IsOn = _lcdButtonByte.FlipButton(MultiPanelPZ70Knobs.ALT_BUTTON);
                                _buttonStatesForRedis[9] = multiPanelKnob.IsOn;
                                Interlocked.Add(ref _doUpdatePanelLCD, 1);
                                break;
                            }
                        case MultiPanelPZ70Knobs.VS_BUTTON:
                            {
                                multiPanelKnob.IsOn = _lcdButtonByte.FlipButton(MultiPanelPZ70Knobs.VS_BUTTON);
                                _buttonStatesForRedis[10] = multiPanelKnob.IsOn;
                                Interlocked.Add(ref _doUpdatePanelLCD, 1);
                                break;
                            }
                        case MultiPanelPZ70Knobs.APR_BUTTON:
                            {
                                multiPanelKnob.IsOn = _lcdButtonByte.FlipButton(MultiPanelPZ70Knobs.APR_BUTTON);
                                _buttonStatesForRedis[11] = multiPanelKnob.IsOn;
                                Interlocked.Add(ref _doUpdatePanelLCD, 1);
                                break;
                            }
                        case MultiPanelPZ70Knobs.REV_BUTTON:
                            {
                                multiPanelKnob.IsOn = _lcdButtonByte.FlipButton(MultiPanelPZ70Knobs.REV_BUTTON);
                                _buttonStatesForRedis[12] = multiPanelKnob.IsOn;
                                Interlocked.Add(ref _doUpdatePanelLCD, 1);
                                break;
                            }
                    }
                }
            }
            UpdateLCD();
            if (!ForwardKeyPresses)
            {
                return;
            }

            foreach (var o in hashSet)
            {
                var multiPanelKnob = (MultiPanelKnob)o;
                /*
                 * IMPORTANT
                 * ---------
                 * The LCD buttons toggle between on and off. It is the toggle value that defines if the button is OFF, not the fact that the user releases the button.
                 * Therefore the forementioned buttons cannot be used as usual in a loop with knobBinding.WhenTurnedOn
                 * Instead the buttons global bool value must be used!
                 * 
                 */
                if (KeyboardEmulationOnly && multiPanelKnob.MultiPanelPZ70Knob == MultiPanelPZ70Knobs.LCD_WHEEL_INC || multiPanelKnob.MultiPanelPZ70Knob == MultiPanelPZ70Knobs.LCD_WHEEL_DEC)
                {
                    Interlocked.Add(ref _doUpdatePanelLCD, 1);
                    LCDDialChangesHandle(multiPanelKnob);
                    UpdateLCD();
                }

                foreach (var knobBinding in _knobBindings)
                {
                    if (knobBinding.OSKeyPress != null && knobBinding.MultiPanelPZ70Knob == multiPanelKnob.MultiPanelPZ70Knob && knobBinding.WhenTurnedOn == multiPanelKnob.IsOn)
                    {
                        if (knobBinding.MultiPanelPZ70Knob == MultiPanelPZ70Knobs.LCD_WHEEL_INC || knobBinding.MultiPanelPZ70Knob == MultiPanelPZ70Knobs.LCD_WHEEL_DEC)
                        {
                            if (!SkipCurrentLcdKnobChange())
                            {
                                knobBinding.OSKeyPress.Execute();
                            }
                        }
                        else
                        {
                            knobBinding.OSKeyPress.Execute();
                        }
                        break;
                    }
                }
                foreach (var bipLinkPZ70 in _bipLinks)
                {
                    if (bipLinkPZ70.BIPLights.Count > 0 && bipLinkPZ70.MultiPanelPZ70Knob == multiPanelKnob.MultiPanelPZ70Knob && bipLinkPZ70.WhenTurnedOn == multiPanelKnob.IsOn)
                    {
                        bipLinkPZ70.Execute();
                        break;
                    }
                }
            }
        }

        private void LCDDialChangesHandle(MultiPanelKnob multiPanelKnob)
        {
            if (SkipCurrentLcdKnobChangeLCD(true))
            {
                return;
            }

            bool increase = multiPanelKnob.MultiPanelPZ70Knob == MultiPanelPZ70Knobs.LCD_WHEEL_INC;
            
            /*switch (_pz70DialPosition)
            {
                case PZ70DialPosition.ALT:
                    {
                        _altLCDKeyEmulatorValueChangeMonitor.Click();
                        if (_altLCDKeyEmulatorValueChangeMonitor.ClickThresholdReached())
                        {
                            if (increase)
                            {
                                ChangeAltLCDValue(500);
                            }
                            else
                            {
                                ChangeAltLCDValue(-500);
                            }
                        }
                        else
                        {
                            if (increase)
                            {
                                ChangeAltLCDValue(100);
                            }
                            else
                            {
                                ChangeAltLCDValue(-100);
                            }
                        }
                        break;
                    }
                case PZ70DialPosition.VS:
                    {
                        _vsLCDKeyEmulatorValueChangeMonitor.Click();
                        if (_vsLCDKeyEmulatorValueChangeMonitor.ClickThresholdReached())
                        {
                            if (increase)
                            {
                                ChangeVsLCDValue(100);
                            }
                            else
                            {
                                ChangeVsLCDValue(-100);
                            }
                        }
                        else
                        {
                            if (increase)
                            {
                                ChangeVsLCDValue(10);
                            }
                            else
                            {
                                ChangeVsLCDValue(-10);
                            }
                        }
                        break;
                    }
                case PZ70DialPosition.IAS:
                    {
                        _iasLCDKeyEmulatorValueChangeMonitor.Click();
                        if (_iasLCDKeyEmulatorValueChangeMonitor.ClickThresholdReached())
                        {
                            if (increase)
                            {
                                ChangeIasLCDValue(50);
                            }
                            else
                            {
                                ChangeIasLCDValue(-50);
                            }
                        }
                        else
                        {
                            if (increase)
                            {
                                ChangeIasLCDValue(5);
                            }
                            else
                            {
                                ChangeIasLCDValue(-5);
                            }
                        }
                        break;
                    }
                case PZ70DialPosition.HDG:
                    {
                        _hdgLCDKeyEmulatorValueChangeMonitor.Click();
                        if (_hdgLCDKeyEmulatorValueChangeMonitor.ClickThresholdReached())
                        {
                            if (increase)
                            {
                                ChangeHdgLCDValue(5);
                            }
                            else
                            {
                                ChangeHdgLCDValue(-5);
                            }
                        }
                        else
                        {
                            if (increase)
                            {
                                ChangeHdgLCDValue(1);
                            }
                            else
                            {
                                ChangeHdgLCDValue(-1);
                            }
                        }
                        break;
                    }
                case PZ70DialPosition.CRS:
                    {
                        _crsLCDKeyEmulatorValueChangeMonitor.Click();
                        if (_crsLCDKeyEmulatorValueChangeMonitor.ClickThresholdReached())
                        {
                            if (increase)
                            {
                                ChangeCrsLCDValue(5);
                            }
                            else
                            {
                                ChangeCrsLCDValue(-5);
                            }
                        }
                        else
                        {
                            if (increase)
                            {
                                ChangeCrsLCDValue(1);
                            }
                            else
                            {
                                ChangeCrsLCDValue(-1);
                            }
                        }
                        break;
                    }
            }*/
            }

        protected bool SkipCurrentLcdKnobChange(bool change = true)
        {
            switch (_lcdKnobSensitivity)
            {
                case 0:
                    {
                        //Do nothing all manipulation is let through
                        break;
                    }
                case -1:
                    {
                        //Skip every 2 manipulations
                        if (change)
                        {
                            _knobSensitivitySkipper++;
                        }

                        if (_knobSensitivitySkipper <= 2)
                        {
                            return true;
                        }
                        _knobSensitivitySkipper = 0;
                        break;
                    }
                case -2:
                    {
                        //Skip every 4 manipulations
                        if (change)
                        {
                            _knobSensitivitySkipper++;
                        }
                        if (_knobSensitivitySkipper <= 4)
                        {
                            return true;
                        }
                        _knobSensitivitySkipper = 0;
                        break;
                    }
            }
            return false;
        }

        protected bool SkipCurrentLcdKnobChangeLCD(bool change = true)
        {
            //Skip every 3 manipulations
            if (change)
            {
                _knobSensitivitySkipper++;
            }

            if (_knobSensitivitySkipper <= 3)
            {
                return true;
            }
            _knobSensitivitySkipper = 0;
            return false;
        }

        public void ClearAllBindings(MultiPanelPZ70KnobOnOff multiPanelPZ70KnobOnOff)
        {
            //This must accept lists
            foreach (var knobBinding in _knobBindings)
            {
                if (knobBinding.MultiPanelPZ70Knob == multiPanelPZ70KnobOnOff.MultiPanelPZ70Knob && knobBinding.WhenTurnedOn == multiPanelPZ70KnobOnOff.On)
                {
                    knobBinding.OSKeyPress = null;
                }
            }
            Common.DebugP("MultiPanelPZ70ArmA _knobBindings : " + _knobBindings.Count);
            IsDirtyMethod();
        }


        public void UpdateLCD()
        {
            //345
            //15600
            //
            //[0x0]
            //[1] [2] [3] [4] [5]
            //[6] [7] [8] [9] [10]
            //[11 BUTTONS]


            /*if (Interlocked.Read(ref _doUpdatePanelLCD) == 0)
            {
                return;
            }*/
            var bytes = new byte[12];
            bytes[0] = 0x0;
            for (var ii = 1; ii < bytes.Length - 1; ii++)
            {
                bytes[ii] = 0xFF;
            }

            bytes[11] = _lcdButtonByte.GetButtonByte();

            bool foundUpperValue = false;
            bool foundLowerValue = false;

            var upperValue = 0;
            var lowerValue = 0;
            lock (_lcdDataVariablesLockObject)
            {
                if (KeyboardEmulationOnly)
                {
                    switch (_pz70DialPosition)
                    {
                        case PZ70DialPosition.ALT:
                            {
                                if (ALTButtonPressed())
                                {
                                    upperValue = _altLCDArmAValue;
                                    foundUpperValue = true;
                                }
                                if (VSButtonPressed())
                                {
                                    lowerValue = Convert.ToInt32(_vsLCDArmAValue);
                                    foundLowerValue = true;
                                }
                                break;
                            }
                        case PZ70DialPosition.VS:
                            {
                                if (ALTButtonPressed())
                                {
                                    upperValue = _altLCDArmAValue;
                                    foundUpperValue = true;
                                }
                                if (VSButtonPressed())
                                {
                                    lowerValue = Convert.ToInt32(_vsLCDArmAValue);
                                    foundLowerValue = true;
                                }
                                break;
                            }
                        case PZ70DialPosition.IAS:
                            {
                                if (IASButtonPressed())
                                {
                                    upperValue = _iasLCDArmAValue;
                                    foundUpperValue = true;
                                }
                                break;
                            }
                        case PZ70DialPosition.HDG:
                            {
                                if (HDGButtonPressed())
                                {
                                    upperValue = _hdgLCDArmAValue;
                                    foundUpperValue = true;
                                }
                                break;
                            }
                        case PZ70DialPosition.CRS:
                            {
                                if (NavButtonPressed() && APRButtonPressed() && REVButtonPressed())
                                {
                                    upperValue = _crsLCDArmAValue;
                                    foundUpperValue = true;
                                }
                                break;
                            }
                    }
                }
            }

            if (foundUpperValue)
            {
                if (upperValue < 0)
                {
                    upperValue = Math.Abs(upperValue);
                }
                var dataAsString = upperValue.ToString();

                var i = dataAsString.Length;
                var arrayPosition = 5;
                do
                {
                    //    3 0 0
                    //1 5 6 0 0
                    //1 2 3 4 5    
                    bytes[arrayPosition] = (byte)dataAsString[i - 1];
                    arrayPosition--;
                    i--;
                } while (i > 0);
            }
            if (foundLowerValue)
            {
                //Important!
                //Lower LCD will show a dash "-" for 0xEE.
                //Smallest negative value that can be shown is -9999
                //Largest positive value that can be shown is 99999
                if (lowerValue < -9999)
                {
                    lowerValue = -9999;
                }
                var dataAsString = lowerValue.ToString();
                //Console.WriteLine(dataAsString);
                var i = dataAsString.Length;
                var arrayPosition = 10;
                do
                {
                    //    3 0 0
                    //1 5 6 0 0
                    //1 2 3 4 5    
                    var s = dataAsString[i - 1];
                    if (s == '-')
                    {
                        bytes[arrayPosition] = 0xEE;
                    }
                    else
                    {
                        bytes[arrayPosition] = (byte)s;
                    }
                    arrayPosition--;
                    i--;
                } while (i > 0);
            }
            lock (_lcdLockObject)
            {
                SendLEDData(bytes);
            }
            Interlocked.Add(ref _doUpdatePanelLCD, -1);
        }

        public void SendLEDData(byte[] array)
        {
            try
            {
                if (HIDSkeletonBase.HIDWriteDevice != null)
                {
                    Common.DebugP("HIDWriteDevice writing feature data " + TypeOfSaitekPanel + " " + GuidString);
                    HIDSkeletonBase.HIDWriteDevice.WriteFeatureData(array);
                }
            }
            catch (Exception e)
            {
                Common.DebugP("SendLEDData() :\n" + e.Message + e.StackTrace);
                SetLastException(e);
            }
        }

        private HashSet<object> GetHashSetOfChangedKnobs(byte[] oldValue, byte[] newValue)
        {
            var result = new HashSet<object>();
            for (var i = 0; i < 3; i++)
            {
                var oldByte = oldValue[i];
                var newByte = newValue[i];

                foreach (var multiPanelKnob in _multiPanelKnobs)
                {
                    if (multiPanelKnob.Group == i && (FlagHasChanged(oldByte, newByte, multiPanelKnob.Mask) || _isFirstNotification))
                    {
                        multiPanelKnob.IsOn = FlagValue(newValue, multiPanelKnob);
                        switch (multiPanelKnob.MultiPanelPZ70Knob)
                        {
                            case MultiPanelPZ70Knobs.KNOB_ALT:
                                {
                                    _buttonStatesForRedis[0] = multiPanelKnob.IsOn;
                                    break;
                                }
                            case MultiPanelPZ70Knobs.KNOB_VS:
                                {
                                    _buttonStatesForRedis[1] = multiPanelKnob.IsOn;
                                    break;
                                }
                            case MultiPanelPZ70Knobs.KNOB_IAS:
                                {
                                    _buttonStatesForRedis[2] = multiPanelKnob.IsOn;
                                    break;
                                }
                            case MultiPanelPZ70Knobs.KNOB_HDG:
                                {
                                    _buttonStatesForRedis[3] = multiPanelKnob.IsOn;
                                    break;
                                }
                            case MultiPanelPZ70Knobs.KNOB_CRS:
                                {
                                    _buttonStatesForRedis[4] = multiPanelKnob.IsOn;
                                    break;
                                }
                        }
                        //Do not add OFF signals for LCD buttons. They are not used. The OFF value is a TOGGLE value and respective button's bool value must be read instead.
                        if (!multiPanelKnob.IsOn)
                        {
                            switch (multiPanelKnob.MultiPanelPZ70Knob)
                            {
                                case MultiPanelPZ70Knobs.AP_BUTTON:
                                case MultiPanelPZ70Knobs.HDG_BUTTON:
                                case MultiPanelPZ70Knobs.NAV_BUTTON:
                                case MultiPanelPZ70Knobs.IAS_BUTTON:
                                case MultiPanelPZ70Knobs.ALT_BUTTON:
                                case MultiPanelPZ70Knobs.VS_BUTTON:
                                case MultiPanelPZ70Knobs.APR_BUTTON:
                                case MultiPanelPZ70Knobs.REV_BUTTON:
                                    {
                                        //Do not add OFF values for these buttons! Read comment above.
                                        break;
                                    }
                                default:
                                    {
                                        result.Add(multiPanelKnob);
                                        break;
                                    }
                            }
                        }
                        else
                        {
                            result.Add(multiPanelKnob);
                        }
                    }
                }
            }
            return result;
        }

        private void IsDirtyMethod()
        {
            OnSettingsChanged();
            IsDirty = true;
        }

        private void CreateMultiKnobs()
        {
            _multiPanelKnobs = MultiPanelKnob.GetMultiPanelKnobs();
        }

        private static bool FlagValue(byte[] currentValue, MultiPanelKnob multiPanelKnob)
        {
            return (currentValue[multiPanelKnob.Group] & multiPanelKnob.Mask) > 0;
        }

        private void DeviceAttachedHandler()
        {
            Startup();
            //IsAttached = true;
        }

        private void DeviceRemovedHandler()
        {
            Shutdown();
            //IsAttached = false;
        }

        private bool ApButtonPressed()
        {
            return _buttonStatesForRedis[5];
        }

        private bool HDGButtonPressed()
        {
            return _buttonStatesForRedis[6];
        }

        private bool NavButtonPressed()
        {
            return _buttonStatesForRedis[7];
        }

        private bool IASButtonPressed()
        {
            return _buttonStatesForRedis[8];
        }

        private bool ALTButtonPressed()
        {
            return _buttonStatesForRedis[9];
        }

        private bool VSButtonPressed()
        {
            return _buttonStatesForRedis[10];
        }

        private bool APRButtonPressed()
        {
            return _buttonStatesForRedis[11];
        }

        private bool REVButtonPressed()
        {
            return _buttonStatesForRedis[12];
        }

        public HashSet<KnobBindingArmaPZ70> KeyBindings
        {
            get { return _knobBindings; }
            set { _knobBindings = value; }
        }

        public HashSet<BIPLinkPZ70> BIPLinkHashSet
        {
            get { return _bipLinks; }
            set { _bipLinks = value; }
        }

        public HashSet<MultiPanelKnob> MultiPanelKnobs
        {
            get { return _multiPanelKnobs; }
            set { _multiPanelKnobs = value; }
        }

        public HashSet<KnobBindingArmaPZ70> KeyBindingsHashSet
        {
            get { return _knobBindings; }
            set { _knobBindings = value; }
        }

        public int LCDKnobSensitivity
        {
            get { return _lcdKnobSensitivity; }
            set { _lcdKnobSensitivity = value; }
        }

        public PZ70DialPosition PZ70_DialPosition
        {
            get => _pz70DialPosition;
            set => _pz70DialPosition = value;
        }

        public override String SettingsVersion()
        {
            return "2X";
        }

        public PZ70LCDArmAButtonByte LCDButtonByte => _lcdButtonByte;
    }


    public class PZ70LCDArmAButtonByte
    {
        /*
LCD Button Byte
00000000
||||||||_ AP_BUTTON
|||||||_ HDG_BUTTON
||||||_ NAV_BUTTON
|||||_ IAS_BUTTON
||||_ ALT_BUTTON
|||_ VS_BUTTON
||_ APR_BUTTON
|_ REV_BUTTON
 */
        private byte _apMask = 1;
        private byte _hdgMask = 2;
        private byte _navMask = 4;
        private byte _iasMask = 8;
        private byte _altMask = 16;
        private byte _vsMask = 32;
        private byte _aprMask = 64;
        private byte _revMask = 128;
        private int _buttonIsOnMask = 8192;

        //bool isSet = (b & mask) != 0
        //Set to 1" b |= mask
        //Set to zero
        //b &= ~mask
        //Toggle
        //b ^= mask
        private byte _buttonByte = 0;

        public PZ70LCDArmAButtonByte()
        {
        }

        public bool FlipButton(MultiPanelPZ70Knobs multiPanelPZ70Knob)
        {
            try
            {
                return FlipButton(GetMaskForButton(multiPanelPZ70Knob));
            }
            catch (Exception e)
            {
                Common.LogError(e, "Flipbutton()");
                throw;
            }
        }

        public bool IsOn(MultiPanelPZ70Knobs multiPanelPZ70Knobs)
        {
            try
            {
                var buttonMask = GetMaskForButton(multiPanelPZ70Knobs);
                return (_buttonByte & buttonMask) != 0;
            }
            catch (Exception e)
            {
                Common.LogError(e, "IsOn()");
                throw;
            }
        }

        public byte GetMaskForButton(MultiPanelPZ70Knobs multiPanelPZ70Knob)
        {
            try
            {
                switch (multiPanelPZ70Knob)
                {
                    case MultiPanelPZ70Knobs.AP_BUTTON:
                        {
                            return ApMask;
                        }
                    case MultiPanelPZ70Knobs.HDG_BUTTON:
                        {
                            return HdgMask;
                        }
                    case MultiPanelPZ70Knobs.NAV_BUTTON:
                        {
                            return NavMask;
                        }
                    case MultiPanelPZ70Knobs.IAS_BUTTON:
                        {
                            return IasMask;
                        }
                    case MultiPanelPZ70Knobs.ALT_BUTTON:
                        {
                            return AltMask;
                        }
                    case MultiPanelPZ70Knobs.VS_BUTTON:
                        {
                            return VsMask;
                        }
                    case MultiPanelPZ70Knobs.APR_BUTTON:
                        {
                            return AprMask;
                        }
                    case MultiPanelPZ70Knobs.REV_BUTTON:
                        {
                            return RevMask;
                        }
                }
                throw new Exception("Multipanel : Failed to find Mask for button " + multiPanelPZ70Knob);
            }
            catch (Exception e)
            {
                Common.LogError(e);
                throw;
            }
        }

        public bool FlipButton(byte buttonMask)
        {
            try
            {
                _buttonByte ^= buttonMask;
                return (_buttonByte & buttonMask) != 0;
            }
            catch (Exception e)
            {
                Common.LogError(e);
                throw;
            }
        }

        public bool SetButtonOnOrOff(byte buttonMask, bool on)
        {
            if (on)
            {
                return SetButtonOn(buttonMask);
            }
            return SetButtonOff(buttonMask);
        }

        public bool SetButtonOff(byte buttonMask)
        {
            try
            {
                _buttonByte &= buttonMask;
                return (_buttonByte & buttonMask) != 0;
            }
            catch (Exception e)
            {
                Common.LogError(e);
                throw;
            }
        }

        public bool SetButtonOn(byte buttonMask)
        {
            try
            {
                _buttonByte |= buttonMask;
                return (_buttonByte & buttonMask) != 0;
            }
            catch (Exception e)
            {
                Common.LogError(e);
                throw;
            }
        }

        public byte GetButtonByte()
        {
            return _buttonByte;
        }

        public bool SetButtonByte(MultiPanelKnob multiPanelPZ70Knob)
        {
            return SetButtonOnOrOff(GetMaskForButton(multiPanelPZ70Knob.MultiPanelPZ70Knob), multiPanelPZ70Knob.IsOn);
        }

        public byte ApMask => _apMask;

        public byte HdgMask => _hdgMask;

        public byte NavMask => _navMask;

        public byte IasMask => _iasMask;

        public byte AltMask => _altMask;

        public byte VsMask => _vsMask;

        public byte AprMask => _aprMask;

        public byte RevMask => _revMask;

        public int ButtonIsOnMask => _buttonIsOnMask;
    }
}
