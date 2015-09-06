﻿using System;
using System.Collections.Generic;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Notifications
{
    public class NotificationDefinition : ProviderDefinition
    {
        public NotificationDefinition()
        {
            Tags = new HashSet<Int32>();
        }

        public bool OnGrab { get; set; }
        public bool OnDownload { get; set; }
        public bool OnUpgrade { get; set; }
        public bool OnRename { get; set; }
        public bool OnSystemUpdateAvailable { get; set; }
        public bool SupportsOnGrab { get; set; }
        public bool SupportsOnDownload { get; set; }
        public bool SupportsOnUpgrade { get; set; }
        public bool SupportsOnRename { get; set; }
        public bool SupportsOnSystemUpdateAvailable { get; set; }
        public HashSet<int> Tags { get; set; }

        public override bool Enable
        {
            get
            {
                return OnGrab || OnDownload || (OnDownload && OnUpgrade);
            }
        }
    }
}
