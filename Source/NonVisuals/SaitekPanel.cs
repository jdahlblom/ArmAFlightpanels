﻿using System;
using System.Collections.Generic;
using ClassLibraryCommon;

namespace NonVisuals
{

    public abstract class SaitekPanel : IProfileHandlerListener
    {
        //These events can be raised by the descendants of this class.
        public delegate void SwitchesHasBeenChangedEventHandler(object sender, SwitchesChangedEventArgs e);
        public event SwitchesHasBeenChangedEventHandler OnSwitchesChangedA;
        
        public delegate void DeviceAttachedEventHandler(object sender, PanelEventArgs e);
        public event DeviceAttachedEventHandler OnDeviceAttachedA;

        public delegate void DeviceDetachedEventHandler(object sender, PanelEventArgs e);
        public event DeviceDetachedEventHandler OnDeviceDetachedA;

        public delegate void SettingsHasChangedEventHandler(object sender, PanelEventArgs e);
        public event SettingsHasChangedEventHandler OnSettingsChangedA;

        public delegate void SettingsHasBeenAppliedEventHandler(object sender, PanelEventArgs e);
        public event SettingsHasBeenAppliedEventHandler OnSettingsAppliedA;

        public delegate void SettingsClearedEventHandler(object sender, PanelEventArgs e);
        public event SettingsClearedEventHandler OnSettingsClearedA;

        public delegate void LedLightChangedEventHandler(object sender, LedLightChangeEventArgs e);
        public event LedLightChangedEventHandler OnLedLightChangedA;


        //For those that wants to listen to this panel
        public void Attach(ISaitekPanelListener iSaitekPanelListener)
        {
            OnDeviceAttachedA += iSaitekPanelListener.DeviceAttached;
            OnSwitchesChangedA += iSaitekPanelListener.SwitchesChanged;
            OnSettingsAppliedA += iSaitekPanelListener.SettingsApplied;
            OnLedLightChangedA += iSaitekPanelListener.LedLightChanged;
            OnSettingsClearedA += iSaitekPanelListener.SettingsCleared;
            OnSettingsChangedA += iSaitekPanelListener.PanelSettingsChanged;
        }

        //For those that wants to listen to this panel
        public void Detach(ISaitekPanelListener iSaitekPanelListener)
        {
            OnDeviceAttachedA -= iSaitekPanelListener.DeviceAttached;
            OnSwitchesChangedA -= iSaitekPanelListener.SwitchesChanged;
            OnSettingsAppliedA -= iSaitekPanelListener.SettingsApplied;
            OnLedLightChangedA -= iSaitekPanelListener.LedLightChanged;
            OnSettingsClearedA -= iSaitekPanelListener.SettingsCleared;
            OnSettingsChangedA -= iSaitekPanelListener.PanelSettingsChanged;
        }

        //For those that wants to listen to this panel when it's settings change
        public void Attach(IProfileHandlerListener iProfileHandlerListener)
        {
            OnSettingsChangedA += iProfileHandlerListener.PanelSettingsChanged;
        }

        //For those that wants to listen to this panel
        public void Detach(IProfileHandlerListener iProfileHandlerListener)
        {
            OnSettingsChangedA -= iProfileHandlerListener.PanelSettingsChanged;
        }

        //Used by descendants that wants to raise the event
        protected virtual void OnSwitchesChanged(HashSet<object> hashSet)
        {
            if (OnSwitchesChangedA != null)
            {
                OnSwitchesChangedA(this, new SwitchesChangedEventArgs() { UniqueId = InstanceId, SaitekPanelEnum = _typeOfSaitekPanel, Switches = hashSet });
            }
        }


        //Used by descendants that wants to raise the event
        protected virtual void OnDeviceAttached()
        {
            if (OnDeviceAttachedA != null)
            {
                //IsAttached = true;
                OnDeviceAttachedA(this, new PanelEventArgs() { UniqueId = InstanceId, SaitekPanelEnum = _typeOfSaitekPanel });
            }
        }

        //Used by descendants that wants to raise the event
        protected virtual void OnDeviceDetached()
        {
            if (OnDeviceDetachedA != null)
            {
                //IsAttached = false;
                OnDeviceDetachedA(this, new PanelEventArgs() { UniqueId = InstanceId, SaitekPanelEnum = _typeOfSaitekPanel });
            }
        }

