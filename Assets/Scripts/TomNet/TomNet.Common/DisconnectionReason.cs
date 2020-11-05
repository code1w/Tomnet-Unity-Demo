/*
 * This file is part of the TomNet package.
 *
 * (a) zhang xiao bin <qunshuok@gmail.com>
 *
 *  2020/09/15
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TomNet.Common
{
    public static class DisconnectionReason
    {
        public static readonly string IDLE = "idle";
        public static readonly string KICK = "kick";
        public static readonly string BAN = "ban";
        public static readonly string MANUAL = "manual";
        public static readonly string UNKNOWN = "unknown";
        private static string[] reasons = new string[3]
        {
            "idle",
            "kick",
            "ban"
        };

        public static string GetReason(int reasonId)
        {
            return reasons[reasonId];
        }
    }
}
