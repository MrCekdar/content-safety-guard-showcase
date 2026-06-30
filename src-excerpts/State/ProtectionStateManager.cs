using System;
using System.Collections.Generic;
using System.Text;

namespace ContentSafetyGuard.State
{
    internal class ProtectionStateManager
    {

        private bool protectionstate;

        public void setProtectionState(bool state)
        {
            protectionstate = state;
        }

        public bool getProtectionState()
        {
            return protectionstate;
        }

    }
}