        //Used by descendants that wants to raise the event
        protected virtual void OnSettingsChanged()
        {
            if (OnSettingsChangedA != null)
            {
                OnSettingsChangedA(this, new PanelEventArgs() { UniqueId = InstanceId, SaitekPanelEnum = _typeOfSaitekPanel });
            }
        }

        //Used by descendants that wants to raise the event
        protected virtual void OnSettingsApplied()
        {
            if (OnSettingsAppliedA != null)
            {
                OnSettingsAppliedA(this, new PanelEventArgs() { UniqueId = InstanceId, SaitekPanelEnum = _typeOfSaitekPanel });
            }
        }

        //Used by descendants that wants to raise the event
        protected virtual void OnSettingsCleared()
        {
            if (OnSettingsClearedA != null)
            {
                OnSettingsClearedA(this, new PanelEventArgs() { UniqueId = InstanceId, SaitekPanelEnum = _typeOfSaitekPanel });
            }
        }

        //Used by descendants that wants to raise the event
        protected virtual void OnLedLightChanged(SaitekPanelLEDPosition saitekPanelLEDPosition, PanelLEDColor panelLEDColor)
        {
            if (OnLedLightChangedA != null)
            {
                OnLedLightChangedA(this, new LedLightChangeEventArgs() { UniqueId = InstanceId, LEDPosition = saitekPanelLEDPosition, LEDColor = panelLEDColor });
            }
        }

        public void PanelSettingsChanged(object sender, PanelEventArgs e)
        {
            //do nada
        }

        public void PanelSettingsReadFromFile(object sender, SettingsReadFromFileEventArgs e)
        {
            ClearPanelSettings(this);
            ImportSettings(e.Settings);
        }

        public void ClearPanelSettings(object sender)
        {
            ClearSettings();
            OnSettingsCleared();
        }

        private int _vendorId;
        private int _productId;
        private Exception _lastException;
        private readonly object _exceptionLockObject = new object();
        private SaitekPanelsEnum _typeOfSaitekPanel;
        private bool _isDirty;
        //private bool _isAttached;
        private bool _forwardKeyPresses;
        private static object _lockObject = new object();
        private static List<SaitekPanel> _saitekPanels = new List<SaitekPanel>();
        private bool _keyboardEmulation;
        /*
         * IMPORTANT STUFF
         */
        private uint _count;
        private bool _synchedOnce;
        private Guid _guid = Guid.NewGuid();
        private string _hash;

        protected SaitekPanel(SaitekPanelsEnum typeOfSaitekPanel, HIDSkeleton hidSkeleton)
        {
            _typeOfSaitekPanel = typeOfSaitekPanel;
            HIDSkeletonBase = hidSkeleton;
            KeyboardEmulationOnly = true;
            _hash = Common.GetMd5Hash(hidSkeleton.InstanceId);
        }

        public abstract string SettingsVersion();
        public abstract void Startup();
        public abstract void Shutdown();
        public abstract void ClearSettings();
        public abstract void ImportSettings(List<string> settings);
        public abstract List<string> ExportSettings();
        public abstract void SavePanelSettings(object sender, ProfileHandlerEventArgs e);
        protected HIDSkeleton HIDSkeletonBase;
        private bool _closed;


        public void SelectedAirframe(object sender, AirframEventArgs e)
        {
            _keyboardEmulation = Common.IsKeyEmulationProfile(e.Airframe);
        }

        //User can choose not to in case switches needs to be reset but not affect the airframe. E.g. after crashing.
        public void SetForwardKeyPresses(object sender, ForwardKeyPressEventArgs e)
        {
            _forwardKeyPresses = e.Forward;
        }

        public bool ForwardKeyPresses
        {
            get { return _forwardKeyPresses; }
            set { _forwardKeyPresses = value; }
        }

        public int VendorId
        {
            get { return _vendorId; }
            set { _vendorId = value; }
        }

        public int ProductId
        {
            get { return _productId; }
            set { _productId = value; }
        }

        public string InstanceId
        {
            get
            {
                return HIDSkeletonBase.InstanceId;
            }
            set { HIDSkeletonBase.InstanceId = value; }
        }

        public string Hash
        {
            get
            {
                return _hash;
            }
        }

        public string GuidString
        {
            get { return _guid.ToString(); }
        }

