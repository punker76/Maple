﻿using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using Maple.Interfaces;

namespace Maple.Core
{
    public abstract class MapleFileSystemContainerBase : MapleFileSystemBase, IFileSystemDirectory
    {
        private ICollectionView _noFilesCollectionView;
        public ICollectionView NoFilesCollectionView
        {
            get { return _noFilesCollectionView; }
            protected set { SetValue(ref _noFilesCollectionView, value); }
        }

        private ICollectionView _defaultCollectionView;
        public ICollectionView DefaultCollectionView
        {
            get { return _defaultCollectionView; }
            protected set { SetValue(ref _defaultCollectionView, value); }
        }

        private IRangeObservableCollection<IFileSystemInfo> _children;
        public IRangeObservableCollection<IFileSystemInfo> Children
        {
            get { return _children; }
            private set { SetValue(ref _children, value); }
        }

        protected MapleFileSystemContainerBase(string name, string fullName, IDepth depth, IFileSystemDirectory parent) : base(name, fullName, depth, parent)
        {
            using (_busyStack.GetToken())
            {
                IsContainer = true;

                Children = new RangeObservableCollection<IFileSystemInfo>();

                NoFilesCollectionView = CollectionViewSource.GetDefaultView(Children);
                DefaultCollectionView = CollectionViewSource.GetDefaultView(Children);

                using (NoFilesCollectionView.DeferRefresh())
                    NoFilesCollectionView.Filter = NoFilesFilter;

                using (DefaultCollectionView.DeferRefresh())
                    DefaultCollectionView.Filter = SearchFilter;
            }
        }

        public override void OnFilterChanged(string filter)
        {
            using (_busyStack.GetToken())
            {
                Filter = filter;
                Children.ForEach(p => p.LoadMetaData());
                Children.ForEach(p => p.Filter = filter);

                using (DefaultCollectionView.DeferRefresh())
                    DefaultCollectionView.Filter = SearchFilter;
            }
        }

        public override void Refresh()
        {
            using (_busyStack.GetToken())
            {
                Children.Clear();
                Children.AddRange(FileSystemExtensions.GetChildren(this, Depth));
                HasContainers = Children.Any(p => p is MapleFileSystemContainerBase);

                OnFilterChanged(string.Empty);
            }
        }
    }
}
