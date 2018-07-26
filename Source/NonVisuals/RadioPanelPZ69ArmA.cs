using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ClassLibraryCommon;
using HidLibrary;

namespace NonVisuals
{

    public enum CurrentRadioMode
    {
        COM1,
        COM2,
        NAV1,
        NAV2,
        ADF,
        DME,
        XPDR
    }

    public class RadioPanelPZ69ArmA : RadioPanelPZ69Base, IRedisDataListener
    {

        /*
         * For a specific toggle/switch/lever/knob the PZ55 can have :
         * - single key binding
         * - seqenced key binding
         */
        private HashSet<KeyBindingPZ69> _keyBindings = new HashSet<KeyBindingPZ69>();
        private HashSet<BIPLinkPZ69> _bipLinks = new HashSet<BIPLinkPZ69>();
        private HashSet<RadioPanelPZ69KnobEmulator> _radioPanelKnobs = new HashSet<RadioPanelPZ69KnobEmulator>();
        private bool _isFirstNotification = true;
        private readonly byte[] _oldRadioPanelValue = { 0, 0, 0 };
        private readonly byte[] _newRadioPanelValue = { 0, 0, 0 };
        private CurrentRadioMode _currentUpperMode = CurrentRadioMode.COM1;
        private CurrentRadioMode _currentLowerMode = CurrentRadioMode.COM1;
        private int _xpdrUpperLeft = 0;
        private int _xpdrUpperRight = 0;
        private int _xpdrLowerLeft = 0;
        private int _xpdrLowerRight = 0;
        private readonly object _lockLCDObject = new object();
        /*//0-360
        private int _relDirToAirPortLCDArmAValue = 0; // 21
        private int _dirToAirportLCDArmAValue = 0; // 22
        private int _relDirAircraftToAirportLCDArmAValue = 0;  // 23
        //0-?
        private int _distanceToAirportLCDArmAValue = 0;  //24
        */
        private readonly string[] _redisFreqStrings = new string[14] { "", "", "", "", "", "", "", "", "", "", "", "", "", "" };
        private readonly string[] _redisPreviousFreqStrings = new string[14] { "", "", "", "", "", "", "", "", "", "", "", "", "", "" };
        private int _distanceToAirportLCDArmAValue = 0;
        private int _gameTimeLCDArmAValue = 0;
        private byte _upperXPDRSkipper = 0;
        private byte _lowerXPDRSkipper = 0;

        private readonly List<RadioPanelPZ69KnobsEmulator> _panelPZ69DialModesLower = new List<RadioPanelPZ69KnobsEmulator>() { RadioPanelPZ69KnobsEmulator.LowerCOM1, RadioPanelPZ69KnobsEmulator.LowerCOM2, RadioPanelPZ69KnobsEmulator.LowerNAV1, RadioPanelPZ69KnobsEmulator.LowerNAV2, RadioPanelPZ69KnobsEmulator.LowerADF, RadioPanelPZ69KnobsEmulator.LowerDME, RadioPanelPZ69KnobsEmulator.LowerXPDR };

        private readonly bool[] _buttonStatesForRedis = new bool[16] { false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false };
        /*
                private double _lowerActive = -1;
                private double _lowerStandby = -1;
        */
        //todo lower rack gamla airport data
        //todo     ta bort alla display values
        //todo fixa informationen i usercontrollen
        //todo inte avstånd i nedre racken
        //todo avstånd i nm

        public RadioPanelPZ69ArmA(HIDSkeleton hidSkeleton) : base(hidSkeleton)
        {
            VendorId = 0x6A3;
            ProductId = 0xD05;
            CreateSwitchKeys();
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
                Common.DebugP("RadioPanelPZ69ArmA.StartUp() : " + ex.Message);
                SetLastException(ex);
            }
        }

        public override void Shutdown()
        {
            try
            {
                RedisManager.RemoveRedisDataListener(this);
                Closed = true;
            }
            catch (Exception e)
            {
                SetLastException(e);
            }
        }

        public void RedisDataAvailable(object sender, RedisDataListenerEventArgs args)
        {
            try
            {
                //120.25_118.75_0000.0_0000.0_110.05_110.05_0000.0_0000.0_0267.0_0267.0
                if (!string.IsNullOrWhiteSpace(args.CurrentAirport))
                {
                    var data = args.CurrentAirport.Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
                    SetRedisFreqString(0, data[0]);
                    SetRedisFreqString(1, data[1]);
                    SetRedisFreqString(2, data[2]);
                    SetRedisFreqString(3, data[3]);
                    SetRedisFreqString(4, data[4]);
                    SetRedisFreqString(5, data[5]);
                    SetRedisFreqString(6, data[6]);
                    SetRedisFreqString(7, data[7]);
                    SetRedisFreqString(8, data[8]);
                    SetRedisFreqString(9, data[9]);
                    /*_relDirToAirPortLCDArmAValue = args.RelativeDirectionToAirport; // 21
                    _dirToAirportLCDArmAValue = args.TrueDirectionToAirport; // 22
                    _relDirAircraftToAirportLCDArmAValue = args.RelativeDirectionAircraftToAirport;  // 23
                    _distanceToAirportLCDArmAValue = args.DistanceToAirport;  //24*/
                }
                else
                {
                    _redisFreqStrings[0] = "";
                    _redisFreqStrings[1] = "";
                    _redisFreqStrings[2] = "";
                    _redisFreqStrings[3] = "";
                    _redisFreqStrings[4] = "";
                    _redisFreqStrings[5] = "";
                    _redisFreqStrings[6] = "";
                    _redisFreqStrings[7] = "";
                    _redisFreqStrings[8] = "";
                    _redisFreqStrings[9] = "";
                }
                _distanceToAirportLCDArmAValue = (int)(args.DistanceToAirport); //24
                _gameTimeLCDArmAValue = args.GameTime;
                ShowFrequenciesOnPanel();
            }
            catch (Exception e)
            {
                SetLastException(e);
            }
        }

