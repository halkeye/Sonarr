﻿using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Download;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Tv;
using NzbDrone.Core.HealthCheck;
using NzbDrone.Core.HealthCheck.Checks;
using NzbDrone.Core.Update;

namespace NzbDrone.Core.Notifications
{
    public class NotificationService
        : IHandle<EpisodeGrabbedEvent>,
          IHandle<EpisodeDownloadedEvent>,
          IHandle<SeriesRenamedEvent>,
          IHandleAsync<HealthCheckCompleteEvent>
    {
        private readonly INotificationFactory _notificationFactory;
        private readonly Logger _logger;
        private readonly IHealthCheckService _healthCheckService;
        private readonly ICheckUpdateService _checkUpdateService;

        private UpdatePackage _lastUpdate;

        public NotificationService(INotificationFactory notificationFactory, IHealthCheckService healthCheckService, ICheckUpdateService checkUpdateService, Logger logger)
        {
            _notificationFactory = notificationFactory;
            _healthCheckService = healthCheckService;
            _checkUpdateService = checkUpdateService;
            _logger = logger;
            _lastUpdate = null;
        }

        private string GetMessage(Series series, List<Episode> episodes, QualityModel quality)
        {
            var qualityString = quality.Quality.ToString();

            if (quality.Revision.Version > 1)
            {
                if (series.SeriesType == SeriesTypes.Anime)
                {
                    qualityString += " v" + quality.Revision.Version;
                }

                else
                {
                    qualityString += " Proper";
                }
            }
            
            if (series.SeriesType == SeriesTypes.Daily)
            {
                var episode = episodes.First();

                return String.Format("{0} - {1} - {2} [{3}]",
                                         series.Title,
                                         episode.AirDate,
                                         episode.Title,
                                         qualityString);
            }

            var episodeNumbers = String.Concat(episodes.Select(e => e.EpisodeNumber)
                                                       .Select(i => String.Format("x{0:00}", i)));

            var episodeTitles = String.Join(" + ", episodes.Select(e => e.Title));

            return String.Format("{0} - {1}{2} - {3} [{4}]",
                                    series.Title,
                                    episodes.First().SeasonNumber,
                                    episodeNumbers,
                                    episodeTitles,
                                    qualityString);
        }

        private bool ShouldHandleSeries(ProviderDefinition definition, Series series)
        {
            var notificationDefinition = (NotificationDefinition) definition;

            if (notificationDefinition.Tags.Empty())
            {
                _logger.Debug("No tags set for this notification.");
                return true;
            }

            if (notificationDefinition.Tags.Intersect(series.Tags).Any())
            {
                _logger.Debug("Notification and series have one or more matching tags.");
                return true;
            }

            //TODO: this message could be more clear
            _logger.Debug("{0} does not have any tags that match {1}'s tags", notificationDefinition.Name, series.Title);
            return false;
        }

        public void Handle(EpisodeGrabbedEvent message)
        {
            var grabMessage = new GrabMessage {
                Message = GetMessage(message.Episode.Series, message.Episode.Episodes, message.Episode.ParsedEpisodeInfo.Quality),
                Series = message.Episode.Series,
                Quality = message.Episode.ParsedEpisodeInfo.Quality,
                Episode = message.Episode
            };

            foreach (var notification in _notificationFactory.OnGrabEnabled())
            {
                try
                {
                    if (!ShouldHandleSeries(notification.Definition, message.Episode.Series)) continue;
                    notification.OnGrab(grabMessage);
                }

                catch (Exception ex)
                {
                    _logger.ErrorException("Unable to send OnGrab notification to: " + notification.Definition.Name, ex);
                }
            }
        }

        public void Handle(EpisodeDownloadedEvent message)
        {
            var downloadMessage = new DownloadMessage();
            downloadMessage.Message = GetMessage(message.Episode.Series, message.Episode.Episodes, message.Episode.Quality);
            downloadMessage.Series = message.Episode.Series;
            downloadMessage.EpisodeFile = message.EpisodeFile;
            downloadMessage.OldFiles = message.OldFiles;

            foreach (var notification in _notificationFactory.OnDownloadEnabled())
            {
                try
                {
                    if (ShouldHandleSeries(notification.Definition, message.Episode.Series))
                    {
                        if (downloadMessage.OldFiles.Empty() || ((NotificationDefinition)notification.Definition).OnUpgrade)
                        {
                            notification.OnDownload(downloadMessage);
                        }
                    }
                }

                catch (Exception ex)
                {
                    _logger.WarnException("Unable to send OnDownload notification to: " + notification.Definition.Name, ex);
                }
            }
        }

        public void Handle(SeriesRenamedEvent message)
        {
            foreach (var notification in _notificationFactory.OnRenameEnabled())
            {
                try
                {
                    if (ShouldHandleSeries(notification.Definition, message.Series))
                    {
                        notification.OnRename(message.Series);
                    }
                }

                catch (Exception ex)
                {
                    _logger.WarnException("Unable to send OnRename notification to: " + notification.Definition.Name, ex);
                }
            }
        }

        public void HandleAsync(HealthCheckCompleteEvent message)
        {
            foreach (var check in _healthCheckService.Results().OfType<UpdateCheck>())
            {
                var lastUpdate = _checkUpdateService.LatestUpdate();
                if (_lastUpdate != null && _lastUpdate.Version == lastUpdate.Version)
                {
                    /* Duplicate notification */
                    return;
                }
                _lastUpdate = lastUpdate;

                foreach (var notification in _notificationFactory.OnSystemUpdateAvailableEnabled())
                {
                    try
                    {
                        notification.OnSystemUpdateAvailable(_lastUpdate);
                    }
                    catch (Exception ex)
                    {
                        _logger.WarnException("Unable to send OnSystemUpdateAvailable notification to: " + notification.Definition.Name, ex);
                    }
                }
            }
        }
    }
}
