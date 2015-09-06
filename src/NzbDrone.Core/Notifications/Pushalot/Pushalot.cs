﻿using System;
using System.Collections.Generic;
using FluentValidation.Results;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Tv;
using NzbDrone.Core.Update;

namespace NzbDrone.Core.Notifications.Pushalot
{
    public class Pushalot : NotificationBase<PushalotSettings>
    {
        private readonly IPushalotProxy _proxy;

        public Pushalot(IPushalotProxy proxy)
        {
            _proxy = proxy;
        }

        public override string Link
        {
            get { return "https://www.Pushalot.com/"; }
        }

        public override void OnGrab(GrabMessage grabMessage)
        {
            const string title = "Episode Grabbed";

            _proxy.SendNotification(title, grabMessage.Message, Settings);
        }

        public override void OnDownload(DownloadMessage message)
        {
            const string title = "Episode Downloaded";

            _proxy.SendNotification(title, message.Message, Settings);
        }

        public override void OnRename(Series series)
        {
        }

        public override void OnSystemUpdateAvailable(UpdatePackage package)
        {
            const string title = "New System Update";
            var body = String.Format("New update is available - {0} - {1}", package.Version.ToString(), package.Url.ToString());

            _proxy.SendNotification(title, body, Settings);
        }

        public override string Name
        {
            get
            {
                return "Pushalot";
            }
        }

        public override bool SupportsOnRename
        {
            get
            {
                return false;
            }
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            failures.AddIfNotNull(_proxy.Test(Settings));

            return new ValidationResult(failures);
        }
    }
}
