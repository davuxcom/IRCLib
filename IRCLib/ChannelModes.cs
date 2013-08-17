using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace IRCLib
{
    public class ChannelModes
    {
        Dictionary<string, string> Modes = new Dictionary<string, string>();
        Dictionary<string, string> Symbols = new Dictionary<string, string>();

        public string GetTopSymbol(string[] modes)
        {
            int top = 99;
            foreach (string m in modes)
            {
                if (Modes.Keys.Contains(m))
                {
                    for (int i = 0; i < Modes.Keys.Count; i++)
                    {
                        if (Modes.Keys.ElementAt(i) == m)
                        {
                            if (i < top)
                            {
                                top = i;
                                break;
                            }
                        }
                    }
                }
            }
            if (top != 99)
            {
                return Modes.Values.ElementAt(top);
            }
            return "";
        }

        public string ModeForSymbol(string symbol)
        {
            if (Symbols.Keys.Contains(symbol))
            {
                return Symbols[symbol];
            }
            return "";
        }

        public string SymbolForMode(string mode)
        {
            if (Modes.Keys.Contains(mode))
            {
                return Modes[mode];
            }
            return "";
        }
        
        public ChannelModes(string prefixStr)
        {
            Trace.WriteLine("Using Modes: " + prefixStr);
            if (prefixStr.StartsWith("(") && prefixStr.IndexOf(")") > -1)
            {
                
                string modes = prefixStr.Substring(1, prefixStr.IndexOf(")") - 1).Trim();
                string present = prefixStr.Substring(1 + prefixStr.IndexOf(")")).Trim();
                if (modes.Length == present.Length)
                {
                    for (int i = 0; i < modes.Length; i++)
                    {
                        Modes.Add(modes[i].ToString(), present[i].ToString());
                        Symbols.Add(present[i].ToString(), modes[i].ToString());
                    }
                }
                else
                {
                    Trace.WriteLine("Bad Modes: " + modes + " _ " + present);
                }
            }
        }
    }
}
