using System.ComponentModel;

namespace ClassLibraryCommon
{

    public enum RadioPanelPZ69Display
    {
        Active,
        Standby,
        UpperActive,
        UpperStandby,
        LowerActive,
        LowerStandby
    }

    public enum KeyPressLength
    {
        //Zero = 0, <-- DCS & keybd_event fungerar inte utan fördröjning mellan tangent tryck & släpp
        //Indefinite = 0,
        FiftyMilliSec = 50,
        HalfSecond = 500,
        Second = 1000,
        SecondAndHalf = 1500,
        TwoSeconds = 2000,
        ThreeSeconds = 3000,
        FourSeconds = 4000,
        FiveSecs = 5000,
        TenSecs = 10000,
        FifteenSecs = 15000,
        TwentySecs = 20000,
        ThirtySecs = 30000,
        FortySecs = 40000,
        SixtySecs = 60000
    }

    public enum APIModeEnum
    {
        keybd_event = 0,
        SendInput = 1
    }

    public enum SaitekPanelsEnum
    {
        Unknown = 0,
        PZ55SwitchPanel = 2,
        PZ69RadioPanel = 4,
        PZ70MultiPanel = 8,
        BackLitPanel = 16,
        TPM = 32
    }


    public enum ProfileMode
    {
        [Description("NoFrameLoadedYet")]
        NOFRAMELOADEDYET,
        [Description("KeyEmulator")]
        KEYEMULATOR,
        [Description("KeyEmulatorArmA")]
        KEYEMULATOR_ARMA
    }

}
