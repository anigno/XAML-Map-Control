﻿// XAML Map Control - http://xamlmapcontrol.codeplex.com/
// © 2015 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System;
using System.ComponentModel;
using System.Globalization;

namespace MapControl
{
    public class TileSourceConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            return new TileSource { UriFormat = value as string };
        }
    }

    [TypeConverter(typeof(TileSourceConverter))]
    public partial class TileSource
    {
    }
}
