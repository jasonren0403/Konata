﻿using System;
using Konata.Msf.Packets.SvcReq;

namespace Konata.Test.Tests
{
    public class TestSvcReqEmpty : Test
    {
        public override bool Run()
        {
            var req = new SvcReqEmpty();
            Print(req);

            return true;
        }
    }
}
