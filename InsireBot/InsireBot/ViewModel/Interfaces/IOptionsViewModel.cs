﻿using Maple.Core;
using System.Globalization;

namespace Maple
{
    public interface IOptionsViewModel : ILoadableViewModel, ISaveableViewModel
    {
        RangeObservableCollection<CultureInfo> Items { get; set; }
        CultureInfo SelectedCulture { get; set; }
    }
}