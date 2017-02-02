﻿using InsireBot.Core;
using System.Globalization;

namespace InsireBot
{
    public class OptionsViewModel : ObservableObject
    {
        private ITranslationManager _manager;
        public RangeObservableCollection<CultureInfo> Items { get; private set; }

        private CultureInfo _selectedCulture;
        public CultureInfo SelectedCulture
        {
            get { return _selectedCulture; }
            set { SetValue(ref _selectedCulture, value, Changed: SyncCulture); }
        }

        public OptionsViewModel(ITranslationManager manager)
        {
            _manager = manager;
            Items = new RangeObservableCollection<CultureInfo>(_manager.Languages);
            SelectedCulture = Properties.Settings.Default.StartUpCulture;
        }

        private void SyncCulture()
        {
            _manager.CurrentLanguage = SelectedCulture;
        }
    }
}
