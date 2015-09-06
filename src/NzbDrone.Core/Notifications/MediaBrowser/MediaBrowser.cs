﻿using System;
using System.Collections.Generic;
using FluentValidation.Results;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Tv;
using NzbDrone.Core.Update;

namespace NzbDrone.Core.Notifications.MediaBrowser
{
    public class MediaBrowser : NotificationBase<MediaBrowserSettings>
    {
        private readonly IMediaBrowserService _mediaBrowserService;

        public MediaBrowser(IMediaBrowserService mediaBrowserService)
        {
            _mediaBrowserService = mediaBrowserService;
        }

        public override string Link
        {
            get { return "http://mediabrowser.tv/"; }
        }

        public override void OnGrab(GrabMessage grabMessage)
        {
            const string title = "Sonarr - Grabbed";

            if (Settings.Notify)
            {
                _mediaBrowserService.Notify(Settings, title, grabMessage.Message);
            }
        }

        public override void OnDownload(DownloadMessage message)
        {
            const string title = "Sonarr - Downloaded";

            if (Settings.Notify)
            {
                _mediaBrowserService.Notify(Settings, title, message.Message);
            }

            if (Settings.UpdateLibrary)
            {
                _mediaBrowserService.Update(Settings, message.Series);
            }
        }

        public override void OnRename(Series series)
        {
            if (Settings.UpdateLibrary)
            {
                _mediaBrowserService.Update(Settings, series);
            }
        }

        public override void OnSystemUpdateAvailable(UpdatePackage package)
        {
            if (Settings.Notify)
            {
                const string title = "Sonarr [TV] - New System Update";
                var body = String.Format("New update is available - {0} - {1}", package.Version.ToString(), package.Url.ToString());
                _mediaBrowserService.Notify(Settings, title, body);
            }
        }

        public override string Name
        {
            get
            {
                return "Emby (Media Browser)";
            }
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            failures.AddIfNotNull(_mediaBrowserService.Test(Settings));

            return new ValidationResult(failures);
        }
    }
}