        private void SetRedisFreqString(int location, string value)
        {
            if (_redisFreqStrings[location] != value)
            {
                if (string.IsNullOrWhiteSpace(_redisFreqStrings[location]))
                {
                    //Not set yet, set both arrays to same value
                    _redisFreqStrings[location] = value;
                    _redisPreviousFreqStrings[location] = value;
                }
                else
                {
                    //Copy old value to "old" array
                    _redisPreviousFreqStrings[location] = _redisFreqStrings[location];
                    _redisFreqStrings[location] = value;
                }
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
                if (!setting.StartsWith("#") && setting.Length > 2 && setting.Contains(InstanceId))
                {
                    if (setting.StartsWith("RadioPanelKey{"))
                    {
                        var keyBinding = new KeyBindingPZ69();
                        keyBinding.ImportSettings(setting);
                        _keyBindings.Add(keyBinding);
                    }
                    else if (setting.StartsWith("RadioPanelBIPLink{"))
                    {
                        var bipLinkPZ69 = new BIPLinkPZ69();
                        bipLinkPZ69.ImportSettings(setting);
                        _bipLinks.Add(bipLinkPZ69);
                    }
                }
            }
            OnSettingsApplied();
        }

        public override List<string> ExportSettings()
        {
            if (Closed)
            {
                return null;
            }
            var result = new List<string>();

            foreach (var keyBinding in _keyBindings)
            {
                if (keyBinding.OSKeyPress != null)
                {
                    result.Add(keyBinding.ExportSettings());
                }
            }
            foreach (var bipLink in _bipLinks)
            {
                var tmp = bipLink.ExportSettings();
                if (!string.IsNullOrEmpty(tmp))
                {
                    result.Add(tmp);
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
            _keyBindings.Clear();
            _bipLinks.Clear();
        }

        public HashSet<KeyBindingPZ69> KeyBindingsHashSet
        {
            get { return _keyBindings; }
        }

        public HashSet<BIPLinkPZ69> BipLinkHashSet
        {
            get { return _bipLinks; }
        }

        private void PZ69SwitchChanged(RadioPanelPZ69KnobEmulator radioPanelKey)
        {
            if (!ForwardKeyPresses)
            {
                return;
            }
            foreach (var keyBinding in _keyBindings)
            {
                if (keyBinding.RadioPanelPZ69Key == radioPanelKey.RadioPanelPZ69Knob && keyBinding.WhenTurnedOn == radioPanelKey.IsOn)
                {
                    keyBinding.OSKeyPress.Execute();
                }
            }
        }

        private void PZ69SwitchChanged(IEnumerable<object> hashSet)
        {
            if (ForwardKeyPresses)
            {
                foreach (var radioPanelKeyObject in hashSet)
                {
                    //Looks which switches has been switched and sees whether any key emulation has been tied to them.
                    var radioPanelKey = (RadioPanelPZ69KnobEmulator)radioPanelKeyObject;
                    foreach (var keyBinding in _keyBindings)
                    {
                        if (keyBinding.OSKeyPress != null && keyBinding.RadioPanelPZ69Key == radioPanelKey.RadioPanelPZ69Knob && keyBinding.WhenTurnedOn == radioPanelKey.IsOn)
                        {
                            keyBinding.OSKeyPress.Execute();
                            break;
                        }
                    }
                    foreach (var bipLinkPZ55 in _bipLinks)
                    {
                        if (bipLinkPZ55.BIPLights.Count > 0 && bipLinkPZ55.RadioPanelPZ69Knob == radioPanelKey.RadioPanelPZ69Knob && bipLinkPZ55.WhenTurnedOn == radioPanelKey.IsOn)
                        {
                            bipLinkPZ55.Execute();
                            break;
                        }
                    }
                }
            }

            foreach (var radioPanelKeyObject in hashSet)
            {
                //Looks which switches has been switched and sees whether any key emulation has been tied to them.
                var radioPanelKey = (RadioPanelPZ69KnobEmulator)radioPanelKeyObject;
                switch (radioPanelKey.RadioPanelPZ69Knob)
                {
                    case RadioPanelPZ69KnobsEmulator.UpperCOM1:
                        {
                            _buttonStatesForRedis[0] = radioPanelKey.IsOn;
                            if (radioPanelKey.IsOn)
                            {
                                _currentUpperMode = CurrentRadioMode.COM1;
                            }
                            break;
                        }
                    case RadioPanelPZ69KnobsEmulator.UpperCOM2:
                        {
                            _buttonStatesForRedis[1] = radioPanelKey.IsOn;
                            if (radioPanelKey.IsOn)
                            {
                                _currentUpperMode = CurrentRadioMode.COM2;
                            }
                            break;
                        }
                    case RadioPanelPZ69KnobsEmulator.UpperNAV1:
                        {
                            _buttonStatesForRedis[2] = radioPanelKey.IsOn;
                            if (radioPanelKey.IsOn)
                            {
                                _currentUpperMode = CurrentRadioMode.NAV1;
                            }
                            break;
                        }
                    case RadioPanelPZ69KnobsEmulator.UpperNAV2:
                        {
                            _buttonStatesForRedis[3] = radioPanelKey.IsOn;
                            if (radioPanelKey.IsOn)
                            {
                                _currentUpperMode = CurrentRadioMode.NAV2;
                            }
                            break;
                        }
                    case RadioPanelPZ69KnobsEmulator.UpperADF:
                        {
                            _buttonStatesForRedis[4] = radioPanelKey.IsOn;
                            if (radioPanelKey.IsOn)
                            {
                                _currentUpperMode = CurrentRadioMode.ADF;
                            }
                            break;
                        }
                    case RadioPanelPZ69KnobsEmulator.UpperDME:
                        {
                            _buttonStatesForRedis[5] = radioPanelKey.IsOn;
                            if (radioPanelKey.IsOn)
                            {
                                _currentUpperMode = CurrentRadioMode.DME;
                            }
                            break;
                        }
                    case RadioPanelPZ69KnobsEmulator.UpperXPDR:
                        {
                            _buttonStatesForRedis[6] = radioPanelKey.IsOn;
                            if (radioPanelKey.IsOn)
                            {
                                _currentUpperMode = CurrentRadioMode.XPDR;
                            }
                            break;
                        }
                    case RadioPanelPZ69KnobsEmulator.UpperFreqSwitch:
                        {
                            if (_currentUpperMode != CurrentRadioMode.XPDR)
                            {
                                _buttonStatesForRedis[7] = radioPanelKey.IsOn;
                                //Do not send info regarding ACT/STBY if in XPDR
                                //RedisManager.SendRedisData("DCS_RadioPanelInput", "1");
                            }
                            break;
                        }
                    case RadioPanelPZ69KnobsEmulator.LowerCOM1:
                        {
                            _buttonStatesForRedis[8] = radioPanelKey.IsOn;
                            if (radioPanelKey.IsOn)
                            {
                                _currentLowerMode = CurrentRadioMode.COM1;
                            }
                            break;
                        }
                    case RadioPanelPZ69KnobsEmulator.LowerCOM2:
                        {
                            _buttonStatesForRedis[9] = radioPanelKey.IsOn;
                            if (radioPanelKey.IsOn)
                            {
                                _currentLowerMode = CurrentRadioMode.COM2;
                            }
                            break;
                        }
                    case RadioPanelPZ69KnobsEmulator.LowerNAV1:
                        {
                            _buttonStatesForRedis[10] = radioPanelKey.IsOn;
                            if (radioPanelKey.IsOn)
                            {
                                _currentLowerMode = CurrentRadioMode.NAV1;
                            }
                            break;
                        }
                    case RadioPanelPZ69KnobsEmulator.LowerNAV2:
                        {
                            _buttonStatesForRedis[11] = radioPanelKey.IsOn;
                            if (radioPanelKey.IsOn)
                            {
                                _currentLowerMode = CurrentRadioMode.NAV2;
                            }
                            break;
                        }
                    case RadioPanelPZ69KnobsEmulator.LowerADF:
                        {
                            _buttonStatesForRedis[12] = radioPanelKey.IsOn;
                            if (radioPanelKey.IsOn)
                            {
                                _currentLowerMode = CurrentRadioMode.ADF;
                            }
                            break;
                        }
                    case RadioPanelPZ69KnobsEmulator.LowerDME:
                        {
                            _buttonStatesForRedis[13] = radioPanelKey.IsOn;
                            if (radioPanelKey.IsOn)
                            {
                                _currentLowerMode = CurrentRadioMode.DME;
                            }
                            break;
                        }
                    case RadioPanelPZ69KnobsEmulator.LowerXPDR:
                        {
                            _buttonStatesForRedis[14] = radioPanelKey.IsOn;
                            if (radioPanelKey.IsOn)
                            {
                                _currentLowerMode = CurrentRadioMode.XPDR;
                            }
                            break;
                        }
                    case RadioPanelPZ69KnobsEmulator.LowerFreqSwitch:
                        {
                            if (_currentLowerMode != CurrentRadioMode.XPDR)
                            {
                                _buttonStatesForRedis[15] = radioPanelKey.IsOn;
                            }
                            break;

                            //Do not send info regarding ACT/STBY if in XPDR
                            //RedisManager.SendRedisData("DCS_RadioPanelInput", "1");
                        }
                }

                switch (radioPanelKey.RadioPanelPZ69Knob)
                {
                    case RadioPanelPZ69KnobsEmulator.UpperLargeFreqWheelInc:
                        {
                            switch (_currentUpperMode)
                            {
                                case CurrentRadioMode.XPDR:
                                    {
                                        IncreaseUpperLeftXPDR();
                                        break;
                                    }
                            }
                            break;
                        }
                    case RadioPanelPZ69KnobsEmulator.UpperLargeFreqWheelDec:
                        {
                            switch (_currentUpperMode)
                            {
                                case CurrentRadioMode.XPDR:
                                    {
                                        DecreaseUpperLeftXPDR();
                                        break;
                                    }
                            }
                            break;
                        }
                    case RadioPanelPZ69KnobsEmulator.UpperSmallFreqWheelInc:
                        {
                            switch (_currentUpperMode)
                            {
                                case CurrentRadioMode.XPDR:
                                    {
                                        IncreaseUpperRightXPDR();
                                        break;
                                    }
                            }
                            break;
                        }
                    case RadioPanelPZ69KnobsEmulator.UpperSmallFreqWheelDec:
                        {
                            switch (_currentUpperMode)
                            {
                                case CurrentRadioMode.XPDR:
                                    {
                                        DecreaseUpperRightXPDR();
                                        break;
                                    }
                            }
                            break;
                        }
                    case RadioPanelPZ69KnobsEmulator.LowerLargeFreqWheelInc:
                        {
                            switch (_currentLowerMode)
                            {
                                case CurrentRadioMode.XPDR:
                                    {
                                        IncreaseLowerLeftXPDR();
                                        break;
                                    }
                            }
                            break;
                        }
                    case RadioPanelPZ69KnobsEmulator.LowerLargeFreqWheelDec:
                        {
                            switch (_currentLowerMode)
                            {
                                case CurrentRadioMode.XPDR:
                                    {
                                        DecreaseLowerLeftXPDR();
                                        break;
                                    }
                            }
                            break;
                        }
                    case RadioPanelPZ69KnobsEmulator.LowerSmallFreqWheelInc:
                        {
                            switch (_currentLowerMode)
                            {
                                case CurrentRadioMode.XPDR:
                                    {
                                        IncreaseLowerRightXPDR();
                                        break;
                                    }
                            }
                            break;
                        }
                    case RadioPanelPZ69KnobsEmulator.LowerSmallFreqWheelDec:
                        {
                            switch (_currentLowerMode)
                            {
                                case CurrentRadioMode.XPDR:
                                    {
                                        DecreaseLowerRightXPDR();
                                        break;
                                    }
                            }
                            break;
                        }
                }
            }
            ShowFrequenciesOnPanel();
        }

        private void ShowFrequenciesOnPanel()
        {
            lock (_lockLCDObject)
            {
                var bytes = new byte[21];
                bytes[0] = 0x0;

                /*
                    _relDirToAirPortLCDArmAValue = (int)(float.Parse(result[21]));
                    _dirToAirportLCDArmAValue = (int)(float.Parse(result[22]));
                    _relDirAircraftToAirportLCDArmAValue = (int)(float.Parse(result[23]));
                    _distanceToAirportLCDArmAValue = (int)(float.Parse(result[24]));
                */
                switch (_currentUpperMode)
                {
                    case CurrentRadioMode.COM1:
                        {
                            if (!CheckUpperRadioValues(_currentUpperMode))
                            {
                                break;
                            }
                            SetPZ69DisplayBytesDefault(ref bytes, _redisFreqStrings[0], PZ69LCDPosition.UPPER_ACTIVE_LEFT);
                            SetPZ69DisplayBytesDefault(ref bytes, _redisFreqStrings[1], PZ69LCDPosition.UPPER_STBY_RIGHT);
                            break;
                        }
                    case CurrentRadioMode.COM2:
                        {
                            if (!CheckUpperRadioValues(_currentUpperMode))
                            {
                                break;
                            }
                            SetPZ69DisplayBytesDefault(ref bytes, _redisFreqStrings[2], PZ69LCDPosition.UPPER_ACTIVE_LEFT);
                            SetPZ69DisplayBytesDefault(ref bytes, _redisFreqStrings[3], PZ69LCDPosition.UPPER_STBY_RIGHT);
                            break;
                        }
                    case CurrentRadioMode.NAV1:
                        {
                            if (!CheckUpperRadioValues(_currentUpperMode))
                            {
                                break;
                            }
                            SetPZ69DisplayBytesDefault(ref bytes, _redisFreqStrings[4], PZ69LCDPosition.UPPER_ACTIVE_LEFT);
                            SetPZ69DisplayBytesDefault(ref bytes, _redisFreqStrings[5], PZ69LCDPosition.UPPER_STBY_RIGHT);
                            break;
                        }
                    case CurrentRadioMode.NAV2:
                        {
                            if (!CheckUpperRadioValues(_currentUpperMode))
                            {
                                break;
                            }
                            SetPZ69DisplayBytesDefault(ref bytes, _redisFreqStrings[6], PZ69LCDPosition.UPPER_ACTIVE_LEFT);
                            SetPZ69DisplayBytesDefault(ref bytes, _redisFreqStrings[7], PZ69LCDPosition.UPPER_STBY_RIGHT);
                            break;
                        }
                    case CurrentRadioMode.ADF:
                        {
                            if (!CheckUpperRadioValues(_currentUpperMode))
                            {
                                break;
                            }
                            SetPZ69DisplayBytesDefault(ref bytes, _redisFreqStrings[8], PZ69LCDPosition.UPPER_ACTIVE_LEFT);
                            SetPZ69DisplayBytesDefault(ref bytes, _redisFreqStrings[9], PZ69LCDPosition.UPPER_STBY_RIGHT);
                            break;
                        }
                    case CurrentRadioMode.DME:
                        {
                            //Always show this
                            SetPZ69DisplayBytesDefault(ref bytes, _distanceToAirportLCDArmAValue.ToString().PadLeft(5, '0'), PZ69LCDPosition.UPPER_STBY_RIGHT);
                            if (!CheckUpperRadioValues(_currentUpperMode))
                            {
                                break;
                            }
                            SetPZ69DisplayBytesDefault(ref bytes, _redisFreqStrings[8], PZ69LCDPosition.UPPER_ACTIVE_LEFT);
                            break;
                        }
                    case CurrentRadioMode.XPDR:
                        {
                            SetPZ69DisplayBytesDefault(ref bytes, " " + DateTime.Now.ToString("HHmm", CultureInfo.InvariantCulture).PadLeft(4, '0'), PZ69LCDPosition.UPPER_ACTIVE_LEFT);
                            SetPZ69DisplayBytesDefault(ref bytes, " " + _xpdrUpperLeft.ToString().PadLeft(2, '0') + _xpdrUpperRight.ToString().PadLeft(2, '0'), PZ69LCDPosition.UPPER_STBY_RIGHT);
                            break;
                        }
                }


                switch (_currentLowerMode)
                {
                    case CurrentRadioMode.COM1:
                        {
                            if (!CheckLowerRadioValues(_currentLowerMode))
                            {
                                break;
                            }
                            SetPZ69DisplayBytesDefault(ref bytes, _redisPreviousFreqStrings[0], PZ69LCDPosition.LOWER_ACTIVE_LEFT);
                            SetPZ69DisplayBytesDefault(ref bytes, _redisPreviousFreqStrings[1], PZ69LCDPosition.LOWER_STBY_RIGHT);
                            break;
                        }
                    case CurrentRadioMode.COM2:
                        {
                            if (!CheckLowerRadioValues(_currentLowerMode))
                            {
                                break;
                            }
                            SetPZ69DisplayBytesDefault(ref bytes, _redisPreviousFreqStrings[2], PZ69LCDPosition.LOWER_ACTIVE_LEFT);
                            SetPZ69DisplayBytesDefault(ref bytes, _redisPreviousFreqStrings[3], PZ69LCDPosition.LOWER_STBY_RIGHT);
                            break;
                        }
                    case CurrentRadioMode.NAV1:
                        {
                            if (!CheckLowerRadioValues(_currentLowerMode))
                            {
                                break;
                            }
                            SetPZ69DisplayBytesDefault(ref bytes, _redisPreviousFreqStrings[4], PZ69LCDPosition.LOWER_ACTIVE_LEFT);
                            SetPZ69DisplayBytesDefault(ref bytes, _redisPreviousFreqStrings[5], PZ69LCDPosition.LOWER_STBY_RIGHT);
                            break;
                        }
                    case CurrentRadioMode.NAV2:
                        {
                            if (!CheckLowerRadioValues(_currentLowerMode))
                            {
                                break;
                            }
                            SetPZ69DisplayBytesDefault(ref bytes, _redisPreviousFreqStrings[6], PZ69LCDPosition.LOWER_ACTIVE_LEFT);
                            SetPZ69DisplayBytesDefault(ref bytes, _redisPreviousFreqStrings[7], PZ69LCDPosition.LOWER_STBY_RIGHT);
                            break;
                        }
                    case CurrentRadioMode.ADF:
                        {
                            if (!CheckLowerRadioValues(_currentLowerMode))
                            {
                                break;
                            }
                            SetPZ69DisplayBytesDefault(ref bytes, _redisPreviousFreqStrings[8], PZ69LCDPosition.LOWER_ACTIVE_LEFT);
                            SetPZ69DisplayBytesDefault(ref bytes, _redisPreviousFreqStrings[9], PZ69LCDPosition.LOWER_STBY_RIGHT);
                            break;
                        }
                    case CurrentRadioMode.DME:
                        {
                            if (!CheckLowerRadioValues(_currentLowerMode))
                            {
                                break;
                            }
                            SetPZ69DisplayBytesDefault(ref bytes, _redisPreviousFreqStrings[8], PZ69LCDPosition.LOWER_ACTIVE_LEFT);
                            SetPZ69DisplayBytesDefault(ref bytes, _redisPreviousFreqStrings[9], PZ69LCDPosition.LOWER_STBY_RIGHT);
                            break;
                        }
                    case CurrentRadioMode.XPDR:
                        {
                            SetPZ69DisplayBytesDefault(ref bytes, " " + _gameTimeLCDArmAValue.ToString().PadLeft(4, '0'), PZ69LCDPosition.LOWER_ACTIVE_LEFT);
                            SetPZ69DisplayBytesDefault(ref bytes, " " + _xpdrLowerLeft.ToString().PadLeft(2, '0') + _xpdrLowerRight.ToString().PadLeft(2, '0'), PZ69LCDPosition.LOWER_STBY_RIGHT);
                            break;
                        }
                }
                /*if (_lowerActive < 0)
                {
                    SetPZ69DisplayBlank(ref bytes, PZ69LCDPosition.LOWER_ACTIVE_LEFT);
                }
                else
                {
                    SetPZ69DisplayBytesDefault(ref bytes, _lowerActive, PZ69LCDPosition.LOWER_ACTIVE_LEFT);
                }

                if (_lowerStandby < 0)
                {
                    SetPZ69DisplayBlank(ref bytes, PZ69LCDPosition.LOWER_STBY_RIGHT);
                }
                else
                {
                    SetPZ69DisplayBytesDefault(ref bytes, _lowerStandby, PZ69LCDPosition.LOWER_STBY_RIGHT);
                }
                */
                SendLCDData(bytes);
            }
        }


        public string GetKeyPressForLoggingPurposes(RadioPanelPZ69KnobEmulator radioPanelKey)
        {
            var result = "";
            foreach (var keyBinding in _keyBindings)
            {
                if (keyBinding.OSKeyPress != null && keyBinding.RadioPanelPZ69Key == radioPanelKey.RadioPanelPZ69Knob && keyBinding.WhenTurnedOn == radioPanelKey.IsOn)
                {
                    result = keyBinding.OSKeyPress.GetNonFunctioningVirtualKeyCodesAsString();
                }
            }
            return result;
        }

        public void AddOrUpdateSingleKeyBinding(RadioPanelPZ69KnobsEmulator radioPanelPZ69Knob, string keys, KeyPressLength keyPressLength, bool whenTurnedOn = true)
        {
            if (string.IsNullOrEmpty(keys))
            {
                var tmp = new RadioPanelPZ69KnobEmulator.RadioPanelPZ69KeyOnOff(radioPanelPZ69Knob, whenTurnedOn);
                ClearAllBindings(tmp);
                return;
            }
            var found = false;
            foreach (var keyBinding in _keyBindings)
            {
                if (keyBinding.RadioPanelPZ69Key == radioPanelPZ69Knob && keyBinding.WhenTurnedOn == whenTurnedOn)
                {
                    if (string.IsNullOrEmpty(keys))
                    {
                        keyBinding.OSKeyPress = null;
                    }
                    else
                    {
                        keyBinding.OSKeyPress = new OSKeyPress(keys, keyPressLength);
                        keyBinding.WhenTurnedOn = whenTurnedOn;
                    }
                    found = true;
                }
            }
            if (!found && !string.IsNullOrEmpty(keys))
            {
                var keyBinding = new KeyBindingPZ69();
                keyBinding.RadioPanelPZ69Key = radioPanelPZ69Knob;
                keyBinding.OSKeyPress = new OSKeyPress(keys, keyPressLength);
                keyBinding.WhenTurnedOn = whenTurnedOn;
                _keyBindings.Add(keyBinding);
            }
            Common.DebugP("RadioPanelPZ69ArmA _keyBindings : " + _keyBindings.Count);
            IsDirtyMethod();
        }

        public void ClearAllBindings(RadioPanelPZ69KnobEmulator.RadioPanelPZ69KeyOnOff radioPanelPZ69KnobOnOff)
        {
            //This must accept lists
            foreach (var keyBinding in _keyBindings)
            {
                if (keyBinding.RadioPanelPZ69Key == radioPanelPZ69KnobOnOff.RadioPanelPZ69Key && keyBinding.WhenTurnedOn == radioPanelPZ69KnobOnOff.On)
                {
                    keyBinding.OSKeyPress = null;
                }
            }
            IsDirtyMethod();
        }

        public void AddOrUpdateSequencedKeyBinding(string information, RadioPanelPZ69KnobsEmulator radioPanelPZ69Knob, SortedList<int, KeyPressInfo> sortedList, bool whenTurnedOn = true)
        {
            //This must accept lists
            var found = false;
            RemoveRadioPanelKnobFromList(ControlListPZ69.KEYS, radioPanelPZ69Knob, whenTurnedOn);
            foreach (var keyBinding in _keyBindings)
            {
                if (keyBinding.RadioPanelPZ69Key == radioPanelPZ69Knob && keyBinding.WhenTurnedOn == whenTurnedOn)
                {
                    if (sortedList.Count == 0)
                    {
                        keyBinding.OSKeyPress = null;
                    }
                    else
                    {
                        keyBinding.OSKeyPress = new OSKeyPress(information, sortedList);
                        keyBinding.WhenTurnedOn = whenTurnedOn;
                    }
                    found = true;
                    break;
                }
            }
            if (!found && sortedList.Count > 0)
            {
                var keyBinding = new KeyBindingPZ69();
                keyBinding.RadioPanelPZ69Key = radioPanelPZ69Knob;
                keyBinding.OSKeyPress = new OSKeyPress(information, sortedList);
                keyBinding.WhenTurnedOn = whenTurnedOn;
                _keyBindings.Add(keyBinding);
            }
            IsDirtyMethod();
        }


        public BIPLinkPZ69 AddOrUpdateBIPLinkKeyBinding(RadioPanelPZ69KnobsEmulator radioPanelPZ69Knob, BIPLinkPZ69 bipLinkPZ69, bool whenTurnedOn = true)
        {
            //This must accept lists
            var found = false;
            BIPLinkPZ69 tmpBIPLinkPZ69 = null;

            RemoveRadioPanelKnobFromList(ControlListPZ69.BIPS, radioPanelPZ69Knob, whenTurnedOn);
            foreach (var bipLink in _bipLinks)
            {
                if (bipLink.RadioPanelPZ69Knob == radioPanelPZ69Knob && bipLink.WhenTurnedOn == whenTurnedOn)
                {
                    bipLink.BIPLights = bipLinkPZ69.BIPLights;
                    bipLink.Description = bipLinkPZ69.Description;
                    bipLink.RadioPanelPZ69Knob = radioPanelPZ69Knob;
                    bipLink.WhenTurnedOn = whenTurnedOn;
                    tmpBIPLinkPZ69 = bipLink;
                    found = true;
                    break;
                }
            }
            if (!found && bipLinkPZ69.BIPLights.Count > 0)
            {
                bipLinkPZ69.RadioPanelPZ69Knob = radioPanelPZ69Knob;
                bipLinkPZ69.WhenTurnedOn = whenTurnedOn;
                tmpBIPLinkPZ69 = bipLinkPZ69;
                _bipLinks.Add(bipLinkPZ69);
            }
            IsDirtyMethod();
            return tmpBIPLinkPZ69;
        }

        private void RemoveRadioPanelKnobFromList(ControlListPZ69 controlListPZ69, RadioPanelPZ69KnobsEmulator radioPanelPZ69Knob, bool whenTurnedOn = true)
        {
            var found = false;
            if (controlListPZ69 == ControlListPZ69.ALL || controlListPZ69 == ControlListPZ69.KEYS)
            {
                foreach (var keyBindingPZ55 in _keyBindings)
                {
                    if (keyBindingPZ55.RadioPanelPZ69Key == radioPanelPZ69Knob && keyBindingPZ55.WhenTurnedOn == whenTurnedOn)
                    {
                        keyBindingPZ55.OSKeyPress = null;
                    }
                    found = true;
                    break;
                }
            }
            if (controlListPZ69 == ControlListPZ69.ALL || controlListPZ69 == ControlListPZ69.BIPS)
            {
                foreach (var bipLink in _bipLinks)
                {
                    if (bipLink.RadioPanelPZ69Knob == radioPanelPZ69Knob && bipLink.WhenTurnedOn == whenTurnedOn)
                    {
                        bipLink.BIPLights.Clear();
                    }
                    found = true;
                    break;
                }
            }

            if (found)
            {
                IsDirtyMethod();
            }
        }

        private void IsDirtyMethod()
        {
            OnSettingsChanged();
            IsDirty = true;
        }

        public void Clear(RadioPanelPZ69KnobsEmulator radioPanelPZ69Knob)
        {
            foreach (var keyBinding in _keyBindings)
            {
                if (keyBinding.RadioPanelPZ69Key == radioPanelPZ69Knob)
                {
                    keyBinding.OSKeyPress = null;
                }
            }
            IsDirtyMethod();
        }

        private void OnReport(HidReport report)
        {
            //if (IsAttached == false) { return; }

            if (report.Data.Length == 3)
            {
                Array.Copy(_newRadioPanelValue, _oldRadioPanelValue, 3);
                Array.Copy(report.Data, _newRadioPanelValue, 3);
                var hashSet = GetHashSetOfSwitchedKeys(_oldRadioPanelValue, _newRadioPanelValue);
                PZ69SwitchChanged(hashSet);
                OnSwitchesChanged(hashSet);
                try
                {
                    RedisManager.SendRedisData("DCS_RadioPanelInput", GetRedisButtonDataAsString());
                }
                catch (Exception e)
                {
                    SetLastException(e);
                }
                _isFirstNotification = false;
                /*if (Common.Debug)
                {
                    var stringBuilder = new StringBuilder();
                    for (var i = 0; i < report.Data.Length; i++)
                    {
                        stringBuilder.Append(report.Data[i] + " ");
                    }
                    Common.DebugP(stringBuilder.ToString());
                    if (hashSet.Count > 0)
                    {
                        Common.DebugP("\nFollowing switches has been changed:\n");
                        foreach (var radioPanelKey in hashSet)
                        {
                            Common.DebugP(((RadioPanelKey)radioPanelKey).RadioPanelPZ69ArmAKey + ", value is " + FlagValue(_newRadioPanelValue, ((RadioPanelKey)radioPanelKey)));
                        }
                    }
                }
                Common.DebugP("\r\nDone!\r\n");*/
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



        private HashSet<object> GetHashSetOfSwitchedKeys(byte[] oldValue, byte[] newValue)
        {
            var result = new HashSet<object>();




            for (var i = 0; i < 3; i++)
            {
                var oldByte = oldValue[i];
                var newByte = newValue[i];

                foreach (var radioPanelKey in _radioPanelKnobs)
                {
                    if (radioPanelKey.Group == i && (FlagHasChanged(oldByte, newByte, radioPanelKey.Mask) || _isFirstNotification))
                    {
                        radioPanelKey.IsOn = FlagValue(newValue, radioPanelKey);
                        result.Add(radioPanelKey);
                    }
                }
            }
            return result;
        }

        private static bool FlagValue(byte[] currentValue, RadioPanelPZ69KnobEmulator radioPanelKey)
        {
            return (currentValue[radioPanelKey.Group] & radioPanelKey.Mask) > 0;
        }

        private void CreateSwitchKeys()
        {
            _radioPanelKnobs = RadioPanelPZ69KnobEmulator.GetRadioPanelKnobs();
        }
        

        public override String SettingsVersion()
        {
            return "0X";
        }


        private int _skipValue = 3;
        private void IncreaseUpperRightXPDR()
        {
            _upperXPDRSkipper++;
            if (_upperXPDRSkipper < _skipValue)
            {
                return;
            }
                _upperXPDRSkipper = 0;
            _xpdrUpperRight = _xpdrUpperRight + 1;
            if (_xpdrUpperRight > 99)
            {
                _xpdrUpperRight = 0;
            }
        }
        private void DecreaseUpperRightXPDR()
        {
            _upperXPDRSkipper++;
            if (_upperXPDRSkipper < _skipValue)
            {
                return;
            }
            _upperXPDRSkipper = 0;
            _xpdrUpperRight = _xpdrUpperRight - 1;
            if (_xpdrUpperRight < 0)
            {
                _xpdrUpperRight = 99;
            }
        }
        private void IncreaseLowerRightXPDR()
        {
            _lowerXPDRSkipper++;
            if (_lowerXPDRSkipper < _skipValue)
            {
                return;
            }
            _lowerXPDRSkipper = 0;
            _xpdrLowerRight = _xpdrLowerRight + 1;
            if (_xpdrLowerRight > 99)
            {
                _xpdrLowerRight = 0;
            }
        }
        private void DecreaseLowerRightXPDR()
        {
            _lowerXPDRSkipper++;
            if (_lowerXPDRSkipper < _skipValue)
            {
                return;
            }
            _lowerXPDRSkipper = 0;
            _xpdrLowerRight = _xpdrLowerRight - 1;
            if (_xpdrLowerRight < 0)
            {
                _xpdrLowerRight = 99;
            }
        }

        private void IncreaseUpperLeftXPDR()
        {
            _upperXPDRSkipper++;
            if (_upperXPDRSkipper < _skipValue)
            {
                return;
            }
            _upperXPDRSkipper = 0;
            _xpdrUpperLeft = _xpdrUpperLeft + 1;
            if (_xpdrUpperLeft > 99)
            {
                _xpdrUpperLeft = 0;
            }
        }
        private void DecreaseUpperLeftXPDR()
        {
            _upperXPDRSkipper++;
            if (_upperXPDRSkipper < _skipValue)
            {
                return;
            }
            _upperXPDRSkipper = 0;
            _xpdrUpperLeft = _xpdrUpperLeft - 1;
            if (_xpdrUpperLeft < 0)
            {
                _xpdrUpperLeft = 99;
            }
        }
        private void IncreaseLowerLeftXPDR()
        {
            _lowerXPDRSkipper++;
            if (_lowerXPDRSkipper < _skipValue)
            {
                return;
            }
            _lowerXPDRSkipper = 0;
            _xpdrLowerLeft = _xpdrLowerLeft + 1;
            if (_xpdrLowerLeft > 99)
            {
                _xpdrLowerLeft = 0;
            }
        }
        private void DecreaseLowerLeftXPDR()
        {
            _lowerXPDRSkipper++;
            if (_lowerXPDRSkipper < _skipValue)
            {
                return;
            }
            _lowerXPDRSkipper = 0;
            _xpdrLowerLeft = _xpdrLowerLeft - 1;
            if (_xpdrLowerLeft < 0)
            {
                _xpdrLowerLeft = 99;
            }
        }

        private bool CheckUpperRadioValues(CurrentRadioMode currentUpperMode)
        {
            switch (currentUpperMode)
            {
                case CurrentRadioMode.COM1:
                    {
                        return !string.IsNullOrWhiteSpace(_redisFreqStrings[0]) && !string.IsNullOrWhiteSpace(_redisFreqStrings[1]);
                    }
                case CurrentRadioMode.COM2:
                    {
                        return !string.IsNullOrWhiteSpace(_redisFreqStrings[2]) && !string.IsNullOrWhiteSpace(_redisFreqStrings[3]);
                    }
                case CurrentRadioMode.NAV1:
                    {
                        return !string.IsNullOrWhiteSpace(_redisFreqStrings[4]) && !string.IsNullOrWhiteSpace(_redisFreqStrings[5]);
                    }
                case CurrentRadioMode.NAV2:
                    {
                        return !string.IsNullOrWhiteSpace(_redisFreqStrings[6]) && !string.IsNullOrWhiteSpace(_redisFreqStrings[7]);
                    }
                case CurrentRadioMode.ADF:
                    {
                        return !string.IsNullOrWhiteSpace(_redisFreqStrings[8]) && !string.IsNullOrWhiteSpace(_redisFreqStrings[9]);
                    }
                case CurrentRadioMode.DME:
                    {
                        return !string.IsNullOrWhiteSpace(_redisFreqStrings[8]);
                    }
            }
            throw new Exception("Failed to find upper radio mode (CheckUpperRadioValues)");
        }

        private bool CheckLowerRadioValues(CurrentRadioMode currentLowerMode)
        {
            switch (currentLowerMode)
            {
                case CurrentRadioMode.COM1:
                    {
                        return !string.IsNullOrWhiteSpace(_redisPreviousFreqStrings[0]) && !string.IsNullOrWhiteSpace(_redisPreviousFreqStrings[1]);
                    }
                case CurrentRadioMode.COM2:
                    {
                        return !string.IsNullOrWhiteSpace(_redisPreviousFreqStrings[2]) && !string.IsNullOrWhiteSpace(_redisPreviousFreqStrings[3]);
                    }
                case CurrentRadioMode.NAV1:
                    {
                        return !string.IsNullOrWhiteSpace(_redisPreviousFreqStrings[4]) && !string.IsNullOrWhiteSpace(_redisPreviousFreqStrings[5]);
                    }
                case CurrentRadioMode.NAV2:
                    {
                        return !string.IsNullOrWhiteSpace(_redisPreviousFreqStrings[6]) && !string.IsNullOrWhiteSpace(_redisPreviousFreqStrings[7]);
                    }
                case CurrentRadioMode.ADF:
                    {
                        return !string.IsNullOrWhiteSpace(_redisPreviousFreqStrings[8]) && !string.IsNullOrWhiteSpace(_redisPreviousFreqStrings[9]);
                    }
                case CurrentRadioMode.DME:
                    {
                        return !string.IsNullOrWhiteSpace(_redisPreviousFreqStrings[8]);
                    }
            }
            throw new Exception("Failed to find upper radio mode (CheckLowerRadioValues)");
        }

        private string GetRedisButtonDataAsString()
        {
            var result = string.Join(", ", _buttonStatesForRedis.Select(b => b ? "1" : "0").ToArray());
            return result;
        }
    }

}
