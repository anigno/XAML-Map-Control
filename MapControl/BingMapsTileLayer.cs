﻿// XAML Map Control - http://xamlmapcontrol.codeplex.com/
// © 2015 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Xml;
#if WINDOWS_RUNTIME
using Windows.UI.Xaml;
#else
using System.Windows;
#endif

namespace MapControl
{
    public class BingMapsTileLayer : TileLayer
    {
        public enum MapMode
        {
            Road, Aerial, AerialWithLabels
        }

        public BingMapsTileLayer()
        {
            MinZoomLevel = 1;
            MaxZoomLevel = 21;
            Loaded += OnLoaded;
        }

        public static string ApiKey { get; set; }

        public MapMode Mode { get; set; }
        public string Culture { get; set; }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;

            if (string.IsNullOrWhiteSpace(ApiKey))
            {
                throw new InvalidOperationException("A Bing Maps API Key must be assigned to the ApiKey property.");
            }

            var uri = string.Format("http://dev.virtualearth.net/REST/V1/Imagery/Metadata/{0}?output=xml&key={1}", Mode, ApiKey);
            var request = HttpWebRequest.CreateHttp(uri);

            request.BeginGetResponse(HandleImageryMetadataResponse, request);
        }

        private void HandleImageryMetadataResponse(IAsyncResult asyncResult)
        {
            try
            {
                var request = (HttpWebRequest)asyncResult.AsyncState;

                using (var response = request.EndGetResponse(asyncResult))
                using (var xmlReader = XmlReader.Create(response.GetResponseStream()))
                {
                    ReadImageryMetadataResponse(xmlReader);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void ReadImageryMetadataResponse(XmlReader xmlReader)
        {
            string imageUrl = null;
            string[] imageUrlSubdomains = null;
            int? zoomMin = null;
            int? zoomMax = null;

            do
            {
                if (xmlReader.NodeType == XmlNodeType.Element)
                {
                    switch (xmlReader.Name)
                    {
                        case "ImageUrl":
                            imageUrl = xmlReader.ReadElementContentAsString();
                            break;
                        case "ImageUrlSubdomains":
                            imageUrlSubdomains = ReadStrings(xmlReader.ReadSubtree());
                            break;
                        case "ZoomMin":
                            zoomMin = xmlReader.ReadElementContentAsInt();
                            break;
                        case "ZoomMax":
                            zoomMax = xmlReader.ReadElementContentAsInt();
                            break;
                        default:
                            xmlReader.Read();
                            break;
                    }
                }
                else
                {
                    xmlReader.Read();
                }
            }
            while (xmlReader.NodeType != XmlNodeType.None);

            if (imageUrl != null && imageUrlSubdomains != null && imageUrlSubdomains.Length > 0)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (string.IsNullOrWhiteSpace(Culture))
                    {
                        Culture = CultureInfo.CurrentUICulture.Name;
                    }

                    TileSource = new BingMapsTileSource(imageUrl.Replace("{culture}", Culture), imageUrlSubdomains);

                    if (zoomMin.HasValue && zoomMin.Value > MinZoomLevel)
                    {
                        MinZoomLevel = zoomMin.Value;
                    }

                    if (zoomMax.HasValue && zoomMax.Value < MaxZoomLevel)
                    {
                        MaxZoomLevel = zoomMax.Value;
                    }
                }));
            }
        }

        private static string[] ReadStrings(XmlReader xmlReader)
        {
            var strings = new List<string>();

            do
            {
                if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name == "string")
                {
                    strings.Add(xmlReader.ReadElementContentAsString());
                }
                else
                {
                    xmlReader.Read();
                }
            }
            while (xmlReader.NodeType != XmlNodeType.None);

            return strings.ToArray();
        }
    }
}
