using System.Runtime.InteropServices;

namespace SmartCourse.App.Services;

public static class NativeMath
{
    private const string DllName = "smartmath";

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int CalculateBonusCredits(int credits);

    public static int GetBonusCreditsSafe(int credits)
    {
        try 
        { 
            return CalculateBonusCredits(credits); 
        }
        catch (DllNotFoundException) 
        { 
            if (credits <= 0) return 0;
            return credits + (credits / 10);
        }
    }
}