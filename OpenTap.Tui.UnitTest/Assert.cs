using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace OpenTap.Tui.UnitTest;

public static class Assert
{
    public static void True(bool result, string message = "")
    {
        if (result) 
            return;
        
        var e = new Exception(message);
        TuiTester.LogConsole(e);
        throw e;
    }
    
    public static void NotNull(object result, string message = "")
    {
        if (result != null)
            return;
        
        var e = new Exception(message);
        TuiTester.LogConsole(e);
        throw e;
    }

    public static void Contains<T>(List<T> list, [NotNull] Func<T, bool> Predicate, string message = "")
    {
        if (list == null)
            True(false, message);
        
        bool result = false;
        foreach (var item in list)
        {
            if (Predicate(item))
            {
                result = true;
                break;
            }
        }

        True(result, message);
    }
}