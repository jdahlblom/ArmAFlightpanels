using System.Collections.Generic;

using NonVisuals;

namespace ArmAFlightpanels
{
    internal class TagDataClassPZ70
    {
        private BIPLinkPZ70 _bipLinkPZ70;
        private OSKeyPress _osKeyPress;

        
        public bool ContainsBIPLink()
        {
            return _bipLinkPZ70 != null && _bipLinkPZ70.BIPLights.Count > 0;
        }

        public bool ContainsOSKeyPress()
        {
            return _osKeyPress != null && _osKeyPress.KeySequence.Count > 0;
        }

        public bool ContainsKeySequence()
        {
            return _osKeyPress != null && _osKeyPress.IsMultiSequenced();
        }

        public bool ContainsSingleKey()
        {
            return _osKeyPress != null && !_osKeyPress.IsMultiSequenced();
        }

        public string GetTextBoxKeyPressInfo()
        {
            if (_osKeyPress.IsMultiSequenced())
            {
                if (!string.IsNullOrWhiteSpace(_osKeyPress.Information))
                {
                    return _osKeyPress.Information;
                }
                return "key press sequence";
            }
            return _osKeyPress.GetSimpleVirtualKeyCodesAsString();
        }

        public SortedList<int, KeyPressInfo> GetKeySequence()
        {
            return _osKeyPress.KeySequence;
        }

        /*public void SetKeySequence(SortedList<int, KeyPressInfo> sortedList)
        {
            _osKeyPress.KeySequence = sortedList;
        }*/

        public bool IsEmpty()
        {
            return (_bipLinkPZ70 == null || _bipLinkPZ70.BIPLights.Count == 0)&& (_osKeyPress == null || _osKeyPress.KeySequence.Count == 0);
        }
        
        public BIPLinkPZ70 BIPLink
        {
            get => _bipLinkPZ70;
            set
            {
                _bipLinkPZ70 = value;
            }
        }

        public OSKeyPress KeyPress
        {
            get => _osKeyPress;
            set
            {
                _osKeyPress = value;
            }
        }

        public void ClearAll()
        {
            _bipLinkPZ70 = null;
            _osKeyPress = null;
        }
    }
}
