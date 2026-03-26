namespace ET
{
    public static partial class TimerInvokeType
    {
        // PackageType.StateSync = 10, so base = 10000
        public const int DirectMoveTimer = PackageType.StateSync * 1000 + 1;
    }
}