        public void SetLastException(Exception ex)
        {
            try
            {
                if (ex == null)
                {
                    return;
                }
                Common.LogError(666, ex, "Via SaitekPanel.SetLastException()");
                lock (_exceptionLockObject)
                {
                    _lastException = new Exception(ex.GetType() + Environment.NewLine + ex.Message + Environment.NewLine + ex.StackTrace);
                }
            }
            catch (Exception)
            {
            }
        }


        /*         
         * These are used for static handling, the OnReport method must be static and to access the object that the OnReport belongs to must go via these methods.
         * If a static object would be kept inside the class it would go nuts considering there can be many panels of same type
         */
        public static void AddSaitekPanelObject(SaitekPanel saitekPanel)
        {
            lock (_lockObject)
            {
                _saitekPanels.Add(saitekPanel);
            }
        }

        public static void RemoveSaitekPanelObject(SaitekPanel saitekPanel)
        {
            lock (_lockObject)
            {
                _saitekPanels.Remove(saitekPanel);
            }
        }

        public static SaitekPanel GetSaitekPanelObject(string instanceId)
        {
            lock (_lockObject)
            {
                foreach (var saitekPanel in _saitekPanels)
                {
                    if (saitekPanel.InstanceId.Equals(instanceId))
                    {
                        return saitekPanel;
                    }
                }
            }
            return null;
        }

        public Exception GetLastException(bool resetException = false)
        {
            Exception result;
            lock (_exceptionLockObject)
            {
                result = _lastException;
                if (resetException)
                {
                    _lastException = null;
                }
            }
            return result;
        }

        public bool IsDirty
        {
            get { return _isDirty; }
            set
            {
                _isDirty = value;
            }
        }

        public SaitekPanelsEnum TypeOfSaitekPanel
        {
            get { return _typeOfSaitekPanel; }
            set { _typeOfSaitekPanel = value; }
        }
        //TODO fixa att man kan koppla in/ur panelerna?
        /*
         * 
        
        public bool IsAttached
        {
            get { return _isAttached; }
            set { _isAttached = value; }
        }
        */
        public bool KeyboardEmulationOnly
        {
            get { return _keyboardEmulation; }
            set { _keyboardEmulation = value; }
        }

        public bool Closed
        {
            get { return _closed; }
            set { _closed = value; }
        }

        protected static bool FlagHasChanged(byte oldValue, byte newValue, int bitMask)
        {
            /*  --------------------------------------- 
             *  Example #1
             *  Old value 10110101
             *  New value 10110001
             *  Bit mask  00000100  <- This is the one we are interested in to see whether it has changed
             *  ---------------------------------------
             *  
             *  XOR       10110101
             *  ^         10110001
             *            --------
             *            00000100   <- Here are the bit(s) that has changed between old & new value
             *            
             *  AND       00000100
             *  &         00000100
             *            --------
             *            00000100   <- This shows that the value for this mask has changed since last time. Now get what is it (ON/OFF) using FlagValue function
             */

            /*  --------------------------------------- 
             *  Example #2
             *  Old value 10110101
             *  New value 10100101
             *  Bit mask  00000100  <- This is the one we are interested in to see whether it has changed
             *  ---------------------------------------
             *  
             *  XOR       10110101
             *  ^         10100101
             *            --------
             *            00010000   <- Here are the bit(s) that has changed between old & new value
             *            
             *  AND       00010000
             *  &         00000100
             *            --------
             *            00000000   <- This shows that the value for this mask has NOT changed since last time.
             */
            return ((oldValue ^ newValue) & bitMask) > 0;
        }

    }

    public class LedLightChangeEventArgs : EventArgs
    {
        public string UniqueId { get; set; }
        public SaitekPanelLEDPosition LEDPosition { get; set; }
        public PanelLEDColor LEDColor { get; set; }
    }



    public class PanelEventArgs : EventArgs
    {
        public string UniqueId { get; set; }
        public SaitekPanelsEnum SaitekPanelEnum { get; set; }
    }



    public class SwitchesChangedEventArgs : EventArgs
    {
        public string UniqueId { get; set; }
        public SaitekPanelsEnum SaitekPanelEnum { get; set; }
        public HashSet<object> Switches { get; set; }
    }

    public class SwitchesChangedEventArgsdd : EventArgs
    {
        public string UniqueId { get; set; }
        public SaitekPanelsEnum SaitekPanelEnum { get; set; }
        public HashSet<object> Switches { get; set; }
    }

    public class ForwardKeyPressEventArgs : EventArgs
    {
        public bool Forward { get; set; }
    }
}
