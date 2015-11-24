using System;
using System.Collections.Generic;
using System.Text;

namespace LMComLib
{
  public enum LibUsage
  {
    no,
    LMComWebAdmin,
  }
  public static class LibConfig
  {
    public static LibUsage Usage;
    public static void DesignTimeEq(bool value)
    {
      if (value == (Usage == LibUsage.LMComWebAdmin)) return;
      throw new Exception("CheckDesignTime");
    }
  }


}
