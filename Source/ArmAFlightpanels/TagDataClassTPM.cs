using System.Collections.Generic;

using NonVisuals;

namespace ArmAFlightpanels
{
    internal class TagDataClassTPM
    {
        private BIPLinkTPM _bipLinkTPM;
        private OSKeyPress _osKeyPress;
        
        public bool ContainsBIPLink()
        {
            return _bipLinkTPM != null && _bipLinkTPM.BIPLights.Count > 0;
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
            return (_bipLinkTPM == null || _bipLinkTPM.BIPLights.Count == 0) && (_osKeyPress == null || _osKeyPress.KeySequence.Count == 0);
        }

        public BIPLinkTPM BIPLink
        {
            get => _bipLinkTPM;
            set
            {
                _bipLinkTPM = value;
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
            _bipLinkTPM = null;
            _osKeyPress = null;
        }
    }
}
