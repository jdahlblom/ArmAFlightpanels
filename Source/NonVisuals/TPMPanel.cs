using System;
using System.Collections.Generic;
using ClassLibraryCommon;
using HidLibrary;

namespace NonVisuals
{

    public class TPMPanel : SaitekPanel
    {

        /*
         * For a switch the TPM can have :
         * - single key binding
         * - seqenced key binding
         */
        private HashSet<KeyBindingTPM> _keyBindings = new HashSet<KeyBindingTPM>();
        private HashSet<BIPLinkTPM> _bipLinks = new HashSet<BIPLinkTPM>();
        private HashSet<TPMPanelSwitch> _tpmPanelSwitches = new HashSet<TPMPanelSwitch>();
        private bool _isFirstNotification = true;
        private byte[] _oldTPMPanelValue = { 0, 0, 0, 0, 0 };
        private byte[] _newTPMPanelValue = { 0, 0, 0, 0, 0 };

        public TPMPanel(HIDSkeleton hidSkeleton) : base(SaitekPanelsEnum.TPM, hidSkeleton)
        {
            //Fixed values
            VendorId = 0x6A3;
            ProductId = 0xB4D;
            CreateSwitchKeys();
            Startup();
        }

        public override sealed void Startup()
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
                Common.DebugP("TPMPanel.StartUp() : " + ex.Message);
                SetLastException(ex);
            }
        }

        public override void Shutdown()
        {
            try
            {
                Closed = true;
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
                if (!setting.StartsWith("#") && setting.Length > 2 && setting.Contains(InstanceId))
                {
                    if (setting.StartsWith("TPMPanelSwitch{"))
                    {
                        var keyBinding = new KeyBindingTPM();
                        keyBinding.ImportSettings(setting);
                        _keyBindings.Add(keyBinding);
                    }
                    else if (setting.StartsWith("TPMPanelBipLink{"))
                    {
                        var tmpBipLink = new BIPLinkTPM();
                        tmpBipLink.ImportSettings(setting);
                        _bipLinks.Add(tmpBipLink);
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
                if (bipLink.BIPLights.Count > 0)
                {
                    result.Add(bipLink.ExportSettings());
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

        public HashSet<KeyBindingTPM> KeyBindingsHashSet
        {
            get { return _keyBindings; }
            set { _keyBindings = value; }
        }

        public HashSet<BIPLinkTPM> BipLinkHashSet
        {
            get { return _bipLinks; }
            set { _bipLinks = value; }
        }

        private void TPMSwitchChanged(TPMPanelSwitch tpmPanelSwitch)
        {
            if (!ForwardKeyPresses)
            {
                return;
            }
            foreach (var keyBinding in _keyBindings)
            {
                if (keyBinding.TPMSwitch == tpmPanelSwitch.TPMSwitch && keyBinding.WhenTurnedOn == tpmPanelSwitch.IsOn)
                {
                    keyBinding.OSKeyPress.Execute();
                }
            }
        }

        private void TPMSwitchChanged(IEnumerable<object> hashSet)
        {
            if (!ForwardKeyPresses)
            {
                return;
            }
            foreach (var tpmPanelSwitchObject in hashSet)
            {
                var tpmPanelSwitch = (TPMPanelSwitch)tpmPanelSwitchObject;
                foreach (var keyBinding in _keyBindings)
                {
                    if (keyBinding.OSKeyPress != null && keyBinding.TPMSwitch == tpmPanelSwitch.TPMSwitch && keyBinding.WhenTurnedOn == tpmPanelSwitch.IsOn)
                    {
                        keyBinding.OSKeyPress.Execute();
                        break;
                    }
                }
                foreach (var bipLinkTPM in _bipLinks)
                {
                    if (bipLinkTPM.BIPLights.Count > 0 && bipLinkTPM.TPMSwitch == tpmPanelSwitch.TPMSwitch && bipLinkTPM.WhenTurnedOn == tpmPanelSwitch.IsOn)
                    {
                        bipLinkTPM.Execute();
                        break;
                    }
                }
            }
        }

        public string GetKeyPressForLoggingPurposes(TPMPanelSwitch tpmPanelSwitch)
        {
            var result = "";
            foreach (var keyBinding in _keyBindings)
            {
                if (keyBinding.OSKeyPress != null && keyBinding.TPMSwitch == tpmPanelSwitch.TPMSwitch && keyBinding.WhenTurnedOn == tpmPanelSwitch.IsOn)
                {
                    result = keyBinding.OSKeyPress.GetNonFunctioningVirtualKeyCodesAsString();
                }
            }
            return result;
        }

        public void AddOrUpdateSingleKeyBinding(TPMPanelSwitches tpmPanelSwitch, string keys, KeyPressLength keyPressLength, bool whenTurnedOn = true)
        {
            if (string.IsNullOrEmpty(keys))
            {
                var tmp = new TPMPanelSwitch.TPMPanelSwitchOnOff(tpmPanelSwitch, whenTurnedOn);
                ClearAllBindings(tmp);
                return;
            }
            var found = false;
            foreach (var keyBinding in _keyBindings)
            {
                if (keyBinding.TPMSwitch == tpmPanelSwitch && keyBinding.WhenTurnedOn == whenTurnedOn)
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
                var keyBinding = new KeyBindingTPM();
                keyBinding.TPMSwitch = tpmPanelSwitch;
                keyBinding.OSKeyPress = new OSKeyPress(keys, keyPressLength);
                keyBinding.WhenTurnedOn = whenTurnedOn;
                _keyBindings.Add(keyBinding);
            }
            Common.DebugP("TPMPanel _keyBindings : " + _keyBindings.Count);
            IsDirtyMethod();
        }

        public void ClearAllBindings(TPMPanelSwitch.TPMPanelSwitchOnOff tpmPanelSwitchOnOff)
        {
            //This must accept lists
            foreach (var keyBinding in _keyBindings)
            {
                if (keyBinding.TPMSwitch == tpmPanelSwitchOnOff.TPMSwitch && keyBinding.WhenTurnedOn == tpmPanelSwitchOnOff.On)
                {
                    keyBinding.OSKeyPress = null;
                }
            }
            Common.DebugP("TPMPanel _keyBindings : " + _keyBindings.Count);
            IsDirtyMethod();
        }

        public void AddOrUpdateSequencedKeyBinding(string information, TPMPanelSwitches tpmPanelSwitch, SortedList<int, KeyPressInfo> sortedList, bool whenTurnedOn = true)
        {
            //This must accept lists
            var found = false;
            RemoveTPMPanelSwitchFromList(ControlListTPM.KEYS, tpmPanelSwitch, whenTurnedOn);
            foreach (var keyBinding in _keyBindings)
            {
                if (keyBinding.TPMSwitch == tpmPanelSwitch && keyBinding.WhenTurnedOn == whenTurnedOn)
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
                var keyBinding = new KeyBindingTPM();
                keyBinding.TPMSwitch = tpmPanelSwitch;
                keyBinding.OSKeyPress = new OSKeyPress(information, sortedList);
                keyBinding.WhenTurnedOn = whenTurnedOn;
                _keyBindings.Add(keyBinding);
            }
            IsDirtyMethod();
        }


        public BIPLinkTPM AddOrUpdateBIPLinkKeyBinding(TPMPanelSwitches tpmPanelSwitch, BIPLinkTPM bipLinkTPM, bool whenTurnedOn = true)
        {
            //This must accept lists
            var found = false;
            BIPLinkTPM tmpBIPLinkTPM = null;

            RemoveTPMPanelSwitchFromList(ControlListTPM.BIPS, tpmPanelSwitch, whenTurnedOn);
            foreach (var bipLink in _bipLinks)
            {
                if (bipLink.TPMSwitch == tpmPanelSwitch && bipLink.WhenTurnedOn == whenTurnedOn)
                {
                    bipLink.BIPLights = bipLinkTPM.BIPLights;
                    bipLink.Description = bipLinkTPM.Description;
                    bipLink.TPMSwitch = tpmPanelSwitch;
                    bipLink.WhenTurnedOn = whenTurnedOn;
                    tmpBIPLinkTPM = bipLink;
                    found = true;
                    break;
                }
            }
            if (!found && bipLinkTPM.BIPLights.Count > 0)
            {
                bipLinkTPM.TPMSwitch = tpmPanelSwitch;
                bipLinkTPM.WhenTurnedOn = whenTurnedOn;
                tmpBIPLinkTPM = bipLinkTPM;
                _bipLinks.Add(bipLinkTPM);
            }
            IsDirtyMethod();
            return tmpBIPLinkTPM;
        }


        
        public void RemoveTPMPanelSwitchFromList(ControlListTPM controlListTPM, TPMPanelSwitches tpmPanelSwitch, bool whenTurnedOn = true)
        {
            var found = false;
            if (controlListTPM == ControlListTPM.ALL || controlListTPM == ControlListTPM.KEYS)
            {
                foreach (var keyBindingTPM in _keyBindings)
                {
                    if (keyBindingTPM.TPMSwitch == tpmPanelSwitch && keyBindingTPM.WhenTurnedOn == whenTurnedOn)
                    {
                        keyBindingTPM.OSKeyPress = null;
                        found = true;
                    }
                }
            }
            if (controlListTPM == ControlListTPM.ALL || controlListTPM == ControlListTPM.BIPS)
            {
                foreach (var bipLink in _bipLinks)
                {
                    if (bipLink.TPMSwitch == tpmPanelSwitch && bipLink.WhenTurnedOn == whenTurnedOn)
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

        private void IsDirtyMethod()
        {
            OnSettingsChanged();
            IsDirty = true;
        }

        public void Clear(TPMPanelSwitches tpmPanelSwitch)
        {
            foreach (var keyBinding in _keyBindings)
            {
                if (keyBinding.TPMSwitch == tpmPanelSwitch)
                {
                    keyBinding.OSKeyPress = null;
                }
            }
            IsDirtyMethod();
        }

        private void OnReport(HidReport report)
        {
            //if (IsAttached == false) { return; }

            if (report.Data.Length == 5)
            {
                Array.Copy(_newTPMPanelValue, _oldTPMPanelValue, 5);
                Array.Copy(report.Data, _newTPMPanelValue, 5);
                var hashSet = GetHashSetOfChangedSwitches(_oldTPMPanelValue, _newTPMPanelValue);
                TPMSwitchChanged(hashSet);
                OnSwitchesChanged(hashSet);
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
                        foreach (var tpmPanelSwitch in hashSet)
                        {
                            Common.DebugP(((TPMPanelSwitch)tpmPanelSwitch).TPMSwitch + ", value is " + FlagValue(_newTPMPanelValue, ((TPMPanelSwitch)tpmPanelSwitch)));
                        }
                    }
                }*/
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

        private void DeviceAttachedHandler()
        {
            Startup();
            OnDeviceAttached();
        }

        private void DeviceRemovedHandler()
        {
            Shutdown();
            OnDeviceDetached();
        }

        private HashSet<object> GetHashSetOfChangedSwitches(byte[] oldValue, byte[] newValue)
        {
            var result = new HashSet<object>();



            for (var i = 0; i < 5; i++)
            {
                var oldByte = oldValue[i];
                var newByte = newValue[i];

                foreach (var tpmPanelSwitch in _tpmPanelSwitches)
                {
                    if (tpmPanelSwitch.Group == i && (FlagHasChanged(oldByte, newByte, tpmPanelSwitch.Mask) || _isFirstNotification))
                    {
                        tpmPanelSwitch.IsOn = FlagValue(newValue, tpmPanelSwitch);
                        result.Add(tpmPanelSwitch);
                    }
                }
            }
            return result;
        }

        private static bool FlagValue(byte[] currentValue, TPMPanelSwitch tpmPanelSwitch)
        {
            return (currentValue[tpmPanelSwitch.Group] & tpmPanelSwitch.Mask) > 0;
        }

        private void CreateSwitchKeys()
        {
            _tpmPanelSwitches = TPMPanelSwitch.GetTPMPanelSwitches();
        }

        public override String SettingsVersion()
        {
            return "0X";
        }
    }



    public enum ControlListTPM : byte
    {
        ALL,
        KEYS,
        BIPS
    }
}
