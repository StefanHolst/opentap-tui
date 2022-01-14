using System;

namespace OpenTap.Tui.UnitTests;

public static class Assert
{
    public static void True(bool result, string message = "")
    {
        try
        {
            if (!result)
                throw new Exception(message);
        }
        catch (Exception e)
        {
            ApplicationTest.LogConsole(e);
            throw;
        }
    }
    
    public static void NotNull(object result, string message = "")
    {
        try
        {
            if (result == null)
                throw new Exception(message);
        }
        catch (Exception e)
        {
            ApplicationTest.LogConsole(e);
            throw;
        }
    }
}