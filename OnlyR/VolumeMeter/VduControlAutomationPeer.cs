using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using OnlyR.Properties;

namespace OnlyR.VolumeMeter
{
    public class VduControlAutomationPeer : FrameworkElementAutomationPeer, IRangeValueProvider
    {
        public VduControlAutomationPeer(VduControl owner) : base(owner)
        {
        }

        private VduControl VduControl => (VduControl)Owner;

        protected override string GetNameCore() => Resources.ACCESSIBILITY_VOLUME_LEVEL_NAME;

        protected override string GetHelpTextCore() => Resources.ACCESSIBILITY_VOLUME_LEVEL_HELP;

        protected override AutomationControlType GetAutomationControlTypeCore() =>
            AutomationControlType.ProgressBar;

        protected override string GetClassNameCore() => nameof(VduControl);

        public override object? GetPattern(PatternInterface patternInterface) =>
            patternInterface == PatternInterface.RangeValue ? this : base.GetPattern(patternInterface);

        // IRangeValueProvider
        public double Value => VduControl.VolumeLevel;
        public bool IsReadOnly => true;
        public double Maximum => 100;
        public double Minimum => 0;
        public double LargeChange => 10;
        public double SmallChange => 1;

        public void SetValue(double value) { }

        internal void RaiseVolumeChangedEvent(int oldValue, int newValue)
        {
            RaisePropertyChangedEvent(
                RangeValuePatternIdentifiers.ValueProperty,
                (double)oldValue,
                (double)newValue);
        }
    }
}
