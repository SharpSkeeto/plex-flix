﻿using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Caching.Memory;
using MyPlexManager.Extensions;
using MyPlexManager.Interfaces;
using MyPlexManager.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MyPlexManager.ViewModels;

[ObservableObject]
public partial class MediaShowSeasonViewModel
{

	private readonly INavigationService _navigationService;
	private readonly IPlexApiService _plexApiClient;
	private readonly IAppSettings? _appSettings;
	private readonly IMemoryCache? _memoryCache;

	[ObservableProperty]
	private string? mediaThumbNailUri;
	[ObservableProperty]
	private string? mediaTitle;
	[ObservableProperty]
	private int mediaYear;
	[ObservableProperty]
	private string? mediaSummary;
	[ObservableProperty]
	private string? mediaTagline;
	[ObservableProperty]
	private string? mediaRoles;
	[ObservableProperty]
	private string? mediaStudio;
	[ObservableProperty]
	private string? mediaGenre;
	[ObservableProperty]
	private string? mediaRating;
	[ObservableProperty]
	private string? mediaReleaseDate;

	public ObservableCollection<Metadata>? MediaMetaData { get; } = new();

	public MediaShowSeasonViewModel(INavigationService navigationService, IPlexApiService plexApiClient, IMemoryCache memoryCache)
	{
		_navigationService = navigationService;
		_plexApiClient = plexApiClient;
		_appSettings = App.CurrentAppSettings;
		_memoryCache = memoryCache;
	}

	public async Task GetShowSeasonDataAsync(object selectedItem)
	{
		var item = (Metadata)selectedItem;
		mediaThumbNailUri = item.thumb;
		mediaTitle = item.title;
		mediaYear = item.year;
		if (DateOnly.TryParse(item.originallyAvailableAt!, out var originalAvail))
		{
			mediaReleaseDate = originalAvail.ToString("MMMM dd, yyyy");
		};
		mediaSummary = item.summary!;
		mediaTagline = item.tagline;
		mediaRating = string.IsNullOrWhiteSpace(item.contentRating) ? "N/A" : item.contentRating;
		if (item.Role is not null)
			mediaRoles = string.Join(", ", item.Role?.Select(t => t.tag)!);

		if (item.Genre is not null)
			mediaGenre = string.Join(", ", item.Genre?.Select(t => t.tag)!);
		mediaStudio = item.studio;

		PlexMediaLibraryItems? plexMediaLibraryItems = null;
		MediaMetaData?.Clear();

		if (_memoryCache?.TryGetValueExt(item.key!, out plexMediaLibraryItems) is false)
		{
			plexMediaLibraryItems = await _plexApiClient.GetPlexChildrenData(item.key!);
			_ = _memoryCache?.Set(item.key!, plexMediaLibraryItems);
		}

		if (plexMediaLibraryItems?.MediaContainer is not null)
		{
			var metadata = plexMediaLibraryItems.MediaContainer?.Metadata;
			foreach (var data in metadata!)
			{
				if ((bool)data.thumb?.StartsWith("/")!)
				{
					data.thumb = $"{_appSettings?.Protocol}://{_appSettings?.Address}:{_appSettings?.Port}" + data.thumb + $"?X-Plex-Token={_appSettings?.Token}";
				}
				MediaMetaData?.Add(data);
			}
		}

		var navItem = _navigationService.GetCurrentNavigationViewItem();
		if (navItem != null)
		{
			navItem.IsSelected = false;
		}

	}

}
